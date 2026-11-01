using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent, RequireComponent(typeof(Button))]
public sealed class ButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private Button button;

    public static void Install(GameObject root)
    {
        foreach (var control in root.GetComponentsInChildren<Button>(true))
            if (control.GetComponent<ButtonAudioFeedback>() == null)
                control.gameObject.AddComponent<ButtonAudioFeedback>();
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Click);
    }

    // CourierButtonFeedback already sends the same cues for gameplay controls.
    private bool CanPlay => button.IsActive() && button.IsInteractable() &&
        GetComponent<NightCourier.CourierButtonFeedback>() == null;

    private void Click() { if (CanPlay) NightCourier.CourierSoundscape.PlayInterface("click"); }
    public void OnPointerEnter(PointerEventData data)
    { if (CanPlay) NightCourier.CourierSoundscape.PlayInterface("hover"); }
    public void OnSelect(BaseEventData data)
    {
        if (CanPlay && (data is AxisEventData || Input.GetKey(KeyCode.Tab)))
            NightCourier.CourierSoundscape.PlayInterface("hover");
    }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Click); }
}
