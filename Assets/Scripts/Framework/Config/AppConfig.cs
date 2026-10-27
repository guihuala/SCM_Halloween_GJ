using UnityEngine;

[CreateAssetMenu(fileName = "AppConfig", menuName = "Template/Config/App Config")]
public class AppConfig : ScriptableObject
{
    [Header("Project")]
    public string projectName = "2D Template";

    [Header("Localization")]
    public string defaultLanguage = "zh-CN";
    public string languagePlayerPrefsKey = "template.language";

    [Header("Scene Flow")]
    public GameScene initialScene = GameScene.MainMenu;
    public GameScene gameplayScene = GameScene.Gameplay;
}
