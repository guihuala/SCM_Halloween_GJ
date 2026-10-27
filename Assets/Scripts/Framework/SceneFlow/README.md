# 场景流程配置

现有项目已经有 `SceneLoader`，这个模块只补一层场景配置表，不替代加载器。

## 当前内容

- `SceneCatalog`：场景枚举到真实场景名的映射，位于 `Resources/Data/SceneCatalog.asset`。
- `SceneCatalogProvider.GetSceneName(scene)`：读取配置并返回真实场景名。
- `SceneLoader.LoadScene(GameScene scene)`：现在会先查 `SceneCatalog`，找不到时回退到 `scene.ToString()`。

## 当前场景

- `MainMenu` -> `MainMenu`
- `Gameplay` -> `GamePlay`

注意：项目里的游戏场景文件名是 `GamePlay.unity`，而枚举是 `Gameplay`。配置表修正了这个大小写差异。
