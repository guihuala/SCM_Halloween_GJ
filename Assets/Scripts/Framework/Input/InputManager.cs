using System.Collections.Generic;
using UnityEngine;

public class InputManager : SingletonPersistent<InputManager>
{
    [Header("General")]
    [SerializeField] private bool inputEnabled = true;
    [SerializeField] private bool gameplayInputEnabled = true;
    [SerializeField] private bool uiInputEnabled = true;

    [Header("Move Axis")]
    [SerializeField] private string horizontalAxis = "Horizontal";
    [SerializeField] private string verticalAxis = "Vertical";
    [SerializeField] private bool normalizeMoveInput = true;

    [Header("Default Keys")]
    [SerializeField] private List<KeyCode> jumpKeys = new List<KeyCode> { KeyCode.Space };
    [SerializeField] private List<KeyCode> attackKeys = new List<KeyCode> { KeyCode.Mouse0, KeyCode.J };
    [SerializeField] private List<KeyCode> interactKeys = new List<KeyCode> { KeyCode.E, KeyCode.F };
    [SerializeField] private List<KeyCode> pauseKeys = new List<KeyCode> { KeyCode.Escape };
    [SerializeField] private List<KeyCode> confirmKeys = new List<KeyCode> { KeyCode.Return, KeyCode.Space };
    [SerializeField] private List<KeyCode> cancelKeys = new List<KeyCode> { KeyCode.Escape };

    private readonly Dictionary<InputActionType, List<KeyCode>> actionKeys = new Dictionary<InputActionType, List<KeyCode>>();

    public Vector2 Move { get; private set; }
    public Vector2 RawMove { get; private set; }
    public Vector2 MousePosition { get; private set; }
    public bool InputEnabled => inputEnabled;
    public bool GameplayInputEnabled => gameplayInputEnabled;
    public bool UIInputEnabled => uiInputEnabled;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this) return;

        RebuildKeyMap();
    }

    private void Update()
    {
        if (!inputEnabled)
        {
            Move = Vector2.zero;
            RawMove = Vector2.zero;
            MousePosition = Input.mousePosition;
            return;
        }

        MousePosition = Input.mousePosition;

        if (!gameplayInputEnabled)
        {
            Move = Vector2.zero;
            RawMove = Vector2.zero;
            return;
        }

        RawMove = new Vector2(Input.GetAxisRaw(horizontalAxis), Input.GetAxisRaw(verticalAxis));
        Move = normalizeMoveInput && RawMove.sqrMagnitude > 1f ? RawMove.normalized : RawMove;
    }

    public bool GetActionDown(InputActionType action)
    {
        if (!CanReadAction(action)) return false;
        return AnyKey(action, Input.GetKeyDown);
    }

    public bool GetAction(InputActionType action)
    {
        if (!CanReadAction(action)) return false;
        return AnyKey(action, Input.GetKey);
    }

    public bool GetActionUp(InputActionType action)
    {
        if (!CanReadAction(action)) return false;
        return AnyKey(action, Input.GetKeyUp);
    }

    public IReadOnlyList<KeyCode> GetKeys(InputActionType action)
    {
        if (!actionKeys.TryGetValue(action, out var keys))
        {
            return System.Array.Empty<KeyCode>();
        }

        return keys;
    }

    public void SetKeys(InputActionType action, IEnumerable<KeyCode> keys)
    {
        if (!actionKeys.TryGetValue(action, out var targetKeys))
        {
            targetKeys = new List<KeyCode>();
            actionKeys[action] = targetKeys;
        }

        targetKeys.Clear();

        if (keys == null) return;

        foreach (var key in keys)
        {
            if (!targetKeys.Contains(key))
            {
                targetKeys.Add(key);
            }
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    public void SetGameplayInputEnabled(bool enabled)
    {
        gameplayInputEnabled = enabled;
    }

    public void SetUIInputEnabled(bool enabled)
    {
        uiInputEnabled = enabled;
    }

    public void RebuildKeyMap()
    {
        actionKeys.Clear();
        actionKeys[InputActionType.Jump] = jumpKeys;
        actionKeys[InputActionType.Attack] = attackKeys;
        actionKeys[InputActionType.Interact] = interactKeys;
        actionKeys[InputActionType.Pause] = pauseKeys;
        actionKeys[InputActionType.Confirm] = confirmKeys;
        actionKeys[InputActionType.Cancel] = cancelKeys;
    }

    private bool CanReadAction(InputActionType action)
    {
        if (!inputEnabled) return false;

        switch (action)
        {
            case InputActionType.Confirm:
            case InputActionType.Cancel:
                return uiInputEnabled;
            default:
                return gameplayInputEnabled;
        }
    }

    private bool AnyKey(InputActionType action, System.Func<KeyCode, bool> keyCheck)
    {
        if (!actionKeys.TryGetValue(action, out var keys) || keys == null)
        {
            return false;
        }

        for (int i = 0; i < keys.Count; i++)
        {
            if (keyCheck(keys[i]))
            {
                return true;
            }
        }

        return false;
    }
}
