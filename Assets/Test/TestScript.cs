using StarFunc.Data;
using StarFunc.Gameplay;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public StarManager starManager;
    public LevelData testLevel;
    void Start()
    {
        starManager.SpawnStars(testLevel.Stars);
    }
}
