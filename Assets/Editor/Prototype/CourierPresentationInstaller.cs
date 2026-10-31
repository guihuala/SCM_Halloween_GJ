using System;
using System.IO;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEngine;

public static class CourierPresentationInstaller
{
    public const string GuidePath="Assets/Resources/Panel/Courier/CourierTutorialPanel.prefab";
    private const string LegacyPath="Assets/Prefab/UI/CourierTutorial.prefab";
    [MenuItem("Tools/Night Courier/Install Fullscreen Map and Tutorial Panel")]
    public static void Install()
    {
        // Move the existing guide to retain its authored text, layout and asset GUID.
        if(!File.Exists(GuidePath))
        {
            string error=AssetDatabase.MoveAsset(LegacyPath,GuidePath);
            if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
        }
        foreach(var path in Directory.GetFiles("Assets/Resources/Panel/Courier","*.prefab"))
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(path==GuidePath)
                {
                    root.name="CourierTutorialPanel";
                    var panel=root.GetComponent<CourierTutorialPanel>()??root.AddComponent<CourierTutorialPanel>();
                    panel.view=root.GetComponent<CourierTutorialView>();
                    var settings=new SerializedObject(panel);
                    foreach(string field in new[]{"backgroundFadeDuration","fadeInDuration","fadeOutDuration"})settings.FindProperty(field).floatValue=0;
                    settings.FindProperty("scaleAnimation").boolValue=false;settings.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    foreach(var guide in root.GetComponentsInChildren<CourierTutorialView>(true))UnityEngine.Object.DestroyImmediate(guide.gameObject);
                    var route=root.GetComponent<CourierRoutePanel>();
                    if(route!=null)route.ApplyFullscreenLayout();
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        if(File.Exists(LegacyPath))AssetDatabase.DeleteAsset(LegacyPath);
        var registry=AssetDatabase.LoadAssetAtPath<UIDatas>("Assets/Resources/Data/UIDataListSO.asset");
        if(!registry.uiDataList.Any(x=>x.uiName=="CourierTutorialPanel"))registry.uiDataList.Add(new UIData{uiName="CourierTutorialPanel",uiPath="Panel/Courier/CourierTutorialPanel"});
        EditorUtility.SetDirty(registry);AssetDatabase.SaveAssets();
        Debug.Log("COURIER_PRESENTATION INSTALLED: fullscreen map and one independent tutorial overlay; embedded guides removed.");
    }
    public static void InstallAndCheck(){Install();CourierPresentationSmokeTest.Run();}
}
