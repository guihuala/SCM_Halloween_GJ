using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Batch entry point: -executeMethod GameFlowSmokeTest.Run (without -quit).
// Uses real scenes/prefabs and does not write or delete player saves.
[InitializeOnLoad]
public static class GameFlowSmokeTest
{
    private const string Active = "GameFlowSmokeTest.Active";
    private static int step;
    private static double next;
    private static double deadline;
    private static int previousGame;

    static GameFlowSmokeTest()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        VerifyDialogueContent();
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.isPlaying = true;
    }

    // One-time migration entry point: export the migrated text-bearing asset through
    // the project's own Excel exporter, then run the normal regression suite.
    public static void MigrateDialogueSourceAndRun()
    {
        var table = AssetDatabase.LoadAssetAtPath<DialogueTable>("Assets/Resources/ConfigTables/DialogueTable.asset");
        DialogueExcelUtility.ExportXlsx(table, "Assets/Config/Dialogue/DialogueTable.xlsx");
        AssetDatabase.Refresh();
        Run();
    }

    private static void VerifyDialogueContent()
    {
        var table = AssetDatabase.LoadAssetAtPath<DialogueTable>("Assets/Resources/ConfigTables/DialogueTable.asset");
        var sheet = ExcelTableReader.ReadFirstSheet("Assets/Config/Dialogue/DialogueTable.xlsx");
        int textColumn = sheet.Rows[0].IndexOf("text");
        Require(textColumn >= 0 && !sheet.Rows[0].Contains("localizationKey"), "Dialogue source schema not migrated");
        Require(sheet.Rows.Count == table.lines.Count + 1, "Dialogue source rows lost");
        foreach (var line in table.lines)
        {
            Require(!string.IsNullOrWhiteSpace(line.text), "Empty dialogue text");
            var source = sheet.Rows.Find(row => row[0] == line.dialogueId && row[1] == line.lineIndex.ToString());
            Require(source != null && source[textColumn] == line.text, "Dialogue source text mismatch");
            for (int option = 0; option < line.options.Count; option++)
            {
                int col = sheet.Rows[0].IndexOf($"option{option + 1}Text");
                Require(col >= 0 && source[col] == line.options[option].text, "Dialogue option text lost");
                Require(source[col + 1] == (line.options[option].nextDialogueId ?? ""), "Dialogue target lost");
                Require(source[col + 2] == (line.options[option].condition ?? ""), "Dialogue condition lost");
                Require(source[col + 3] == (line.options[option].effects ?? ""), "Dialogue effects lost");
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Click(string panel, string button)
    {
        var root = UIManager.Instance.GetPanel(panel);
        Require(root != null, "Missing panel: " + panel);
        foreach (var item in root.GetComponentsInChildren<Button>())
            if (item.name == button) { item.onClick.Invoke(); return; }
        throw new Exception("Missing button: " + button);
    }

    private static readonly System.Collections.Generic.HashSet<int> captured = new System.Collections.Generic.HashSet<int>();
    private static bool Capture(string name)
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-flowScreenshots") < 0 || !captured.Add(step)) return false;
        ScreenCapture.CaptureScreenshot("/tmp/scm-" + name + ".png");
        next = EditorApplication.timeSinceStartup + 0.3;
        return true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying) return;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 90;
        if (EditorApplication.timeSinceStartup > deadline) { Finish(false, "Timed out at step " + step); return; }
        if (EditorApplication.timeSinceStartup < next) return;
        if (SceneLoader.Instance == null || SceneLoader.Instance.IsLoading()) return;
        try
        {
            var game = GameManager.Instance;
            switch (step)
            {
                case 0:
                    Require(UnityEngine.Object.FindObjectOfType<TitleUIController>() != null, "Menu missing");
                    Require(InputManager.Instance != null, "Input manager missing");
                    UnityEngine.Object.FindObjectOfType<TitleUIController>().OnNewGameButtonClicked();
                    break;
                case 1:
                    Require(SceneManager.GetActiveScene().name == "GamePlay", "Start did not load gameplay");
                    Require(GameObject.Find("SaveTestPanel") == null, "Demo UI still present");
                    Require(!UIManager.Instance.IsPanelOpen("SavePanel"), "Start forced save selection");
                    Require(game != null && Time.timeScale == 1, "Game did not start");
                    game.PauseGame();
                    break;
                case 2:
                    Require(game.CurrentState == GameManager.GameState.Paused && Time.timeScale == 0, "Pause failed");
                    Require(!InputManager.Instance.GameplayInputEnabled, "Gameplay input active while paused");
                    if (Capture("pause")) return;
                    Click("PausePanel", "SettingsButton");
                    break;
                case 3:
                    Require(UIManager.Instance.GetPanel("SettingPanel") is SettingPanel, "Settings component missing");
                    var settings = UIManager.Instance.GetPanel("SettingPanel");
                    Require(settings.GetComponentsInChildren<Slider>(true).Length == 2, "Expected only two volume sliders");
                    Require(settings.GetComponentsInChildren<Button>(true).Length == 1, "Unexpected settings buttons");
                    Require(settings.GetComponentsInChildren<Dropdown>(true).Length == 0 && settings.GetComponentsInChildren<Toggle>(true).Length == 0, "Video settings still present");
                    if (Capture("settings")) return;
                    UIManager.Instance.ClosePanel("SettingPanel");
                    Click("PausePanel", "ResumeButton");
                    break;
                case 4:
                    Require(Time.timeScale == 1 && InputManager.Instance.GameplayInputEnabled, "Resume failed");
                    game.WinGame();
                    break;
                case 5:
                    Require(Time.timeScale == 0 && game.CurrentState == GameManager.GameState.GameOver, "Win did not stop game");
                    Require(UIManager.Instance.GetPanel("GameResultPanel").GetComponentInChildren<Text>().text == "挑战成功", "Win title incorrect");
                    if (Capture("result")) return;
                    previousGame = game.GetInstanceID();
                    Click("GameResultPanel", "RestartButton");
                    break;
                case 6:
                    Require(game.GetInstanceID() != previousGame && Time.timeScale == 1, "Win restart failed");
                    Require(!UIManager.Instance.IsPanelOpen("GameResultPanel"), "Stale result panel cache");
                    game.LoseGame();
                    break;
                case 7:
                    Require(UIManager.Instance.GetPanel("GameResultPanel").GetComponentInChildren<Text>().text == "游戏结束", "Lose title incorrect");
                    Click("GameResultPanel", "MainMenuButton");
                    break;
                case 8:
                    Require(SceneManager.GetActiveScene().name == "MainMenu" && Time.timeScale == 1, "Return to menu failed");
                    Require(UnityEngine.Object.FindObjectsOfType<AudioManager>().Length == 1, "Duplicate audio managers");
                    Require(UnityEngine.Object.FindObjectsOfType<InputManager>().Length == 1, "Duplicate input managers");
                    UnityEngine.Object.FindObjectOfType<TitleUIController>().OnNewGameButtonClicked();
                    break;
                case 9:
                    previousGame = game.GetInstanceID();
                    game.PauseGame();
                    break;
                case 10:
                    Click("PausePanel", "RestartButton");
                    break;
                case 11:
                    Require(game.GetInstanceID() != previousGame && Time.timeScale == 1, "Paused restart failed");
                    // Optional TimeManager must no longer overwrite pause on subsequent frames.
                    new GameObject("SmokeTimeManager").AddComponent<TimeManager>();
                    TimeManager.Instance.SetTimeFactor(0.5f);
                    game.PauseGame();
                    break;
                case 12:
                    Require(Time.timeScale == 0, "TimeManager overwrote pause");
                    game.ResumeGame();
                    Require(Time.timeScale == 0.5f, "Slow motion not restored");
                    TimeManager.Instance.SetTimeFactor(1f);
                    // StopSfx must touch only SFX, even when BGM shares the name.
                    var bgm = new GameObject("SmokeBGM").AddComponent<AudioSource>();
                    var sfx = new GameObject("SmokeSFX").AddComponent<AudioSource>();
                    AudioManager.Instance.bgmAudioInfoList.Add(new AudioInfo { audioName = "smoke", audioSource = bgm });
                    AudioManager.Instance.sfxAudioInfoList.Add(new AudioInfo { audioName = "smoke", audioSource = sfx });
                    AudioManager.Instance.StopSfx("smoke");
                    Require(AudioManager.Instance.bgmAudioInfoList.Exists(x => x.audioName == "smoke"), "StopSfx removed BGM");
                    Require(!AudioManager.Instance.sfxAudioInfoList.Exists(x => x.audioName == "smoke"), "StopSfx did not remove SFX");
                    game.PauseGame();
                    break;
                case 13:
                    Click("PausePanel", "MainMenuButton");
                    break;
                case 14:
                    Require(SceneManager.GetActiveScene().name == "MainMenu" && Time.timeScale == 1, "Paused return failed");
                    Finish(true, "15 steps: direct start, pause, settings, resume, win, restart, lose, menu, repeat start, paused restart, slow motion, SFX isolation");
                    return;
            }
            Debug.Log("FLOW_SMOKE step " + step + " passed");
            step++;
            next = EditorApplication.timeSinceStartup + 0.6;
        }
        catch (Exception ex) { Finish(false, ex.ToString()); }
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (SessionState.GetBool(Active, false) && EditorApplication.isPlaying &&
            (type == LogType.Exception || type == LogType.Error || type == LogType.Assert))
            Finish(false, message + "\n" + stack);
    }

    private static void Finish(bool passed, string message)
    {
        SessionState.SetBool(Active, false);
        Debug.Log("FLOW_SMOKE " + (passed ? "PASS " : "FAIL ") + message);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
