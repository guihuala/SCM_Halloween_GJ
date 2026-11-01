using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Exercise the shipped views and content, rather than a second mock UI.
[InitializeOnLoad]
public static class CourierLanguageAudit
{
    const string Key = "CourierLanguageAudit.Active", Output = "/tmp/courier-language-audit";
    static int phase, measured, captures;
    static double next;
    static NightCourierPrototype owner;
    static readonly List<string> findings = new List<string>();
    static readonly List<string> screens = new List<string>();
    static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static CourierLanguageAudit() { EditorApplication.update += Tick; }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        SessionState.SetInt(Key + ".Preference", PlayerPrefs.GetInt(CourierLocalization.PreferenceKey, -1));
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        if (next == 0) next = EditorApplication.timeSinceStartup + 4;
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (phase == 0)
            {
                var menu = UnityEngine.Object.FindObjectOfType<TitleUIController>();
                Matrix("main-menu", menu.gameObject, null);
                foreach (var path in new[] { "SettingPanel", "PausePanel", "CreditPanel", "ConfirmPanel", "GameResultPanel", "LoadingScreen" })
                {
                    var prefab = Resources.Load<GameObject>("Panel/" + path);
                    var view = path == "LoadingScreen" ? UnityEngine.Object.Instantiate(prefab) : UnityEngine.Object.Instantiate(prefab, UIManager.Instance.UIRoot, false);
                    if (path == "ConfirmPanel") view.GetComponent<ConfirmPanel>().Configure("Delete Save", "Delete the save in slot 3?", null);
                    if (path == "GameResultPanel") view.GetComponent<GameResultPanel>().Configure(false);
                    if (path == "LoadingScreen") { view.GetComponent<LoadingScreen>().SetAlpha(1); view.GetComponent<LoadingScreen>().SetProgress(.37f, true); }
                    Matrix(path, view, null);
                    UnityEngine.Object.DestroyImmediate(view);
                }
                // No SavePanel prefab is shipped; audit its authored strings separately.
                EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/GamePlay.unity", new LoadSceneParameters(LoadSceneMode.Single));
                phase++; next = EditorApplication.timeSinceStartup + 5; return;
            }
            owner = NightCourierPrototype.Current;
            if (owner == null) throw new Exception("Gameplay owner missing");
            owner.enabled = false; Time.timeScale = 0;
            owner.Tutorial.Dismiss();
            foreach (var p in UnityEngine.Object.FindObjectsOfType<CourierPanel>()) p.gameObject.SetActive(false);
            var route = UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            var hud = UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
            var tutorial = UIManager.Instance.GetPanel("CourierTutorialPanel") as CourierTutorialPanel;
            if (phase == 1)
            {
                owner.Run.State = ShiftState.Docked; owner.Run.Node = 1;
                SetDialogue(new string[0]);
                route.gameObject.SetActive(true); var routeSettings = new SerializedObject(route); routeSettings.FindProperty("reducedPanelMotion").boolValue = true; routeSettings.ApplyModifiedPropertiesWithoutUndo();
                Matrix("map-empty", route.gameObject, () => Refresh(route));
                route.SetCargoOpen(true);
                Matrix("orders-waiting", route.gameObject, () => Refresh(route));
                foreach (var p in owner.Run.Parcels.Where(p => p.Enabled)) p.State = ParcelState.Loaded;
                Matrix("orders-loaded", route.gameObject, () => Refresh(route));
                route.SetCargoOpen(false); route.Select(4);
                Matrix("route-preview", route.gameObject, () => { Refresh(route); route.PreviewRoute(4); });
                owner.Run.PreferSafeRoutes = false;
                Matrix("route-fast", route.gameObject, () => { Refresh(route); route.PreviewRoute(4); });
                var hint = route.loadText.GetComponent<CourierIconHint>();
                var tip = route.GetComponent<CourierTooltip>();
                Matrix("order-tooltip", route.gameObject, () => { Refresh(route); tip.Show(hint, true); tip.SendMessage("LateUpdate"); });
                tip.Hide(hint);
                var feedback = route.FriendlyFeedback;
                feedback.Show(route.loadText.rectTransform, "Order turns left: 2", CourierIconKind.Warning, 99, false);
                Matrix("clock-warning", route.gameObject, () => Refresh(route)); feedback.Clear();
                foreach (var reason in new[] { "Finish the conversation first.", "Close the guide first.", "Wait until you arrive.", "Visit the sender to collect this order.", "This order cannot be collected now." })
                    CheckTranslation("denied-action", reason);
                route.gameObject.SetActive(false);
                foreach (var name in new[] { "CourierCargoPanel", "CourierVehiclePanel" })
                {
                    var view = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Panel/Courier/" + name), UIManager.Instance.UIRoot, false);
                    var panel = view.GetComponent<CourierPanel>(); panel.Bind(owner);
                    Matrix(name, view, () => Refresh(panel)); UnityEngine.Object.DestroyImmediate(view);
                }
                owner.Tutorial.ShowGuide(); tutorial.gameObject.SetActive(true);
                Matrix("delivery-guide", tutorial.gameObject, () => tutorial.view.Refresh());
                tutorial.gameObject.SetActive(false); owner.Tutorial.Dismiss();
                phase++; next = EditorApplication.timeSinceStartup + .1; return;
            }
            if (phase == 2)
            {
                owner.Run.State = ShiftState.Docked; hud.gameObject.SetActive(true); hud.reducedMotion = true;
                // Every manuscript line, including the longest speeches with a portrait.
                foreach (var language in new[] { CourierLanguage.English, CourierLanguage.SimplifiedChinese, CourierLanguage.TraditionalChinese })
                {
                    CourierLocalization.SetLanguage(language);
                    foreach (var scene in owner.Run.Story.Data.scenes)
                    foreach (var line in scene.lines)
                    {
                        SetDialogue(new[] { line.speaker + ": " + line.text }); Refresh(hud); hud.CompleteSubtitle();
                        CheckText("dialogue/" + scene.id, hud.subtitleText);
                    }
                }
                var longest = owner.Run.Story.Data.scenes.SelectMany(s => s.lines).OrderByDescending(l => l.text.Length).First();
                SetDialogue(new[] { longest.speaker + ": " + longest.text });
                Matrix("long-dialogue", hud.gameObject, () => { Refresh(hud); hud.CompleteSubtitle(); });
                SetDialogue(new string[0]); hud.gameObject.SetActive(false);
                var events = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Panel/Courier/CourierEventPanel"), UIManager.Instance.UIRoot, false);
                var eventPanel = events.GetComponent<CourierEventPanel>();
                owner.Run.EncounterIndex = 0; eventPanel.Bind(owner);
                for (int i = 0; i < owner.Run.Events.Length; i++)
                {
                    owner.Run.EncounterIndex = i;
                    Matrix("event-" + i, events, () => Refresh(eventPanel), i == 2 || i == 10);
                }
                UnityEngine.Object.DestroyImmediate(events);
                owner.Run.Results.Clear();
                foreach (var group in owner.Run.Parcels.Where(p => p.Enabled).GroupBy(p => p.Origin + ":" + p.Destination))
                {
                    var p = group.First(); int i = owner.Run.Results.Count;
                    owner.Run.Results.Add(new DeliveryResult { Origin = p.Origin, Destination = p.Destination, Cargo = string.Join(" + ", group.Select(x => x.Name)),
                        Turns = 8 + i, TargetTurns = 10, Late = i > 2, Damaged = i % 2 == 1, DamageReason = "heat" });
                }
                var result = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Panel/Courier/CourierResultPanel"), UIManager.Instance.UIRoot, false);
                var resultPanel = result.GetComponent<CourierResultPanel>(); resultPanel.Bind(owner);
                Matrix("delivery-results", result, () => Refresh(resultPanel));
                resultPanel.orderHistory.verticalNormalizedPosition = 0;
                Matrix("delivery-results-bottom", result, () => Refresh(resultPanel));
                UnityEngine.Object.DestroyImmediate(result);
                Finish(0); return;
            }
        }
        catch (Exception e) { Debug.LogException(e); findings.Add("EXCEPTION\t" + e); Finish(1); }
    }
    static void SetDialogue(string[] lines)
    {
        typeof(NightCourierPrototype).GetField("dialogue", Private).SetValue(owner, lines);
        typeof(NightCourierPrototype).GetField("dialogueIndex", Private).SetValue(owner, 0);
    }
    static void Refresh(CourierPanel panel) { panel.GetType().GetMethod("Refresh", Private).Invoke(panel, null); }
    static void Matrix(string name, GameObject view, Action refresh, bool capture = true)
    {
        foreach (var width in new[] { 1920, 1440 })
        foreach (var language in new[] { CourierLanguage.English, CourierLanguage.SimplifiedChinese, CourierLanguage.TraditionalChinese, CourierLanguage.English })
        {
            CourierLocalization.SetLanguage(language);
            Render(name, view, refresh, width, width == 1920 ? 1080 : 900, capture);
        }
        screens.Add(name);
    }
    static void Render(string name, GameObject view, Action refresh, int width, int height, bool capture)
    {
        var canvas = view.GetComponentInParent<Canvas>().rootCanvas;
        var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float distance = canvas.planeDistance;
        var go = new GameObject("LanguageAuditCamera", typeof(Camera)); var camera = go.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = CourierColors.Night; camera.orthographic = true;
        var rt = new RenderTexture(width, height, 24); camera.targetTexture = rt; var oldRT = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
            var scaler = canvas.GetComponent<CanvasScaler>(); if (scaler != null) typeof(CanvasScaler).GetMethod("Handle", Private).Invoke(scaler, null);
            Canvas.ForceUpdateCanvases(); refresh?.Invoke();
            var route = view.GetComponent<CourierRoutePanel>(); if (route != null)
            {
                route.SendMessage("LateUpdate");
                var risks = route.GetComponent<CourierRouteRiskView>(); if (risks != null) risks.SendMessage("LateUpdate");
            }
            Canvas.ForceUpdateCanvases();
            foreach (var group in view.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1;
            foreach (var label in view.GetComponentsInChildren<Text>(true))
                if (label.isActiveAndEnabled) CheckText(name + "/" + width, label);
            foreach (var hint in view.GetComponentsInChildren<CourierIconHint>(true))
                if (hint.isActiveAndEnabled) CheckTranslation(name + "/hint/" + hint.name, hint.message);
            if (view.GetComponent<LoadingScreen>() != null)
            {
                var fields = new SerializedObject(view.GetComponent<LoadingScreen>());
                var status = (fields.FindProperty("statusText").objectReferenceValue as Text).rectTransform;
                var percent = (fields.FindProperty("progressText").objectReferenceValue as Text).rectTransform;
                var mask = fields.FindProperty("progressMask").objectReferenceValue as RectMask2D;
                var bar = mask.transform.parent as RectTransform;
                if (Bounds(status).Overlaps(Bounds(bar)) || Bounds(percent).Overlaps(Bounds(bar)) || Bounds(status).Overlaps(Bounds(percent)))
                    findings.Add("OVERLAP\t" + CourierLocalization.Language + "\tLoadingScreen/" + width);
            }
            if (capture)
            {
                camera.Render(); RenderTexture.active = rt; texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                File.WriteAllBytes(Output + "/" + name + "-" + width + "-" + CourierLocalization.Language + ".png", texture.EncodeToPNG()); captures++;
            }
        }
        finally
        {
            canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = distance; RenderTexture.active = oldRT;
            camera.targetTexture = null; if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go); Canvas.ForceUpdateCanvases();
        }
    }
    static Rect Bounds(RectTransform rect)
    {
        var points = new Vector3[4]; rect.GetWorldCorners(points);
        return Rect.MinMaxRect(points[0].x, points[0].y, points[2].x, points[2].y);
    }
    static void CheckText(string screen, Text label)
    {
        if (string.IsNullOrWhiteSpace(label.text)) return;
        measured++; string path = screen + "/" + label.name;
        if (CourierLocalization.Language == CourierLanguage.English && Regex.IsMatch(label.text, @"[\u3400-\u9fff]"))
            findings.Add("STALE_LANGUAGE\tEnglish\t" + path + "\t" + Clean(label.text));
        CheckTranslation(path, label.text, true);
        var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
        if (label.resizeTextForBestFit)
        {
            var generator = new TextGenerator(); generator.Populate(label.text, settings);
            settings.fontSize = generator.fontSizeUsedForBestFit; settings.resizeTextForBestFit = false;
        }
        float preferred = new TextGenerator().GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
        if (preferred > label.rectTransform.rect.height + 2 && label.verticalOverflow == VerticalWrapMode.Truncate)
            findings.Add("OVERFLOW\t" + CourierLocalization.Language + "\t" + path + "\t" + preferred + "/" + label.rectTransform.rect.height + "\t" + Clean(label.text));
    }
    static void CheckTranslation(string screen, string value, bool rendered = false)
    {
        if (string.IsNullOrWhiteSpace(value) || !CourierLocalization.IsChinese) return;
        string translated = rendered ? value : CourierLocalization.T(value);
        if (translated.Contains("·")) findings.Add("SEPARATOR\t" + CourierLocalization.Language + "\t" + screen + "\t" + Clean(translated));
        // The supplied Chinese manuscript deliberately keeps names such as
        // Vampire, Drow, Jiangshi and the word "party". Do not treat verbatim
        // manuscript lines as untranslated interface text.
        if (rendered && owner != null && owner.Run.Story != null &&
            (screen.StartsWith("dialogue/") || screen.EndsWith("/SubtitleText")) &&
            owner.Run.Story.Data.scenes.SelectMany(scene => scene.lines).Any(line => CourierLocalization.T(line.text) == value)) return;
        // The supplied event copy also retains "trick or treat!" and "teenagers".
        if (rendered && owner != null && screen.StartsWith("event-") &&
            owner.Run.Events.Any(e => e.Description != value && CourierLocalization.T(e.Description) == value)) return;
        string cleaned = Regex.Replace(translated, "<[^>]*>", "");
        cleaned = Regex.Replace(cleaned, @"(?<![A-Za-z])(Goolie|Gooy|Gigi|Moku|Ally|Melody|Unity|WASD|Tab|Esc|H|W|Q|E|F)(?![A-Za-z])", "");
        if (Regex.IsMatch(cleaned, "[A-Za-z]{2,}"))
            findings.Add("ENGLISH\t" + CourierLocalization.Language + "\t" + screen + "\t" + Clean(translated));
    }
    static string Clean(string text) => text.Replace("\t", " ").Replace("\n", " / ");
    static void Finish(int code)
    {
        File.WriteAllLines(Output + "/findings.tsv", findings.Distinct());
        File.WriteAllLines(Output + "/screens.txt", screens);
        Debug.Log("COURIER_LANGUAGE_AUDIT: " + measured + " text checks, " + captures + " renders, " + findings.Distinct().Count() + " findings.");
        int pref = SessionState.GetInt(Key + ".Preference", -1);
        if (pref < 0) PlayerPrefs.DeleteKey(CourierLocalization.PreferenceKey); else PlayerPrefs.SetInt(CourierLocalization.PreferenceKey, pref);
        PlayerPrefs.Save(); Time.timeScale = 1; SessionState.SetBool(Key, false); EditorApplication.Exit(code == 0 && findings.Count == 0 ? 0 : 1);
    }
}
