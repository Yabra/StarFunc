using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    public class LocalSaveService : ISaveService
    {
        static readonly string SavePath =
            Path.Combine(Application.persistentDataPath, "save.json");

        static readonly string ChecksumPath =
            Path.Combine(Application.persistentDataPath, "save.sha256");

        static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        // Shared in-process PlayerSaveData reference. Every service that calls
        // Load() at startup gets the SAME instance, so their mutations are
        // visible to each other. Without this, each service held a private
        // copy and the last Save() wrote a stale snapshot — silently dropping
        // fragments, hint consumption, etc.
        PlayerSaveData _current;

        public bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public PlayerSaveData Load()
        {
            if (_current != null)
                return _current;

            if (File.Exists(SavePath))
            {
                var json = File.ReadAllText(SavePath, Encoding.UTF8);

                if (!VerifyChecksum(json))
                {
                    Debug.LogWarning("LocalSaveService: checksum mismatch — save file may be corrupted or tampered with. Starting fresh.");
                }
                else
                {
                    try
                    {
                        _current = JsonConvert.DeserializeObject<PlayerSaveData>(json, JsonSettings);
                        if (_current != null) return _current;
                    }
                    catch (JsonException ex)
                    {
                        Debug.LogError($"LocalSaveService: failed to deserialize save — {ex.Message}. Starting fresh.");
                    }
                }
            }

            // No file (first launch) or unrecoverable load failure — seed a
            // shared empty instance so every service Load() call returns the
            // same reference instead of each fabricating its own.
            _current = new PlayerSaveData();
            return _current;
        }

        public void Save(PlayerSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            data.IncrementVersion();

            // Adopt the saved instance as the canonical one so subsequent
            // Load() callers (and a post-cloud-merge Save with a different
            // reference) all see the latest state.
            _current = data;

            var json = JsonConvert.SerializeObject(data, JsonSettings);
            File.WriteAllText(SavePath, json, Encoding.UTF8);
            WriteChecksum(json);
        }

        public void Delete()
        {
            _current = null;
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            if (File.Exists(ChecksumPath))
                File.Delete(ChecksumPath);
        }

        bool VerifyChecksum(string json)
        {
            if (!File.Exists(ChecksumPath))
                return false;

            var stored = File.ReadAllText(ChecksumPath, Encoding.UTF8).Trim();
            var computed = ComputeSha256(json);
            return string.Equals(stored, computed, StringComparison.OrdinalIgnoreCase);
        }

        void WriteChecksum(string json)
        {
            File.WriteAllText(ChecksumPath, ComputeSha256(json), Encoding.UTF8);
        }

        static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
