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
                    owner.Tutorial.Dismiss();break;
                case 1:
                    foreach(var n in CourierStreetMap.Nodes){var parent=n.StopId<6?page.stopButtons[n.StopId].transform:page.mapArtwork.NodeTarget(n.StopId);Hit(parent.Find("NodeHitArea") as RectTransform,parent.GetComponent<Button>());}
                    page.SetCargoOpen(true);break;
                case 2:
                    Check(page.CargoOpen&&page.cargoDrawer.gameObject.activeInHierarchy&&page.cargoDrawer.GetComponent<CanvasGroup>().alpha>.95f,"Orders hidden");
                    Check(Math.Abs(page.cargoDrawer.anchoredPosition.y+184)<2,"Orders drifted off screen");
                    Check(page.cargoDrawer.rect.height>page.cargoDrawer.rect.width*.8f,"Orders still too flat");
                    Directory.CreateDirectory("ArtPreviews");ScreenCapture.CaptureScreenshot("ArtPreviews/usability-orders.png");break;
                case 3:page.SetCargoOpen(false);run.Node=0;owner.BeginConversation();break;
                case 4:
                    var hud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
                    for(int i=0;i<100&&owner.ConversationActive&&owner.DialogueSpeaker!="Goolie";i++)owner.AdvanceDialogue();
                    Check(owner.DialogueSpeaker=="Goolie","No protagonist line in opening");break;
                case 5:
                    var dialogueHud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
                    Check(dialogueHud.speakerPortrait.enabled&&dialogueHud.speakerPortrait.sprite!=null,"Protagonist portrait missing");
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
