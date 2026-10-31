using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    // Reuse the hand-drawn HUD vocabulary; descriptions stay available to pointer and keyboard focus.
    public static class CourierSymbolUI
    {
        public static CourierIconGraphic Icon(Transform parent,string name,CourierIconKind kind,float x,float y,float size,Color color,string description="")
        {
            var child=parent.Find(name);
            if(child==null){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(CourierIconGraphic));go.transform.SetParent(parent,false);child=go.transform;}
            var icon=child.GetComponent<CourierIconGraphic>();icon.SetKind(kind);icon.color=color;
            var rect=icon.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=Vector2.one*size;
            icon.raycastTarget=!string.IsNullOrEmpty(description);
            if(icon.raycastTarget)Hint(child,description);
            return icon;
        }
        public static void Hint(Transform target,string description)
        {
            var hint=target.GetComponent<CourierIconHint>()??target.gameObject.AddComponent<CourierIconHint>();hint.message=description;
        }
        public static void Button(Button button,CourierIconKind kind,string description,bool keepNumbers=false)
        {
            foreach(var label in button.GetComponentsInChildren<Text>(true))label.enabled=keepNumbers;
            foreach(var old in button.GetComponentsInChildren<CourierIconGraphic>(true))if(old.name!="ActionSymbol")old.enabled=false;
            var icon=Icon(button.transform,"ActionSymbol",kind,0,0,44,CourierColors.Mint);
            icon.raycastTarget=false;
            var rect=icon.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
            rect.anchoredPosition=keepNumbers?new Vector2(-48,0):Vector2.zero;
            var image=button.targetGraphic as Image;
            if(image!=null&&image.color.grayscale>.45f)icon.color=CourierColors.Night;
            Hint(button.transform,description);
        }
    }
}
