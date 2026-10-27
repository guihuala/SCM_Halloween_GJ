# Game Jam 基础流程（当前版本）

Unity 2022.3.62f3。打开 `Assets/Scenes/MainMenu.unity`，点击新游戏直接进入 `GamePlay`；也可直接运行 GamePlay。

- 暂停：HUD 按钮或 Escape；暂停菜单支持继续、重开、设置和返回主菜单。
- 设置：仅保留背景音乐、音效音量及返回；Escape 返回。输入键位在 InputManager Inspector 配置。
- 胜利：玩法脚本调用 `GameManager.Instance.WinGame()`。
- 失败：调用 `GameManager.Instance.LoseGame()`；原 `EndGame()` 等同失败结算。
- 结算面板提供重开与返回主菜单。重复结算不会生成多个面板。
- 重开：`GameManager.Instance.RestartGame()`，重新加载当前场景。每局玩法数据应由场景对象持有，在新场景中初始化；持久化业务状态需自行重置。
- 存档、对话及导表仍是可选工具；默认菜单不提供存档入口，GamePlay 不再包含测试内容。现有存档不会因开始或重开被删除。
- 没有内置胜负条件：在实际玩法达到成功/失败条件时调用上述接口。
- TimeManager 为可选慢动作工具；暂停状态由 GameManager 统一协调，不再逐帧覆盖时间。

## 验证

`Assets/Editor/GameFlowSmokeTest.cs` 提供批处理入口 `GameFlowSmokeTest.Run`（启动 Unity 时传 `-executeMethod GameFlowSmokeTest.Run`，不要加 `-quit`）。它运行真实场景，验证开始、暂停、设置、恢复、胜负结算、连续重开、返回菜单、单例数量、慢动作恢复和音效停止隔离；不操作玩家存档。

界面固定中文，本地化模块已移除；对话正文直接填写在 DialogueTable 的 text 字段。

以下为保留模块的参考说明；默认场景以本节流程为准。

# Unity Core Framework 使用说明文档

## 1\. 简介

本框架提供了一套轻量级、高性能的游戏核心管理系统，旨在解决游戏开发中常见的生命周期管理、资源复用、事件解耦和场景切换问题。

**主要模块：**

* **ObjectPool**: 高性能对象池（零 GC 复用）。
* **MsgCenter**: 基于 int Key 的消息发布/订阅系统。
* **GameManager**: 游戏流程状态机。
* **SceneLoader**: 带有过渡动画的异步场景加载器。
* **Config**: 模板级配置读取。
* **SceneFlow**: 场景枚举与真实场景名的配置映射。
* **TimeManager**: 时间流速控制。

-----

## 2\. 核心模块详解

### 2.1 ObjectPool (对象池系统) - [已优化]

用于管理高频生成的游戏对象（如子弹、特效、敌人），避免频繁 `Instantiate/Destroy` 带来的 CPU 开销和内存碎片。

**优化特性：**

* **零 GC 运行**：移除了字符串操作，对象名称在生成时一次性处理。
* **层级缓存**：内部缓存父节点 Transform，不再使用 `GameObject.Find`，大幅提升获取速度。
* **自动归位**：回收对象时自动归类到对应的子节点下，保持 Hierarchy 整洁。

#### API 使用方法：

**1. 获取对象 (代替 Instantiate)**

```csharp
// 这里的 prefab 可以是任何 GameObject
GameObject bullet = ObjectPool.Instance.GetObject(bulletPrefab);
bullet.transform.position = muzzle.position;
bullet.transform.rotation = muzzle.rotation;
```

**2. 回收对象 (代替 Destroy)**

```csharp
ObjectPool.Instance.PushObject(bullet);
```

**3. 自动回收脚本 (`PushSelfBase`)**
挂载在需要自动回收的 Prefab 上（如爆炸特效、临时的子弹）。

* **注意**：由于对象是复用的，**必须使用 `OnEnable` 重置状态**，不要使用 `Start`。
* **设置**：在 Inspector 中调整 `Life Time` 即可。

<!-- end list -->

```csharp
// PushSelfBase.cs 使用示例
private void OnEnable() 
{
    // 重置逻辑写在这里，例如：
    timer = 0f;
    isPushed = false;
    rb.velocity = Vector3.zero; // 如果有刚体
}
```

-----

### 2.2 MsgCenter (消息中心)

基于观察者模式的全局事件总线，使用 `int` 代替 `string` 作为事件 Key，性能更佳。

#### 配置：

在 `MsgConst.cs` 中定义常量，避免魔法数字。

```csharp
// MsgConst.cs
public class MsgConst
{
    public const int GameStart = 1001;
    public const int PlayerDead = 1002;
    public const int ScoreChanged = 1003;
}
```

#### API 使用方法：

**1. 发送消息 (Send)**

```csharp
// 无参数
MsgCenter.SendMsgAct(MsgConst.PlayerDead);

// 有参数 (params object[])
MsgCenter.SendMsg(MsgConst.ScoreChanged, 100, "Bonus");
```

**2. 监听消息 (Register)**
建议在 `Awake` 或 `Start` 中注册。

```csharp
void Start()
{
    // 注册无参事件
    MsgCenter.RegisterMsgAct(MsgConst.PlayerDead, OnPlayerDead);
    
    // 注册有参事件
    MsgCenter.RegisterMsg(MsgConst.ScoreChanged, OnScoreChanged);
}

// 对应回调
void OnPlayerDead() { ... }
void OnScoreChanged(params object[] args) 
{
    int score = (int)args[0];
    string type = (string)args[1];
}
```

**3. 移除监听 (Unregister)**
**重要**：务必在 `OnDestroy` 中注销，防止内存泄漏或空引用报错。

```csharp
void OnDestroy()
{
    MsgCenter.UnregisterMsgAct(MsgConst.PlayerDead, OnPlayerDead);
    MsgCenter.UnregisterMsg(MsgConst.ScoreChanged, OnScoreChanged);
}
```

-----

### 2.3 GameManager (游戏总控)

管理游戏的全局状态（运行、暂停、结束）。

* **状态枚举**：`Playing`, `Paused`, `GameOver`
* **功能**：
    * `StartGame()`: 开始游戏流程。
    * `PauseGame()`: 暂停游戏（自动设置 TimeScale 为 0，呼出 UI）。
    * `ResumeGame()`: 恢复游戏。
    * `ReturnToMainMenu()`: 重置状态并加载主菜单场景。

-----

### 2.4 SceneLoader (场景加载器)

提供平滑的场景切换体验，包含淡入淡出动画和进度条。

* **Setup**：确保场景中存在 `SceneLoader` 单例，并且 Inspector 中引用了 Loading UI (CanvasGroup, Image, Text)。
* **API**：

<!-- end list -->

```csharp
// 加载枚举中定义的场景
SceneLoader.Instance.LoadScene(GameScene.Gameplay, () => {
    Debug.Log("加载完成回调");
});

// 重新加载当前关卡
SceneLoader.Instance.ReloadCurrentScene();
```

-----

### 2.5 TimeManager (时间管理)

控制游戏的全局时间流速。

* **IsPaused**: 获取当前是否暂停。
* **TimeFactor**: 获取当前时间倍率。
* **API**：

<!-- end list -->

```csharp
// 开启 "子弹时间" (0.1 倍速)
TimeManager.Instance.SetTimeFactor(0.1f);

// 恢复正常速度
TimeManager.Instance.SetTimeFactor(1f);
```

时间倍率只在状态改变时应用，不需要 Update 逐帧写入。

-----

### 2.6 Save Validation (存档验证)

存档系统内置轻量校验，保存和读取时都会检查核心字段，避免损坏数据继续流入游戏流程。

**当前检查内容：**

* 存档版本号是否合法。
* 槽位编号是否和当前槽位一致。
* 创建时间、最后保存时间格式是否正确。
* 游戏时间、金币、关卡编号是否为负数。
* 已解锁关卡列表是否为空项或重复项。

**编辑器菜单：**

* `Tools/Template/Save/Validate Current Save Data`：运行时检查当前内存里的存档。
* `Tools/Template/Save/Validate Local Save Files`：扫描本机 `Application.persistentDataPath/Saves` 下的存档 JSON。

如果读取到硬错误，`SaveManager` 会拒绝使用该存档并回退到新档；如果只是警告，会输出到 Console 方便排查。

-----

## 3\. 快速集成指南

1.  **场景搭建**：

    * 创建一个名为 `_App` 或 `Managers` 的初始场景或预制体。
    * 挂载 `GameManager`, `ObjectPool`, `TimeManager`, `MsgCenter`。
    * 挂载 `SceneLoader` 并赋值 UI 组件（进度条 Image、Loading 面板 CanvasGroup）。

2.  **对象池准备**：

    * 制作子弹或特效的 Prefab。
    * 挂载 `PushSelfBase` 脚本。
    * 设置 `Life Time` (例如 3秒)。

3.  **脚本调用**：

    * 在角色攻击脚本中，将 `Instantiate(bullet)` 替换为 `ObjectPool.Instance.GetObject(bullet)`。
    * 在 UI 脚本中，通过 `MsgCenter` 监听 `MsgConst.ScoreChanged` 来更新分数显示。

-----

**文档版本**：1.0
**更新日期**：2025-11-23
