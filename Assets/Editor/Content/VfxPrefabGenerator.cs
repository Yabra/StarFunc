using System.IO;
using UnityEditor;
using UnityEngine;

namespace StarFunc.Editor.Content
{
    /// <summary>
    /// One-shot generator that creates the five VFX prefabs referenced by
    /// <c>VfxConfig</c> (StarFunc/Config/VfxConfig). Each prefab is a small
    /// non-looping <see cref="ParticleSystem"/> tuned for its feedback event:
    /// gold burst on star placement, red burst on error, confetti on level
    /// complete, slow glow trail for constellation restore, expanding ring on
    /// sector unlock. The generated prefabs auto-destroy via <c>StopAction</c>.
    ///
    /// Run: <c>StarFunc → Generate VFX Prefabs</c>. Existing prefabs at the
    /// target paths are overwritten so designers can re-run after tweaking the
    /// generator.
    /// </summary>
    public static class VfxPrefabGenerator
    {
        const string EffectsRoot = "Assets/Prefabs/Effects";

        [MenuItem("StarFunc/Generate VFX Prefabs")]
        public static void GenerateAll()
        {
            EnsureFolder(EffectsRoot);

            CreateBurstPrefab(
                name: "VFX_StarPlaced",
                duration: 0.5f,
                color: new Color(1f, 0.85f, 0.3f, 1f),
                burstCount: 28,
                startSpeed: 5f,
                startSize: 0.18f,
                lifetime: 0.5f,
                gravity: 0.5f);

            CreateBurstPrefab(
                name: "VFX_StarError",
                duration: 0.3f,
                color: new Color(0.95f, 0.25f, 0.25f, 1f),
                burstCount: 22,
                startSpeed: 4f,
                startSize: 0.16f,
                lifetime: 0.3f,
                gravity: 0.8f);

            CreateBurstPrefab(
                name: "VFX_LevelComplete",
                duration: 2f,
                color: new Color(1f, 0.8f, 0.4f, 1f),
                burstCount: 80,
                startSpeed: 7f,
                startSize: 0.22f,
                lifetime: 2f,
                gravity: 1.2f);

            CreateBurstPrefab(
                name: "VFX_ConstellationRestored",
                duration: 1.2f,
                color: new Color(0.6f, 0.85f, 1f, 1f),
                burstCount: 16,
                startSpeed: 1.5f,
                startSize: 0.14f,
                lifetime: 1.2f,
                gravity: 0f);

            CreateBurstPrefab(
                name: "VFX_SectorUnlock",
                duration: 1.5f,
                color: new Color(0.95f, 0.7f, 1f, 1f),
                burstCount: 50,
                startSpeed: 6f,
                startSize: 0.2f,
                lifetime: 1.5f,
                gravity: 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VfxPrefabGenerator] Generated 5 VFX prefabs in " + EffectsRoot);
        }

        static void CreateBurstPrefab(
            string name, float duration, Color color, int burstCount,
            float startSpeed, float startSize, float lifetime, float gravity)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();

            // ParticleSystem cannot be configured before its modules exist; the
            // AddComponent above gives us the default configured one. Now tune.
            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.maxParticles = Mathf.Max(burstCount * 2, 200);

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sortingOrder = 10;
                // Default-Particle material — same one Unity uses for new
                // particle systems out of the box.
                var defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>(
                    "Default-ParticleSystem.mat");
                if (defaultMat != null) renderer.sharedMaterial = defaultMat;
            }

            string path = $"{EffectsRoot}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
