using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CreditPanelSmokeTest
{
    private const string Active = "CreditPanelSmokeTest.Active";
    private static int step;
    private static double next;
    private static double deadline;

    static CreditPanelSmokeTest() { EditorApplication.update += Tick; }

    [MenuItem("Tools/UI/Check Credits Panel")]
    public static void Run()
    {
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        Application.runInBackground = true;
        EditorApplication.isPlaying = true;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying) return;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 60;
        if (EditorApplication.timeSinceStartup > deadline) { Finish(false, "Timeout"); return; }
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            var menu = UnityEngine.Object.FindObjectOfType<TitleUIController>();
            if (menu == null || UIManager.Instance == null || SceneLoader.Instance.IsLoading()) return;
            var panel = UIManager.Instance.GetPanel("CreditPanel") as CreditPanel;
            switch (step)
            {
                case 0:
                    Require(menu.creditsButton != null, "Missing main menu entry");
                    Canvas.ForceUpdateCanvases();
                    var buttons = new[] { menu.newGameButton, menu.settingsButton, menu.creditsButton, menu.exitButton };
                    for (int i = 1; i < buttons.Length; i++)
                        Require(buttons[i - 1].transform.position.y > buttons[i].transform.position.y, "Menu order or overlap");
                    ScreenCapture.CaptureScreenshot("/tmp/scm-credits-menu.png");
                    break;
                case 1:
                    menu.creditsButton.onClick.Invoke();
                    break;
                case 2:
                    Require(panel != null && panel.GetComponent<CanvasGroup>().interactable, "Panel did not open");
                    Require(EventSystem.current.currentSelectedGameObject == panel.GetComponentInChildren<Button>().gameObject, "Back button not focused");
                    Canvas.ForceUpdateCanvases();
                    var body = panel.transform.Find("MessageText").GetComponent<Text>();
                    Require(body.text.Contains("Wolfgang_") && body.text.Contains("Kenney"), "Audio credits missing");
                    Require(body.preferredHeight <= body.rectTransform.rect.height, "Credits text clipped");
                    Require(body.cachedTextGenerator.vertexCount > 4, "Credits not rendered");
                    ScreenCapture.CaptureScreenshot("/tmp/scm-credits-panel.png");
                    break;
                case 3:
                    // Repeated open must preserve the single registered instance.
                    menu.creditsButton.onClick.Invoke();
                    Require(UnityEngine.Object.FindObjectsOfType<CreditPanel>().Length == 1, "Duplicate credit panels");
                    panel.GetComponentInChildren<Button>().onClick.Invoke();
                    Require(!UIManager.Instance.IsPanelOpen("CreditPanel"), "Back failed");
                    Require(EventSystem.current.currentSelectedGameObject == menu.creditsButton.gameObject, "Menu focus not restored");
                    break;
                case 4:
                    Require(UnityEngine.Object.FindObjectOfType<CreditPanel>() == null, "Closing animation failed to destroy panel");
                    menu.creditsButton.onClick.Invoke();
                    break;
                case 5:
                    Require(panel != null, "Reopen failed");
                    panel.Back(); // Same close path used by Escape.
                    break;
                default:
                    Require(UnityEngine.Object.FindObjectOfType<CreditPanel>() == null, "Reopened panel did not close");
                    Finish(true, "Entry, layout, credits, focus, back, duplicate open and reopen passed");
                    return;
            }
            step++;
            next = EditorApplication.timeSinceStartup + 1;
        }
        catch (Exception e) { Finish(false, e.ToString()); }
    }

    private static void Finish(bool success, string message)
    {
        SessionState.SetBool(Active, false);
        Debug.Log("CREDIT_PANEL " + (success ? "PASS: " : "FAIL: ") + message);
        if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}
