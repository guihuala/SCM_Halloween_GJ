using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // Static artwork shares the live map's coordinate system. Existing node buttons own all input.
    public sealed class CourierMapArtwork : MonoBehaviour
    {
        public CourierMapArtCatalog catalog;
        public Image terrainImage;
        public RectTransform landmarkLayer;
        public Image[] landmarkImages=new Image[0];
        public Image[] landmarkHitAreas=new Image[0];
        public CourierRouteGraphic travelOverlay;
        public CourierVehicleIconView travelerIcon;
        [Min(24)] public float travelerSize=56;
        private CourierRouteGraphic primary;
        private Color originalIdle,originalAdjacent,originalSelected;
        private bool capturedColors;
        private static readonly CourierRun Preview=new CourierRun();

        public void Apply(CourierRoutePanel panel,Stop[] stops=null)
        {
            if(!enabled)return;
            if(catalog==null)catalog=Resources.Load<CourierMapArtCatalog>("NightVoyage/MapArtwork");
            var map=(RectTransform)transform;
            primary=panel.mapGraphic;
            if(!capturedColors)
            {originalIdle=primary.idleRoadColor;originalAdjacent=primary.adjacentRoadColor;originalSelected=primary.selectedRoadColor;capturedColors=true;}
            EnsureLayers(panel);
            terrainImage.sprite=catalog!=null?catalog.terrain:null;
            terrainImage.color=catalog!=null?catalog.terrainTint:Color.white;
            terrainImage.gameObject.SetActive(terrainImage.sprite!=null);
            terrainImage.preserveAspect=false; // Organic terrain stretches; landmarks below preserve their proportions.
            primary.drawTravel=false;
            if(catalog!=null)
            {primary.idleRoadColor=catalog.idleRoadColor;primary.adjacentRoadColor=catalog.adjacentRoadColor;primary.selectedRoadColor=catalog.selectedRoadColor;}
            else
            {primary.idleRoadColor=originalIdle;primary.adjacentRoadColor=originalAdjacent;primary.selectedRoadColor=originalSelected;}
            travelOverlay.gameObject.SetActive(true);travelOverlay.Owner=primary.Owner;travelOverlay.drawRoads=false;travelOverlay.drawTravel=true;travelOverlay.SetVerticesDirty();
            landmarkLayer.gameObject.SetActive(true);
            var nodes=stops??Preview.Stops;
            float size=Mathf.Max(32,Mathf.Min(catalog!=null?catalog.maximumLandmarkSize:160,map.rect.height*(catalog!=null?catalog.landmarkHeightFraction:.28f)));
            for(int i=0;i<landmarkImages.Length&&i<nodes.Length;i++)
            {
                var entry=catalog!=null?catalog.ForStop(i):null;
                var image=landmarkImages[i];image.sprite=entry!=null?entry.sprite:null;
                bool visible=image.sprite!=null;image.gameObject.SetActive(visible);landmarkHitAreas[i].gameObject.SetActive(visible);
                var point=CourierRouteGraphic.NodePoint(nodes[i],map.rect.size);
                var rt=image.rectTransform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=entry!=null?entry.anchor:new Vector2(.5f,.15625f);
                float side=size*(entry!=null?Mathf.Max(.1f,entry.scale):1);rt.sizeDelta=new Vector2(side,side);
                rt.anchoredPosition=new Vector2(point.x,-point.y)+(entry!=null?entry.offset:Vector2.zero);
                image.preserveAspect=true;image.raycastTarget=false;
                // Hit region is a child of the original Button, so all existing click/hover handlers remain in use.
                var button=(RectTransform)panel.stopButtons[i].transform;
                var hit=landmarkHitAreas[i].rectTransform;hit.anchorMin=hit.anchorMax=new Vector2(0,1);hit.pivot=rt.pivot;hit.sizeDelta=rt.sizeDelta;
                hit.anchoredPosition=rt.anchoredPosition-button.anchoredPosition;
                var label=panel.stopLabels[i];var lr=label.rectTransform;
                float labelHeight=map.rect.height<400?56:72;
                float labelTop=Mathf.Clamp(point.y+20,4,Mathf.Max(4,map.rect.height-labelHeight-4));
                lr.anchoredPosition=new Vector2(point.x-110-button.anchoredPosition.x,-labelTop-button.anchoredPosition.y);
                lr.sizeDelta=new Vector2(220,labelHeight);label.alignment=TextAnchor.UpperCenter;label.fontSize=map.rect.height<400?22:26;
                panel.stopCounts[i].rectTransform.sizeDelta=new Vector2(90,36);
            }
            RefreshTraveler();
            primary.SetVerticesDirty();
        }
        // Follow the final turn/progress state after the simulation's Update, including mid-road exhaustion.
        private void LateUpdate(){RefreshTraveler();}
        private void RefreshTraveler()
        {
            if(travelOverlay==null||travelerIcon==null)return;
            var owner=travelOverlay.Owner;
            var run=owner!=null?owner.Run:null;
            bool traveling=run!=null&&run.ActiveRoad!=null&&(run.State==ShiftState.Driving||run.State==ShiftState.Encounter||run.State==ShiftState.Pushing);
            travelerIcon.gameObject.SetActive(traveling);
            if(traveling)
            {
                travelerIcon.SetVehicle(run.VehicleIndex);
                var rect=travelOverlay.rectTransform;
                var a=CourierRouteGraphic.NodePoint(run.Stops[run.Node],rect.rect.size);
                var b=CourierRouteGraphic.NodePoint(run.Stops[run.ActiveRoad.Other(run.Node)],rect.rect.size);
                var point=Vector2.Lerp(a,b,run.Progress);
                var marker=(RectTransform)travelerIcon.transform;marker.anchorMin=marker.anchorMax=new Vector2(0,1);marker.pivot=new Vector2(.5f,.5f);
                marker.anchoredPosition=new Vector2(point.x,-point.y);marker.sizeDelta=Vector2.one*Mathf.Max(24,travelerSize);
            }
            // Missing custom vehicle art keeps a visible dot; the normal exhausted state always has a ghost glyph.
            travelOverlay.drawTravelDot=traveling&&!travelerIcon.artwork.enabled&&!travelerIcon.ghost.gameObject.activeSelf;
            travelOverlay.travelHaloRadius=Mathf.Max(13,travelerSize*.6f);
            travelOverlay.SetVerticesDirty();
        }
        private void EnsureLayers(CourierRoutePanel panel)
        {
            if(terrainImage==null)terrainImage=EnsureImage(transform,"TerrainArtwork");
            Stretch(terrainImage.rectTransform);terrainImage.raycastTarget=false;terrainImage.transform.SetAsFirstSibling();
            panel.mapGraphic.transform.SetSiblingIndex(1);
            if(landmarkLayer==null)
            {
                var old=transform.Find("LandmarkArtwork");
                landmarkLayer=old!=null?(RectTransform)old:CreateRect(transform,"LandmarkArtwork");
            }
            Stretch(landmarkLayer);landmarkLayer.SetSiblingIndex(2);
            if(travelOverlay==null)
            {
                var old=transform.Find("TravelOverlay");
                var rect=old!=null?(RectTransform)old:CreateRect(transform,"TravelOverlay");
                travelOverlay=rect.GetComponent<CourierRouteGraphic>()??rect.gameObject.AddComponent<CourierRouteGraphic>();
            }
            Stretch(travelOverlay.rectTransform);travelOverlay.raycastTarget=false;travelOverlay.transform.SetSiblingIndex(3);
            if(travelerIcon==null)travelerIcon=CourierVehicleIconView.Ensure(travelOverlay.transform,"TravelerIcon",Vector2.zero,Vector2.one*Mathf.Max(24,travelerSize));
            travelerIcon.artwork.raycastTarget=false;travelerIcon.ghost.raycastTarget=false;
            if(landmarkImages.Length!=panel.stopButtons.Length)landmarkImages=new Image[panel.stopButtons.Length];
            if(landmarkHitAreas.Length!=panel.stopButtons.Length)landmarkHitAreas=new Image[panel.stopButtons.Length];
            for(int i=0;i<landmarkImages.Length;i++)
            {
                if(landmarkImages[i]==null)landmarkImages[i]=EnsureImage(landmarkLayer,"Landmark_"+i);
                var outline=panel.stopLabels[i].GetComponent<Outline>()??panel.stopLabels[i].gameObject.AddComponent<Outline>();
                outline.effectColor=new Color(.04f,.07f,.09f,.9f);outline.effectDistance=new Vector2(1,-1);
                if(landmarkHitAreas[i]==null)landmarkHitAreas[i]=EnsureImage(panel.stopButtons[i].transform,"LandmarkHitArea");
                landmarkHitAreas[i].color=Color.clear;landmarkHitAreas[i].raycastTarget=true;landmarkHitAreas[i].transform.SetAsFirstSibling();
            }
        }
        private static RectTransform CreateRect(Transform parent,string name)
        {var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);return (RectTransform)obj.transform;}
        private static Image EnsureImage(Transform parent,string name)
        {
            var old=parent.Find(name);var rect=old!=null?(RectTransform)old:CreateRect(parent,name);
            return rect.GetComponent<Image>()??rect.gameObject.AddComponent<Image>();
        }
        private static void Stretch(RectTransform rect)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private void OnDisable()
        {
            if(primary!=null)
            {primary.drawTravel=true;if(capturedColors){primary.idleRoadColor=originalIdle;primary.adjacentRoadColor=originalAdjacent;primary.selectedRoadColor=originalSelected;}primary.SetVerticesDirty();}
            if(terrainImage!=null)terrainImage.gameObject.SetActive(false);
            if(landmarkLayer!=null)landmarkLayer.gameObject.SetActive(false);
            if(travelOverlay!=null)travelOverlay.gameObject.SetActive(false);
            foreach(var hit in landmarkHitAreas)if(hit!=null)hit.gameObject.SetActive(false);
        }
    }
}
