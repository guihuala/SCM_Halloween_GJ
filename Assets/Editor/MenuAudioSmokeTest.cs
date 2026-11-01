using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Run in an isolated project: -executeMethod MenuAudioSmokeTest.Run (without -quit).
[InitializeOnLoad]
public static class MenuAudioSmokeTest
{
    private const string Active = "MenuAudioSmokeTest.Active";
    private static int step;
    private static double next, deadline;
    static MenuAudioSmokeTest() { EditorApplication.update += Tick; }

    public static void Run()
    {
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Application.runInBackground = true;
        EditorApplication.isPlaying = true;
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    private static AudioSource Source(MenuAudio audio, string field) =>
        (AudioSource)typeof(MenuAudio).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(audio);

    private static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying) return;
        if (deadline == 0) { deadline = EditorApplication.timeSinceStartup + 90; next = EditorApplication.timeSinceStartup + 3; }
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Menu audio check timed out at " + step);
            var audio = UnityEngine.Object.FindObjectOfType<MenuAudio>();
            Require(audio != null, "Menu audio service missing");
            var music = Source(audio, "music"); var effects = Source(audio, "effects");
            var title = UnityEngine.Object.FindObjectOfType<TitleUIController>();
            switch (step)
            {
                case 0:
                    Require(title != null && music.clip != null && music.clip.length > 10 && music.loop && music.isPlaying,
                        "Menu loop is missing or not playing");
                    Require(music.outputAudioMixerGroup != null && effects.outputAudioMixerGroup != null,
                        "Menu sources do not respect mixer settings");
                    foreach (var button in title.GetComponentsInChildren<Button>(true))
                        Require(button.GetComponents<ButtonAudioFeedback>().Length == 1, "Menu button audio missing/duplicated");
                    title.settingsButton.onClick.Invoke();
                    Require(effects.isPlaying, "Click did not play");
                    break;
                case 1:
                    var panel = UIManager.Instance.GetPanel("SettingPanel");
                    Require(panel != null && music.isPlaying, "Settings stopped menu music");
                    foreach (var button in panel.GetComponentsInChildren<Button>(true))
                        Require(button.GetComponent<ButtonAudioFeedback>() != null, "Popup button audio missing");
                    var control = panel.GetComponentInChildren<Button>();
                    var feedback = control.GetComponent<ButtonAudioFeedback>();
                    effects.Stop(); control.interactable = false;
                    feedback.OnPointerEnter(new PointerEventData(EventSystem.current));
                    Require(!effects.isPlaying, "Disabled button played hover");
                    control.interactable = true;
                    feedback.OnPointerEnter(new PointerEventData(EventSystem.current));
                    Require(effects.isPlaying, "Hover did not play");
                    UIManager.Instance.ClosePanel("SettingPanel");
                    break;
                case 2:
                    title.OnNewGameButtonClicked();
                    break;
                case 3:
                    if (SceneLoader.Instance.IsLoading()) { next = EditorApplication.timeSinceStartup + .3; return; }
                    Require(!music.isPlaying, "Menu music overlaps gameplay");
                    Require(UnityEngine.Object.FindObjectsOfType<MenuAudio>().Length == 1, "Duplicate persistent audio");
                    GameManager.Instance.ReturnToMainMenu();
                    break;
                case 4:
                    if (SceneLoader.Instance.IsLoading()) { next = EditorApplication.timeSinceStartup + .3; return; }
                    Require(title != null && music.isPlaying && UnityEngine.Object.FindObjectsOfType<MenuAudio>().Length == 1,
                        "Returning to menu failed to resume a single loop");
                    SessionState.SetBool(Active, false);
                    Debug.Log("MENU_AUDIO PASS: music, mixer routing, menu/popup hover and click, disabled controls, gameplay handoff, return to menu");
                    EditorApplication.Exit(0);
                    return;
            }
            step++; next = EditorApplication.timeSinceStartup + 2;
        }
        catch (Exception exception)
        {
            SessionState.SetBool(Active, false);
            Debug.LogException(exception); EditorApplication.Exit(1);
        }
    }
}
