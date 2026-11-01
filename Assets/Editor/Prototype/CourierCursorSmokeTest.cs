using System;
using System.IO;
using NightCourier;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierCursorSmokeTest
{
    private const string Flag = "/tmp/courier-cursor-check";
    private const string Result = "/tmp/courier-cursor-result.txt";
    static CourierCursorSmokeTest() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (!File.Exists(Flag) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Flag); Run();
    }
    [MenuItem("Tools/Night Courier/Checks/Cursors")]
    public static void Run()
    {
        GameObject root = null;
        try
        {
            string[] names = { "default", "pointer", "pressed", "grab", "grabbing", "blocked", "inspect" };
            foreach (string name in names)
            {
                string path = "Assets/Resources/NightVoyage/Cursors/" + name + ".png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Require(texture != null && texture.isReadable && texture.width == 48 && texture.height == 48, "Invalid texture: " + name);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Require(importer.textureType == TextureImporterType.Cursor && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed, "Invalid import: " + name);
                bool opaque = false, clear = false;
                foreach (Color32 pixel in texture.GetPixels32()) { opaque |= pixel.a > 0; clear |= pixel.a == 0; }
                Require(opaque && clear, "Blank or opaque background: " + name);
            }
            root = new GameObject("CursorCheck", typeof(RectTransform), typeof(CanvasGroup));
            root.hideFlags = HideFlags.HideAndDontSave;
            var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(root.transform);
            var button = buttonObject.GetComponent<Button>();
            var label = new GameObject("Label", typeof(RectTransform)); label.transform.SetParent(buttonObject.transform);
            Require(CourierCursor.Resolve(label) == CourierCursor.State.Pointer, "Button child not clickable");
            button.interactable = false;
            Require(CourierCursor.Resolve(label) == CourierCursor.State.Blocked, "Disabled button not blocked");
            button.interactable = true; root.GetComponent<CanvasGroup>().interactable = false;
            Require(CourierCursor.Resolve(label) == CourierCursor.State.Blocked, "CanvasGroup not respected");
            root.GetComponent<CanvasGroup>().interactable = true;
            var hintObject = new GameObject("Hint", typeof(RectTransform), typeof(CourierIconHint)); hintObject.transform.SetParent(root.transform);
            var hint = hintObject.GetComponent<CourierIconHint>(); hint.message = "Order details";
            Require(CourierCursor.Resolve(hintObject) == CourierCursor.State.Inspect, "Hint not inspectable");
            hint.showOnClick = true;
            Require(CourierCursor.Resolve(hintObject) == CourierCursor.State.Pointer, "Clickable hint not clickable");
            var mapObject = new GameObject("Map", typeof(RectTransform), typeof(CourierMapCanvas)); mapObject.transform.SetParent(root.transform);
            Require(CourierCursor.Resolve(mapObject) == CourierCursor.State.Default, "Inactive map suggests dragging");
            var cargoObject = new GameObject("Cargo", typeof(RectTransform), typeof(CourierCargoRow)); cargoObject.transform.SetParent(root.transform);
            Require(!cargoObject.GetComponent<CourierCargoRow>().CanDrag && !cargoObject.GetComponent<CourierCargoRow>().IsDragging, "Unbound cargo suggests dragging");
            Require(CourierCursor.Resolve(root) == CourierCursor.State.Default && CourierCursor.Resolve(null) == CourierCursor.State.Default, "Background not default");
            File.WriteAllText(Result, "PASS: 7 atlas-derived transparent cursor textures; button and child, disabled button, CanvasGroup, hover/click hints, inactive map/cargo, background.\n");
            Debug.Log("Courier cursor checks passed.");
        }
        catch (Exception exception) { File.WriteAllText(Result, "FAIL: " + exception); Debug.LogException(exception); }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
