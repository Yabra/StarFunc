using System;
using System.Collections.Generic;
using UnityEditor;

namespace MCP.Server
{
    // Captures the asset paths Unity writes during a SaveAssets / SaveOpenScenes call.
    // Unity invokes OnWillSaveAssets on every AssetModificationProcessor right before
    // it writes any .asset / .unity file; we forward paths into the active capture list
    // when one is set, and pass the array through unchanged so VCS plugins still work.
    //
    // Single-active-capture model (no concurrency): the Unity editor processes save
    // operations on its main thread, and our MCP handlers dispatch to that same thread,
    // so two captures cannot run simultaneously.
    public class AssetSaveTracker : UnityEditor.AssetModificationProcessor
    {
        static List<string> _capture;

        public static List<string> Capture(Action saveAction)
        {
            if (saveAction == null) throw new ArgumentNullException(nameof(saveAction));
            var list = new List<string>();
            _capture = list;
            try { saveAction(); }
            finally { _capture = null; }
            return list;
        }

        // Called by Unity for every save Unity initiates. Magic name — discovered via reflection.
        static string[] OnWillSaveAssets(string[] paths)
        {
            if (_capture != null && paths != null)
                _capture.AddRange(paths);
            return paths;
        }
    }
}
