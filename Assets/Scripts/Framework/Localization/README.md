# 本地化系统

这个模块提供轻量级文本本地化，适合模板和原型阶段使用。

## 当前内容

- `LocalizationTable`：文本表，位于 `Resources/Data/LocalizationTable.asset`。
- `LocalizationManager.Get(key)`：通过 key 获取当前语言文本。
- `LocalizationManager.SetLanguage(languageCode)`：切换语言，语言选择保存在 `PlayerPrefs`。
- `LocalizedText`：挂在 Unity UI `Text` 上，通过 key 自动刷新。
- `LocalizedImage`：挂在 Unity UI `Image` 上，通过 key 和当前语言切换 Sprite。
- `LocalizationAssetTable`：本地化图片资源表，位于 `Resources/Data/LocalizationAssetTable.asset`。

## Excel 配置

本地化文本的主要编辑入口是 `Assets/Config/Localization.xlsx`。

第一行是表头：

| key | zh-CN | en-US |
| --- | --- | --- |
| ui.save | 存档 | Save |

导入菜单：

- `Tools/Template/Localization/Validate Excel`：检查重复 key、缺失文本、重复语言列。
- `Tools/Template/Localization/Import Excel`：把 Excel 导入到 `Resources/Data/LocalizationTable.asset`。

运行时只读取 `LocalizationTable.asset`，Excel 只作为编辑期配置源。

## 约定

- 本地化设置是用户偏好，保存在 `PlayerPrefs`，不进入游戏存档。
- 找不到 key 时返回 key 本身，便于开发阶段发现漏配文本。
- 资产本地化建议只放“含文字的图片、地区版本图标”等资源；普通无文字 UI 图标不需要进入本地化表。
