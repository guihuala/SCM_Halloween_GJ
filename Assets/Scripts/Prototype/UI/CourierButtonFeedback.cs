using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace NightCourier
{
    [RequireComponent(typeof(Button))]
    public sealed class CourierButtonFeedback : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler,ISubmitHandler
    {
        public CourierInteractionEffect effectPrefab;
        private CourierInteractionEffect effect;
        private Button button;
        private bool hovered,selected,pressed;
        public CourierInteractionEffect Effect {get{return effect;}}
        private void Awake()
        {
            button=GetComponent<Button>();
            if(effectPrefab==null)effectPrefab=Resources.Load<CourierInteractionEffect>("Feedback/CourierInteractionEffect");
            if(effectPrefab!=null)
            {
                effect=Instantiate(effectPrefab,transform,false);effect.name="InteractionFeedback";
                var rect=(RectTransform)effect.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
                rect.offsetMin=new Vector2(-4,-4);rect.offsetMax=new Vector2(4,4);effect.ResetEffect();
            }
            button.onClick.AddListener(Play);
        }
        // Can also be triggered by a UnityEvent or animation event.
        public void Play(){if(effect!=null&&button.IsActive()&&button.IsInteractable())effect.Play();}
        private void Update(){if(effect!=null)effect.SetState(hovered||selected,pressed&&button.IsInteractable(),!button.IsInteractable());}
        public void OnPointerEnter(PointerEventData e){hovered=true;}
        public void OnPointerExit(PointerEventData e){hovered=false;pressed=false;}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnSelect(BaseEventData e){selected=e is AxisEventData||Input.GetKey(KeyCode.Tab)||Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.DownArrow);}
        public void OnDeselect(BaseEventData e){selected=false;}
        public void OnSubmit(BaseEventData e){Play();}
        private void OnDisable(){hovered=selected=pressed=false;if(effect!=null)effect.ResetEffect();}
        private void OnDestroy(){if(button!=null)button.onClick.RemoveListener(Play);}
    }
}
