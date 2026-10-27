# SaveManager 使用说明

`SaveManager` 是一个独立的 JSON 存档模块。它不会自动接入 `GameManager`、`UIManager` 或 `SettingPanel`，只有在场景中挂载并主动调用时才会生效。

## 场景接入

1. 在管理器物体上挂载 `SaveManager`。
2. 如需启动时自动读取存档，勾选 `Load On Awake`。
3. 默认保存目录为 `Application.persistentDataPath/Saves`。
4. 每个槽位是独立 JSON 文件，例如 `save_slot_01.json`。

## 可选 UI

项目内已提供 `SavePanel` 预制体：

```csharp
UIManager.Instance.OpenPanel("SavePanel");
```

这个面板通过现有 `UIManager` 加载，路径已登记在 `UIDataListSO` 中。面板上的槽位列表、元数据文本和按钮都直接摆在 prefab 里，运行时脚本只负责读取引用和响应点击。

## 常用 API

```csharp
SaveManager.Instance.NewGame();
SaveManager.Instance.LoadGame();
SaveManager.Instance.SaveGame();
SaveManager.Instance.DeleteSave();
bool hasSave = SaveManager.Instance.HasSave();
```

指定槽位：

```csharp
SaveManager.Instance.NewGame(0);
SaveManager.Instance.SaveGame(0);
SaveManager.Instance.LoadGame(0);
SaveManager.Instance.DeleteSave(0);
SaveSlotMeta meta = SaveManager.Instance.GetSlotMeta(0);
```

## 修改数据

```csharp
SaveData data = SaveManager.Instance.CurrentData;
data.coin += 100;
data.currentLevel = 2;

SaveManager.Instance.SaveGame();
```

## 对话运行状态

`SaveManager.SaveGame()` 会自动把 `DialogueRuntimeState` 中的对话标记、临时物品、任务状态和数字变量写入 `SaveData.dialogue`。

`SaveManager.LoadGame()` 会恢复这些对话状态，`NewGame()`、读取失败、删除当前槽位或没有可读存档时会清空它们，避免上一局留下的分支状态影响新游戏。

`GamePlay` 场景里的存档测试面板提供了一个示例：点击“测试对话存档”打开带分支效果的对话，可以依次测试“接取任务”“未找到项链”“模拟找到项链”“交付完成”“完成后再次询问”等状态。状态栏会显示任务、项链、好感和魅力变量，再用“保存 / 读取”验证这些对话状态是否能随存档恢复。

## 监听事件

```csharp
SaveManager.Instance.SaveCompleted += OnSaveCompleted;

void OnSaveCompleted(SaveData data)
{
    Debug.Log("Saved at " + data.lastSaveTime);
}
```

## 扩展建议

`SaveData` 是模板数据结构，可以按项目需要增加字段。为了保持模板低耦合，除内置的轻量对话运行状态外，建议业务系统自己决定何时读取和写入数据。

设置项、音量、分辨率、输入绑定等更适合继续使用 `PlayerPrefs` 或平台设置存储，不建议混入游戏进度存档。
