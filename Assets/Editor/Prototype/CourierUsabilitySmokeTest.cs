using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierUsabilitySmokeTest
{
    const string Flag="/tmp/courier-usability-run",Result="/tmp/courier-usability-result.txt",Key="CourierUsability.Active";
    static int phase; static double next;
    static CourierUsabilitySmokeTest(){EditorApplication.update+=Update;}
    [MenuItem("Tools/Night Courier/Checks/Usability and Depot Start")]
    public static void Run()
    {
        try {
            File.WriteAllText(Result,"Running\n");Debug.Log(CourierChecks.RunAll());
            SessionState.SetString(Key+"Scene",EditorSceneManager.GetActiveScene().path);
            EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
            phase=0;next=0;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }catch(Exception e){Finish(false,e.ToString());}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Update()
    {
        if(File.Exists(Flag)&&!EditorApplication.isCompiling&&!EditorApplication.isPlaying){File.Delete(Flag);Run();}
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();if(owner==null||!owner.HasPlanner)return;
        var pump=owner.GetComponent<CourierStreetMapCheckPump>()??owner.gameObject.AddComponent<CourierStreetMapCheckPump>();pump.Check=Tick;
    }
    static void Hit(RectTransform target,Button button)
    {
        Canvas.ForceUpdateCanvases();var canvas=target.GetComponentInParent<Canvas>().rootCanvas;
        var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,target.TransformPoint(target.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Node center obstructed: "+target.parent.name+" / "+string.Join(",",hits.Select(h=>h.gameObject.name)));
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.8;
        try {
            var owner=NightCourierPrototype.Current;var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            var run=owner.Run;
            switch(phase++) {
                case 0:
                    Check(run.Node==CourierStreetMap.Node("S").StopId,"New shift must begin at depot");
                    Check(!owner.ConversationActive&&run.Stops[run.Node].Name=="Parcel Depot","Depot opening blocked");
                    Check(run.Stops[1].Sign=="FOREST","Forest label missing");
                    var guide=UIManager.Instance.GetPanel("CourierTutorialPanel") as CourierTutorialPanel;
                    Check(guide!=null&&guide.view.instruction.text.Contains("Orders"),"Tutorial did not initialize");
                    var guideHint=guide.view.strip.Find("GuideSymbol").GetComponent<CourierIconHint>();
                    var guideTooltip=guide.GetComponent<CourierTooltip>();Check(guideTooltip!=null,"Guide has no tooltip host");
                    guideTooltip.Show(guideHint,true);Check(guideTooltip.bubble.gameObject.activeSelf&&guideTooltip.label.text.Contains("Orders"),"Guide explanation unreachable");
                    owner.Tutorial.Dismiss();break;
                case 1:
                    foreach(var n in CourierStreetMap.Nodes){var parent=n.StopId<6?page.stopButtons[n.StopId].transform:page.mapArtwork.NodeTarget(n.StopId);Hit(parent.Find("NodeHitArea") as RectTransform,parent.GetComponent<Button>());}
                    page.SetCargoOpen(true);break;
                case 2:
                    Check(page.CargoOpen&&page.cargoDrawer.gameObject.activeInHierarchy&&page.cargoDrawer.GetComponent<CanvasGroup>().alpha>.95f,"Orders hidden");
                    Check(Math.Abs(page.cargoDrawer.anchoredPosition.y+184)<2,"Orders drifted off screen");
                    Check(page.cargoDrawer.rect.height>page.cargoDrawer.rect.width*.8f,"Orders still too flat");
                    foreach(var label in page.stopLabels)Check(label.preferredHeight<=label.rectTransform.rect.height+.5f,"Map name clipped: "+label.text);
                    foreach(var station in page.mapArtwork.stationButtons){var label=station.GetComponentInChildren<Text>();Check(!label.enabled&&station.transform.Find("ActionSymbol")!=null&&station.GetComponent<CourierIconHint>()!=null,"Station symbol or explanation missing");}
                    Check(!page.objectiveText.enabled&&!page.vehicleText.enabled,"Redundant HUD text still shown");
                    Check(page.cargoToggleText.text=="0/11"&&page.cargoToggleButton.transform.Find("ActionSymbol")!=null,"Orders icon/counter missing");
                    Check(!page.cargoCloseButton.GetComponentInChildren<Text>().enabled,"Close label still shown");
                    foreach(var row in page.GetComponentsInChildren<CourierCargoRow>())
                    {
                        Check(row.nameText.preferredHeight<=row.nameText.rectTransform.rect.height+.5f,"Order name clipped");
                        Check(row.destinationText.preferredHeight<=row.destinationText.rectTransform.rect.height+.5f,"Order route clipped");
                        Check(row.timeText.preferredHeight<=row.timeText.rectTransform.rect.height+.5f,"Order turns clipped");
                    }
                    Directory.CreateDirectory("ArtPreviews");ScreenCapture.CaptureScreenshot("ArtPreviews/usability-orders.png");break;
                case 3:page.SetCargoOpen(false);run.Node=0;owner.BeginConversation();break;
                case 4:
                    var hud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
                    for(int i=0;i<100&&owner.ConversationActive&&owner.DialogueSpeaker!="Goolie";i++)owner.AdvanceDialogue();
                    Check(owner.DialogueSpeaker=="Goolie","No protagonist line in opening");break;
                case 5:
                    var dialogueHud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
                    Check(dialogueHud.speakerPortrait.enabled&&dialogueHud.speakerPortrait.sprite!=null,"Protagonist portrait missing");
                    Check(dialogueHud.speakerName.preferredHeight<=dialogueHud.speakerName.rectTransform.rect.height+.5f,"Speaker name clipped");
                    Check(dialogueHud.subtitleText.preferredHeight<=dialogueHud.subtitleText.rectTransform.rect.height+.5f,"Dialogue clipped");
                    ScreenCapture.CaptureScreenshot("ArtPreviews/usability-goolie.png");
                    break;
                case 6:for(int i=0;i<200&&owner.ConversationActive;i++)owner.AdvanceDialogue();break;
                case 7:
                    var map=(RectTransform)page.mapGraphic.transform.parent;
                    var prompt=(RectTransform)page.nodePromptButton.transform;var corners=new Vector3[4];prompt.GetWorldCorners(corners);
                    Check(map.InverseTransformPoint(corners[0]).y>=map.rect.yMax,"Talk overlaps map");
                    ScreenCapture.CaptureScreenshot("ArtPreviews/usability-map.png");break;
                case 8:
                    var hint=page.loadText.transform.parent.Find("LoadIcon").GetComponent<CourierIconHint>();
                    page.GetComponent<CourierTooltip>().Show(hint,true);break;
                case 9:
                    var tooltip=page.GetComponent<CourierTooltip>();
                    var a=new Vector3[4];var b=new Vector3[4];tooltip.bubble.GetWorldCorners(a);page.loadText.rectTransform.GetWorldCorners(b);
                    Check(a[2].y<b[0].y||a[0].y>b[2].y,"Tooltip obscures order numbers");
                    ScreenCapture.CaptureScreenshot("ArtPreviews/usability-tooltip.png");break;
                case 10:
                    page.Select(3);break;
                case 11:
                    Check(page.destinationTooltip.gameObject.activeSelf&&!page.riskText.enabled,"Route preview should use risk symbol");
                    bool wasSafe=run.PreferSafeRoutes;page.routeModeButton.onClick.Invoke();Check(run.PreferSafeRoutes!=wasSafe,"Route symbol does not switch mode");
                    ScreenCapture.CaptureScreenshot("ArtPreviews/usability-route-symbols.png");break;
                case 12:
                    run.Node=0;Check(run.LoadParcel(run.Parcels[0]),"Cannot load symbol interaction fixture");run.EventProbability=1;
                    for(int i=0;i<20&&run.State!=ShiftState.Encounter;i++){Check(owner.DepartTo(CourierStreetMap.Node("C").StopId),"Cannot travel to event fixture");owner.SkipRide();}
                    Check(run.State==ShiftState.Encounter,"Event fixture missing");break;
                case 13:
                    var eventPanel=UIManager.Instance.GetPanel("CourierEventPanel") as CourierEventPanel;
                    Check(eventPanel!=null&&eventPanel.safeButton.transform.Find("ActionSymbol")!=null&&!eventPanel.safeButton.GetComponentInChildren<Text>().enabled,"Event confirm symbol missing");
                    eventPanel.safeButton.onClick.Invoke();Check(run.State==ShiftState.Docked,"Event confirm symbol does not resolve event");break;
                case 14:
                    var recipient=page.transform.Find("Window/ActiveRecipientLabel").GetComponent<Text>();
                    Check(recipient.gameObject.activeSelf&&recipient.text==run.Stops[run.ActiveOrder.Destination].Name,"Current recipient missing from HUD");
                    foreach(var station in page.mapArtwork.stationButtons){string message=station.GetComponent<CourierIconHint>().message;Check(message.Contains("5 vehicle turns")&&message.Contains("2 edges"),"Station rules incomplete");}
                    run.ActiveOrder.HeatSensitive=run.ActiveOrder.Fragile=true;
                    string cargoInfo=CourierInformation.Order(run,run.ActiveOrder);Check(cargoInfo.Contains("Flame")&&cargoInfo.Contains("Umbrella")&&cargoInfo.Contains("Glass"),"Multiple cargo requirements lost");
                    page.Select(3);run.ActiveOrder.Damaged=true;break;
                case 15:
                    Check(page.timeCostText.text.Contains("/"),"Forecast order target missing");
                    Check(page.transform.Find("Window/ActiveOrderStateSymbol").GetComponent<CourierIconGraphic>().kind==CourierIconKind.Warning,"Damaged order warning missing");
                    var info=page.transform.Find("Window/InformationButton");
                    ExecuteEvents.Execute(info.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);break;
                case 16:
                    var help=page.GetComponent<CourierTooltip>();
                    Check(help.bubble.gameObject.activeSelf&&help.label.text.Contains("max 1/order")&&help.label.text.Contains("WASD"),"Rules information cannot be opened");
                    Check(help.label.preferredHeight<=help.label.rectTransform.rect.height+.5f,"Rules information clipped");
                    ScreenCapture.CaptureScreenshot("ArtPreviews/information-audit.png");break;
                default:Finish(true,"Rules, depot opening, all 32 node centers, vertical Orders, Goolie portrait, Talk outside map.");break;
            }
        }catch(Exception e){Finish(false,e.ToString());}
    }
    static void Finish(bool success,string message)
    {
        SessionState.SetBool(Key,false);File.WriteAllText(Result,(success?"PASS ":"FAIL ")+message);
        if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;
        EditorApplication.delayCall+=()=>{string scene=SessionState.GetString(Key+"Scene","");if(!string.IsNullOrEmpty(scene))EditorSceneManager.OpenScene(scene);};
    }
}
