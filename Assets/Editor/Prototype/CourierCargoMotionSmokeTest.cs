using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierCargoMotionSmokeTest
{
    private const string Key="CourierCargoMotion.Active";
    private static int step;
    private static double next,deadline;
    private static Vector3 rest;
    static CourierCargoMotionSmokeTest(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        step=0;deadline=0;SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.isPlaying=true;
    }
    private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void Dismiss(NightCourierPrototype owner)
    {
        owner.Tutorial.Dismiss();
        for(int i=0;i<200&&owner.ConversationActive;i++)owner.AdvanceDialogue();
        Require(!owner.ConversationActive,"Dialogue failed to finish");
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+50;next=EditorApplication.timeSinceStartup+3;}
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Cargo motion timeout at "+step);
            if(EditorApplication.timeSinceStartup<next)return;
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            var panel=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            Require(owner!=null&&panel!=null,"Gameplay missing");
            var feedback=panel.GetComponent<CourierCargoFeedback>();
            Require(feedback!=null,"Cargo feedback not bound");
            switch(step)
            {
                case 0:
                    Require(feedback.ActiveFlights==0,"Initial binding played a transfer");
                    Dismiss(owner);panel.SetCargoOpen(true);break;
                case 1:
                    var row=panel.GetComponentsInChildren<CourierCargoRow>().First(r=>r.Item==owner.Run.Parcels[0]);
                    rest=panel.loadText.transform.localScale;
                    row.actionButton.onClick.Invoke();feedback.Observe(false);
                    Require(owner.Run.ActiveOrder!=null&&feedback.ActiveFlights==1,"Click load missing or duplicated flight");
                    Require(!row.LoadByDrop(),"Already loaded parcel accepted again");feedback.Observe(false);
                    Require(feedback.ActiveFlights==1,"Failed load played another flight");
                    Time.timeScale=0;break;
                case 2:
                    Require(feedback.ActiveFlights==1,"Flight did not persist while paused");
                    Require(panel.loadText.transform.localScale.x>rest.x,"Counter did not bounce while paused");
                    var flying=panel.GetComponentsInChildren<Image>().First(i=>i.name=="FlyingCargo");
                    Require(flying.sprite!=null&&!flying.raycastTarget&&flying.transform.parent.name=="CargoTransferFeedback","Flight artwork, raycasts or scroll-mask separation incorrect");
                    Require(flying.canvasRenderer.GetAlpha()>0,"Flight is invisible");
                    var group=flying.GetComponentInParent<CanvasGroup>();
                    Require(!group.blocksRaycasts&&!group.interactable,"Flying overlay intercepts input");
                    if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)ScreenCapture.CaptureScreenshot("/tmp/courier-cargo-flight.png");
                    break;
                case 3:
                    Require(feedback.ActiveFlights==0&&panel.loadText.transform.localScale==rest,"Paused flight or counter did not settle");
                    Require(!panel.GetComponentsInChildren<Image>().Any(i=>i.name=="FlyingCargo"||i.name=="CargoTrail"),"Flying cargo left a trail");
                    Time.timeScale=1;CourierChecks.Go(owner.Run,3);feedback.Observe(false);
                    Require(owner.Run.Parcels[0].State==ParcelState.Delivered&&feedback.ActiveFlights==1,"Delivery flight missing");
                    Require(panel.GetComponentsInChildren<Text>().Any(t=>t.text=="Delivered ×1"),"Delivery count feedback missing");break;
                case 4:
                    owner.RestartShift();Require(feedback.ActiveFlights==0,"Restart left delivery effects");
                    Dismiss(owner);panel.SetCargoOpen(true);break;
                case 5:
                    var dragged=panel.GetComponentsInChildren<CourierCargoRow>().First(r=>r.Item==owner.Run.Parcels[0]);
                    var pointer=new PointerEventData(EventSystem.current){position=new Vector2(Screen.width*.65f,Screen.height*.3f),pointerDrag=dragged.gameObject};
                    dragged.OnBeginDrag(pointer);dragged.OnDrag(pointer);panel.dropZone.OnDrop(pointer);dragged.OnEndDrag(pointer);
                    feedback.Observe(false);
                    Require(feedback.ActiveFlights==1&&dragged.DropFeedbackOrigin.HasValue,"Drop lost its pointer origin or flight");
                    owner.RestartShift();Dismiss(owner);
                    Require(feedback.ActiveFlights==0,"Restart did not clear drop feedback");
                    Require(owner.Run.LoadParcel(owner.Run.Parcels[0]),"Reduced-motion fixture failed");feedback.Observe(true);
                    Require(feedback.ActiveFlights==0&&!panel.loadText.GetComponent<CourierValuePulse>().IsPlaying,"Reduced motion still animates");
                    // A two-item order must use both item sprites, with one count announcement.
                    owner.RestartShift();Dismiss(owner);
                    var batch=owner.Run.Parcels.GroupBy(p=>new{p.Origin,p.Destination}).First(g=>g.Count()>1).First();
                    CourierChecks.Go(owner.Run,batch.Origin);Require(owner.Run.LoadParcel(batch),"Batch fixture failed");feedback.Observe(false);
                    Require(feedback.ActiveFlights==2&&panel.GetComponentsInChildren<Text>().Any(t=>t.text=="+2 on board"),"Batch feedback was not counted per item");
                    feedback.enabled=false;Require(feedback.ActiveFlights==0,"Disabling feedback left flights");
                    SessionState.SetBool(Key,false);Debug.Log("COURIER_CARGO_MOTION PASS: click/drop, failed-action deduplication, pause, sprites, raycasts, counters, automatic delivery, restart, reduced motion, batches and cleanup.");
                    EditorApplication.Exit(0);return;
            }
            step++;next=EditorApplication.timeSinceStartup+(step==2?.16:step==3?.95:.55);
        }
        catch(Exception error){Time.timeScale=1;SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
