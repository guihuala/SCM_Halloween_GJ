using System;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CourierTextJuiceSmokeTest
{
    private const string Key = "CourierTextJuiceSmoke.Active";
    private static int step;
    private static double next, deadline;
    private static string firstLine;
    private static float pausedElapsed;
    static CourierTextJuiceSmokeTest() { EditorApplication.update += Tick; }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        if (deadline == 0) { deadline = EditorApplication.timeSinceStartup + 60; next = EditorApplication.timeSinceStartup + 3; }
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Text animation test timed out at " + step);
            if (EditorApplication.timeSinceStartup < next) return;
            var owner = UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            var hud = UnityEngine.Object.FindObjectOfType<CourierHudPanel>();
            Require(owner != null && hud != null, "Gameplay HUD missing");
            var juice = hud.subtitleText.GetComponent<CourierTextJuice>();
            Require(juice != null, "Subtitle animation missing");
            switch (step)
            {
                case 0:
                    firstLine = owner.Subtitle;
                    juice.Present("Test line replacement", true);
                    // An input in the same frame as a line change must complete the current line.
                    owner.AdvanceDialogue();
                    Require(owner.Subtitle == firstLine && !juice.IsPlaying, "First input skipped dialogue instead of completing it");
                    hud.advanceButton.onClick.Invoke();
                    Require(owner.Subtitle != firstLine, "Arrow failed to advance completed line");
                    hud.CompleteSubtitle();
                    juice.Present("Pause test: this text should remain in its entrance animation while paused.", true);
                    GameManager.Instance.PauseGame();
                    pausedElapsed = Time.time;
                    break;
                case 1:
                    // HUD restores the actual line once, then its playback must stay frozen.
                    Require(Time.time == pausedElapsed && juice.IsPlaying, "Text animation advanced during pause");
                    GameManager.Instance.ResumeGame();
                    break;
                case 2:
                    Require(!juice.IsPlaying, "Text failed to settle naturally");
                    juice.Present("<b>Rich text</b>\nSecond line", true);
                    Canvas.ForceUpdateCanvases();
                    Require(hud.subtitleText.text == "<b>Rich text</b>\nSecond line", "Reveal modified the full layout string");
                    Require(juice.Complete() && !juice.IsPlaying, "Completion did not restore text");
                    juice.reducedMotion = true;
                    juice.Present("Reduced motion", true);
                    Require(!juice.IsPlaying, "Reduced motion still animates");
                    SessionState.SetBool(Key, false);
                    Debug.Log("COURIER_TEXT_JUICE PASS: same-frame input, arrow advance, pause, natural settle, rich text, reduced motion.");
                    EditorApplication.Exit(0);
                    return;
            }
            step++;
            next = EditorApplication.timeSinceStartup + (step == 2 ? 12 : .5);
        }
        catch (Exception error)
        {
            SessionState.SetBool(Key, false);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
