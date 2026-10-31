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
    private static Color Ink=>CourierColors.Night;
    private static Color Card=>CourierColors.Panel;
    private static Color Soft=>CourierColors.Raised;
    private static Color White=>CourierColors.Paper;
    private static Color Muted=>CourierColors.Muted;
    private static Color Mint=>CourierColors.Mint;
    private static Color Orange=>CourierColors.Gold;
    private static Font font;
    [MenuItem("Tools/Night Courier/Rebuild UI Prefabs (overwrite layouts)")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder); Directory.CreateDirectory("Assets/Prefab/UI"); AssetDatabase.Refresh();
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/NightVoyage/Fonts/Itim-Regular.ttf");
        if(font==null)throw new System.InvalidOperationException("Project font is missing: Itim");
        ExportContentTables(); BuildRow(); BuildTutorial(); BuildHud(); BuildRoute(); BuildEvent(); BuildResult();
        var registry = AssetDatabase.LoadAssetAtPath<UIDatas>("Assets/Resources/Data/UIDataListSO.asset");
        registry.uiDataList.RemoveAll(x=>x.uiName=="CourierCargoPanel"||x.uiName=="CourierVehiclePanel");
        foreach (var name in new[]{"CourierHudPanel","CourierRoutePanel","CourierEventPanel","CourierResultPanel"})
        {
            var entry = registry.uiDataList.FirstOrDefault(x=>x.uiName==name);
            if(entry==null) { entry=new UIData{uiName=name}; registry.uiDataList.Add(entry); }
            entry.uiPath="Panel/Courier/"+name;
        }
        EditorUtility.SetDirty(registry); AssetDatabase.SaveAssets();
        CourierFeedbackPrefabBuilder.Install();
        InstallTravelControls();
        InstallMapOnly();
        CourierCharacterInstaller.Install();
        CourierPresentationInstaller.Install();
        CourierMapPageInstaller.Install();
        CourierHudArtInstaller.Install();
        CourierPaletteInstaller.Install();
        CourierHudBackgroundInstaller.Install();
        Debug.Log("COURIER_PREFABS PASS: cinematic HUD, unified map/trunk, event, endings and cargo card saved.");
    }
    [MenuItem("Tools/Night Courier/Add Travel Controls (preserve layouts)")]
    public static void InstallTravelControls()
    {
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/NightVoyage/Fonts/Itim-Regular.ttf");
        foreach(var name in new[]{"CourierHudPanel","CourierRoutePanel","CourierEventPanel"})
        {
            string path=Folder+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var panel=root.GetComponent<CourierPanel>();
                if(panel.pauseButton==null)
                {
                    panel.pauseButton=IconButton(root.transform,"TravelPauseButton",CourierIconKind.Pause,"Pause game",0,0,64);
                    var rect=(RectTransform)panel.pauseButton.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-24,-24);
                }
                panel.pauseButton.GetComponent<CourierIconHint>().message="Pause game";
                var hud=panel as CourierHudPanel;
                if(hud!=null&&hud.skipRideButton==null)
                {
                    hud.skipRideButton=Button(root.transform.Find("HudLayout"),"SkipRideButton","Skip ride  [F]",1496,873,264,64,false);
                    hud.skipRideButton.GetComponentInChildren<Text>().fontSize=28;
                    hud.skipRideButton.gameObject.AddComponent<CourierIconHint>().message="Skip to the next event or stop. Time, wear and cargo effects still apply.";
                }
                foreach(var button in hud!=null?new[]{panel.pauseButton,hud.skipRideButton}:new[]{panel.pauseButton})
                {
                    var feedback=button.GetComponent<CourierButtonFeedback>()??button.gameObject.AddComponent<CourierButtonFeedback>();
                    feedback.effectPrefab=AssetDatabase.LoadAssetAtPath<CourierInteractionEffect>("Assets/Resources/Feedback/CourierInteractionEffect.prefab");
                    var old=button.GetComponent<CourierFocusRing>();if(old!=null){if(old.ring!=null)old.ring.enabled=false;Object.DestroyImmediate(old);}
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Debug.Log("COURIER_TRAVEL_CONTROLS PASS: pause and skip installed, existing layouts preserved.");
    }
    public static void InstallTravelAndRun(){InstallTravelControls();NightCourierTravelSmokeTest.Run();}
    [MenuItem("Tools/Night Courier/Use Map-only Presentation (preserve layout)")]
    public static void InstallMapOnly()
    {
        string hudPath=Folder+"/CourierHudPanel.prefab";var root=PrefabUtility.LoadPrefabContents(hudPath);
        try
        {
            var hud=root.GetComponent<CourierHudPanel>();var background=root.GetComponent<Image>();background.color=Color.clear;background.raycastTarget=false;
            if(hud.stageGraphic!=null){Object.DestroyImmediate(hud.stageGraphic.gameObject);hud.stageGraphic=null;}
            var road=(RectTransform)hud.roadStatus.transform;road.anchoredPosition=new Vector2(0,-5);road.sizeDelta=new Vector2(220,65);
            var clock=(RectTransform)road.Find("ClockIcon");clock.anchoredPosition=Vector2.zero;
            hud.timeText.rectTransform.anchoredPosition=new Vector2(50,0);hud.timeText.rectTransform.sizeDelta=new Vector2(170,38);
            var track=(RectTransform)road.Find("ProgressFillBackground");track.sizeDelta=new Vector2(220,5);hud.progressFill.rectTransform.sizeDelta=new Vector2(220,5);
            PrefabUtility.SaveAsPrefabAsset(root,hudPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        string routePath=Folder+"/CourierRoutePanel.prefab";root=PrefabUtility.LoadPrefabContents(routePath);
        try
        {
            var panel=root.GetComponent<CourierRoutePanel>();root.GetComponent<Image>().color=Ink;
            if(panel.closeButton!=null){Object.DestroyImmediate(panel.closeButton.gameObject);panel.closeButton=null;}
            PrefabUtility.SaveAsPrefabAsset(root,routePath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("COURIER_MAP_ONLY PASS: permanent map, no street stage, dialogue and travel HUD retained.");
    }
    public static void InstallMapOnlyAndRun(){InstallMapOnly();NightCourierPanelSmokeTest.Run();}
    private static string Csv(params object[] cells)
    {return string.Join(",",cells.Select(c=>"\""+System.Convert.ToString(c,System.Globalization.CultureInfo.InvariantCulture).Replace("\"","\"\"")+"\""));}
    private static void ExportContentTables()
    {
        const string dir="Assets/Resources/NightVoyage";Directory.CreateDirectory(dir);var run=new CourierRun();
        var vehicles=new System.Collections.Generic.List<string>{"id,name,ability,capacity,durability,speed,wear"};
        for(int i=0;i<run.Vehicles.Length;i++){var v=run.Vehicles[i];vehicles.Add(Csv(i,v.Name,v.Ability,v.Capacity,v.MaxDurability,v.Speed,v.Wear));}
        var parcels=new System.Collections.Generic.List<string>{"id,name,weight,reward,fragile,heavy,dry,target_turns,origin,destination"};
        for(int i=0;i<run.Parcels.Count;i++){var p=run.Parcels[i];parcels.Add(Csv(i,p.Name,p.Weight,p.Reward,p.Fragile?1:0,p.Heavy?1:0,p.Dry?1:0,p.TimeLimit,p.Origin,p.Destination));}
        var events=new System.Collections.Generic.List<string>{"id,title,description,safe_action,risk_action,safe_turns,risk_turns,fragile_damage,water_damage,effect"};
        for(int i=0;i<run.Events.Length;i++){var e=run.Events[i];events.Add(Csv(i,e.Title,e.Description,e.Safe.Split('·')[0].Trim(),e.Risky.Split('·')[0].Trim(),e.SafeMinutes,e.RiskDamage,e.BreakFragile?1:0,e.WetDry?1:0,e.Effect));}
        var names=new[]{"Vehicles","Parcels","Events"};var tables=new[]{vehicles,parcels,events};
        for(int i=0;i<names.Length;i++){var path=dir+"/"+names[i]+".csv";if(!File.Exists(path))File.WriteAllText(path,string.Join("\n",tables[i])+"\n",new System.Text.UTF8Encoding(true));}
        AssetDatabase.Refresh();
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
        var t=Element(name,parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=CourierTypography.For(t);t.text=content;t.fontSize=size;t.color=color??White;
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
        serialized.FindProperty("fadeInDuration").floatValue=modal?BasePanel.EnterMotionDuration:0;
        serialized.FindProperty("fadeOutDuration").floatValue=BasePanel.ExitMotionDuration;
        serialized.FindProperty("scaleAnimation").boolValue=false;
        serialized.ApplyModifiedPropertiesWithoutUndo();return panel;
    }
    private static CourierIconGraphic Icon(Transform parent,string name,CourierIconKind kind,float x,float y,float size,Color color,string hint="")
    {
        var rt=Element(name,parent,x,y,size,size);var icon=rt.gameObject.AddComponent<CourierIconGraphic>();icon.kind=kind;icon.color=color;icon.raycastTarget=hint.Length>0;
        if(hint.Length>0)rt.gameObject.AddComponent<CourierIconHint>().message=hint;return icon;
    }
    private static Button IconButton(Transform parent,string name,CourierIconKind kind,string hint,float x,float y,float size,bool primary=false)
    {
        var b=Button(parent,name,"",x,y,size,size,primary);Object.DestroyImmediate(b.transform.Find("Label").gameObject);
        Icon(b.transform,"Icon",kind,(size-40)/2,(size-40)/2,40,primary?Ink:White);
        b.gameObject.AddComponent<CourierIconHint>().message=hint;return b;
    }
    private static Button IconLabelButton(Transform parent,string name,string text,CourierIconKind kind,string hint,float x,float y,float w,float h)
    {
        var b=Button(parent,name,text,x,y,w,h,false);var label=b.GetComponentInChildren<Text>();label.fontSize=22;label.rectTransform.anchoredPosition=new Vector2(60,0);label.rectTransform.sizeDelta=new Vector2(w-72,h);
        Icon(b.transform,"Icon",kind,12,(h-36)/2,36,White);b.gameObject.AddComponent<CourierIconHint>().message=hint;return b;
    }
    private static void Tooltip(CourierPanel panel)
    {
        var t=panel.gameObject.AddComponent<CourierTooltip>();var bubble=Element("IconTooltip",panel.transform,0,0,300,60);bubble.anchorMin=bubble.anchorMax=new Vector2(.5f,.5f);
        Background(bubble,new Color(.025f,.04f,.055f,.98f));var group=bubble.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;
        t.bubble=bubble;t.label=Text(bubble,"Hint","",14,12,272,36,21,White);bubble.gameObject.SetActive(false);
    }
    private static void Save(CourierPanel panel)
    {
        Tooltip(panel);PrefabUtility.SaveAsPrefabAsset(panel.gameObject,Folder+"/"+panel.GetType().Name+".prefab");Object.DestroyImmediate(panel.gameObject);}
    private static void BuildTutorial()
    {
        var root=Element("CourierTutorialPanel",null,0,0,1920,1080);Stretch(root);
        var view=root.gameObject.AddComponent<CourierTutorialView>();
        var panel=root.gameObject.AddComponent<CourierTutorialPanel>();panel.view=view;
        var settings=new SerializedObject(panel);settings.FindProperty("backgroundFadeDuration").floatValue=0;settings.FindProperty("fadeInDuration").floatValue=BasePanel.EnterMotionDuration;settings.FindProperty("fadeOutDuration").floatValue=BasePanel.ExitMotionDuration;settings.FindProperty("scaleAnimation").boolValue=false;settings.ApplyModifiedPropertiesWithoutUndo();
        view.focusFrames=new RectTransform[2];
        for(int i=0;i<2;i++)
        {
            var frame=Element("Focus_"+i,root,0,0,100,100);frame.anchorMin=frame.anchorMax=new Vector2(.5f,.5f);view.focusFrames[i]=frame;
            for(int edge=0;edge<4;edge++)
            {
                var line=Element("Edge_"+edge,frame,0,0,3,3);Background(line,Orange);
                line.anchorMin=edge==0?new Vector2(0,1):edge==3?new Vector2(1,0):Vector2.zero;
                line.anchorMax=edge==1?new Vector2(1,0):edge==2?new Vector2(0,1):Vector2.one;
                line.pivot=edge==0?new Vector2(.5f,1):edge==1?new Vector2(.5f,0):edge==2?new Vector2(0,.5f):new Vector2(1,.5f);
                line.sizeDelta=edge<2?new Vector2(0,3):new Vector2(3,0);line.anchoredPosition=Vector2.zero;
            }
            frame.gameObject.SetActive(false);
        }
        var strip=Element("GuideStrip",root,0,64,1300,128);strip.anchorMin=strip.anchorMax=new Vector2(.5f,1);strip.pivot=new Vector2(.5f,1);strip.anchoredPosition=new Vector2(0,-64);Background(strip,Card);
        view.strip=strip;view.heading=Text(strip,"StepTitle","FIRST SHIFT  1/6",24,8,1140,30,22,Mint);
        view.instruction=Text(strip,"Instruction","Press E to continue your first conversation.",24,40,1140,80,26);view.instruction.alignment=TextAnchor.UpperLeft;
        view.skipButton=Button(strip,"SkipGuideButton","Skip",1200,40,80,48,false);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,Folder+"/CourierTutorialPanel.prefab");Object.DestroyImmediate(root.gameObject);
    }
    private static Image Fill(Transform parent,string name,float x,float y,float w,float h,Color color)
    {
        Background(Element(name+"Background",parent,x,y,w,h),Soft);var image=Background(Element(name,parent,x,y,w,h),color);
        image.sprite=Resources.Load<Sprite>("NightVoyage/HUD/ProgressFill");image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.fillOrigin=0;image.fillAmount=1;return image;
    }
    private static void BuildHud()
    {
        RectTransform card;var p=Panel<CourierHudPanel>("CourierHudPanel",false,out card,1800,1000);
        var stage=Element("Stage",card,0,40,1800,800);p.stageGraphic=stage.gameObject.AddComponent<CourierStageGraphic>();p.stageGraphic.raycastTarget=true;
        var road=Element("RoadStatus",card,600,5,600,65);p.roadStatus=road.gameObject;
        Icon(road,"ClockIcon",CourierIconKind.Clock,205,0,36,Orange,"Time until dawn");
        p.timeText=Text(road,"TimeText","240 min",255,0,180,38,26,Orange);
        p.progressFill=Fill(road,"ProgressFill",0,51,600,5,Mint);
        Background(Element("SubtitleBar",card,0,810,1800,190),new Color(0,0,0,.88f));p.subtitleText=Text(card,"SubtitleText","Zero Route",40,827,1600,153,30);
        p.advanceButton=IconButton(card,"AdvanceButton",CourierIconKind.Arrow,"Continue dialogue - E",1696,873,64);
        Save(p);
    }
    private static void BuildRow()
    {
        var rt=Element("CourierCargoRow",null,0,0,502,132);Background(rt,Soft,true);
        var row=rt.gameObject.AddComponent<CourierCargoRow>();var layout=rt.gameObject.AddComponent<LayoutElement>();layout.preferredHeight=132;layout.minHeight=132;
        row.nameText=Text(rt,"ParcelName","Party Theme Tape",12,0,420,44,32);
        Icon(rt,"RecipientIcon",CourierIconKind.Pin,12,50,28,Muted,"Delivery destination");row.destinationText=Text(rt,"Destination","Abandoned TV Tower",52,44,270,40,28,Muted);
        Icon(rt,"WeightIcon",CourierIconKind.Box,348,50,28,Muted,"Cargo weight in slots");row.weightText=Text(rt,"WeightText","1",384,44,40,40,28,Muted);
        var tags=Element("Tags",rt,12,92,410,32);var group=tags.gameObject.AddComponent<HorizontalLayoutGroup>();group.spacing=12;group.childControlWidth=group.childControlHeight=false;group.childForceExpandWidth=group.childForceExpandHeight=false;
        row.storyIcon=Icon(tags,"StoryIcon",CourierIconKind.Story,0,0,32,Mint,"Story parcel - cannot be left behind");
        row.fragileIcon=Icon(tags,"FragileIcon",CourierIconKind.Fragile,0,0,32,Orange,"Fragile - damaged by rough roads or risky choices");
        row.heavyIcon=Icon(tags,"HeavyIcon",CourierIconKind.Heavy,0,0,32,Orange,"All batches can be carried by a vehicle or ghost");
        row.dryIcon=Icon(tags,"DryIcon",CourierIconKind.Dry,0,0,32,Orange,"Keep dry - protect it during river crossings");
        row.damagedIcon=Icon(tags,"DamagedIcon",CourierIconKind.Warning,0,0,32,Orange,"Cargo conditions violated - still deliverable with a lower evaluation");
        var timed=Element("Timed",tags,0,0,112,32);row.timeGroup=timed.gameObject;
        var clock=Icon(timed,"ClockIcon",CourierIconKind.Clock,0,0,32,Orange,"Timed delivery");row.timeHint=clock.GetComponent<CourierIconHint>();row.timeText=Text(timed,"TimeText","60",44,-3,62,38,28,Orange);
        row.actionButton=IconButton(rt,"LoadButton",CourierIconKind.Load,"Load into trunk",438,28,56,true);var buttonRect=(RectTransform)row.actionButton.transform;buttonRect.anchorMin=buttonRect.anchorMax=new Vector2(1,1);buttonRect.anchoredPosition=new Vector2(-64,-28);
        row.actionIcon=row.actionButton.GetComponentInChildren<CourierIconGraphic>();row.actionHint=row.actionButton.GetComponent<CourierIconHint>();
        row.PrepareArtwork();
        PrefabUtility.SaveAsPrefabAsset(rt.gameObject,"Assets/Prefab/UI/CourierCargoRow.prefab");Object.DestroyImmediate(rt.gameObject);
    }
    private static Transform Scroll(Transform parent,string name,float x,float y,float w,float h)
    {
        var root=Element(name,parent,x,y,w,h);var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
        var viewport=Element("Viewport",root,0,0,w-18,h);Stretch(viewport);viewport.offsetMax=new Vector2(-18,0);Background(viewport,Color.clear,true);viewport.gameObject.AddComponent<RectMask2D>();
        var content=Element("Content",viewport,0,0,w-18,0);content.anchorMax=new Vector2(1,1);content.sizeDelta=Vector2.zero;
        var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=8;layout.padding=new RectOffset(0,0,0,4);layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var bar=Element("Scrollbar",root,0,0,5,h);bar.anchorMin=new Vector2(1,0);bar.anchorMax=new Vector2(1,1);bar.offsetMin=new Vector2(-5,0);bar.offsetMax=Vector2.zero;Background(bar,Soft,true);
        var handle=Element("Handle",bar,0,0,5,h);Stretch(handle);var image=Background(handle,Muted,true);var scrollbar=bar.gameObject.AddComponent<Scrollbar>();scrollbar.targetGraphic=image;scrollbar.handleRect=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;
        scroll.viewport=viewport;scroll.content=content;scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;return content;
    }
    private static void BuildRoute()
    {
        RectTransform card;var p=Panel<CourierRoutePanel>("CourierRoutePanel",true,out card,1300,660);
        p.closeButton=IconButton(card,"CloseButton",CourierIconKind.Close,"Close - Esc",1220,16,56);
        p.objectiveText=Text(card,"ObjectiveText","Deliver the party theme tape.",24,14,690,48,22,Mint);
        var map=Element("RoadMap",card,24,70,690,400);var edges=Element("RoadConnections",map,0,0,690,400);p.mapGraphic=edges.gameObject.AddComponent<CourierRouteGraphic>();p.mapGraphic.raycastTarget=false;
        var run=new CourierRun();int count=6;p.stopButtons=new Button[count];p.stopLabels=new Text[count];p.stopCounts=new Text[count];p.stopIcons=new CourierIconGraphic[count];p.stopHints=new CourierIconHint[count];
        for(int i=0;i<count;i++)
        {
            var point=CourierRouteGraphic.NodePoint(run.Stops[i]);var b=Button(map,"Stop_"+i,run.Stops[i].Name,point.x-90,point.y-36,180,164,false);
            var colors=b.colors;colors.normalColor=new Color(1,1,1,0);colors.highlightedColor=Color.white;colors.selectedColor=new Color(1,1,1,.3f);colors.disabledColor=new Color(1,1,1,0);b.colors=colors;
            var label=b.GetComponentInChildren<Text>();label.fontSize=28;label.alignment=TextAnchor.UpperCenter;label.raycastTarget=true;
            // Put names outside the road network, with room for the longer English names.
            Rect nameBounds;
            switch(i)
            {
                case 0:nameBounds=new Rect(0,point.y+40,124,96);break;
                case 1:nameBounds=new Rect(point.x-100,point.y-66,200,48);break;
                case 2:nameBounds=new Rect(point.x-80,point.y+26,160,48);break;
                case 3:nameBounds=new Rect(point.x-70,point.y+26,180,96);break;
                case 4:nameBounds=new Rect(point.x-90,point.y-110,180,96);break;
                default:nameBounds=new Rect(point.x-67,point.y+26,160,96);break;
            }
            label.rectTransform.anchoredPosition=new Vector2(nameBounds.x-(point.x-90),-(nameBounds.y-(point.y-36)));
            label.rectTransform.sizeDelta=nameBounds.size;
            p.stopButtons[i]=b;p.stopLabels[i]=label;p.stopIcons[i]=Icon(b.transform,"Marker",CourierIconKind.Dot,72,18,36,Muted);
            p.stopCounts[i]=Text(b.transform,"ParcelCount","",117,18,35,36,28,Mint);p.stopHints[i]=b.gameObject.AddComponent<CourierIconHint>();p.stopHints[i].message=run.Stops[i].Name;
        }
        p.PrepareMapArtwork();p.mapArtwork.Apply(p);
        p.destinationText=Text(card,"DestinationText","Choose a destination",24,496,590,40,28);
        var costs=Element("SelectedCosts",card,24,540,580,42);p.costGroup=costs.gameObject;
        Icon(costs,"ClockIcon",CourierIconKind.Clock,0,3,32,Muted,"Estimated travel turns");p.timeCostText=Text(costs,"TimeCostText","",44,0,130,40,24);
        Icon(costs,"WearIcon",CourierIconKind.Wrench,180,3,32,Muted,"Estimated vehicle turns consumed");p.wearCostText=Text(costs,"WearCostText","",224,0,120,40,24);
        p.riskText=Text(card,"RoadRiskText","",24,592,590,44,20,Orange);
        p.departButton=IconButton(card,"DepartButton",CourierIconKind.Arrow,"Depart",642,540,64,true);p.departHint=p.departButton.GetComponent<CourierIconHint>();
        p.vehicleText=Text(card,"VehicleText","Scooter",756,14,440,48,28);
        Icon(card,"DurabilityIcon",CourierIconKind.Wrench,756,72,36,Muted,"Remaining vehicle turns");p.durabilityFill=Fill(card,"DurabilityFill",804,88,120,8,Mint);p.durabilityText=Text(card,"DurabilityText","50/50",936,72,120,36,24);
        Icon(card,"LoadIcon",CourierIconKind.Box,1056,72,36,Muted,"One active order batch at a time");p.loadText=Text(card,"LoadText","1/3",1104,72,172,36,24);
        p.possessButton=IconLabelButton(card,"PossessButton","Bicycle",CourierIconKind.Ghost,"Possess ",756,120,250,52);p.possessIcon=p.possessButton.GetComponentInChildren<CourierIconGraphic>();p.possessHint=p.possessButton.GetComponent<CourierIconHint>();
        p.PrepareVehicleArtwork();
        p.repairButton=IconLabelButton(card,"RepairButton","Free",CourierIconKind.Wrench,"Restore vehicle turns for free",1056,120,220,52);
        var waiting=Element("WaitingSection",card,756,182,520,180);p.waitingSection=waiting;Text(waiting,"WaitingHeading","Pick Up",0,0,500,30,20,Muted);p.waitingContent=Scroll(waiting,"WaitingParcels",0,38,520,142);
        var loaded=Element("LoadedSection",card,756,342,520,250);p.loadedSection=loaded;Text(loaded,"TrunkHeading","On Board",0,0,500,30,20,Mint);
        p.loadedContent=Scroll(loaded,"LoadedParcels",0,38,520,220);p.loadedScroll=(RectTransform)p.loadedContent.parent.parent;p.dropZone=p.loadedScroll.gameObject.AddComponent<CourierTrunkDropZone>();
        p.rowPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/CourierCargoRow.prefab").GetComponent<CourierCargoRow>();Save(p);
    }
    private static void BuildEvent()
    {
        RectTransform card;var p=Panel<CourierEventPanel>("CourierEventPanel",true,out card,900,380);
        p.titleText=Text(card,"EventTitleText","Road Event",32,16,836,54,32);p.descriptionText=Text(card,"EventDescriptionText","",32,78,836,82,24,Muted);
        p.safeButton=Button(card,"SafeChoiceButton","Take care",32,196,836,68);p.riskButton=Button(card,"RiskChoiceButton","Take a risk",32,284,836,68,false);
        foreach(var button in new[]{p.safeButton,p.riskButton}){var label=button.GetComponentInChildren<Text>();label.alignment=TextAnchor.MiddleLeft;label.rectTransform.anchoredPosition=new Vector2(20,0);label.rectTransform.sizeDelta=new Vector2(510,68);}
        Icon(p.safeButton.transform,"ClockIcon",CourierIconKind.Clock,660,16,36,Ink);p.safeCostText=Text(p.safeButton.transform,"TimeCostText","",706,0,108,68,24,Ink);
        Icon(p.riskButton.transform,"WearIcon",CourierIconKind.Wrench,562,16,36,White);p.riskCostText=Text(p.riskButton.transform,"WearCostText","",608,0,80,68,24);
        p.fragileRisk=Icon(p.riskButton.transform,"FragileRiskIcon",CourierIconKind.Fragile,716,16,36,Orange,"Damages fragile cargo");p.dryRisk=Icon(p.riskButton.transform,"DryRiskIcon",CourierIconKind.Dry,768,16,36,Orange,"Soaks cargo that must stay dry");Save(p);
    }
    private static void BuildResult()
    {
        RectTransform card;var p=Panel<CourierResultPanel>("CourierResultPanel",true,out card,1100,720);
        p.titleText=Text(card,"ResultTitleText","Shift Complete",32,16,1036,58,34,Mint);p.summaryText=Text(card,"ResultSummaryText","",32,88,1036,510,24);
        p.restartButton=IconButton(card,"RestartButton",CourierIconKind.Restart,"Another Shift",912,624,64,true);p.menuButton=IconButton(card,"MainMenuButton",CourierIconKind.Home,"Main Menu",1004,624,64);Save(p);
    }
}
