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
        Text(card,"Subtitle",subtitle,40,94,1280,45,22,Muted);
        if(close) panel.closeButton=Button(card,"CloseButton","关闭",card.sizeDelta.x-145,30,105,54,false);
        Background(Element("HeaderDivider",card,40,150,card.sizeDelta.x-80,2),Soft);
    }
    private static void Save(CourierPanel panel)
    { PrefabUtility.SaveAsPrefabAsset(panel.gameObject,Folder+"/"+panel.GetType().Name+".prefab");Object.DestroyImmediate(panel.gameObject); }
    private static void BuildHud()
    {
        RectTransform card;var panel=Panel<CourierHudPanel>("CourierHudPanel",false,out card,1800,1000);
        panel.locationText=Text(card,"LocationText","停靠站 · 午夜邮局",0,15,900,64,42);
        Text(card,"GoalDescription","在天亮前，送达4件快递。",0,81,900,38,24,Muted);
        panel.timeText=Text(card,"TimeText","天亮还剩\n240 分钟",1020,18,215,96,26,Orange);
        panel.conditionText=Text(card,"ConditionText","车辆耐久\n70 / 70",1245,18,205,96,26);
        panel.goalText=Text(card,"GoalText","送达目标\n0 / 4 件",1465,18,205,96,26,Mint);
        panel.pauseButton=Button(card,"PauseButton","暂停",1680,32,120,60,false);
        var stage=Element("Stage",card,0,165,1800,520);panel.stageGraphic=stage.gameObject.AddComponent<CourierStageGraphic>();panel.stageGraphic.raycastTarget=false;
        panel.stageSignText=Text(stage,"StationSignText","午夜邮局",40,25,1000,55,30,Mint);
        panel.npcText=Text(stage,"NpcHint","委托人 · 靠近按 E 接货",880,415,340,40,22);
        panel.parkedText=Text(stage,"ParkedVehicleHint","可附身载具",1250,445,440,40,22,Mint);
        panel.ghostButton=Button(card,"GhostButton","下车探索 · Tab",36,590,245,60,false);
        panel.instructionText=Text(card,"NextStepText","下一步：打开「接货与货舱」，选择要送的快递。",0,716,1800,60,32);
        panel.controlsText=Text(card,"ControlsText","下车后可操控幽灵，与委托人或废弃车辆互动。",0,778,1750,42,22,Muted);
        panel.cargoButton=Button(card,"CargoButton","1  接货与货舱",0,846,570,78);
        panel.routeButton=Button(card,"RouteButton","2  路线与出发",615,846,570,78);
        panel.vehicleButton=Button(card,"VehicleButton","载具与维修",1230,846,570,78,false);
        panel.feedbackText=Text(card,"FeedbackText","夜班开始。",0,947,1800,40,22,Mint);
        Save(panel);
    }
    private static void BuildRow()
    {
        var rt=Element("CourierCargoRow",null,0,0,610,164);Background(rt,Soft);
        var row=rt.gameObject.AddComponent<CourierCargoRow>();var layout=rt.gameObject.AddComponent<LayoutElement>();layout.preferredHeight=164;layout.minHeight=164;
        row.nameText=Text(rt,"ParcelName","余温尚存的信",18,12,430,40,25);
        row.destinationText=Text(rt,"Destination","送往 霓虹餐厅 · 重量 1 格 · 酬金 ¥30",18,54,420,48,20,Muted);
        row.requirementsText=Text(rt,"Requirements","【限时】 剩余65分钟",18,110,420,40,20,Orange);
        row.actionButton=Button(rt,"LoadButton","装上车",464,54,126,60);row.actionLabel=row.actionButton.GetComponentInChildren<Text>();
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
        RectTransform card;var p=Panel<CourierCargoPanel>("CourierCargoPanel",true,out card);
        Header(p,card,"接货与货舱","选货 → 查看目的地 → 装上车。左边是本站委托，右边是你的货物。");
        p.stationText=Text(card,"StationVoice","午夜邮局的委托",40,169,940,78,22,Muted);
        p.capacityText=Text(card,"CapacityText","货舱载重 0 / 4 格",1030,175,370,60,28,Mint);
        Text(card,"WaitingHeading","本站待取",40,263,620,43,28);Text(card,"OnboardHeading","已装上车",755,263,620,43,28);
        p.waitingContent=Scroll(card,"WaitingParcels",40,322,650,380);p.loadedContent=Scroll(card,"LoadedParcels",755,322,645,380);
        p.waitingEmptyText=Text(card,"WaitingEmpty","本站没有待取快递。\n去下一站看看。",65,385,570,120,26,Muted);
        p.loadedEmptyText=Text(card,"LoadedEmpty","货舱还是空的。\n点击左侧「装上车」接一件快递。",780,385,575,120,26,Muted);
        p.adviceText=Text(card,"LoadAdvice","先看目的地，再装货。",40,725,1350,50,22,Orange);
        p.continueButton=Button(card,"ContinueButton","装好了，选路线 →",985,793,415,60);
        p.rowPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/CourierCargoRow.prefab").GetComponent<CourierCargoRow>();Save(p);
    }
    private static void BuildRoute()
    {
        RectTransform card;var p=Panel<CourierRoutePanel>("CourierRoutePanel",true,out card);
        Header(p,card,"路线与出发","点击一个相邻站点，再看右侧代价，确认后出发。");
        p.locationText=Text(card,"LocationText","你在午夜邮局",40,170,1360,45,25,Mint);
        var map=Element("RoadMap",card,20,228,850,560);Background(map,new Color(.045f,.07f,.1f));
        var edges=Element("RoadConnections",map,0,0,850,560);p.mapGraphic=edges.gameObject.AddComponent<CourierRouteGraphic>();p.mapGraphic.raycastTarget=false;
        var data=new CourierRun();p.stopButtons=new Button[data.Stops.Length];p.stopLabels=new Text[data.Stops.Length];
        for(int i=0;i<data.Stops.Length;i++)
        {
            var point=CourierRouteGraphic.NodePoint(data.Stops[i]);
            var b=Button(map,"Stop_"+i,data.Stops[i].Name+"\n点击选择",point.x-82,point.y-40,164,80,false);
            p.stopButtons[i]=b;p.stopLabels[i]=b.GetComponentInChildren<Text>();p.stopLabels[i].fontSize=21;
        }
        Background(Element("RouteDetailCard",card,900,228,500,495),Soft);
        p.destinationText=Text(card,"DestinationText","先选一个相邻站点",925,250,450,78,32);
        p.costText=Text(card,"TravelCostText","点击地图查看耗时与耐久消耗。",925,345,450,150,25);
        p.riskText=Text(card,"RoadRiskText","实线：公路 · 虚线：碎石路",925,520,445,80,24,Orange);
        p.tipText=Text(card,"RouteAdviceText","先装上信，送到霓虹餐厅。",925,625,445,83,22,Muted);
        Text(card,"MapLegend","实线：平稳公路    虚线：碎石路",40,799,700,45,22,Muted);
        p.cargoButton=Button(card,"CargoButton","返回配载",900,789,220,65,false);
        p.departButton=Button(card,"DepartButton","确认路线，出发",1140,789,260,65);Save(p);
    }
    private static void BuildVehicle()
    {
        RectTransform card;var p=Panel<CourierVehiclePanel>("CourierVehiclePanel",true,out card);
        Header(p,card,"载具与维修","比较当前车辆与本站废弃车辆。附身后，货物会自动随你转移。");
        Background(Element("CurrentVehicleCard",card,40,187,650,385),Soft);Background(Element("ParkedVehicleCard",card,750,187,650,385),Soft);
        p.currentText=Text(card,"CurrentVehicleText","当前载具 · 轻型轿车",65,210,600,60,30,Mint);
        p.currentStatsText=Text(card,"CurrentStatsText","载重容量 4 格\n当前耐久 70 / 70\n\n不能承运重货。",65,295,590,250,26);
        p.parkedText=Text(card,"ParkedVehicleText","本站没有其他可附身载具",775,210,600,60,30);
        p.parkedStatsText=Text(card,"ParkedStatsText","皮卡在废车场，货车在南山墓园。",775,295,590,250,26);
        p.possessButton=Button(card,"PossessButton","附身这辆车",750,594,650,65);
        p.repairText=Text(card,"RepairAdviceText","本站没有维修服务。",40,601,650,60,24,Muted);
        p.repairButton=Button(card,"RepairButton","免费维修 · 耗时12分钟",40,689,650,65,false);
        p.feedbackText=Text(card,"ActionFeedbackText","换车不会恢复耐久，维修才会。",40,790,1340,55,25,Orange);Save(p);
    }
    private static void BuildEvent()
    {
        RectTransform card;var p=Panel<CourierEventPanel>("CourierEventPanel",true,out card,1000,690);
        Header(p,card,"公路突发事件","请先处理事件，再继续巡航。",false);
        p.titleText=Text(card,"EventTitleText","横穿公路的幽灵鹿",40,183,920,70,36);
        p.descriptionText=Text(card,"EventDescriptionText","鹿影停在车灯中央。选择期间不消耗时间。",40,280,920,145,27,Muted);
        p.safeButton=Button(card,"SafeChoiceButton","谨慎处理 · 花费时间",40,466,920,76);
        p.riskButton=Button(card,"RiskChoiceButton","冒险通过 · 损耗耐久",40,566,920,76,false);Save(p);
    }
    private static void BuildResult()
    {
        RectTransform card;var p=Panel<CourierResultPanel>("CourierResultPanel",true,out card,1000,730);
        Header(p,card,"夜班结算","每一件快递，都有它的归处。",false);
        p.titleText=Text(card,"ResultTitleText","夜班完成！",40,182,920,65,38,Mint);
        p.summaryText=Text(card,"ResultSummaryText","成功送达 4 / 4 件\n本班收入 ¥185\n已用时间 140分钟",40,278,920,280,28);
        p.restartButton=Button(card,"RestartButton","再跑一班",40,610,440,72);
        p.menuButton=Button(card,"MainMenuButton","返回主菜单",520,610,440,72,false);Save(p);
    }
}
