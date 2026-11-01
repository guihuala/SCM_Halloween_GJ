using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightCourier
{
    // One persistent cursor for menu and gameplay. Only the frontmost UI hit is considered,
    // so dialogs cannot expose interactive cursors belonging to the map underneath.
    [DefaultExecutionOrder(500)]
    public sealed class CourierCursor : MonoBehaviour
    {
        public enum State { Default, Pointer, Pressed, Grab, Grabbing, Blocked, Inspect }
        private static CourierCursor instance;
        private static readonly string[] Names = { "default", "pointer", "pressed", "grab", "grabbing", "blocked", "inspect" };
        private readonly Texture2D[] textures = new Texture2D[7];
        private readonly List<RaycastResult> hits = new List<RaycastResult>(32);
        private PointerEventData pointer;
        private EventSystem eventSystem;
        private GameObject dragOwner;
        private bool focused = true;
        private int applied = -1;
        public State CurrentState { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (instance != null) return;
            var go = new GameObject("CourierCursor");
            instance = go.AddComponent<CourierCursor>();
            DontDestroyOnLoad(go);
        }
        private void Awake()
        {
            for (int i = 0; i < Names.Length; i++)
                textures[i] = Resources.Load<Texture2D>("NightVoyage/Cursors/" + Names[i]);
        }
        public static State Resolve(GameObject target)
        {
            if (target == null) return State.Default;
            var selectable = target.GetComponentInParent<Selectable>();
            if (selectable != null)
                return selectable.IsActive() && selectable.IsInteractable() ? State.Pointer : State.Blocked;
            var cargo = target.GetComponentInParent<CourierCargoRow>();
            if (cargo != null && cargo.CanDrag) return State.Grab;
            var hint = target.GetComponentInParent<CourierIconHint>();
            if (hint != null && hint.isActiveAndEnabled && !string.IsNullOrEmpty(hint.message))
                return hint.showOnClick ? State.Pointer : State.Inspect;
            var map = target.GetComponentInParent<CourierMapCanvas>();
            if (map != null) return map.panel != null && map.panel.CanNavigateMap ? State.Grab : State.Default;
            var stage = target.GetComponentInParent<CourierStageGraphic>();
            if (stage != null && stage.CanInteractAt(Input.mousePosition)) return State.Pointer;
            return State.Default;
        }
        private void LateUpdate()
        {
            if (!focused || !Input.mousePresent || !Cursor.visible || Cursor.lockState != CursorLockMode.None) return;
            var system = EventSystem.current;
            GameObject target = null;
            if (system != null)
            {
                if (eventSystem != system) { eventSystem = system; pointer = new PointerEventData(system); }
                pointer.Reset(); pointer.position = Input.mousePosition;
                hits.Clear(); system.RaycastAll(pointer, hits);
                if (hits.Count > 0) target = hits[0].gameObject;
            }
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
            {
                dragOwner = target != null ? ExecuteEvents.GetEventHandler<IDragHandler>(target) : null;
            }
            bool dragging = dragOwner != null && dragOwner.gameObject.activeInHierarchy &&
                ((dragOwner.GetComponent<CourierCargoRow>() is CourierCargoRow row && row.IsDragging) ||
                 (dragOwner.GetComponent<CourierMapCanvas>() is CourierMapCanvas map && map.IsDragging));
            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(2)) { dragOwner = null; dragging = false; }
            var state = dragging ? State.Grabbing : Resolve(target);
            if (Input.GetMouseButton(0) && state == State.Pointer) state = State.Pressed;
            Apply(state);
        }
        private void Apply(State state)
        {
            CurrentState = state;
            if (applied == (int)state) return;
            applied = (int)state;
            var texture = textures[applied];
            Vector2 hotspot = state == State.Pointer || state == State.Pressed ? new Vector2(20, 7) :
                state == State.Grab || state == State.Grabbing ? new Vector2(24, 24) : new Vector2(6, 4);
            Cursor.SetCursor(texture, texture != null ? hotspot : Vector2.zero, CursorMode.Auto);
        }
        private void OnApplicationFocus(bool value)
        {
            focused = value; dragOwner = null; applied = -1;
            if (!value) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        private void OnDisable()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); applied = -1; dragOwner = null;
        }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
