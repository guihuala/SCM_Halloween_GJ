using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace NightCourier
{
    [RequireComponent(typeof(Button))]
    public sealed class CourierButtonFeedback : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler,ISubmitHandler
    {
        public CourierInteractionEffect effectPrefab;
        [Tooltip("Optional visual bounds for controls whose clickable area differs from their artwork.")]
        public RectTransform feedbackTarget;
        public bool suppressOutline;
        private readonly Vector3[] feedbackCorners=new Vector3[4];
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
            button.onClick.AddListener(PlayClick);
        }
        // Can also be triggered by a UnityEvent or animation event.
        public void Play()
        {
            if(!button.IsActive()||!button.IsInteractable())return;
            // Clicks keep the ordinary interaction state without a separate flash.
            if(effect!=null&&!suppressOutline)effect.SetState(hovered||selected,pressed,!button.IsInteractable());
        }
        private void PlayClick(){if(!button.IsActive()||!button.IsInteractable())return;Play();CourierSoundscape.PlayInterface("click");}
        private void Update(){if(effect!=null){if(suppressOutline)effect.ResetEffect();else effect.SetState(hovered||selected,pressed&&button.IsInteractable(),!button.IsInteractable());}}
        private void LateUpdate()
        {
            if(effect==null||feedbackTarget==null)return;
            feedbackTarget.GetWorldCorners(feedbackCorners);
            var parent=(RectTransform)effect.transform.parent;
            var lo=parent.InverseTransformPoint(feedbackCorners[0]);var hi=parent.InverseTransformPoint(feedbackCorners[2]);
            var rect=(RectTransform)effect.transform;
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2((lo.x+hi.x)*.5f-parent.rect.xMin,(lo.y+hi.y)*.5f-parent.rect.yMax);
            rect.sizeDelta=new Vector2(hi.x-lo.x+8,hi.y-lo.y+8);

        }
        public void OnPointerEnter(PointerEventData e){hovered=true;if(button.IsInteractable())CourierSoundscape.PlayInterface("hover");}
        public void OnPointerExit(PointerEventData e){hovered=false;pressed=false;}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnSelect(BaseEventData e){selected=e is AxisEventData||Input.GetKey(KeyCode.Tab)||Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.DownArrow);if(selected&&button.IsInteractable())CourierSoundscape.PlayInterface("hover");}
        public void OnDeselect(BaseEventData e){selected=false;}
        public void OnSubmit(BaseEventData e){Play();}
        private void OnDisable(){hovered=selected=pressed=false;if(effect!=null)effect.ResetEffect();}
        private void OnDestroy(){if(button!=null)button.onClick.RemoveListener(PlayClick);}
    }
}
