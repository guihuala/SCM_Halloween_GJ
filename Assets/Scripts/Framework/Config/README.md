# 配置系统

这个模块用于放置和读取模板级配置，避免把项目名、默认语言、默认场景等通用参数写死在业务代码里。

## 当前内容

- `AppConfig`：项目级配置，位于 `Resources/Data/AppConfig.asset`。
- `AppConfigProvider`：静态读取入口，通过 `AppConfigProvider.Config` 获取配置。

## 使用建议

- 只放通用模板配置，不放具体游戏玩法数据。
- 运行时设置仍然走 `PlayerPrefs`，不要写进游戏存档。
- 后续如果配置变多，可以按模块拆成多个 `ScriptableObject`，例如 AudioConfig、InputConfig、SceneCatalog。
