using StarFunc.Core;
using UnityEngine;

namespace StarFunc.Infrastructure
{
    public class BootInitializer : MonoBehaviour
    {
        void Awake()
        {
            var sfmObject = new GameObject("[SceneFlowManager]");
            DontDestroyOnLoad(sfmObject);
            var sceneFlowManager = sfmObject.AddComponent<SceneFlowManager>();
            ServiceLocator.Register<SceneFlowManager>(sceneFlowManager);
        }

        void Start()
        {
            var sfm = ServiceLocator.Get<SceneFlowManager>();
            sfm.LoadScene("Hub");
        }
    }
}
