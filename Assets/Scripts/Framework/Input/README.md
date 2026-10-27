# InputManager 使用说明

`InputManager` 是一个独立的输入封装模块。它基于 Unity 旧版 `Input` API，不依赖 Unity New Input System，也不会主动控制游戏流程。

## 场景接入

1. 在管理器物体上挂载 `InputManager`。
2. 在 Inspector 中调整默认键位。
3. 业务脚本通过 `InputManager.Instance` 查询输入。

## 常用 API

```csharp
Vector2 move = InputManager.Instance.Move;
Vector2 rawMove = InputManager.Instance.RawMove;
Vector2 mousePos = InputManager.Instance.MousePosition;

bool interactDown = InputManager.Instance.GetActionDown(InputActionType.Interact);
bool attacking = InputManager.Instance.GetAction(InputActionType.Attack);
bool jumpUp = InputManager.Instance.GetActionUp(InputActionType.Jump);
```

## 输入开关

```csharp
InputManager.Instance.SetInputEnabled(false);
InputManager.Instance.SetGameplayInputEnabled(false);
InputManager.Instance.SetUIInputEnabled(true);
```

## 修改键位

```csharp
InputManager.Instance.SetKeys(InputActionType.Interact, new[] { KeyCode.E });
```

## 设计说明

`InputManager` 只负责读取输入，不直接调用 `GameManager`、`UIManager` 或玩家控制器。这样项目可以按需选择使用，也方便以后把内部实现替换成 Unity New Input System。

