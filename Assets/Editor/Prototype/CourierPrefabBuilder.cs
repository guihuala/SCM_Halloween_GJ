using System.IO;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Prefabs contain real Text/Image/Button/ScrollRect objects and serialized component references.
public static class CourierPrefabBuilder
{
    private const string Folder = "Assets/Resources/Panel/Courier";
    private static readonly Color Ink = new Color(.035f,.055f,.085f), Card = new Color(.075f,.11f,.15f), Soft = new Color(.105f,.15f,.19f);
    private static readonly Color White = new Color(.92f,.95f,.93f), Muted = new Color(.66f,.74f,.77f), Mint = new Color(.45f,.94f,.77f), Orange = new Color(1,.66f,.34f);
    private static Font font;
    [MenuItem("Tools/Night Courier/Rebuild UI Prefabs (overwrite layouts)")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder); Directory.CreateDirectory("Assets/Prefab/UI"); AssetDatabase.Refresh();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildRow(); BuildHud(); BuildCargo(); BuildRoute(); BuildVehicle(); BuildEvent(); BuildResult();
        var registry = AssetDatabase.LoadAssetAtPath<UIDatas>("Assets/Resources/Data/UIDataListSO.asset");
        foreach (var name in new[]{"CourierHudPanel","CourierCargoPanel","CourierRoutePanel","CourierVehiclePanel","CourierEventPanel","CourierResultPanel"})
        {
            var entry = registry.uiDataList.FirstOrDefault(x=>x.uiName==name);
            if(entry==null) { entry=new UIData{uiName=name}; registry.uiDataList.Add(entry); }
            entry.uiPath="Panel/Courier/"+name;
        }
        EditorUtility.SetDirty(registry); AssetDatabase.SaveAssets();
        Debug.Log("COURIER_PREFABS PASS: six panels and a cargo row saved with serialized references.");
    }
    public static void BuildAndRun() { Build(); NightCourierPanelSmokeTest.Run(); }
    private static RectTransform Element(string name,Transform parent,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();
        if(parent!=null) rt.SetParent(parent,false);
        rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);
        return rt;
    }
    private static void Center(RectTransform rt,float w,float h)
    { rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=Vector2.zero;rt.sizeDelta=new Vector2(w,h); }
    private static void Stretch(RectTransform rt)
    { rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.pivot=new Vector2(.5f,.5f);rt.offsetMin=rt.offsetMax=Vector2.zero; }
    private static Image Background(RectTransform rt,Color c,bool raycast=false)
    {var image=rt.gameObject.AddComponent<Image>();image.color=c;image.raycastTarget=raycast;return image;}
    private static Text Text(Transform parent,string name,string content,float x,float y,float w,float h,int size=24,Color? color=null)
    {
        var t=Element(name,parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=content;t.fontSize=size;t.color=color??White;
        t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.lineSpacing=1.18f;
        return t;
    }
    private static Button Button(Transform parent,string name,string caption,float x,float y,float w,float h,bool primary=true)
    {
        var rt=Element(name,parent,x,y,w,h);var image=Background(rt,primary?Mint:Soft,true);var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.highlightedColor=new Color(.85f,1,.94f);colors.selectedColor=Color.white;colors.pressedColor=new Color(.6f,.8f,.72f);colors.disabledColor=new Color(.4f,.45f,.48f,.75f);colors.fadeDuration=.1f;button.colors=colors;
        var outline=rt.gameObject.AddComponent<Outline>();outline.effectColor=Orange;outline.effectDistance=new Vector2(3,-3);outline.useGraphicAlpha=false;outline.enabled=false;
        var focus=rt.gameObject.AddComponent<CourierFocusRing>();focus.ring=outline;
        var text=Text(rt,"Label",caption,12,0,w-24,h,24,primary?Ink:White);text.alignment=TextAnchor.MiddleCenter;
        return button;
    }
    private static T Panel<T>(string name,bool modal,out RectTransform card,float width=1440,float height=880) where T:CourierPanel
    {
        var root=Element(name,null,0,0,1920,1080);Stretch(root);
        Background(root,modal?new Color(0,0,0,.78f):Ink,true);root.gameObject.AddComponent<CanvasGroup>();
        card=Element(modal?"Window":"HudLayout",root,0,0,width,height);Center(card,width,height);
        if(modal) Background(card,Card);
        var panel=root.gameObject.AddComponent<T>();
        var serialized=new SerializedObject(panel);
        serialized.FindProperty("backgroundFadeDuration").floatValue=modal?.08f:0;
        serialized.FindProperty("fadeInDuration").floatValue=modal?.12f:0;
        serialized.FindProperty("fadeOutDuration").floatValue=0;
        serialized.FindProperty("scaleAnimation").boolValue=false;
        serialized.ApplyModifiedPropertiesWithoutUndo();return panel;
    }
    private static void Header(CourierPanel panel,RectTransform card,string title,string subtitle,bool close=true)
    {
        Text(card,"Title",title,40,25,1100,64,40);
        if(close) panel.closeButton=Button(card,"CloseButton","关闭",card.sizeDelta.x-145,30,105,54,false);
    }
    private static void Save(CourierPanel panel)
    { PrefabUtility.SaveAsPrefabAsset(panel.gameObject,Folder+"/"+panel.GetType().Name+".prefab");Object.DestroyImmediate(panel.gameObject); }
    private static void BuildHud()
    {
        RectTransform card;var p=Panel<CourierHudPanel>("CourierHudPanel",false,out card,1800,1000);
        p.locationText=Text(card,"LocationText","午夜邮局",0,16,800,60,36);
        p.timeText=Text(card,"TimeText","剩余240分钟",1000,23,260,48,26,Orange);
        p.conditionText=Text(card,"ConditionText","耐久70/70",1280,23,220,48,26);
        p.goalText=Text(card,"GoalText","送达0/4",1510,23,170,48,26,Mint);
        p.pauseButton=Button(card,"PauseButton","暂停",1700,18,100,54,false);
        var stage=Element("Stage",card,0,120,1800,750);p.stageGraphic=stage.gameObject.AddComponent<CourierStageGraphic>();p.stageGraphic.raycastTarget=false;
        p.controlsText=Text(card,"ControlsText","Tab 下车",0,924,800,48,22,Muted);
        p.cargoButton=Button(card,"CargoButton","接货",1130,914,210,64);
        p.routeButton=Button(card,"RouteButton","出发",1360,914,210,64);
        p.vehicleButton=Button(card,"VehicleButton","车辆",1590,914,210,64,false);
        Save(p);
    }
    private static void BuildRow()
    {
        var rt=Element("CourierCargoRow",null,0,0,550,126);Background(rt,Soft);
        var row=rt.gameObject.AddComponent<CourierCargoRow>();var layout=rt.gameObject.AddComponent<LayoutElement>();layout.preferredHeight=126;layout.minHeight=126;
        row.nameText=Text(rt,"ParcelName","余温尚存的信",16,8,380,36,24);
        row.destinationText=Text(rt,"Destination","→ 霓虹餐厅 · 1格",16,46,380,32,21,Muted);
        row.requirementsText=Text(rt,"Requirements","限时65分钟",16,85,380,30,19,Orange);
        row.actionButton=Button(rt,"LoadButton","装车",410,35,112,54);row.actionLabel=row.actionButton.GetComponentInChildren<Text>();
        PrefabUtility.SaveAsPrefabAsset(rt.gameObject,"Assets/Prefab/UI/CourierCargoRow.prefab");Object.DestroyImmediate(rt.gameObject);
    }
    private static Transform Scroll(Transform parent,string name,float x,float y,float width,float height)
    {
        var root=Element(name,parent,x,y,width,height);Background(root,new Color(.045f,.07f,.1f));
        var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
        var viewport=Element("Viewport",root,0,0,width-24,height);Background(viewport,Color.clear,true);viewport.gameObject.AddComponent<RectMask2D>();
        var content=Element("Content",viewport,0,0,width-24,0);content.anchorMax=new Vector2(1,1);content.sizeDelta=new Vector2(0,0);
        var group=content.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=14;group.padding=new RectOffset(0,0,0,10);group.childControlWidth=true;group.childControlHeight=true;group.childForceExpandHeight=false;group.childForceExpandWidth=true;
        var fitter=content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var bar=Element("Scrollbar",root,width-15,0,12,height);Background(bar,Soft,true);
        var handle=Element("Handle",bar,0,0,12,height);Stretch(handle);var image=Background(handle,Muted,true);
        var scrollbar=bar.gameObject.AddComponent<Scrollbar>();scrollbar.targetGraphic=image;scrollbar.handleRect=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;
        scroll.viewport=viewport;scroll.content=content;scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        return content;
    }
    private static void BuildCargo()
    {
        RectTransform card;var p=Panel<CourierCargoPanel>("CourierCargoPanel",true,out card,1280,730);
        Header(p,card,"货物","");
        p.capacityText=Text(card,"CapacityText","载重0/4格",800,32,290,48,26,Mint);
        Text(card,"WaitingHeading","待取",40,110,550,42,26);Text(card,"OnboardHeading","已装车",670,110,550,42,26);
        p.waitingContent=Scroll(card,"WaitingParcels",40,166,570,420);p.loadedContent=Scroll(card,"LoadedParcels",670,166,570,420);
        p.waitingEmptyText=Text(card,"WaitingEmpty","暂无货物",60,290,500,70,24,Muted);
        p.loadedEmptyText=Text(card,"LoadedEmpty","货舱为空",690,290,500,70,24,Muted);
        p.adviceText=Text(card,"LoadAdvice","满载：速度降低，损耗增加",40,622,780,50,22,Orange);
        p.continueButton=Button(card,"ContinueButton","选路线",1020,630,220,60);
        p.rowPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/CourierCargoRow.prefab").GetComponent<CourierCargoRow>();Save(p);
    }
    private static void BuildRoute()
    {
        RectTransform card;var p=Panel<CourierRoutePanel>("CourierRoutePanel",true,out card,1440,780);
        Header(p,card,"路线","");p.locationText=card.Find("Title").GetComponent<Text>();
        var map=Element("RoadMap",card,20,130,850,560);Background(map,new Color(.045f,.07f,.1f));
        var edges=Element("RoadConnections",map,0,0,850,560);p.mapGraphic=edges.gameObject.AddComponent<CourierRouteGraphic>();p.mapGraphic.raycastTarget=false;
        var data=new CourierRun();p.stopButtons=new Button[data.Stops.Length];p.stopLabels=new Text[data.Stops.Length];
        for(int i=0;i<data.Stops.Length;i++)
        {
            var point=CourierRouteGraphic.NodePoint(data.Stops[i]);
            var b=Button(map,"Stop_"+i,data.Stops[i].Name,point.x-82,point.y-40,164,80,false);
            p.stopButtons[i]=b;p.stopLabels[i]=b.GetComponentInChildren<Text>();p.stopLabels[i].fontSize=21;
        }
        p.destinationText=Text(card,"DestinationText","选择目的地",925,160,450,65,32);
        p.costText=Text(card,"TravelCostText","",925,270,450,130,27);
        p.riskText=Text(card,"RoadRiskText","",925,440,445,75,24,Orange);
        Text(card,"MapLegend","实线 公路   ·   虚线 碎石路",40,710,700,40,20,Muted);
        p.cargoButton=Button(card,"CargoButton","货物",900,685,200,60,false);
        p.departButton=Button(card,"DepartButton","出发",1120,685,280,60);Save(p);
    }
    private static void BuildVehicle()
    {
        RectTransform card;var p=Panel<CourierVehiclePanel>("CourierVehiclePanel",true,out card,1140,650);
        Header(p,card,"车辆","");
        p.currentText=Text(card,"CurrentVehicleText","轻型轿车",40,138,500,55,30,Mint);
        p.currentStatsText=Text(card,"CurrentStatsText","容量4格 · 耐久70/70",40,220,500,150,25);
        p.parkedText=Text(card,"ParkedVehicleText","无可附身车辆",600,138,500,55,30);
        p.parkedStatsText=Text(card,"ParkedStatsText","",600,220,500,150,25);
        p.possessButton=Button(card,"PossessButton","附身",600,415,500,60);
        p.repairText=Text(card,"RepairAdviceText","废车场、加油站可维修",40,415,500,60,22,Muted);
        p.repairButton=Button(card,"RepairButton","修复 · 12分钟",40,415,500,60,false);
        p.feedbackText=Text(card,"ActionFeedbackText","",40,535,1060,50,24,Orange);Save(p);
    }
    private static void BuildEvent()
    {
        RectTransform card;var p=Panel<CourierEventPanel>("CourierEventPanel",true,out card,1000,430);
        p.titleText=Text(card,"EventTitleText","公路事件",40,25,920,64,34);
        p.descriptionText=Text(card,"EventDescriptionText","",40,108,920,80,25,Muted);
        p.safeButton=Button(card,"SafeChoiceButton","谨慎处理",40,220,920,66);
        p.riskButton=Button(card,"RiskChoiceButton","冒险通过",40,314,920,66,false);Save(p);
    }
    private static void BuildResult()
    {
        RectTransform card;var p=Panel<CourierResultPanel>("CourierResultPanel",true,out card,1000,440);
        p.titleText=Text(card,"ResultTitleText","夜班完成",40,25,920,64,36,Mint);
        p.summaryText=Text(card,"ResultSummaryText","",40,115,920,195,26);
        p.restartButton=Button(card,"RestartButton","重开",40,340,440,60);
        p.menuButton=Button(card,"MainMenuButton","主菜单",520,340,440,60,false);Save(p);
    }
}
