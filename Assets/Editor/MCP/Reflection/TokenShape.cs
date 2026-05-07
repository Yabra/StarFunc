using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MCP.Reflection
{
    // Distinguishes "absent" (null/JTokenType.Null) from "wrong shape" for optional JSON args.
    // Absent → returns null (caller treats as default). Wrong shape → throws ArgumentException
    // with a JSON.stringify hint when the value looks like a stringified envelope.
    //
    // Some MCP clients incorrectly JSON.stringify nested complex values before sending. We can't
    // fix the client, so when a string token contains parseable JSON ({...} or [...]) we recover
    // by parsing it back into a real token and emit a warning so the client bug stays visible.
    //
    // Use these wherever a tool reads an optional nested object/array from its args, instead of
    // `as JArray` / `is JObject` patterns that silently fall through on stringified input.
    public static class TokenShape
    {
        public static JArray ExpectArrayOrNull(JToken token, string fieldName)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            var recovered = TryRecoverStringifiedJson(token, fieldName);
            if (recovered != null) token = recovered;
            if (token is JArray arr) return arr;
            throw new ArgumentException(BuildShapeError(fieldName, "array", token));
        }

        public static JObject ExpectObjectOrNull(JToken token, string fieldName)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            var recovered = TryRecoverStringifiedJson(token, fieldName);
            if (recovered != null) token = recovered;
            if (token is JObject obj) return obj;
            throw new ArgumentException(BuildShapeError(fieldName, "object", token));
        }

        // If `token` is a String holding parseable JSON object/array, return the parsed token
        // and log a warning. Otherwise return null (caller keeps the original token).
        // Single-level recovery only — clients that double-stringify still fail with a clear error.
        public static JToken TryRecoverStringifiedJson(JToken token, string fieldName)
        {
            if (!(token is JValue jv) || jv.Type != JTokenType.String) return null;
            var raw = (string)jv ?? "";
            var trimmed = raw.TrimStart();
            if (!trimmed.StartsWith("[") && !trimmed.StartsWith("{")) return null;
            try
            {
                var parsed = JToken.Parse(raw);
                if (parsed is JObject || parsed is JArray)
                {
                    // Disabled: client sends stringified JSON consistently and we can't fix it,
                    // so warning every call just adds noise. Re-enable if a different client appears.
                    // Debug.LogWarning($"[MCP] auto-parsed stringified JSON for '{fieldName}' — client should send nested JSON as a real object/array, not as a string.");
                    return parsed;
                }
                return null;
            }
            catch (JsonException) { return null; }
        }

        public static string BuildShapeError(string fieldName, string expectedKind, JToken token)
        {
            var got = token?.Type.ToString() ?? "null";
            var msg = $"'{fieldName}' must be a JSON {expectedKind}, got {got}.";
            if (token is JValue jv && jv.Type == JTokenType.String)
            {
                var s = ((string)jv ?? "").TrimStart();
                if (s.StartsWith("[") || s.StartsWith("{"))
                    msg += $" The value looks like a JSON-encoded string and failed to parse — check that the {expectedKind} is well-formed JSON.";
            }
            return msg;
        }
    }
}
