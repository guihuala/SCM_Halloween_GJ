using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public enum GameScene
{
    MainMenu = 0,   // 主菜单
    Gameplay = 1,   // 游戏场景
}

public class SceneLoader : SingletonPersistent<SceneLoader>
{
    [Header("加载界面设置")]
    [SerializeField] private string loadingScreenResourcePath = "Panel/LoadingScreen";
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float minLoadingTime = 0f;

    private AsyncOperation loadingOperation;
    private bool isLoading = false;
    private LoadingScreen loadingScreen;

    /// <summary>
    /// 加载指定枚举场景
    /// </summary>
    public void LoadScene(GameScene scene, Action onComplete = null)
    {
        if (isLoading) return;
        
        string sceneName = SceneCatalogProvider.GetSceneName(scene);
        StartCoroutine(LoadSceneRoutine(sceneName, onComplete));
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene(Action onComplete = null)
    {
        if (isLoading) return;
        
        string currentScene = SceneManager.GetActiveScene().name;
        StartCoroutine(LoadSceneRoutine(currentScene, onComplete));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, Action onComplete)
    {
        isLoading = true;
        var prefab = Resources.Load<GameObject>(loadingScreenResourcePath);
        var loadingScreenPrefab = prefab != null ? prefab.GetComponent<LoadingScreen>() : null;
        if (loadingScreenPrefab != null)
        {
            // Only create the overlay during transitions, under the persistent manager.
            loadingScreen = Instantiate(loadingScreenPrefab, transform, false);
            loadingScreen.SetAlpha(0f);
            loadingScreen.SetProgress(0f);
        }
        if (DialogueManager.Instance != null) DialogueManager.Instance.Stop();
        if (UIManager.Instance != null) UIManager.Instance.CloseAllPanelsImmediate();
        if (InputManager.Instance != null) InputManager.Instance.SetGameplayInputEnabled(false);
        float startTime = Time.realtimeSinceStartup;

        // 淡入加载界面
        yield return StartCoroutine(FadeLoadingScreen(0f, 1f));

        // 开始异步加载场景
        loadingOperation = SceneManager.LoadSceneAsync(sceneName);
        loadingOperation.allowSceneActivation = false;

        // 更新加载进度
        while (!loadingOperation.isDone)
        {
            float progress = Mathf.Clamp01(loadingOperation.progress / 0.9f);
            UpdateProgressUI(progress);

            // 确保最小加载时间，然后激活场景
            if (loadingOperation.progress >= 0.9f && 
                Time.realtimeSinceStartup - startTime >= minLoadingTime)
            {
                Time.timeScale = 1f;
                loadingOperation.allowSceneActivation = true;
            }

            yield return null;
        }

        // 等待一帧确保场景完全加载
        yield return null;

        if (loadingScreen != null)
        {
            loadingScreen.SetProgress(1f);
            while (!loadingScreen.IsProgressComplete) yield return null;
        }

        // 淡出加载界面
        yield return StartCoroutine(FadeLoadingScreen(1f, 0f));

        if (loadingScreen != null)
        {
            Destroy(loadingScreen.gameObject);
            loadingScreen = null;
        }

        isLoading = false;
        if (InputManager.Instance != null) InputManager.Instance.SetGameplayInputEnabled(true);
        onComplete?.Invoke();
    }

    private IEnumerator FadeLoadingScreen(float startAlpha, float targetAlpha)
    {
        if (loadingScreen == null) yield break;

        loadingScreen.SetAlpha(startAlpha);
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            loadingScreen.SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration));
            yield return null;
        }

        loadingScreen.SetAlpha(targetAlpha);
    }

    private void UpdateProgressUI(float progress)
    {
        if (loadingScreen != null) loadingScreen.SetProgress(progress);
    }

    /// <summary>
    /// 检查是否正在加载
    /// </summary>
    public bool IsLoading() => isLoading;
}
