# SaveManager 与 InputManager 设计文档

本文档用于在正式编码前约定存档系统和输入系统的设计目标、使用方式与扩展方向。整体原则是保持模板轻量，优先服务 2D 游戏原型快速开发，同时避免后续扩展时推倒重来。

---

## 1. 模块目标

### 1.1 SaveManager

提供统一的游戏数据保存和读取入口，避免各个系统直接散落调用 `PlayerPrefs` 或自行读写文件。

适合保存：

* 玩家进度
* 关卡解锁状态
* 金币、分数、道具数量
* 简单角色数据
* 游戏设置
* 原型阶段的调试数据

不建议直接保存：

* 大型二进制资源
* 截图、录像等媒体文件
* 需要服务器校验的联网游戏关键数据

### 1.2 InputManager

提供统一输入查询入口，避免业务脚本直接散落读取 `Input.GetKey`、`Input.GetAxis` 等 Unity 输入 API。

适合管理：

* 移动输入
* 跳跃、攻击、交互、暂停
* UI 确认、取消
* 鼠标位置、点击
* 后续键位重绑定

---

## 2. SaveManager 设计

### 2.1 推荐目录

建议新增：

```text
Assets/Scripts/Framework/Save/
```

建议文件：

```text
SaveManager.cs
SaveData.cs
ISaveable.cs
```

### 2.2 核心数据结构

`SaveData` 作为全局存档数据容器，初期可以保持简单：

```csharp
[System.Serializable]
public class SaveData
{
    public int version = 1;
    public string lastSaveTime;

    public int coin;
    public int currentLevel;
    public List<string> unlockedLevels = new List<string>();
    public DialogueSaveData dialogue = new DialogueSaveData();
}
```

`dialogue` 保存模板内置的轻量对话运行状态，包括 flags、items、quests 和 numbers。`SaveManager` 在保存前捕获 `DialogueRuntimeState`，读取存档后恢复它，新游戏会清空旧状态。

后续可继续扩展：

* `PlayerSaveData`
* `LevelSaveData`
* `InventorySaveData`
* `SettingsSaveData`

### 2.3 存储方式

第一版建议使用 JSON 文件：

```text
Application.persistentDataPath/save.json
```

原因：

* 比 `PlayerPrefs` 更适合保存结构化数据。
* 文件可读，调试方便。
* 原型阶段足够轻量。
* 后续可加密、压缩、分槽存档。

设置项可以有两种方案：

* 继续使用现有 `PlayerPrefs`，保持改动最小。
* 迁移到 `SaveData.settings`，统一由 `SaveManager` 管理。

第一版建议保留现有设置逻辑，只让 `SaveManager` 管理游戏进度数据。

### 2.4 对外 API 草案

```csharp
public class SaveManager : SingletonPersistent<SaveManager>
{
    public SaveData CurrentData { get; private set; }

    public void NewGame();
    public void LoadGame();
    public void SaveGame();
    public void DeleteSave();
    public bool HasSave();
}
```

使用示例：

```csharp
SaveManager.Instance.CurrentData.coin += 100;
SaveManager.Instance.SaveGame();
```

### 2.5 可选接口：ISaveable

如果后续系统变多，可以增加接口，让各模块自己写入和读取数据：

```csharp
public interface ISaveable
{
    void CaptureSaveData(SaveData data);
    void RestoreSaveData(SaveData data);
}
```

第一版可以不强制使用，避免模板变重。

### 2.6 与现有系统联动

建议联动点：

* `SettingPanel` 的清除存档按钮调用 `SaveManager.DeleteSave()`。
* `GameManager.StartGame()` 可根据需要调用 `NewGame()` 或 `LoadGame()`。
* 关卡胜利时调用 `SaveGame()`。
* 使用 `MsgCenter` 广播存档事件。

建议消息常量：

```csharp
public const int SaveLoaded = 2001;
public const int SaveCompleted = 2002;
public const int SaveDeleted = 2003;
```

---

## 3. InputManager 设计

### 3.1 推荐目录

建议新增：

```text
Assets/Scripts/Framework/Input/
```

建议文件：

```text
InputManager.cs
InputActionType.cs
```

### 3.2 第一版技术选型

当前项目还没有引入 Unity New Input System，第一版建议继续基于旧版 `UnityEngine.Input` 封装。

原因：

* 改动小。
* 和现有项目兼容。
* 原型开发速度快。
* 后续如果引入 New Input System，只需要替换 `InputManager` 内部实现，业务层 API 可以不变。

### 3.3 输入动作枚举

```csharp
public enum InputActionType
{
    Jump,
    Attack,
    Interact,
    Pause,
    Confirm,
    Cancel
}
```

### 3.4 对外 API 草案

```csharp
public class InputManager : SingletonPersistent<InputManager>
{
    public Vector2 Move { get; private set; }
    public Vector2 MousePosition { get; private set; }

    public bool GetActionDown(InputActionType action);
    public bool GetAction(InputActionType action);
    public bool GetActionUp(InputActionType action);
}
```

使用示例：

```csharp
Vector2 move = InputManager.Instance.Move;

if (InputManager.Instance.GetActionDown(InputActionType.Interact))
{
    // 执行交互
}
```

### 3.5 默认键位建议

| 动作 | 默认键位 |
| :--- | :--- |
| Move | WASD / Arrow Keys |
| Jump | Space |
| Attack | Mouse0 / J |
| Interact | E / F |
| Pause | Escape |
| Confirm | Enter / Space |
| Cancel | Escape |

### 3.6 暂停输入处理

建议 `InputManager` 不直接改变游戏状态，只广播或暴露输入。

例如：

```csharp
if (InputManager.Instance.GetActionDown(InputActionType.Pause))
{
    GameManager.Instance.PauseGame();
}
```

这样可以保持职责清晰：

* `InputManager` 只负责输入。
* `GameManager` 负责游戏状态。
* `UIManager` 负责面板显示。

### 3.7 后续扩展方向

可以逐步增加：

* 键位重绑定
* 手柄支持
* 输入锁定，例如过场动画、UI 打开时禁用角色输入
* 输入上下文，例如 Gameplay / UI / Dialogue
* 输入缓冲，例如提前按跳跃
* Coyote Time 辅助，例如离地后短时间仍允许跳跃

---

## 4. 建议实现顺序

第一阶段：

1. 新增 `SaveData`。
2. 新增 `SaveManager`，支持新建、读取、保存、删除。
3. 新增 `InputActionType`。
4. 新增 `InputManager`，封装移动和常用动作。

第二阶段：

1. 将 `SettingPanel` 的清除数据按钮接入 `SaveManager`。
2. 在 `MsgConst` 中补充存档相关消息。
3. 添加一个简单示例脚本，展示如何保存金币或关卡进度。

第三阶段：

1. 增加多个存档槽。
2. 增加键位重绑定。
3. 增加输入上下文。
4. 考虑迁移 Unity New Input System。

---

## 5. 注意事项

* 存档读写要避免每帧调用，建议只在关键节点保存。
* JSON 存档第一版不做加密，方便调试。
* 输入系统不要直接耦合具体玩家脚本。
* 业务脚本尽量通过 `InputManager` 查询输入，避免后续迁移成本。
* 如果游戏暂停时仍需要 UI 动画，应继续使用 DOTween 的不受时间缩放更新方式，现有 `BasePanel` 已经这样处理。
