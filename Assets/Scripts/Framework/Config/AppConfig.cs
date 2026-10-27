using UnityEngine;

[CreateAssetMenu(fileName = "AppConfig", menuName = "Template/Config/App Config")]
public class AppConfig : ScriptableObject
{
    [Header("Project")]
    public string projectName = "2D Template";

    [Header("Scene Flow")]
    public GameScene initialScene = GameScene.MainMenu;
    public GameScene gameplayScene = GameScene.Gameplay;
}
