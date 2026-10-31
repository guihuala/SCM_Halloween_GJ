using NightCourier;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CourierMapPageInstaller
{
    private const string Folder="Assets/Resources/Panel/Courier/";
    [MenuItem("Tools/Night Courier/Arrange Map Page (Map and Cargo Drawer)")]
    public static void Install()
    {
        var root=PrefabUtility.LoadPrefabContents(Folder+"CourierRoutePanel.prefab");
        try
        {
            var panel=root.GetComponent<CourierRoutePanel>();var page=root.transform.Find("Window");
            // These were presentation-only objects from the retired portrait/sidebar layout.
            foreach(string name in new[]{"GoolieIllustration","CourierName","CourierRole","CourierDivider","CargoBackdrop"})
            {var old=page.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
            Color mint=new Color(.45f,.94f,.77f),muted=new Color(.66f,.74f,.77f);
            if(panel.cargoDrawer==null)
            {
                panel.cargoDrawer=Element(page,"CargoDrawer");
                var background=panel.cargoDrawer.gameObject.AddComponent<Image>();background.color=new Color(.045f,.075f,.105f,.99f);background.raycastTarget=true;
                var border=panel.cargoDrawer.gameObject.AddComponent<Outline>();border.effectColor=new Color(.45f,.94f,.77f,.35f);border.effectDistance=new Vector2(1,-1);
                panel.waitingSection.SetParent(panel.cargoDrawer,false);panel.loadedSection.SetParent(panel.cargoDrawer,false);
            }
            panel.cargoHeading=Label(panel,panel.cargoDrawer,panel.cargoHeading,"CargoHeading","CARGO",24,mint);
            panel.cargoHeading.transform.SetParent(panel.cargoDrawer,false);panel.cargoHeading.text="CARGO";panel.cargoHeading.fontSize=24;
            panel.waitingEmptyText=Label(panel,panel.waitingSection,panel.waitingEmptyText,"WaitingEmptyText","Nothing to pick up at this stop.",24,muted);
            panel.loadedEmptyText=Label(panel,panel.loadedSection,panel.loadedEmptyText,"LoadedEmptyText","Your trunk is empty.",24,muted);
            if(panel.cargoToggleButton==null)panel.cargoToggleButton=Button(panel,page,"CargoToggleButton","Cargo  [Tab]");
            panel.cargoToggleText=panel.cargoToggleButton.GetComponentInChildren<Text>(true);
            if(panel.cargoCloseButton==null)panel.cargoCloseButton=Button(panel,panel.cargoDrawer,"CargoCloseButton","Close  [Tab]");
            if(panel.nodePromptButton==null)
            {
                panel.nodePromptButton=Button(panel,panel.mapGraphic.transform.parent,"NodePromptButton","Talk  [E]");
                panel.nodePromptButton.GetComponent<Image>().color=new Color(.05f,.18f,.16f,.96f);
                var colors=panel.nodePromptButton.colors;colors.disabledColor=Color.white;panel.nodePromptButton.colors=colors;
                var border=panel.nodePromptButton.gameObject.AddComponent<Outline>();border.effectColor=new Color(.45f,.94f,.77f,.35f);border.effectDistance=new Vector2(1,-1);
            }
            panel.nodePromptText=panel.nodePromptButton.GetComponentInChildren<Text>(true);
            panel.nodePromptText.color=mint;
            panel.nodePromptButton.gameObject.SetActive(false);
            Decoration(page,"PageHeaderRule",new Color(.45f,.94f,.77f,.18f));
            Decoration(page,"SummaryRule",new Color(.45f,.94f,.77f,.18f));
            if(panel.objectiveText!=null)panel.objectiveText.fontSize=26;
            panel.ApplyFullscreenLayout();panel.SetCargoOpen(false);
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"CourierRoutePanel.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("COURIER_MAP_PAGE INSTALLED: full-width map, top vehicle status, collapsible two-column cargo drawer; portrait removal preserved.");
    }
    private static RectTransform Element(Transform parent,string name)
    {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);return rect;}
    private static Image Decoration(Transform parent,string name,Color color)
    {
        var current=parent.Find(name);if(current!=null)return current.GetComponent<Image>();
        var image=Element(parent,name).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    private static Text Label(CourierRoutePanel panel,Transform parent,Text existing,string name,string content,int size,Color color)
    {
        if(existing!=null)return existing;
        var text=Element(parent,name).gameObject.AddComponent<Text>();text.text=content;text.font=panel.vehicleText.font;text.fontSize=size;text.color=color;
        text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;return text;
    }
    private static Button Button(CourierRoutePanel panel,Transform parent,string name,string caption)
    {
        var rect=Element(parent,name);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.105f,.15f,.19f);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var label=Label(panel,rect,null,"Label",caption,24,new Color(.92f,.95f,.93f));label.alignment=TextAnchor.MiddleCenter;
        label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
        var feedback=rect.gameObject.AddComponent<CourierButtonFeedback>();feedback.effectPrefab=Resources.Load<CourierInteractionEffect>("Feedback/CourierInteractionEffect");return button;
    }
    public static void InstallAndCheck(){Install();CourierMapPageSmokeTest.Run();}
}
