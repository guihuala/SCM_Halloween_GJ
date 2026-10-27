using UnityEngine;

public static class SceneCatalogProvider
{
    private const string CatalogPath = "Data/SceneCatalog";

    private static SceneCatalog _catalog;

    public static SceneCatalog Catalog
    {
        get
        {
            if (_catalog == null)
            {
                _catalog = Resources.Load<SceneCatalog>(CatalogPath);
            }

            return _catalog;
        }
    }

    public static string GetSceneName(GameScene scene)
    {
        SceneCatalog catalog = Catalog;

        if (catalog != null && catalog.TryGetSceneName(scene, out string sceneName))
        {
            return sceneName;
        }

        return scene.ToString();
    }
}
