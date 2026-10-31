using System;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierFeedbackSmokeTest
{
    private const string Key="CourierFeedbackSmoke.Active";
    private static int step;
    private static double next,deadline;
    private static Vector3 buttonScale;
    private static GameObject lastBubble;
    static CourierFeedbackSmokeTest(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        EditorApplication.ExecuteMenuItem("Window/General/Game");SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");EditorApplication.isPlaying=true;
    }
    private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+80;next=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Feedback check timed out");
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();Require(owner!=null,"Missing prototype");
            if(step==8)
            {
                Require(lastBubble==null||!lastBubble.activeInHierarchy,"Closing panel left a tooltip");
                SessionState.SetBool(Key,false);Debug.Log("COURIER_FEEDBACK_UI PASS: shared prefabs, delayed hover, disabled reasons, keyboard focus, text wrapping, viewport bounds, no raycast interception, paused press/play, fixed button layout and cleanup.");EditorApplication.Exit(0);return;
            }
            var panel=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            if(step==0){owner.ShowPlanner("CourierRoutePanel");step++;next=EditorApplication.timeSinceStartup+.5;return;}
            var tip=panel.GetComponent<CourierTooltip>();var button=panel.departButton;var hint=button.GetComponent<CourierIconHint>();var feedback=button.GetComponent<CourierButtonFeedback>();
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            switch(step)
            {
                case 1:
                    Require(feedback!=null&&feedback.Effect!=null,"Feedback prefab missing");
                    Require(tip.bubble.GetComponent<CourierHintView>()!=null,"Shared tooltip prefab missing");
                    buttonScale=button.transform.localScale;
                    ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
                    Require(tip.bubble.GetComponent<CanvasGroup>().alpha==0,"Hover delay missing");break;
                case 2:
                    Require(!button.interactable&&feedback.Effect.group.alpha>.9f,"Disabled action has no visible explanation feedback");
                    Require(tip.label.text.Contains("Select a stop")&&tip.bubble.GetComponent<CanvasGroup>().alpha>.9f,"Disabled action explanation missing");
                    Require(button.transform.localScale==buttonScale,"Feedback changed button layout");
                    foreach(var image in feedback.Effect.edges)Require(!image.raycastTarget,"Effect intercepts pointer");
                    Require(!feedback.Effect.group.blocksRaycasts&&!tip.bubble.GetComponent<CanvasGroup>().blocksRaycasts,"Overlay intercepts input");
                    hint.message="Updated condition\nThis condition changed while the same button stayed hovered. The hint should resize without clipping or requiring the pointer to leave.";break;
                case 3:
                    // The planner refreshes its own hints every frame, so use a tag hint to test live text updates.
                    var fixture=new GameObject("LiveHintProbe",typeof(RectTransform));fixture.transform.SetParent(panel.transform,false);
                    var tag=fixture.AddComponent<CourierIconHint>();
                    tag.message="Updated condition\nThis is a longer explanation of the selected delivery. The shared tooltip must wrap and resize within the panel, without covering the selected control.";
                    tip.Show(tag,true);break;
                case 4:
                    Require(tip.label.text.StartsWith("Updated condition"),"Hint did not refresh");
                    Require(tip.label.preferredHeight<=tip.label.rectTransform.rect.height+1,"Hint text clipped");
                    if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot("/tmp/courier-feedback-tooltip.png");
                    var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(panel.transform,tip.bubble);var rect=((RectTransform)panel.transform).rect;
                    Require(rect.Contains(bounds.min)&&rect.Contains(bounds.max),"Hint escaped viewport");
                    ExecuteEvents.Execute(button.gameObject,new AxisEventData(EventSystem.current){moveDir=MoveDirection.Right},ExecuteEvents.selectHandler);
                    ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerExitHandler);break;
                case 5:
                    Require(tip.bubble.gameObject.activeSelf&&feedback.Effect.group.alpha>.9f,"Pointer exit hid keyboard focus");
                    ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.deselectHandler);
                    Require(!tip.bubble.gameObject.activeSelf,"Deselect left stale tooltip");
                    panel.stopButtons[3].onClick.Invoke();Time.timeScale=0;
                    ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
                    ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);break;
                case 6:
                    Require(feedback.Effect.group.alpha>.9f&&feedback.Effect.transform.localScale.x>1,"Paused press feedback did not play");
                    Require(button.transform.localScale==buttonScale,"Pressed effect shifted button");
                    ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                    feedback.Play();break;
                case 7:
                    button.gameObject.SetActive(false);Require(!tip.bubble.gameObject.activeSelf&&feedback.Effect.group.alpha==0,"Disabled button left stale feedback");
                    button.gameObject.SetActive(true);Time.timeScale=1;lastBubble=tip.bubble.gameObject;owner.ClosePlanner();break;
            }
            Debug.Log("COURIER_FEEDBACK_UI step "+step+" passed");step++;next=EditorApplication.timeSinceStartup+.5;
        }
        catch(Exception ex){Time.timeScale=1;SessionState.SetBool(Key,false);Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
