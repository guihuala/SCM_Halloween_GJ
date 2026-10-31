using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierPresentationSmokeTest
{
    private const string Key="CourierPresentation.Active";
    private static int step,guideId;
    private static double next,deadline;
    static CourierPresentationSmokeTest(){EditorApplication.update+=Tick;}
    private static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
    public static void Run()
    {
        foreach(var name in new[]{"CourierHudPanel","CourierRoutePanel","CourierEventPanel","CourierResultPanel"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Panel/Courier/"+name+".prefab");
            Require(prefab.GetComponentsInChildren<CourierTutorialView>(true).Length==0,"Embedded guide remains in "+name);
        }
        Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/CourierTutorial.prefab")==null,"Legacy guide asset remains");
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static void FocusMatches(CourierTutorialView view,RectTransform target,int index=0)
    {
        Canvas.ForceUpdateCanvases();
        var frame=view.focusFrames[index];Require(frame.gameObject.activeInHierarchy,"Focus frame missing");
        Require(Vector3.Distance(frame.TransformPoint(frame.rect.center),target.TransformPoint(target.rect.center))<1,"Focus does not follow target across panels");
    }
    private static void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
        var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Tutorial overlay blocks underlying button: "+button.name);
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+70;next=EditorApplication.timeSinceStartup+3;}
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Presentation test timed out at "+step);
            if(EditorApplication.timeSinceStartup<next)return;
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            var planner=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            var hud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
            var overlay=UIManager.Instance.GetPanel("CourierTutorialPanel") as CourierTutorialPanel;
            Require(owner!=null&&planner!=null&&hud!=null,"Gameplay panels missing");
            if(step<8)
            {
                Require(overlay!=null&&UnityEngine.Object.FindObjectsOfType<CourierTutorialView>(true).Length==1,"Expected exactly one tutorial overlay");
                if(step==0)guideId=overlay.GetInstanceID();else Require(guideId==overlay.GetInstanceID(),"Tutorial was recreated on panel changes");
            }
            switch(step)
            {
                case 0:
                    Canvas.ForceUpdateCanvases();
                    var page=(RectTransform)planner.transform.Find("Window");var screen=(RectTransform)planner.transform;
                    Require(page.rect.size==screen.rect.size&&page.offsetMin==Vector2.zero&&page.offsetMax==Vector2.zero,"Map page is not fullscreen");
                    Require(planner.mapGraphic.rectTransform.rect.width>1000,"Road map did not expand with the page");
                    for(int i=0;i<6;i++)
                    {
                        var point=CourierRouteGraphic.NodePoint(owner.Run.Stops[i],planner.mapGraphic.rectTransform.rect.size);
                        var marker=planner.stopIcons[i].rectTransform;
                        var local=new Vector3(planner.mapGraphic.rectTransform.rect.xMin+point.x,planner.mapGraphic.rectTransform.rect.yMax-point.y,0);
                        Require(Vector3.Distance(marker.TransformPoint(marker.rect.center),planner.mapGraphic.transform.TransformPoint(local))<1,"Map road and stop marker misaligned: "+i);
                    }
                    FocusMatches(overlay.view,(RectTransform)hud.advanceButton.transform);
                    while(owner.ConversationActive)owner.AdvanceDialogue();owner.ShowPlanner("CourierRoutePanel");break;
                case 1:
                    var row=planner.GetComponentsInChildren<CourierCargoRow>().First(x=>x.Item==owner.Run.Parcels[0]);
                    FocusMatches(overlay.view,(RectTransform)row.actionButton.transform);Click(row.actionButton);break;
                case 2:FocusMatches(overlay.view,planner.stopLabels[3].rectTransform);planner.stopButtons[3].onClick.Invoke();break;
                case 3:FocusMatches(overlay.view,(RectTransform)planner.departButton.transform);Click(planner.departButton);break;
                case 4:owner.Run.Tick(10000);break;
                case 5:
                    var encounter=UIManager.Instance.GetPanel("CourierEventPanel") as CourierEventPanel;
                    Require(encounter!=null,"Encounter panel missing");
                    FocusMatches(overlay.view,(RectTransform)encounter.safeButton.transform);
                    FocusMatches(overlay.view,(RectTransform)encounter.riskButton.transform,1);
                    Require(overlay.transform.GetSiblingIndex()>encounter.transform.GetSiblingIndex(),"Tutorial appears behind encounter");
                    GameManager.Instance.PauseGame();break;
                case 6:Require(!overlay.view.strip.gameObject.activeSelf&&overlay.view.focusFrames.All(x=>!x.gameObject.activeSelf),"Tutorial remains visible during pause");GameManager.Instance.ResumeGame();break;
                case 7:Require(overlay.view.strip.gameObject.activeSelf,"Guide did not return after pause");Click(overlay.view.skipButton);break;
                case 8:
                    Require(overlay==null&&!UIManager.Instance.IsPanelOpen("CourierTutorialPanel"),"Dismissed guide not closed");
                    owner.ResolveEncounter(true);owner.Run.Tick(10000);break;
                case 9:
                    owner.RestartShift();break;
                case 10:
                    Require(!UIManager.Instance.IsPanelOpen("CourierTutorialPanel"),"Dismissed guide reopened on restart");
                    SessionState.SetBool(Key,false);Debug.Log("COURIER_PRESENTATION PASS: fullscreen page, scaled roads and markers, one guide, cross-panel focus, native clicks, pause, dismiss, arrival and restart.");EditorApplication.Exit(0);return;
            }
            step++;next=EditorApplication.timeSinceStartup+.4;
        }
        catch(Exception error){SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
