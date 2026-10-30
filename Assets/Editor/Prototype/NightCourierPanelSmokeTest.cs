using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[InitializeOnLoad]
public static class NightCourierPanelSmokeTest
{
    private const string Key="CourierPanelSmoke.Active";
    private static int step;
    private static double next,deadline;
    private static float pausedAt;
    private static bool tooltipCaptured;
    static NightCourierPanelSmokeTest()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(message,stack,type)=>{if(SessionState.GetBool(Key,false)&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)){SessionState.SetBool(Key,false);EditorApplication.Exit(1);}};
    }
    public static void Run()
    {
        Debug.Log("COURIER_RULES PASS "+CourierChecks.RunAll());
        foreach(var name in new[]{"CourierHudPanel","CourierRoutePanel","CourierEventPanel","CourierResultPanel"})
            Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Panel/Courier/"+name+".prefab")!=null,"Missing prefab "+name);
        SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    private static void Require(bool ok,string error){if(!ok)throw new Exception(error);}
    private static T Panel<T>(string name)where T:BasePanel{var p=UIManager.Instance.GetPanel(name)as T;Require(p!=null,"Missing panel "+name);return p;}
    private static CourierRoutePanel Planner{get{return Panel<CourierRoutePanel>("CourierRoutePanel");}}
    private static void Shot(string name){if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot("/tmp/courier-v2-"+name+".png");}
    private static void ClickStage(CourierHudPanel hud,float x,float y)
    {
        var rt=hud.stageGraphic.rectTransform;var rect=rt.rect;float scale=Mathf.Min(rect.width/1440,rect.height/440);
        var local=new Vector2(rect.xMin+(rect.width-1440*scale)/2+x*scale,rect.yMax-(rect.height-440*scale)/2-y*scale);
        var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(local)),button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Require(hits.Count>0&&hits[0].gameObject==hud.stageGraphic.gameObject,"Stage raycast blocked");ExecuteEvents.Execute(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    private static CourierCargoRow Row(string name){return Planner.GetComponentsInChildren<CourierCargoRow>().First(r=>r.nameText.text==name);}
    private static void FastLeg(NightCourierPrototype owner)
    {
        for(int i=0;i<50&&(owner.Run.State==ShiftState.Driving||owner.Run.State==ShiftState.Pushing||owner.Run.State==ShiftState.Encounter);i++)
        {if(owner.Run.State==ShiftState.Encounter)owner.ResolveEncounter(true);else owner.Run.Tick(10000);}
        if(!owner.Run.Finished)owner.BeginConversation();
    }
    private static void VerifyEnglishPresentation()
    {
        var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/BarlowCondensed-Light.ttf");
        foreach(var text in UnityEngine.Object.FindObjectsOfType<Text>(true))
        {
            Require(text.font==font,"Project font missing on "+text.name);
            Require(!System.Text.RegularExpressions.Regex.IsMatch(text.text,@"[\u3400-\u9fff]"),"Non-English UI text on "+text.name);
        }
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+100;next=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup>deadline){SessionState.SetBool(Key,false);Debug.LogError("V2 UI timeout at "+step);EditorApplication.Exit(1);return;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();Require(owner!=null,"Missing prototype");var hud=Panel<CourierHudPanel>("CourierHudPanel");
            VerifyEnglishPresentation();
            switch(step)
            {
                case 0:Require(owner.ConversationActive&&!hud.roadStatus.activeSelf,"Cinematic opening missing");Shot("opening");break;
                case 1:while(owner.ConversationActive)owner.AdvanceDialogue();ClickStage(hud,395,320);break;
                case 2:
                    Require(Planner.stopButtons.Length==6,"Map node count incorrect");
                    Require(Planner.transform.Find("Window/LocationText")==null&&Planner.transform.Find("Window/LoadFill")==null&&Planner.transform.Find("Window/CapacityWarning")==null,"Redundant planner elements remain");
                    Require(!Planner.repairButton.gameObject.activeSelf,"Full durability repair action should be hidden");
                    Require(Row("Engine Parts").GetComponent<LayoutElement>().preferredHeight==84,"Plain parcel kept empty tag row");
                    var row=Row("Engine Parts");var target=(RectTransform)Planner.loadedContent.parent;
                    Vector2 screen=RectTransformUtility.WorldToScreenPoint(null,target.TransformPoint(target.rect.center));
                    var data=new PointerEventData(EventSystem.current){position=screen,pointerDrag=row.gameObject};
                    ExecuteEvents.Execute(row.gameObject,data,ExecuteEvents.beginDragHandler);
                    var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
                    Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<CourierTrunkDropZone>()==Planner.dropZone,"Trunk drop target blocked");
                    ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.dropHandler);ExecuteEvents.Execute(row.gameObject,data,ExecuteEvents.endDragHandler);break;
                case 3:
                    Require(Planner.loadText.text.Contains("Full"),"Inline full-load warning missing");
                    Require(owner.Run.Load==3&&Row("Engine Parts").transform.parent==Planner.loadedContent,"Drag loading failed");
                    Require(!Row("Unmarked Parcel").actionButton.gameObject.activeSelf,"Main parcel can be discarded via UI");
                    Require(Planner.stopIcons[5].kind==CourierIconKind.Lock,"Storm lock icon missing");
                    Require(!Planner.waitingSection.gameObject.activeSelf&&Planner.loadedScroll.sizeDelta.y>300,"Empty pickup section did not collapse");
                    var fragile=Row("Unmarked Parcel").fragileIcon;var tip=Planner.GetComponent<CourierTooltip>();
                    ExecuteEvents.Execute(fragile.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
                    Require(tip.bubble.gameObject.activeSelf&&tip.label.text.Contains("Fragile"),"Icon hover hint missing");
                    ExecuteEvents.Execute(fragile.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);
                    Require(!tip.bubble.gameObject.activeSelf,"Hover hint remained after exit");
                    ExecuteEvents.Execute(Planner.departButton.gameObject,new AxisEventData(EventSystem.current){moveDir=MoveDirection.Right},ExecuteEvents.selectHandler);
                    Require(tip.bubble.gameObject.activeSelf&&tip.label.text.Contains("destination"),"Keyboard icon hint missing");
                    ExecuteEvents.Execute(Planner.departButton.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.deselectHandler);
                    Planner.stopButtons[4].onClick.Invoke();Require(Planner.departButton.interactable&&Planner.mapGraphic.SelectedRoute.Count==2,"Nonadjacent destination cannot be selected");break;
                case 4:
                    var glyph=Row("Unmarked Parcel").fragileIcon;
                    if(!tooltipCaptured)
                    {
                        ExecuteEvents.Execute(glyph.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
                        var tooltip=Planner.GetComponent<CourierTooltip>();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(Planner.transform,tooltip.bubble);
                        var panelRect=((RectTransform)Planner.transform).rect;
                        Require(panelRect.Contains(bounds.min)&&panelRect.Contains(bounds.max),"Icon hint outside viewport");
                        Shot("tooltip");tooltipCaptured=true;next=EditorApplication.timeSinceStartup+.65;return;
                    }
                    ExecuteEvents.Execute(glyph.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);
                    Shot("planner");break;
                case 5:Planner.departButton.onClick.Invoke();Require(owner.Run.State==ShiftState.Driving&&!owner.HasPlanner,"UI departure failed");GameManager.Instance.PauseGame();pausedAt=owner.Run.Elapsed;break;
                case 6:Require(owner.Run.Elapsed==pausedAt,"Pause advances clock");GameManager.Instance.ResumeGame();owner.Run.Tick(10000);break;
                case 7:
                    if(!UIManager.Instance.IsPanelOpen("CourierEventPanel")||Panel<CourierEventPanel>("CourierEventPanel").GetComponentsInChildren<CanvasGroup>().Any(g=>g.alpha<.99f)){next=EditorApplication.timeSinceStartup+.2;return;}
                    Shot("event");break;
                case 8:Panel<CourierEventPanel>("CourierEventPanel").safeButton.onClick.Invoke();owner.Run.Tick(10000);break;
                case 9:Require(owner.Run.HasJourney&&owner.Run.PlannedDestination==4,"Intermediate stop lost destination");Require(owner.Run.Node==1&&owner.ConversationActive&&owner.Run.Parcels[1].State==ParcelState.Loaded,"Restaurant story did not hand over tape");ClickStage(hud,395,320);break;
                case 10:Row("Hot Coffee").actionButton.onClick.Invoke();Planner.stopButtons[2].onClick.Invoke();Planner.departButton.onClick.Invoke();FastLeg(owner);owner.ShowPlanner("CourierRoutePanel");break;
                case 11:Planner.possessButton.onClick.Invoke();Require(owner.Run.VehicleIndex==1,"Bike UI unlock/possession failed");Row("Farewell Letters").actionButton.onClick.Invoke();Planner.stopButtons[3].onClick.Invoke();Planner.departButton.onClick.Invoke();FastLeg(owner);owner.ShowPlanner("CourierRoutePanel");break;
                case 12:Require(owner.Run.Parcels[2].State==ParcelState.Loaded,"Ferry key missing");Row("Sailor's Watch").actionButton.onClick.Invoke();Planner.stopButtons[1].onClick.Invoke();Shot("ferry-planner");break;
                case 13:Planner.departButton.onClick.Invoke();FastLeg(owner);owner.ClosePlanner();CourierChecks.Travel(owner.Run,4);owner.BeginConversation();owner.ShowPlanner("CourierRoutePanel");break;
                case 14:Require(owner.Run.TruthRevealed,"DJ reveal missing");Planner.possessButton.onClick.Invoke();Require(owner.Run.VehicleIndex==2,"Pickup UI possession failed");Planner.stopButtons[5].onClick.Invoke();Require(Planner.departButton.interactable,"Final road did not unlock");Shot("final-road");break;
                case 15:Planner.departButton.onClick.Invoke();FastLeg(owner);break;
                case 16:Require(owner.Run.State==ShiftState.EndingA,"Native UI route did not reach ending A");Require(Panel<CourierResultPanel>("CourierResultPanel").titleText.text.Contains("Perfect Delivery"),"Ending A presentation missing");Shot("ending-a");break;
                case 17:Panel<CourierResultPanel>("CourierResultPanel").restartButton.onClick.Invoke();Require(owner.Run.Node==0&&owner.Run.Load==1&&owner.ConversationActive,"Restart did not restore main story");owner.Run.Durabilities[0]=0;Require(owner.DepartTo(4),"Broken vehicle cannot depart for recovery");break;
                case 18:Require(owner.Run.State==ShiftState.Pushing,"Push state missing");Shot("pushing");owner.Run.Tick(10000);break;
                case 19:Require(owner.Run.Node==1&&owner.Run.Durability>0,"Push recovery blocked continuation");while(owner.ConversationActive)owner.AdvanceDialogue();
                    hud.advanceButton.onClick.Invoke();Require(owner.Run.State==ShiftState.Driving&&owner.Run.PlannedDestination==4,"Intermediate continue control failed");FastLeg(owner);
                    Require(owner.Run.Node==4&&!owner.Run.HasJourney,"Continue control did not complete multi-stop journey");
                    owner.Run.Elapsed=300;CourierChecks.Travel(owner.Run,3,false);CourierChecks.Travel(owner.Run,4,false);Require(owner.PossessParkedVehicle(),"Late route cannot possess pickup");CourierChecks.Travel(owner.Run,5,false);break;
                case 20:Require(Panel<CourierResultPanel>("CourierResultPanel").titleText.text.Contains("Road Keeper"),"Ending B presentation missing");Shot("ending-b");break;
                case 21:SessionState.SetBool(Key,false);Debug.Log("COURIER_V2_UI PASS 22 steps: cinematic dialogue, unified planner, native drag drop, vehicle unlocks, dry/fragile cargo, pause, main-story ending A, push recovery, late ending B, restart.");EditorApplication.Exit(0);return;
            }
            Debug.Log("COURIER_V2_UI step "+step+" passed");step++;next=EditorApplication.timeSinceStartup+.65;
        }
        catch(Exception ex){SessionState.SetBool(Key,false);Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
