using StarFunc.Core;
using StarFunc.Data;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    /// <summary>
    /// Attach to a DontDestroyOnLoad object to auto-save on pause and quit.
    /// Requires <see cref="ISaveService"/> and a live <see cref="PlayerSaveData"/>
    /// to be registered in <see cref="ServiceLocator"/>.
    /// </summary>
    public class AutoSaveHandler : MonoBehaviour
    {
        PlayerSaveData _data;
        ISaveService _saveService;

        public void Initialize(ISaveService saveService, PlayerSaveData data)
        {
            _saveService = saveService;
            _data = data;
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                TrySave();
        }

        void OnApplicationQuit()
        {
            TrySave();
        }

        void TrySave()
        {
            if (_saveService == null || _data == null)
                return;

            try
            {
                _saveService.Save(_data);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"AutoSaveHandler: failed to save — {ex.Message}");
            }
        }
    }
}
