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
        public Image routeImage;
        public RectTransform landmarkLayer;
        public Image[] landmarkImages=new Image[0];
        public Image[] landmarkHitAreas=new Image[0];
        public CourierRouteGraphic travelOverlay;
        public CourierVehicleIconView travelerIcon;
        [Min(24)] public float travelerSize=56;
        private CourierRouteGraphic primary;
        private Color originalIdle,originalAdjacent,originalSelected;
        private bool capturedColors;
        private bool originalDrawRegions;
        private bool originalBaseRoads,originalBaseNodes;
        private Material terrainBlendMaterial,originalTerrainMaterial;
        private static readonly int EdgeFadeId=Shader.PropertyToID("_EdgeFade");
        private static readonly int SpriteUVRectId=Shader.PropertyToID("_SpriteUVRect");
        private static readonly CourierRun Preview=new CourierRun();

        public void Apply(CourierRoutePanel panel,Stop[] stops=null)
        {
            if(!enabled)return;
            if(catalog==null)catalog=Resources.Load<CourierMapArtCatalog>("NightVoyage/MapArtwork");
            var map=(RectTransform)transform;
            primary=panel.mapGraphic;
            if(!capturedColors)
            {originalIdle=primary.idleRoadColor;originalAdjacent=primary.adjacentRoadColor;originalSelected=primary.selectedRoadColor;originalDrawRegions=primary.drawRegions;originalBaseRoads=primary.drawBaseRoads;originalBaseNodes=primary.drawBaseNodes;capturedColors=true;}
            EnsureLayers(panel);
            terrainImage.sprite=catalog!=null?catalog.terrain:null;
            terrainImage.color=catalog!=null?catalog.terrainTint:Color.white;
            bool hasTerrain=terrainImage.sprite!=null;
            terrainImage.gameObject.SetActive(hasTerrain);
            // Fit the *map canvas*, not the sprite's aspect, so pixels and graph coordinates
            // share exactly the same letterboxing at every window size.
            FitMapImage(terrainImage,map.rect.size);
            ConfigureTerrainEdge();
            routeImage.sprite=catalog!=null?catalog.routeArtwork:null;
            routeImage.color=Color.white;
            bool hasRouteArtwork=routeImage.sprite!=null;
            routeImage.gameObject.SetActive(hasRouteArtwork);
            FitMapImage(routeImage,map.rect.size);
            primary.drawBaseRoads=!hasRouteArtwork;
            primary.drawBaseNodes=!hasRouteArtwork;
            primary.drawRegions=!hasTerrain;
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
                if(background!=null){background.canvasRenderer.SetAlpha(0);background.raycastTarget=false;}
                foreach(var graphic in panel.stopButtons[i].GetComponentsInChildren<Graphic>())
                    graphic.raycastTarget=false;
                var feedback=panel.stopButtons[i].GetComponent<CourierButtonFeedback>();
                if(feedback!=null){feedback.feedbackTarget=rt;feedback.suppressOutline=true;}
                // The legacy button rectangle is an input region, not the landmark's visual bounds.
                var focus=panel.stopButtons[i].GetComponent<CourierFocusRing>();
                if(focus!=null){if(focus.ring!=null)focus.ring.enabled=false;focus.ring=null;}
                // Hit region is a child of the original Button, so all existing click/hover handlers remain in use.
                var button=(RectTransform)panel.stopButtons[i].transform;
                var hit=landmarkHitAreas[i].rectTransform;hit.anchorMin=hit.anchorMax=new Vector2(0,1);hit.pivot=rt.pivot;hit.sizeDelta=rt.sizeDelta;
                hit.anchoredPosition=rt.anchoredPosition-button.anchoredPosition;
                landmarkHitAreas[i].raycastTarget=true;
                EnsureNodeHit(button,new Vector2(point.x,-point.y)-button.anchoredPosition);
                var label=panel.stopLabels[i];var lr=label.rectTransform;
                float labelHeight=52;
                float labelTop=Mathf.Clamp(point.y+12,4,Mathf.Max(4,map.rect.height-labelHeight-4));
                lr.anchoredPosition=new Vector2(point.x-112-button.anchoredPosition.x,-labelTop-button.anchoredPosition.y);
                lr.sizeDelta=new Vector2(224,labelHeight);label.alignment=TextAnchor.UpperCenter;label.fontSize=28;label.raycastTarget=true;
                var counts=panel.stopCounts[i].rectTransform;
                counts.anchorMin=counts.anchorMax=counts.pivot=new Vector2(0,1);
                counts.anchoredPosition=new Vector2(point.x-112-button.anchoredPosition.x,-labelTop-labelHeight-4-button.anchoredPosition.y);
                counts.sizeDelta=new Vector2(224,40);panel.stopCounts[i].alignment=TextAnchor.UpperCenter;
                if(labelTop+labelHeight+44>map.rect.height)
                {
                    counts.anchoredPosition=new Vector2(point.x+116-button.anchoredPosition.x,-labelTop-8-button.anchoredPosition.y);
                    counts.sizeDelta=new Vector2(140,40);panel.stopCounts[i].alignment=TextAnchor.UpperLeft;
                }
                float badgeX=counts.anchoredPosition.x+(panel.stopCounts[i].alignment==TextAnchor.UpperLeft?-36:72);
                var badge=CourierSymbolUI.Icon(button,"OrderBadgeSymbol",CourierIconKind.Box,badgeX,-counts.anchoredPosition.y+4,28,CourierColors.Muted);
                badge.gameObject.SetActive(!string.IsNullOrWhiteSpace(panel.stopCounts[i].text));
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
                    label.color=CourierColors.Muted;label.fontSize=24;
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
                var p=CourierRouteGraphic.MapPoint(node.Point,size);rect.anchoredPosition=new Vector2(p.x,-p.y-7);rect.sizeDelta=new Vector2(name.Length>3?144:64,44);
                EnsureNodeHit(rect,new Vector2(0,7));
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
                    var image=GetOrAdd<Image>(rect.gameObject);image.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Button");image.type=Image.Type.Sliced;image.color=CourierColors.Paper;
                    var outline=GetOrAdd<Outline>(rect.gameObject);outline.enabled=false;
                    var button=GetOrAdd<Button>(rect.gameObject);button.targetGraphic=image;
                    button.onClick.AddListener(()=>panel.Select(target));
                    var hover=GetOrAdd<CourierMapNodeHover>(rect.gameObject);hover.panel=panel;hover.node=target;
                    var textRect=CreateRect(rect,"Label");Stretch(textRect);
                    var text=GetOrAdd<Text>(textRect.gameObject);text.font=panel.stopLabels[0].font;text.fontSize=22;text.alignment=TextAnchor.MiddleCenter;text.color=CourierColors.Night;text.raycastTarget=false;
                    stationButtons[i]=button;
                }
                var statusRect=CreateRect(transform,"ProtectionStatus");protectionStatus=GetOrAdd<Text>(statusRect.gameObject);
                protectionStatus.font=CourierTypography.Body;protectionStatus.fontSize=24;protectionStatus.color=CourierColors.Muted;protectionStatus.raycastTarget=false;
            }
            for(int i=0;i<3;i++)
            {
                var button=stationButtons[i];int target=6+i;
                var point=CourierRouteGraphic.NodePoint(run.Stops[target],size);var rect=(RectTransform)button.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(point.x+(i==2?80:-80),-point.y);rect.sizeDelta=new Vector2(72,72);
                // Keep the larger facility control clear of character landmarks and their labels.
                var preferred=new Vector2(point.x+(i==2?80:-80),point.y);
                var candidates=new[]{preferred,new Vector2(point.x+80,point.y),new Vector2(point.x-80,point.y-88),new Vector2(point.x+80,point.y+88),new Vector2(point.x,point.y+88)};
                foreach(var center in candidates)
                {
                    var bounds=new Rect(center.x-36,center.y-36,72,72);
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
                EnsureNodeHit(rect,new Vector2(point.x,-point.y)-rect.anchoredPosition);
                button.interactable=usable;
                string title=i==0?"H · Heat shield":i==1?"W · Waterproof":"Q · Suspension";
                var stationLabel=button.GetComponentInChildren<Text>();
                stationLabel.text=title+"\n"+"Swap · +1 turn";
                CourierSymbolUI.Button(button,i==0?CourierIconKind.Heat:i==1?CourierIconKind.Dry:CourierIconKind.Fragile,CourierInformation.Station(run,target));
                var oldSymbol=button.transform.Find("ActionSymbol");if(oldSymbol!=null)oldSymbol.GetComponent<CourierIconGraphic>().enabled=false;
                var vehicle=CourierVehicleIconView.Ensure(button.transform,"StationVehicleArtwork",Vector2.zero,Vector2.one*56);
                vehicle.SetVehicle(i);var vehicleRect=(RectTransform)vehicle.transform;vehicleRect.anchorMin=vehicleRect.anchorMax=vehicleRect.pivot=Vector2.one*.5f;vehicleRect.anchoredPosition=Vector2.zero;
                var background=button.targetGraphic as Image;
                if(background!=null){background.sprite=Resources.Load<Sprite>("NightVoyage/HUD/Button");background.type=Image.Type.Sliced;background.color=CourierColors.Paper;}
                foreach(var border in button.GetComponents<Outline>())border.enabled=false;
                stationLabel.rectTransform.offsetMin=new Vector2(12,8);stationLabel.rectTransform.offsetMax=new Vector2(-12,-8);
                var colors=button.colors;colors.normalColor=run.HasProtection(target)?CourierColors.Mint:Color.white;colors.highlightedColor=CourierColors.Hover;button.colors=colors;
            }
            var sr=protectionStatus.rectTransform;sr.anchorMin=sr.anchorMax=sr.pivot=new Vector2(0,1);sr.anchoredPosition=new Vector2(24,-8);sr.sizeDelta=new Vector2(760,38);
            protectionStatus.enabled=false;
        }
        private void EnsureLayers(CourierRoutePanel panel)
        {
            if(terrainImage==null)terrainImage=EnsureImage(transform,"TerrainArtwork");
            Stretch(terrainImage.rectTransform);terrainImage.raycastTarget=false;terrainImage.transform.SetAsFirstSibling();
            if(routeImage==null)routeImage=EnsureImage(transform,"RouteArtwork");
            routeImage.raycastTarget=false;routeImage.transform.SetSiblingIndex(1);
            panel.mapGraphic.transform.SetSiblingIndex(2);
            if(landmarkLayer==null)
            {
                var old=transform.Find("LandmarkArtwork");
                landmarkLayer=old!=null?(RectTransform)old:CreateRect(transform,"LandmarkArtwork");
            }
            Stretch(landmarkLayer);landmarkLayer.SetSiblingIndex(3);
            if(travelOverlay==null)
            {
                var old=transform.Find("TravelOverlay");
                var rect=old!=null?(RectTransform)old:CreateRect(transform,"TravelOverlay");
                travelOverlay=rect.GetComponent<CourierRouteGraphic>()??rect.gameObject.AddComponent<CourierRouteGraphic>();
            }
            Stretch(travelOverlay.rectTransform);travelOverlay.raycastTarget=false;travelOverlay.transform.SetSiblingIndex(4);
            if(travelerIcon==null)travelerIcon=CourierVehicleIconView.Ensure(travelOverlay.transform,"TravelerIcon",Vector2.zero,Vector2.one*Mathf.Max(24,travelerSize));
            if(travelerIcon.ghostArtwork==null)travelerIcon.ghostArtwork=Resources.Load<Sprite>("NightVoyage/VehicleIcons/Ghost_");
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
        // A separate transparent surface covers the drawn circle as well as the label/building.
        // Pointer events bubble to the same Button and hover handler for both surfaces.
        private static void EnsureNodeHit(RectTransform parent,Vector2 position)
        {
            var hit=EnsureImage(parent,"NodeHitArea");var rect=hit.rectTransform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);
            // Landmark and station parents use top-left / center anchors respectively.
            rect.anchorMin=rect.anchorMax=parent.pivot;
            rect.pivot=Vector2.one*.5f;rect.anchoredPosition=position;rect.sizeDelta=Vector2.one*56;
            hit.color=new Color(1,1,1,.001f);hit.raycastTarget=true;
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
        private static void FitMapImage(Image image,Vector2 size)
        {
            var rect=image.rectTransform;
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=CourierRouteGraphic.MapBounds(size).size;
            image.preserveAspect=false;
        }
        private void ConfigureTerrainEdge()
        {
            if(terrainBlendMaterial==null)
            {
                var source=Resources.Load<Material>("NightVoyage/MapEdgeBlend");
                if(source==null)return;
                originalTerrainMaterial=terrainImage.material;
                terrainBlendMaterial=new Material(source){name="Map Edge Blend (Instance)",hideFlags=HideFlags.DontSave};
                terrainImage.material=terrainBlendMaterial;
            }
            terrainBlendMaterial.SetFloat(EdgeFadeId,catalog!=null?catalog.terrainEdgeFeather:.16f);
            var uv=terrainImage.sprite!=null?UnityEngine.Sprites.DataUtility.GetOuterUV(terrainImage.sprite):new Vector4(0,0,1,1);
            terrainBlendMaterial.SetVector(SpriteUVRectId,uv);
            // A stencil Mask may cache a rendering copy of the image material.
            var rendered=terrainImage.materialForRendering;
            if(rendered!=null&&rendered!=terrainBlendMaterial)
            {
                rendered.SetFloat(EdgeFadeId,catalog!=null?catalog.terrainEdgeFeather:.16f);
                rendered.SetVector(SpriteUVRectId,uv);
            }
        }
        private void OnDisable()
        {
            if(terrainBlendMaterial!=null)
            {
                if(terrainImage!=null&&terrainImage.material==terrainBlendMaterial)terrainImage.material=originalTerrainMaterial;
                if(Application.isPlaying)Destroy(terrainBlendMaterial);else DestroyImmediate(terrainBlendMaterial);
                terrainBlendMaterial=null;
            }
            if(primary!=null)
            {primary.drawTravel=true;if(capturedColors){primary.idleRoadColor=originalIdle;primary.adjacentRoadColor=originalAdjacent;primary.selectedRoadColor=originalSelected;primary.drawRegions=originalDrawRegions;primary.drawBaseRoads=originalBaseRoads;primary.drawBaseNodes=originalBaseNodes;}primary.SetVerticesDirty();}
            if(terrainImage!=null)terrainImage.gameObject.SetActive(false);
            if(routeImage!=null)routeImage.gameObject.SetActive(false);
            if(landmarkLayer!=null)landmarkLayer.gameObject.SetActive(false);
            if(travelOverlay!=null)travelOverlay.gameObject.SetActive(false);
            foreach(var hit in landmarkHitAreas)if(hit!=null)hit.gameObject.SetActive(false);
        }
    }
}
