using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierFullFlowRecording
{
    private const string Key="CourierFullFlow.Active";
    private static string output,chapter="Main menu";
    private static CourierFullFlowRecorder recorder;
    private static Camera camera;
    private static RenderTexture target;
    private static Texture2D pixels;
    private static StreamWriter timestamps;
    private static double start,nextCapture;
    private static int frames,assertions;
    private static bool completed,pausingTested;
    private static readonly List<string> results=new List<string>(),errors=new List<string>();
    private static NightCourierPrototype Owner=>NightCourierPrototype.Current;
    private static CourierRoutePanel Page=>UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
    private static double Now=>Time.realtimeSinceStartupAsDouble-start;
    static CourierFullFlowRecording(){EditorApplication.update+=Attach;}
    [MenuItem("Tools/Night Courier/Checks/Record full systems flow")]
    public static void Run()
    {
        output=Environment.GetEnvironmentVariable("SCM_FLOW_OUTPUT")??Path.GetFullPath("ArtPreviews/FullFlow");
        Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));
        SessionState.SetString(Key+".Output",output);SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    private static void Attach()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||recorder!=null)return;
        if(UnityEngine.Object.FindObjectOfType<TitleUIController>()==null)return;
        output=SessionState.GetString(Key+".Output","");
        var go=new GameObject("FullFlowRecordingProbe");UnityEngine.Object.DontDestroyOnLoad(go);
        recorder=go.AddComponent<CourierFullFlowRecorder>();go.AddComponent<AudioListener>();
        camera=go.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.085f);
        target=new RenderTexture(1280,720,24);camera.targetTexture=target;
        pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        timestamps=new StreamWriter(Path.Combine(output,"frames.tsv"));
        start=Time.realtimeSinceStartupAsDouble;nextCapture=0;frames=assertions=0;completed=false;pausingTested=false;
        results.Clear();errors.Clear();Application.logMessageReceived+=Log;
        recorder.BeginAudio(Path.Combine(output,"game-audio.pcm"));recorder.CaptureFrame=Capture;
        recorder.StartCoroutine(Guard(Flow()));
    }
    private static void Log(string message,string trace,LogType type)
    {
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)
        {errors.Add(message+"\n"+trace);File.AppendAllText(Path.Combine(output,"runtime-errors.txt"),message+"\n"+trace+"\n");}
    }
    private static void Capture()
    {
        if(completed||Now<nextCapture)return;
        try
        {
            nextCapture=Now+.1;
            foreach(var listener in UnityEngine.Object.FindObjectsOfType<AudioListener>())listener.enabled=listener.gameObject==recorder.gameObject;
            foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
                if(canvas.isRootCanvas){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;}
            Canvas.ForceUpdateCanvases();
            var page=UIManager.Instance!=null?Page:null;
            if(Owner!=null&&Owner.HasPlanner&&page!=null&&page.mapArtwork!=null)
            {
                page.ApplyFullscreenLayout();
                foreach(var glow in page.mapArtwork.GetComponentsInChildren<CourierLandmarkGlow>())glow.SendMessage("LateUpdate");
            }
            Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();RenderTexture.active=previous;
            string file=frames.ToString("D6")+".jpg";File.WriteAllBytes(Path.Combine(output,"frames",file),pixels.EncodeToJPG(82));
            timestamps.WriteLine(file+"\t"+Now.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));timestamps.Flush();frames++;
        }
        catch(Exception error){Finish(false,error.ToString());}
    }
    private static IEnumerator Guard(IEnumerator flow)
    {
        var stack=new Stack<IEnumerator>();stack.Push(flow);
        while(stack.Count>0&&!completed)
        {
            object value=null;bool more=false;
            try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}
            catch(Exception error){Finish(false,error.ToString());yield break;}
            if(!more){stack.Pop();continue;}
            if(value is IEnumerator nested){stack.Push(nested);continue;}
            yield return value;
        }
        if(!completed)Finish(true,"Complete");
    }
    private static void Check(bool ok,string message)
    {assertions++;if(!ok)throw new Exception(chapter+": "+message);}
    private static void Chapter(string value)
    {
        chapter=value;string line=Now.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"\t"+value;
        results.Add(line);File.AppendAllText(Path.Combine(output,"chapters.tsv"),line+"\n");Debug.Log("FULL_FLOW "+line);
    }
    private static object Wait(float seconds)=>new WaitForSecondsRealtime(seconds);
    private static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(obj);
    private static PointerEventData Pointer(RectTransform rect)
    {
        Canvas.ForceUpdateCanvases();var canvas=rect.GetComponentInParent<Canvas>().rootCanvas;
        return new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
    }
    private static void Click(Button button)
    {
        Check(button!=null&&button.IsActive()&&button.IsInteractable(),"Inactive button "+(button!=null?button.name:"null"));
        var data=Pointer((RectTransform)button.transform);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Blocked button "+button.name+"; hits "+string.Join(",",hits.Take(4).Select(h=>h.gameObject.name)));
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    private static void Hover(CourierIconHint hint)
    {hint.OnPointerEnter(Pointer((RectTransform)hint.transform));}
    private static IEnumerator Dialogue(bool readable=false)
    {
        int guard=0;
        while(Owner!=null&&Owner.ConversationActive)
        {
            Check(guard++<180,"Dialogue loop");
            var hud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
            hud.CompleteSubtitle();yield return Wait(readable?.55f:.08f);Owner.AdvanceDialogue();
        }
        yield return Wait(.12f);
    }
    private static void SelectNode(int id)
    {
        var root=id<6?Page.stopButtons[id].transform:Page.mapArtwork.NodeTarget(id);
        var hit=root.Find("NodeHitArea") as RectTransform;
        var data=Pointer(hit);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==root.GetComponent<Button>(),"Node hit blocked: "+id);
        ExecuteEvents.Execute(root.gameObject,data,ExecuteEvents.pointerClickHandler);
        Check(Page.SelectedMapNode==id,"Map selection did not update");
    }
    private static IEnumerator Travel(int destination,bool allowPause=true)
    {
        int guard=0;
        while(Owner.Run.Node!=destination&&!Owner.Run.Finished)
        {
            Check(guard++<150,"Journey loop to "+destination);yield return Dialogue();
            // Continue the existing plan at waypoints; its preview can cover the destination hit area.
            if(Page.SelectedMapNode!=destination)SelectNode(destination);
            yield return Wait(.14f);Click(Page.departButton);yield return Wait(.4f);
            if(allowPause&&!pausingTested&&Owner.Run.State==ShiftState.Driving)
            {
                pausingTested=true;Chapter("NORMAL PLAY / pause, settings and resume during travel");
                GameManager.Instance.PauseGame();yield return Wait(.5f);
                float before=Owner.Run.DisplayProgress;int turns=Owner.Run.OrderTurns;
                var pause=UIManager.Instance.GetPanel("PausePanel") as PausePanel;
                Click(Field<Button>(pause,"settingsButton"));yield return Wait(.5f);
                Check(UIManager.Instance.IsPanelOpen("SettingPanel"),"Pause settings did not open");
                yield return Wait(.6f);Check(before==Owner.Run.DisplayProgress&&turns==Owner.Run.OrderTurns,"Paused simulation advanced");
                Click(Field<Button>(UIManager.Instance.GetPanel("SettingPanel"),"backButton"));yield return Wait(.3f);
                Click(Field<Button>(pause,"resumeButton"));yield return Wait(.3f);Check(Owner.Active,"Resume failed");
            }
            if(Owner.Run.State==ShiftState.Driving)Check(Owner.SkipRide(),"Fast-forward failed");
            yield return Wait(.16f);
            if(Owner.Run.State==ShiftState.Encounter)
            {
                var eventPanel=UIManager.Instance.GetPanel("CourierEventPanel") as CourierEventPanel;
                Check(eventPanel!=null&&(eventPanel.illustration==null||!eventPanel.illustration.gameObject.activeInHierarchy),"Event image should be absent");
                File.AppendAllText(Path.Combine(output,"natural-events.txt"),Owner.Run.EncounterTitle+"\n");
                yield return Wait(1.2f);Click(eventPanel.safeButton);yield return Wait(.2f);
            }
            Check(Owner.Run.State==ShiftState.Docked||Owner.Run.Finished,"Journey did not reach a stop");
        }
        Check(Owner.Run.Node==destination,"Wrong destination");yield return Dialogue();
    }
    private static IEnumerator Exchange(int station)
    {
        Chapter("NORMAL PLAY / exchange vehicle "+(station==6?"H":station==7?"W":"Q"));
        yield return Travel(station);yield return Wait(.2f);
        Click(Page.possessButton);yield return Wait(.5f);
        Check(Owner.Run.VehicleIndex==station-6&&Owner.Run.Durability==5,"Vehicle exchange failed");
    }
    private static IEnumerator Flow()
    {
        Chapter("RULE CHECKS / graph, orders, route forecasts, events, environment, tutorial and completion");
        File.WriteAllText(Path.Combine(output,"rule-checks.txt"),CourierChecks.RunAll());
        Chapter("NORMAL PLAY / main menu and settings");yield return Wait(2);
        var title=UnityEngine.Object.FindObjectOfType<TitleUIController>();Check(title!=null,"Menu missing");
        Click(title.settingsButton);yield return Wait(.6f);
        var settings=UIManager.Instance.GetPanel("SettingPanel");
        var bgm=Field<Slider>(settings,"bgmVolumeSlider");var sfx=Field<Slider>(settings,"sfxVolumeSlider");
        float oldBgm=bgm.value,oldSfx=sfx.value;bgm.value=.42f;sfx.value=.63f;yield return Wait(.8f);
        Check(Mathf.Abs(AudioManager.Instance.bgmVolumeFactor-.42f)<.01f&&Mathf.Abs(AudioManager.Instance.sfxVolumeFactor-.63f)<.01f,"Audio sliders not wired");
        bgm.value=oldBgm;sfx.value=oldSfx;Click(Field<Button>(settings,"backButton"));yield return Wait(.5f);
        Click(title.creditsButton);yield return Wait(1.8f);Check(UIManager.Instance.IsPanelOpen("CreditPanel"),"Credits did not open");
        Click(Field<Button>(UIManager.Instance.GetPanel("CreditPanel"),"backButton"));yield return Wait(.4f);
        Click(title.newGameButton);
        for(int i=0;Owner==null||!Owner.HasPlanner;i++){Check(i<200,"Gameplay loading timed out");yield return Wait(.1f);}
        yield return Wait(1);Chapter("NORMAL PLAY / depot, tutorial and order information");
        Check(Owner.Run.Node==CourierStreetMap.Node("S").StopId&&!Owner.ConversationActive,"Depot opening invalid");
        Check(Owner.Run.BatchCount==11,"Configured order count invalid");
        var tutorial=UIManager.Instance.GetPanel("CourierTutorialPanel") as CourierTutorialPanel;
        Check(tutorial!=null,"Tutorial missing");
        var tutorialHint=tutorial.GetComponentsInChildren<CourierIconHint>().First(h=>!string.IsNullOrEmpty(h.message));
        Hover(tutorialHint);yield return Wait(1);tutorialHint.OnPointerExit(Pointer((RectTransform)tutorialHint.transform));
        var audio=Owner.GetComponent<CourierSoundscape>();
        foreach(string cue in CourierSoundscape.CueNames)Check(audio.HasCue(cue),"Missing cue "+cue);
        var config=JsonUtility.FromJson<CourierAudioConfig>(Resources.Load<TextAsset>("NightVoyage/AudioCues").text);
        foreach(var cue in config.cues)Check(audio.HasCue(cue.cue),"Missing configured cue "+cue.cue);
        foreach(string loop in new[]{"night_waltz","night_wind","vehicle_engine"})
        {var clip=Resources.Load<AudioClip>("Audio/Courier/Loops/"+loop);Check(clip!=null&&clip.LoadAudioData(),"Loop decode failed "+loop);}
        Click(Page.cargoToggleButton);yield return Wait(.6f);Check(Page.CargoOpen,"Orders did not open");
        Check(Page.GetComponentsInChildren<CourierCargoRow>().Count()==11,"Order rows missing");
        var scroll=Page.waitingContent.GetComponentInParent<ScrollRect>();scroll.verticalNormalizedPosition=0;yield return Wait(1);
        scroll.verticalNormalizedPosition=1;yield return Wait(.5f);Click(Page.cargoCloseButton);yield return Wait(.5f);

        Chapter("NORMAL PLAY / map zoom, drag and legend-linked highlights");
        var map=Page.GetComponentInChildren<CourierMapCanvas>();var pointer=Pointer((RectTransform)map.transform);
        pointer.scrollDelta=new Vector2(0,2);map.OnScroll(pointer);yield return Wait(.5f);Check(map.zoom>1.16f,"Map zoom failed");
        map.OnBeginDrag(pointer);pointer.delta=new Vector2(90,20);map.OnDrag(pointer);map.OnEndDrag(pointer);yield return Wait(.5f);
        pointer.scrollDelta=new Vector2(0,-2);map.OnScroll(pointer);map.ApplyLayout();yield return Wait(.4f);
        // Restore the map pose before verifying node hit targets.
        typeof(CourierMapCanvas).GetField("pan",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(map,Vector2.zero);
        typeof(CourierMapCanvas).GetField("velocity",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(map,Vector2.zero);
        map.zoom=1.16f;yield return Wait(.7f);
        var legend=Page.GetComponentInChildren<CourierMapLegend>();legend.OnPointerEnter(Pointer((RectTransform)legend.transform));yield return Wait(.5f);
        Check(legend.IsExpanded,"Legend hover did not open");
        foreach(var entry in legend.GetComponentsInChildren<CourierMapLegendEntry>())
        {
            entry.OnPointerEnter(Pointer((RectTransform)entry.transform));yield return Wait(.25f);
            if(entry.target>=0)Check(legend.HighlightsNode(entry.target),"Legend target missing "+entry.target);
            entry.OnPointerExit(Pointer((RectTransform)entry.transform));
        }
        legend.OnPointerExit(Pointer((RectTransform)legend.transform));yield return Wait(.4f);Check(!legend.IsExpanded,"Legend did not close");
        yield return Exchange(6);yield return Exchange(7);
        int index=0;
        while(!Owner.Run.Finished)
        {
            var run=Owner.Run;var parcel=run.Parcels.First(p=>p.State==ParcelState.Waiting);
            Chapter("NORMAL PLAY / order "+(++index)+" of 11 / "+parcel.Name);
            yield return Travel(parcel.Origin);yield return Dialogue(index==1);
            Click(Page.cargoToggleButton);yield return Wait(.5f);
            var row=Page.GetComponentsInChildren<CourierCargoRow>().First(r=>r.Item==parcel);
            var rowScroll=row.GetComponentInParent<ScrollRect>();
            Canvas.ForceUpdateCanvases();var content=rowScroll.content;
            // Scroll the chosen supplier row into the viewport before clicking or dragging.
            var local=(Vector2)content.InverseTransformPoint(row.transform.position);
            content.anchoredPosition=new Vector2(content.anchoredPosition.x,Mathf.Clamp(-local.y,0,Mathf.Max(0,content.rect.height-rowScroll.viewport.rect.height)));
            yield return Wait(.25f);Hover(row.nameText.GetComponent<CourierIconHint>());yield return Wait(.5f);
            row.nameText.GetComponent<CourierIconHint>().OnPointerExit(Pointer(row.nameText.rectTransform));
            if(index==1)
            {
                var drag=Pointer((RectTransform)row.transform);drag.pointerDrag=row.gameObject;row.OnBeginDrag(drag);yield return Wait(.4f);
                drag.position=Pointer(Page.loadedSection).position;row.OnDrag(drag);yield return Wait(.4f);
                Page.dropZone.OnDrop(drag);row.OnEndDrag(drag);
            }
            else Click(row.actionButton);
            yield return Wait(.4f);Check(parcel.State==ParcelState.Loaded,"Order pickup failed");
            Check(!run.Unload(parcel),"Active order can be cancelled unexpectedly");
            Page.SetCargoOpen(false);yield return Wait(.3f);SelectNode(parcel.Destination);yield return Wait(.35f);
            if(index==1)
            {
                bool safe=run.PreferSafeRoutes;Click(Page.routeModeButton);yield return Wait(.6f);Check(run.PreferSafeRoutes!=safe,"Route mode switch failed");
                Click(Page.routeModeButton);yield return Wait(.4f);
            }
            yield return Travel(parcel.Destination);Check(parcel.State==ParcelState.Delivered,"Automatic delivery failed");
            Check(run.Results.Count==index,"Delivery result count wrong");
            var result=run.Results.Last();
            File.AppendAllText(Path.Combine(output,"deliveries.tsv"),index+"\t"+result.Cargo+"\t"+result.Turns+"/"+result.TargetTurns+"\t"+result.Evaluation+"\t"+result.OutcomeDetail+"\n");
            yield return Wait(.4f);
            if(run.UnlockedVehicles[2]&&index<11&&run.VehicleIndex!=2&&index>=9)yield return Exchange(8);
        }
        yield return Dialogue();yield return Wait(1);
        Chapter("NORMAL PLAY / all 11 orders, result album and hover details");
        Check(Owner.Run.Delivered==12&&Owner.Run.Results.Count==11,"Full shift incomplete");
        var album=UIManager.Instance.GetPanel("CourierResultPanel") as CourierResultPanel;
        Check(album!=null&&album.orderCards.childCount==11,"Result album missing orders");
        yield return Wait(1.5f);album.orderHistory.verticalNormalizedPosition=0;yield return Wait(1.5f);
        var lastCard=album.orderCards.GetChild(10);var hint=lastCard.GetComponent<CourierIconHint>();
        if(hint!=null){Hover(hint);yield return Wait(1);hint.OnPointerExit(Pointer((RectTransform)hint.transform));}
        album.orderHistory.verticalNormalizedPosition=1;yield return Wait(.5f);Click(album.restartButton);yield return Wait(1);
        Chapter("NORMAL PLAY / restart resets orders, tutorial and depot");
        Check(Owner.Run.Results.Count==0&&Owner.Run.ActiveOrder==null&&Owner.Run.Node==CourierStreetMap.Node("S").StopId,"Restart did not clear shift");
        Check(!Owner.Tutorial.Finished,"Restart did not restore tutorial");
        Owner.Tutorial.Dismiss();yield return Wait(.4f);
        yield return ControlledBranches();
        Chapter("END / return to main menu");GameManager.Instance.ReturnToMainMenu();
        for(int i=0;UnityEngine.Object.FindObjectOfType<TitleUIController>()==null;i++){Check(i<200,"Return to menu timed out");yield return Wait(.1f);}
        yield return Wait(2);Check(errors.Count==0,"Runtime errors: "+string.Join("\n",errors));
    }
    private static IEnumerator ControlledBranches()
    {
        for(int eventIndex=0;eventIndex<12;eventIndex++)
        {
            Owner.RestartShift();Owner.Tutorial.Dismiss();yield return Wait(.2f);
            var run=Owner.Run;run.Node=0;run.Parcels[0].TimeLimit=1;Check(run.LoadParcel(run.Parcels[0]),"Branch pickup failed");run.EventProbability=1;
            bool delay=run.Events[eventIndex].Effect==RoadEventEffect.Wait;
            string nodeId=delay?"C":"N";var end=CourierStreetMap.Node(nodeId);
            run.Roads[0]=new Road(0,end.StopId,6,0){StreetPath=new[]{new StreetEdge(CourierStreetMap.Nodes[0],end,StreetSurface.Normal,null)}};
            Chapter("CONTROLLED BRANCH / event "+(eventIndex+1)+" of 12 / "+run.Events[eventIndex].Title);
            Check(Owner.Depart(run.Roads[0]),"Branch departure failed");yield return Wait(.35f);Owner.SkipRide();
            Check(run.State==ShiftState.Encounter,"Forced event not reached");run.EncounterIndex=eventIndex;yield return Wait(.4f);
            var panel=UIManager.Instance.GetPanel("CourierEventPanel") as CourierEventPanel;
            Check(panel!=null&&(panel.illustration==null||!panel.illustration.gameObject.activeInHierarchy),"Event image should be absent");
            Check(panel.descriptionText.preferredHeight<=panel.descriptionText.rectTransform.rect.height+.5f,"Event text clipped "+eventIndex);
            int turns=run.OrderTurns;float fuel=run.Durability;yield return Wait(1.1f);Click(panel.safeButton);yield return Wait(.3f);
            Check(run.State==ShiftState.Docked&&run.OrderTurns==turns+(delay?1:0)&&run.Durability==fuel,"Wrong event cost");
            Check(run.Node==(delay?end.StopId:0),"Wrong event destination");
            Check(run.OrderEventTriggered,"Event opportunity not consumed");
        }
        for(int vehicle=-1;vehicle<3;vehicle++)
        {
            Owner.RestartShift();Owner.Tutorial.Dismiss();yield return Wait(.2f);var run=Owner.Run;
            run.Node=0;run.EventProbability=0;run.Parcels[0].HeatSensitive=run.Parcels[0].Dry=run.Parcels[0].Fragile=true;
            Check(run.LoadParcel(run.Parcels[0]),"Surface fixture pickup failed");run.VehicleIndex=vehicle;
            if(vehicle>=0)run.Durabilities[vehicle]=1;
            var surface=vehicle<0?StreetSurface.Water:vehicle==0?StreetSurface.Heat:vehicle==1?StreetSurface.Water:StreetSurface.Bumpy;
            run.Roads[0]=new Road(0,3,6,0){StreetPath=new[]{new StreetEdge(CourierStreetMap.Nodes[0],CourierStreetMap.Nodes[3],surface,null)}};
            Chapter("CONTROLLED BRANCH / "+(vehicle<0?"unprotected cargo damage":"protected surface and vehicle exhaustion "+vehicle));
            Check(Owner.Depart(run.Roads[0]),"Surface fixture departure failed");yield return Wait(.5f);Owner.SkipRide();yield return Wait(.4f);
            Check(run.Results.Count==1&&run.Results[0].Damaged==(vehicle<0),"Surface protection mismatch");
            Check(run.IsGhost,"Exhausted vehicle did not become ghost");yield return Dialogue();yield return Wait(.5f);
        }
        Check(recorder.AudioBytes>0&&recorder.Peak>.0001f,"Audio capture is silent");
    }
    private static void Finish(bool success,string message)
    {
        if(completed)return;completed=true;SessionState.SetBool(Key,false);
        Application.logMessageReceived-=Log;
        if(recorder!=null){recorder.CaptureFrame=null;recorder.EndAudio();}
        timestamps?.Dispose();
        File.WriteAllText(Path.Combine(output,"status.txt"),(success?"PASS":"FAIL")+"\n"+message+"\nAssertions: "+assertions+"\nFrames: "+frames+"\nDuration: "+Now+"\nRuntime errors: "+errors.Count+"\n");
        File.WriteAllText(Path.Combine(output,"audio-info.txt"),AudioSettings.outputSampleRate+"\n"+(recorder!=null?recorder.Channels:2)+"\n"+(recorder!=null?recorder.Peak:0)+"\n");
        Debug.Log("FULL_FLOW "+(success?"PASS ":"FAIL ")+message);EditorApplication.Exit(success?0:1);
    }
}
