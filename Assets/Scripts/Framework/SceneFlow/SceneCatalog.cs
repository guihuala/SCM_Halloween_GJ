using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SceneCatalogEntry
{
    public GameScene scene;
    public string sceneName;
    public string displayNameKey;
    public bool useLoadingScreen = true;
}

[CreateAssetMenu(fileName = "SceneCatalog", menuName = "Template/Scene Flow/Scene Catalog")]
public class SceneCatalog : ScriptableObject
{
    public List<SceneCatalogEntry> scenes = new List<SceneCatalogEntry>();

    public bool TryGetSceneName(GameScene scene, out string sceneName)
    {
        foreach (SceneCatalogEntry entry in scenes)
        {
            if (entry != null && entry.scene == scene && !string.IsNullOrEmpty(entry.sceneName))
            {
                sceneName = entry.sceneName;
                return true;
            }
        }

        sceneName = string.Empty;
        return false;
    }

    public SceneCatalogEntry GetEntry(GameScene scene)
    {
        foreach (SceneCatalogEntry entry in scenes)
        {
            if (entry != null && entry.scene == scene)
            {
                return entry;
            }
        }

        return null;
    }
}
