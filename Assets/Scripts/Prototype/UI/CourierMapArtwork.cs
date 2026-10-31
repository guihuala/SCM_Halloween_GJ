using UnityEngine;
using System.Linq;
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
            terrainImage.gameObject.SetActive(false); // V2.4 regions/roads are drawn from the live street graph.
            terrainImage.preserveAspect=false; // Organic terrain stretches; landmarks below preserve their proportions.
            primary.drawTravel=false;
            if(catalog!=null)
            {primary.idleRoadColor=CourierColors.Road;primary.adjacentRoadColor=CourierColors.Road;primary.selectedRoadColor=CourierColors.Gold;primary.previewRoadColor=CourierColors.Mint;}
            else
            {primary.idleRoadColor=originalIdle;primary.adjacentRoadColor=originalAdjacent;primary.selectedRoadColor=originalSelected;}
            travelOverlay.gameObject.SetActive(true);travelOverlay.Owner=primary.Owner;travelOverlay.drawRoads=false;travelOverlay.drawTravel=true;travelOverlay.SetVerticesDirty();
            landmarkLayer.gameObject.SetActive(true);
            var nodes=stops??Preview.Stops;
            float size=Mathf.Clamp(map.rect.height*.09f,36,76);
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
                var buttonColors=panel.stopButtons[i].colors;
                buttonColors.normalColor=buttonColors.highlightedColor=buttonColors.selectedColor=buttonColors.pressedColor=buttonColors.disabledColor=Color.clear;
                panel.stopButtons[i].colors=buttonColors;
                var background=panel.stopButtons[i].targetGraphic;
                if(background!=null)background.canvasRenderer.SetAlpha(0);
                var feedback=panel.stopButtons[i].GetComponent<CourierButtonFeedback>();
                if(feedback!=null)feedback.feedbackTarget=rt;
                // The legacy button rectangle is an input region, not the landmark's visual bounds.
                var focus=panel.stopButtons[i].GetComponent<CourierFocusRing>();
                if(focus!=null){if(focus.ring!=null)focus.ring.enabled=false;focus.ring=null;}
                // Hit region is a child of the original Button, so all existing click/hover handlers remain in use.
                var button=(RectTransform)panel.stopButtons[i].transform;
                var hit=landmarkHitAreas[i].rectTransform;hit.anchorMin=hit.anchorMax=new Vector2(0,1);hit.pivot=rt.pivot;hit.sizeDelta=rt.sizeDelta;
                hit.anchoredPosition=rt.anchoredPosition-button.anchoredPosition;
                var label=panel.stopLabels[i];var lr=label.rectTransform;
                float labelHeight=36;
                float labelTop=Mathf.Clamp(point.y+12,4,Mathf.Max(4,map.rect.height-labelHeight-4));
                lr.anchoredPosition=new Vector2(point.x-100-button.anchoredPosition.x,-labelTop-button.anchoredPosition.y);
                lr.sizeDelta=new Vector2(200,labelHeight);label.alignment=TextAnchor.UpperCenter;label.fontSize=map.rect.height<400?20:24;
                panel.stopCounts[i].rectTransform.sizeDelta=new Vector2(90,36);
            }
            RefreshStreetLabels(panel,map.rect.size);
            RefreshStations(panel,map.rect.size);
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
            bool show=run!=null;
            travelerIcon.gameObject.SetActive(show);
            if(show)
            {
                travelerIcon.SetVehicle(run.VehicleIndex);
                var rect=travelOverlay.rectTransform;
                var point=traveling?CourierRouteGraphic.TravelPoint(run,rect.rect.size):CourierRouteGraphic.NodePoint(run.Stops[run.Node],rect.rect.size);
                var marker=(RectTransform)travelerIcon.transform;marker.anchorMin=marker.anchorMax=new Vector2(0,1);marker.pivot=new Vector2(.5f,.5f);
                marker.anchoredPosition=new Vector2(point.x,-point.y);marker.sizeDelta=Vector2.one*Mathf.Max(24,travelerSize);
            }
            // Missing custom vehicle art keeps a visible dot; the normal exhausted state always has a ghost glyph.
            travelOverlay.drawTravelDot=traveling&&!travelerIcon.artwork.enabled&&!travelerIcon.ghost.gameObject.activeSelf;
            travelOverlay.travelHaloRadius=Mathf.Max(13,travelerSize*.6f);
            travelOverlay.SetVerticesDirty();
        }
        public Button[] stationButtons;
        private Text[] streetLabels;
        private Text protectionStatus;
        private readonly Vector3[] stationCorners=new Vector3[4];
        private void RefreshStreetLabels(CourierRoutePanel panel,Vector2 size)
        {
            if(streetLabels==null)
            {
                streetLabels=new Text[CourierStreetMap.Nodes.Length];
                for(int i=0;i<streetLabels.Length;i++)
                {
                    var rect=CreateRect(landmarkLayer,"StreetLabel_"+CourierStreetMap.Nodes[i].Id);
                    var label=GetOrAdd<Text>(rect.gameObject);label.font=panel.stopLabels[0].font;
                    label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
                    label.color=CourierColors.Muted;label.fontSize=18;
                    var outline=GetOrAdd<Outline>(rect.gameObject);outline.effectColor=CourierColors.Night;outline.effectDistance=new Vector2(1,-1);
                    if(CourierStreetMap.Nodes[i].StopId>=9)
                    {
                        int target=CourierStreetMap.Nodes[i].StopId;
                        var button=GetOrAdd<Button>(rect.gameObject);button.targetGraphic=label;
                        button.onClick.AddListener(()=>panel.Select(target));label.raycastTarget=true;
                        var hover=GetOrAdd<CourierMapNodeHover>(rect.gameObject);hover.panel=panel;hover.node=target;
                    }
                    streetLabels[i]=label;
                }
            }
            for(int i=0;i<streetLabels.Length;i++)
            {
                var node=CourierStreetMap.Nodes[i];var label=streetLabels[i];
                label.gameObject.SetActive(node.StopId>=9);
                string name=node.Id=="S"?"S · Depot":node.Id=="H"?"H · Heat":node.Id=="W"?"W · Water":node.Id=="Q"?"Q · Suspension":node.Id;
                var run=primary.Owner!=null?primary.Owner.Run:Preview;
                if(run.DelayEventNodes.Contains(node.Id))name+=" +1";
                if(run.RetreatEventNodes.Contains(node.Id))name+=" <";
                var nodeButton=label.GetComponent<Button>();
                if(nodeButton!=null)nodeButton.interactable=primary.Owner!=null&&primary.Owner.Active&&!primary.Owner.ConversationActive&&run.State==ShiftState.Docked;
                label.text=name;var rect=label.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,1);
                var p=CourierRouteGraphic.MapPoint(node.Point,size);rect.anchoredPosition=new Vector2(p.x,-p.y-7);rect.sizeDelta=new Vector2(name.Length>3?100:48,40);
            }
        }
        public RectTransform NodeTarget(int id)
        {
            if(id>=6&&id<=8)return (RectTransform)stationButtons[id-6].transform;
            int index=System.Array.FindIndex(CourierStreetMap.Nodes,n=>n.StopId==id);
            return streetLabels[index].rectTransform;
        }
        private void RefreshStations(CourierRoutePanel panel,Vector2 size)
        {
            var run=primary.Owner!=null?primary.Owner.Run:Preview;
            bool usable=primary.Owner!=null&&primary.Owner.Active&&!primary.Owner.ConversationActive&&run.State==ShiftState.Docked&&!run.Finished;
            if(stationButtons==null||stationButtons.Length!=3||protectionStatus==null)
            {
                stationButtons=new Button[3];
                for(int i=0;i<3;i++)
                {
                    int target=6+i;var rect=CreateRect(landmarkLayer,"StationButton_"+target);
                    var image=GetOrAdd<Image>(rect.gameObject);image.color=CourierColors.Alpha(CourierColors.Raised,.98f);
                    var outline=GetOrAdd<Outline>(rect.gameObject);outline.effectColor=CourierColors.Road;outline.effectDistance=new Vector2(1,-1);
                    var button=GetOrAdd<Button>(rect.gameObject);button.targetGraphic=image;
                    button.onClick.AddListener(()=>panel.Select(target));
                    var hover=GetOrAdd<CourierMapNodeHover>(rect.gameObject);hover.panel=panel;hover.node=target;
                    var textRect=CreateRect(rect,"Label");Stretch(textRect);
                    var text=GetOrAdd<Text>(textRect.gameObject);text.font=panel.stopLabels[0].font;text.fontSize=22;text.alignment=TextAnchor.MiddleCenter;text.color=CourierColors.Paper;text.raycastTarget=false;
                    stationButtons[i]=button;
                }
                var statusRect=CreateRect(transform,"ProtectionStatus");protectionStatus=GetOrAdd<Text>(statusRect.gameObject);
                protectionStatus.font=panel.stopLabels[0].font;protectionStatus.fontSize=24;protectionStatus.color=CourierColors.Muted;protectionStatus.raycastTarget=false;
            }
            for(int i=0;i<3;i++)
            {
                var button=stationButtons[i];int target=6+i;
                var point=CourierRouteGraphic.NodePoint(run.Stops[target],size);var rect=(RectTransform)button.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(point.x+(i==2?126:-126),-point.y);rect.sizeDelta=new Vector2(180,68);
                // Keep the larger facility control clear of character landmarks and their labels.
                var preferred=new Vector2(point.x+(i==2?126:-126),point.y);
                var candidates=new[]{preferred,new Vector2(point.x+126,point.y),new Vector2(point.x-126,point.y-84),new Vector2(point.x+126,point.y+84),new Vector2(point.x,point.y+84)};
                foreach(var center in candidates)
                {
                    var bounds=new Rect(center.x-90,center.y-34,180,68);
                    if(bounds.xMin<0||bounds.yMin<0||bounds.xMax>size.x||bounds.yMax>size.y)continue;
                    bool overlaps=false;
                    foreach(var graphic in panel.stopLabels.Cast<Graphic>().Concat(panel.stopCounts.Cast<Graphic>()).Concat(landmarkImages.Cast<Graphic>()))
                    {
                        graphic.rectTransform.GetWorldCorners(stationCorners);
                        var lo=transform.InverseTransformPoint(stationCorners[0]);var hi=transform.InverseTransformPoint(stationCorners[2]);var mapRect=((RectTransform)transform).rect;
                        var occupied=Rect.MinMaxRect(lo.x-mapRect.xMin,mapRect.yMax-hi.y,hi.x-mapRect.xMin,mapRect.yMax-lo.y);
                        if(bounds.Overlaps(occupied)){overlaps=true;break;}
                    }
                    if(!overlaps){rect.anchoredPosition=new Vector2(center.x,-center.y);break;}
                }
                button.interactable=usable;
                string title=i==0?"H · Heat shield":i==1?"W · Waterproof":"Q · Suspension";
                button.GetComponentInChildren<Text>().text=title+"\n"+"Exchange · +1 turn";
                var colors=button.colors;colors.normalColor=run.HasProtection(target)?CourierColors.Mint:Color.white;colors.highlightedColor=CourierColors.Hover;button.colors=colors;
            }
            var sr=protectionStatus.rectTransform;sr.anchorMin=sr.anchorMax=sr.pivot=new Vector2(0,1);sr.anchoredPosition=new Vector2(24,-8);sr.sizeDelta=new Vector2(760,38);
            protectionStatus.text="Protection   "+run.ProtectionSummary;
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
                outline.effectColor=CourierColors.Alpha(CourierColors.Night,.9f);outline.effectDistance=new Vector2(1,-1);
                if(landmarkHitAreas[i]==null)landmarkHitAreas[i]=EnsureImage(panel.stopButtons[i].transform,"LandmarkHitArea");
                // A fully transparent mesh is culled by Unity and cannot receive pointer events.
                landmarkHitAreas[i].color=new Color(1,1,1,.001f);landmarkHitAreas[i].raycastTarget=true;landmarkHitAreas[i].transform.SetAsFirstSibling();
            }
        }
        private static T GetOrAdd<T>(GameObject obj) where T:Component
        {var component=obj.GetComponent<T>();return component!=null?component:obj.AddComponent<T>();}
        private static RectTransform CreateRect(Transform parent,string name)
        {
            var existing=parent.Find(name) as RectTransform;if(existing!=null)return existing;
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);return (RectTransform)obj.transform;
        }
        private static Image EnsureImage(Transform parent,string name)
        {
            var old=parent.Find(name);var rect=old!=null?(RectTransform)old:CreateRect(parent,name);
            return GetOrAdd<Image>(rect.gameObject);
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
