using System;
using System.Text;
using System.Threading.Tasks;
using MCP.Reflection;
using MCP.Server;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCP.Protocol
{
    public static class McpHandler
    {
        const int ArgSummaryMaxLength = 240;
        const int StringValueMaxLength = 60;

        public const string ProtocolVersion = "2025-03-26";
        public const string ServerName = "unity-editor-mcp";
        public const string ServerVersion = "0.1.0";

        public static async Task<JsonRpcMessage> Handle(JsonRpcMessage req)
        {
            try
            {
                switch (req.Method)
                {
                    case "initialize":
                        return JsonRpcMessage.Success(req.Id, BuildInitializeResult());

                    case "ping":
                        return JsonRpcMessage.Success(req.Id, new JObject());

                    case "tools/list":
                        return JsonRpcMessage.Success(req.Id, BuildToolsList());

                    case "tools/call":
                        return await HandleToolsCall(req);

                    default:
                        return JsonRpcMessage.Failure(req.Id, JsonRpcErrorCodes.MethodNotFound, $"Unknown method: {req.Method}");
                }
            }
            catch (Exception e)
            {
                return JsonRpcMessage.Failure(req.Id, JsonRpcErrorCodes.InternalError, e.Message, JToken.FromObject(e.ToString()));
            }
        }

        static JObject BuildInitializeResult()
        {
            return new JObject
            {
                ["protocolVersion"] = ProtocolVersion,
                ["capabilities"] = new JObject
                {
                    ["tools"] = new JObject { ["listChanged"] = false }
                },
                ["serverInfo"] = new JObject
                {
                    ["name"] = ServerName,
                    ["version"] = ServerVersion
                }
            };
        }

        static JObject BuildToolsList()
        {
            var arr = new JArray();
            foreach (var t in ToolRegistry.All)
            {
                arr.Add(new JObject
                {
                    ["name"] = t.Name,
                    ["description"] = t.Description ?? "",
                    ["inputSchema"] = t.InputSchema ?? new JObject { ["type"] = "object" }
                });
            }
            return new JObject { ["tools"] = arr };
        }

        static async Task<JsonRpcMessage> HandleToolsCall(JsonRpcMessage req)
        {
            var p = req.Params as JObject ?? new JObject();
            var name = (string)p["name"];
            var args = p["arguments"] as JObject ?? new JObject();

            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("[MCP] tools/call rejected: missing tool name");
                return JsonRpcMessage.Failure(req.Id, JsonRpcErrorCodes.InvalidParams, "Missing tool name");
            }

            if (!ToolRegistry.TryGet(name, out var tool))
            {
                Debug.LogWarning($"[MCP] tools/call rejected: unknown tool '{name}'");
                return JsonRpcMessage.Failure(req.Id, JsonRpcErrorCodes.MethodNotFound, $"Unknown tool: {name}");
            }

            Debug.Log($"[MCP] {name}({await BuildArgSummary(args)})");

            try
            {
                var result = await tool.Handler(args);
                var resultSummary = await BuildResultSummary(result);
                if (!string.IsNullOrEmpty(resultSummary))
                    Debug.Log($"[MCP] {name} → {resultSummary}");
                var content = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = result?.ToString(Newtonsoft.Json.Formatting.None) ?? "null"
                    }
                };
                return JsonRpcMessage.Success(req.Id, new JObject
                {
                    ["content"] = content,
                    ["structuredContent"] = result ?? JValue.CreateNull(),
                    ["isError"] = false
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP] {name} failed: {e.Message}");
                var content = new JArray
                {
                    new JObject { ["type"] = "text", ["text"] = e.Message }
                };
                return JsonRpcMessage.Success(req.Id, new JObject
                {
                    ["content"] = content,
                    ["isError"] = true
                });
            }
        }

        // Bounces summary computation onto the main thread so EntityId→Object resolution
        // (and AssetDatabase calls) are safe. Falls back to a raw summary if dispatch throws.
        static async Task<string> BuildArgSummary(JObject args)
        {
            try { return await MainThreadDispatcher.Run(() => SummarizeArgs(args)); }
            catch { return "(summary unavailable)"; }
        }

        // Summarizes the tool's return value, surfacing identifying fields (paths, refs, names,
        // counts) so the log shows what the tool actually operated on. Empty string when the
        // result has no identifying content (e.g. {ok:true}) — caller suppresses the line.
        static async Task<string> BuildResultSummary(JToken result)
        {
            if (result == null || result.Type == JTokenType.Null) return "";
            try { return await MainThreadDispatcher.Run(() => SummarizeResult(result)); }
            catch { return ""; }
        }

        // Identifying keys in tool results, in display order. Walked into the top-level result
        // object only — nested structures stay summarized as "{N keys}" / "[N]".
        static readonly string[] InterestingResultKeys =
        {
            "path", "newPath", "oldPath", "sourcePath", "assetPath",
            "name", "guid", "mode", "isDirty", "isLoaded",
            "mainAsset", "importer", "root",
            "totalItems", "succeeded", "failed",
            "totalFound", "returned", "count",
            "index", "alreadyExisted", "cleanBuild",
            "savedPaths",
            "value"
        };

        static string SummarizeResult(JToken result)
        {
            if (result == null || result.Type == JTokenType.Null) return "";
            if (!(result is JObject jo)) return SummarizeValue(result);

            // ObjectRef envelope returned directly (e.g. asset_create, gameobject_create).
            if (jo["__ref"] != null) return SummarizeObject(jo);

            var sb = new StringBuilder();
            foreach (var key in InterestingResultKeys)
            {
                if (jo[key] == null) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(key).Append('=').Append(SummarizeValue(jo[key]));
                if (sb.Length > ArgSummaryMaxLength)
                {
                    sb.Length = ArgSummaryMaxLength;
                    sb.Append('…');
                    break;
                }
            }
            return sb.ToString();
        }

        // Compact, single-line summary of tool arguments for the Editor console.
        // Goal: readable for humans glancing at logs, not a faithful JSON dump.
        // ObjectRef envelopes collapse to <asset:Path> / <scene:Path> / <guid:…> / <id:N "Name">.
        // MUST run on main thread — calls EditorUtility.EntityIdToObject and AssetDatabase.
        static string SummarizeArgs(JObject args)
        {
            if (args == null || args.Count == 0) return "";
            var sb = new StringBuilder();
            bool first = true;
            foreach (var prop in args.Properties())
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(prop.Name).Append('=').Append(SummarizeValue(prop.Value));
                if (sb.Length > ArgSummaryMaxLength)
                {
                    sb.Length = ArgSummaryMaxLength;
                    sb.Append('…');
                    break;
                }
            }
            return sb.ToString();
        }

        static string SummarizeValue(JToken token)
        {
            if (token == null) return "null";
            switch (token.Type)
            {
                case JTokenType.Null:
                    return "null";
                case JTokenType.String:
                    var s = (string)token ?? "";
                    return s.Length > StringValueMaxLength
                        ? "\"" + s.Substring(0, StringValueMaxLength) + "…\""
                        : "\"" + s + "\"";
                case JTokenType.Integer:
                    var asLong = (long)token;
                    var resolvedName = TryResolveEntityIdName(asLong);
                    return resolvedName != null
                        ? asLong + " \"" + resolvedName + "\""
                        : asLong.ToString();
                case JTokenType.Float:
                case JTokenType.Boolean:
                    return token.ToString();
                case JTokenType.Array:
                    return SummarizeArray((JArray)token);
                case JTokenType.Object:
                    return SummarizeObject((JObject)token);
                default:
                    return token.Type.ToString();
            }
        }

        // Inline a small array of short strings so savedPaths/dependencies/etc. are readable in
        // the log; fall back to a count for everything else.
        static string SummarizeArray(JArray arr)
        {
            if (arr.Count == 0) return "[]";
            if (arr.Count <= 5)
            {
                int totalLen = 0;
                foreach (var t in arr)
                {
                    if (t.Type != JTokenType.String) return "[" + arr.Count + "]";
                    totalLen += ((string)t)?.Length ?? 0;
                    if (totalLen > 120) return "[" + arr.Count + "]";
                }
                var sb = new StringBuilder("[");
                for (int i = 0; i < arr.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append('"').Append((string)arr[i]).Append('"');
                }
                sb.Append(']');
                return sb.ToString();
            }
            return "[" + arr.Count + "]";
        }

        static string SummarizeObject(JObject jo)
        {
            if (jo["__ref"] != null)
            {
                var assetPath = (string)jo["assetPath"];
                if (!string.IsNullOrEmpty(assetPath)) return "<asset:" + assetPath + ">";
                var hierarchyPath = (string)jo["path"];
                if (!string.IsNullOrEmpty(hierarchyPath))
                {
                    var sceneName = (string)jo["sceneName"];
                    return "<scene:" + (string.IsNullOrEmpty(sceneName) ? "" : sceneName + ":") + hierarchyPath + ">";
                }
                var guid = (string)jo["guid"];
                if (!string.IsNullOrEmpty(guid))
                {
                    var shortGuid = guid.Substring(0, Math.Min(8, guid.Length));
                    var pathFromGuid = AssetDatabase.GUIDToAssetPath(guid);
                    return !string.IsNullOrEmpty(pathFromGuid)
                        ? "<guid:" + shortGuid + " \"" + pathFromGuid + "\">"
                        : "<guid:" + shortGuid + ">";
                }
                if (jo["instanceId"] != null && jo["instanceId"].Type == JTokenType.Integer)
                {
                    var iid = (long)jo["instanceId"];
                    var nameHint = (string)jo["name"] ?? TryResolveEntityIdName(iid);
                    return nameHint != null ? "<id:" + iid + " \"" + nameHint + "\">" : "<id:" + iid + ">";
                }
                return "<ref>";
            }
            if (jo["__type"] != null) return "<type:" + (string)jo["__type"] + ">";
            return "{" + jo.Count + " keys}";
        }

        // Resolves a wire-format EntityId to the underlying Object's name, or null if it
        // doesn't resolve / is destroyed. Most non-id integers (ports, counts, depths) won't
        // resolve and will fall through cleanly. Caller MUST be on the main thread.
        static string TryResolveEntityIdName(long wireId)
        {
            try
            {
                var obj = EntityIdUtil.Resolve(wireId);
                if (obj == null) return null;
                var n = obj.name;
                return string.IsNullOrEmpty(n) ? null : n;
            }
            catch { return null; }
        }
    }
}
