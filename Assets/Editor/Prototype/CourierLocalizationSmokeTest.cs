using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierLocalizationSmokeTest
{
    private const string Key = "CourierLocalizationSmoke.Active";
    private static int step, originalPreference;
    private static double next, deadline;
    private static SettingPanel settings;
    static CourierLocalizationSmokeTest() { EditorApplication.update += Tick; }
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<SettingPanel>("Assets/Resources/Panel/SettingPanel.prefab");
        Require(prefab.transform.Find("BG/LanguageSelector") != null, "Language row must be authored in the prefab");
        var serialized = new SerializedObject(prefab);
        foreach (var field in new[] { "previousLanguageButton", "currentLanguageButton", "nextLanguageButton", "languageValue" })
            Require(serialized.FindProperty(field).objectReferenceValue != null, "Unassigned prefab reference: " + field);
        Require(prefab.GetComponentsInChildren<CourierLanguageSelector>(true).Length == 3, "Keyboard selectors must be serialized");
        originalPreference = PlayerPrefs.GetInt(CourierLocalization.PreferenceKey, -1);
        SessionState.SetInt(Key + ".Previous", originalPreference);
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    private static void Require(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        if (deadline == 0) { deadline = EditorApplication.timeSinceStartup + 120; next = EditorApplication.timeSinceStartup + 5; }
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Localization timed out at step " + step);
            if (EditorApplication.timeSinceStartup < next) return;
            var owner = UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            var hud = UnityEngine.Object.FindObjectOfType<CourierHudPanel>();
            Require(owner != null && hud != null, "Gameplay did not initialize");
            if (step == 0)
            {
                VerifyMissingReferenceRecovery();
                VerifyFormattedSwitching();
                owner.Run.Node = 0; owner.BeginConversation();
                CourierLocalization.SetLanguage(CourierLanguage.SimplifiedChinese);
                Require(CourierLocalization.T("Spring Mead") == "春蜜酒", "Parcel translation missing");
                var story = JsonUtility.FromJson<CourierStoryData>(Resources.Load<TextAsset>("NightVoyage/Story").text);
                foreach (var scene in story.scenes)
                foreach (var line in scene.lines)
                {
                    Require(CourierLocalization.T(line.text) == line.sourceChinese, "Story mismatch: " + scene.id);
                    Require(!string.IsNullOrWhiteSpace(line.sourceChinese), "Missing Chinese story");
                }
                var catalog = Resources.Load<TextAsset>("NightVoyage/Localization/Chinese").text;
                var font = CourierTypography.Chinese;
                Require(font != null, "Bundled Chinese font missing");
                foreach (char ch in catalog.Distinct().Where(ch => ch >= 0x3400 && ch <= 0x9fff))
                    Require(font.HasCharacter(ch), "Missing font glyph: " + ch);
                GameManager.Instance.PauseGame();
                settings = UIManager.Instance.OpenPanel("SettingPanel", "PausePanel", UIPanelLayer.Popup) as SettingPanel;
            }
            else if (step == 1)
            {
                Require(hud.subtitleText.text.Contains("苍松谷"), "Active dialogue did not switch to simplified Chinese: " + hud.subtitleText.text);
                Capture("/tmp/courier-settings-hans.png");
                Require(hud.subtitleText.font == CourierTypography.Chinese, "Dialogue font was not refreshed");
                Require(hud.subtitleText.preferredHeight <= hud.subtitleText.rectTransform.rect.height + 1, "Chinese dialogue clipped");
                Require(settings != null, "Settings missing");
                Click(settings.transform.Find("BG/LanguageSelector/NextLanguage").GetComponent<Button>());
                Require(CourierLocalization.Language == CourierLanguage.TraditionalChinese, "Right button did not cycle");
                Require(PlayerPrefs.GetInt(CourierLocalization.PreferenceKey) == 2, "Language was not persisted");
            }
            else if (step == 2)
            {
                Require(hud.subtitleText.text.Contains("蒼松谷"), "Active dialogue did not switch to traditional Chinese");
                Capture("/tmp/courier-settings-hant.png");
                var center = settings.transform.Find("BG/LanguageSelector/CurrentLanguage");
                center.GetComponent<CourierLanguageSelector>().OnMove(new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Right });
                Require(CourierLocalization.Language == CourierLanguage.English, "Keyboard right did not wrap");
            }
            else if (step == 3)
            {
                Require(hud.subtitleText.text == owner.Subtitle, "English source was not restored");
                Require(hud.subtitleText.font != CourierTypography.Chinese, "English font was not restored");
                var center = settings.transform.Find("BG/LanguageSelector/CurrentLanguage");
                center.GetComponent<CourierLanguageSelector>().OnMove(new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Left });
                Require(CourierLocalization.Language == CourierLanguage.TraditionalChinese, "Keyboard left did not wrap");
                Click(settings.transform.Find("BG/LanguageSelector/PreviousLanguage").GetComponent<Button>());
                Require(CourierLocalization.Language == CourierLanguage.SimplifiedChinese, "Left button did not cycle");
                UIManager.Instance.ClosePanel("SettingPanel"); GameManager.Instance.ResumeGame();
            }
            else if (step == 4)
            {
                Capture("/tmp/courier-dialogue-hans.png");
                CourierLocalization.SetLanguage(CourierLanguage.TraditionalChinese);
            }
            else if (step == 5)
            {
                Capture("/tmp/courier-dialogue-hant.png");
            }
            else if (step == 6)
            {
                CourierLocalization.SetLanguage(CourierLanguage.English);
                Debug.Log("COURIER_LOCALIZATION PASS: full story, glyph coverage, live dialogue, paused settings, both arrows, keyboard, wrap, persistence and font restoration.");
                Finish(0); return;
            }
            step++; next = EditorApplication.timeSinceStartup + 1;
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }
    private static void VerifyFormattedSwitching()
    {
        var go = new GameObject("FormattedLocalizationProbe", typeof(RectTransform), typeof(Text));
        try
        {
            var label = go.GetComponent<Text>(); var formatter = Resources.Load<CourierHintView>("Feedback/CourierHint");
            CourierLocalization.SetFormatted(label, "Cassette Tape\nLate by 2 turn(s)", formatter.FormatMessage);
            CourierLocalization.SetLanguage(CourierLanguage.SimplifiedChinese);
            Require(label.text.Contains("录音带"), "Open tooltip failed to switch to simplified Chinese");
            Require(!CourierLocalization.T("Ghost · no protection · 5 turns").Contains("·"), "Composed Chinese text retained middle dots");
            Require(CourierLocalization.T("Preparing your next stop...") == "正在加载……", "Loading copy was not updated");
            Require(CourierLocalization.T("Delete the save in slot 3?") == "要删除第 3 个存档吗？", "Dynamic prompt has incorrect word order");
            Require(CourierLocalization.T("Order turns left: 2") == "还剩 2 回合", "Countdown lacks its unit");
            CourierLocalization.SetLanguage(CourierLanguage.TraditionalChinese);
            Require(label.text.Contains("錄音帶"), "Open tooltip failed to switch to traditional Chinese");
            Require(!CourierLocalization.T("Ghost · no protection · 5 turns").Contains("·"), "Composed traditional Chinese retained middle dots");
            CourierLocalization.Apply(go);
            CourierLocalization.SetLanguage(CourierLanguage.English);
            Require(label.text.Contains("Cassette Tape") && label.text.Contains("Late by"), "Formatted tooltip lost its English source");
            Require(CourierLocalization.T("Ghost · no protection · 5 turns") == "Ghost · no protection · 5 turns", "English punctuation changed");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    private static void VerifyMissingReferenceRecovery()
    {
        var parent = new GameObject("SettingsReferenceRecoveryProbe");
        parent.SetActive(false);
        try
        {
            var clone = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Panel/SettingPanel"), parent.transform);
            var panel = clone.GetComponent<SettingPanel>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var names = new[] { "previousLanguageButton", "currentLanguageButton", "nextLanguageButton", "languageValue" };
            foreach (var name in names) typeof(SettingPanel).GetField(name, flags).SetValue(panel, null);
            var languageRow = clone.transform.Find("BG/LanguageSelector");
            int childCount = languageRow.GetComponentsInChildren<Transform>(true).Length;
            parent.SetActive(true);
            foreach (var name in names)
                Require(typeof(SettingPanel).GetField(name, flags).GetValue(panel) != null, "Failed to recover authored control: " + name);
            Require(childCount == languageRow.GetComponentsInChildren<Transform>(true).Length, "Reference recovery generated language UI");
            var original = CourierLocalization.Language;
            clone.transform.Find("BG/LanguageSelector/NextLanguage").GetComponent<Button>().onClick.Invoke();
            Require(CourierLocalization.Language != original, "Recovered language control has no listener");
            CourierLocalization.SetLanguage(original);
        }
        finally { UnityEngine.Object.DestroyImmediate(parent); }
    }
    private static void Click(Button button)
    {
        var rect = (RectTransform)button.transform;
        var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var data = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        Require(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button,
            "Language button blocked from pointer input: " + button.name + " by " + (hits.Count > 0 ? hits[0].gameObject.name : "no hit"));
        ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
    }
    private static void Capture(string path)
    {
        var panel = UnityEngine.Object.FindObjectOfType<CourierHudPanel>();
        var canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
        var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float distance = canvas.planeDistance;
        var go = new GameObject("LocalizationPreviewCamera", typeof(Camera)); var camera = go.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = CourierColors.Night; camera.orthographic = true;
        var target = new RenderTexture(1920, 1080, 24); camera.targetTexture = target; var old = RenderTexture.active;
        var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
            Canvas.ForceUpdateCanvases(); panel.CompleteSubtitle(); Canvas.ForceUpdateCanvases();
            camera.Render(); RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = distance; RenderTexture.active = old;
            camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(texture); target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(go); Canvas.ForceUpdateCanvases();
        }
    }
    private static void Finish(int code)
    {
        int old = SessionState.GetInt(Key + ".Previous", -1);
        if (old < 0) PlayerPrefs.DeleteKey(CourierLocalization.PreferenceKey); else PlayerPrefs.SetInt(CourierLocalization.PreferenceKey, old);
        PlayerPrefs.Save(); SessionState.SetBool(Key, false); EditorApplication.Exit(code);
    }
}
