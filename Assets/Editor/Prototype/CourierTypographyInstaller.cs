using System.IO;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CourierTypographyInstaller
{
    [MenuItem("Tools/Night Courier/Apply Handwritten Fonts (preserve layouts)")]
    public static void Install()
    {
        if (CourierTypography.Body == null || CourierTypography.Display == null)
            throw new System.InvalidOperationException("Handwritten fonts are missing.");
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Panel", "Assets/Resources/Feedback", "Assets/Prefab/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try { CourierTypography.Apply(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var path in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GamePlay.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects()) CourierTypography.Apply(root);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        AssetDatabase.SaveAssets();
        Debug.Log("COURIER_TYPOGRAPHY PASS: Short Stack headings/actions, Itim dialogue/orders/hints.");
    }
}
