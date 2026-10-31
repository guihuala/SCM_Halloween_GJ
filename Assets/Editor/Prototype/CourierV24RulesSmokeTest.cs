using System;
using System.IO;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierV24RulesSmokeTest
{
    private const string Key="CourierV24RulesSmoke.Active";
    private static int step;
    private static double next;
    static CourierV24RulesSmokeTest(){EditorApplication.update+=Attach;}
    [MenuItem("Tools/Night Courier/Checks/V2.4 Updated Rules and Event Art")]
    public static void Run()
    {
        File.WriteAllText("/tmp/courier-v24-check.txt","Running\n");
        try
        {
            Debug.Log(CourierChecks.RunAll());
            var configured=new CourierRun();CourierContentOverrides.Apply(configured);
            Check(configured.BatchCount==11&&configured.Vehicles.All(v=>v.Speed==2&&v.MaxDurability==5),"Configured rules differ");
            int[] limits={4,3,4,5,4,4,4,4,4,4,4,4};
            for(int i=0;i<limits.Length;i++)Check(configured.Parcels[i].TimeLimit==limits[i],"Wrong order deadline: "+i);
            Check(configured.Parcels[10].Destination==2&&configured.Parcels[11].Destination==1,"Restored order destinations wrong");
            Check(configured.Events.Count(e=>e.Effect==RoadEventEffect.Wait)==7,"Configured event pool differs");
            Check(configured.Parcels[1].HeatSensitive&&!configured.Parcels[1].Dry&&configured.Parcels[6].Dry,"Configured tags differ");
            Check(Math.Abs(configured.EventProbability-.35f)<.001f,"Event probability override missing");
            foreach(string name in new[]{"social","weather"})Check(Resources.Load<Sprite>("NightVoyage/EventIllustrations/"+name)!=null,"Missing event sprite "+name);
            SessionState.SetString(Key+".Scene",EditorSceneManager.GetActiveScene().path);
            EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
            step=0;next=0;SessionState.SetBool(Key,true);Application.runInBackground=true;EditorApplication.isPlaying=true;
        }
        catch(Exception e){File.WriteAllText("/tmp/courier-v24-check.txt",e.ToString());Debug.LogException(e);}
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Attach()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();if(owner==null)return;
        var pump=owner.GetComponent<CourierStreetMapCheckPump>()??owner.gameObject.AddComponent<CourierStreetMapCheckPump>();pump.Check=Tick;
    }
    private static void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var canvas=button.GetComponentInParent<Canvas>().rootCanvas;
        var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Button blocked: "+button.name+" hits "+string.Join(",",hits.Select(h=>h.gameObject.name)));
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    private static void Finish(bool ok,string message)
    {
        SessionState.SetBool(Key,false);File.WriteAllText("/tmp/courier-v24-check.txt",(ok?"PASS ":"FAIL ")+message);
        EditorApplication.isPlaying=false;
        EditorApplication.delayCall+=()=>{var path=SessionState.GetString(Key+".Scene","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);};
    }
    private static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.7;
        try
        {
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();if(owner==null||!owner.HasPlanner)return;
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            var run=owner.Run;
            switch(step)
            {
                case 0:
                    owner.Tutorial.Dismiss();for(int i=0;i<100&&owner.ConversationActive;i++)owner.AdvanceDialogue();page.SetCargoOpen(false);break;
                case 1:
                    Click(page.mapArtwork.NodeTarget(CourierStreetMap.Node("C").StopId).GetComponent<Button>());
                    Check(page.destinationTooltip.gameObject.activeSelf,"Junction preview missing");
                    Directory.CreateDirectory("ArtPreviews");ScreenCapture.CaptureScreenshot("ArtPreviews/v24-updated-map.png");break;
                case 2:
                    run.Node=6;break;
                case 3:
                    Click(page.possessButton);Check(run.HeatShield&&run.Durability==5,"Station exchange UI failed");
                    run.UseGhost();run.Node=0;Check(run.LoadParcel(run.Parcels[0]),"Cannot load order");run.EventProbability=1;
                    for(int i=0;i<10&&run.State!=ShiftState.Encounter;i++){owner.DepartTo(CourierStreetMap.Node("C").StopId);owner.SkipRide();}
                    Check(run.State==ShiftState.Encounter,"Event panel not reached");run.EncounterIndex=0;break;
                case 4:
                    var panel=UIManager.Instance.GetPanel("CourierEventPanel") as CourierEventPanel;
                    Check(panel!=null&&panel.illustration.sprite!=null&&panel.illustration.gameObject.activeInHierarchy,"Event illustration missing");
                    foreach(var entry in run.Events)
                    {
                        string old=panel.descriptionText.text;panel.descriptionText.text=entry.Description+"\n\nFall back one node; no extra turn or vehicle use.";
                        Check(panel.descriptionText.preferredHeight<=panel.descriptionText.rectTransform.rect.height,"Event description overflows: "+entry.Title);panel.descriptionText.text=old;
                    }
                    ScreenCapture.CaptureScreenshot("ArtPreviews/v24-event-social.png");break;
                case 5:run.EncounterIndex=5;break;
                case 6:
                    ScreenCapture.CaptureScreenshot("ArtPreviews/v24-event-weather.png");break;
                case 7:
                    var eventPanel=UIManager.Instance.GetPanel("CourierEventPanel") as CourierEventPanel;
                    int turns=run.OrderTurns;Click(eventPanel.safeButton);
                    Check(run.State==ShiftState.Docked&&run.OrderTurns==turns+1,"Event confirm UI failed");break;
                default:Finish(true,"Rules, configured data, street-node raycast, station exchange, event artwork, text fit and event confirmation.");return;
            }
            step++;
        }
        catch(Exception e){Debug.LogException(e);Finish(false,e.ToString());}
    }
}
