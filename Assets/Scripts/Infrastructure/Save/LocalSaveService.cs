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

        public bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public PlayerSaveData Load()
        {
            if (!File.Exists(SavePath))
                return null;

            var json = File.ReadAllText(SavePath, Encoding.UTF8);

            if (!VerifyChecksum(json))
            {
                Debug.LogWarning("LocalSaveService: checksum mismatch — save file may be corrupted or tampered with.");
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<PlayerSaveData>(json, JsonSettings);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"LocalSaveService: failed to deserialize save — {ex.Message}");
                return null;
            }
        }

        public void Save(PlayerSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            data.IncrementVersion();

            var json = JsonConvert.SerializeObject(data, JsonSettings);
            File.WriteAllText(SavePath, json, Encoding.UTF8);
            WriteChecksum(json);
        }

        public void Delete()
        {
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
