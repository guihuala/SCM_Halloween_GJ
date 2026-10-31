using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    [RequireComponent(typeof(Image))]
    public sealed class CourierVehicleIconView : MonoBehaviour
    {
        public CourierVehicleIconCatalog catalog;
        public Image artwork;
        public CourierIconGraphic ghost;
        public Sprite ghostArtwork;
        public void SetVehicle(int id)
        {
            if(catalog==null)catalog=Resources.Load<CourierVehicleIconCatalog>("NightVoyage/VehicleIcons");
            if(artwork==null)artwork=GetComponent<Image>();
            artwork.sprite=id<0?ghostArtwork:catalog!=null?catalog.ForVehicle(id):null;
            artwork.enabled=artwork.sprite!=null;artwork.preserveAspect=true;artwork.raycastTarget=false;
            if(ghost!=null)ghost.gameObject.SetActive(id<0&&artwork.sprite==null);
        }
        public static CourierVehicleIconView Ensure(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var child=parent.Find(name);
            var view=child!=null?child.GetComponent<CourierVehicleIconView>():null;
            if(view==null)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(CourierVehicleIconView));go.transform.SetParent(parent,false);view=go.GetComponent<CourierVehicleIconView>();
            }
            var rt=(RectTransform)view.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=position;rt.sizeDelta=size;
            view.artwork=view.GetComponent<Image>();view.artwork.color=Color.white;
            if(view.ghost==null)
            {
                var go=new GameObject("GhostFallback",typeof(RectTransform),typeof(CanvasRenderer),typeof(CourierIconGraphic));go.transform.SetParent(view.transform,false);
                var g=(RectTransform)go.transform;g.anchorMin=Vector2.zero;g.anchorMax=Vector2.one;g.offsetMin=g.offsetMax=Vector2.zero;
                view.ghost=go.GetComponent<CourierIconGraphic>();view.ghost.SetKind(CourierIconKind.Ghost);view.ghost.color=CourierColors.Mint;view.ghost.raycastTarget=false;
            }
            view.SetVehicle(0);return view;
        }
    }
}
