using System.Collections.Generic;
using UnityEngine;

namespace NightCourier
{
    // Animate artwork, leaving the landmark hit regions and map graph fixed.
    public sealed class CourierMapMotion : MonoBehaviour
    {
        public CourierRoutePanel panel;
        private readonly Dictionary<RectTransform,Vector3> originalScales=new Dictionary<RectTransform,Vector3>();
        private CourierRoundedRing selectionRing;
        private CourierClickBurst burst;
        private int pulseNode=-1,lastSelected=-1,lastHovered=-1;
        private float pulseAge=1,ringAlpha,routeArrival=1;
        private StreetEdge[] lastPath;
        private readonly Vector3[] corners=new Vector3[4];
        private RectTransform Visual(int node)
        {
            var art=panel.mapArtwork;if(art==null)return null;
            if(node>=0&&node<art.landmarkImages.Length&&art.landmarkImages[node]!=null)return art.landmarkImages[node].rectTransform;
            if(node>=6&&node<=8&&art.stationButtons!=null&&art.stationButtons.Length>node-6&&art.stationButtons[node-6]!=null)
                return art.stationButtons[node-6].transform.Find("StationVehicleArtwork") as RectTransform;
            return null;
        }
        public void PlaySelection(int node)
        {
            if(panel==null||panel.MapReducedMotion||!panel.CanNavigateMap)return;
            pulseNode=node;pulseAge=0;
            routeArrival=0;panel.mapGraphic.highlightProgress=0;
            var target=Visual(node)??panel.mapArtwork.NodeTarget(node);
            if(target==null)return;
            if(burst==null)
            {
                var go=new GameObject("MapSelectionRipple",typeof(RectTransform),typeof(CanvasRenderer),typeof(CourierClickBurst));
                go.transform.SetParent(transform,false);burst=go.GetComponent<CourierClickBurst>();
            }
            Fit(burst.rectTransform,target,16);burst.Play(CourierColors.Gold);
        }
        private void Fit(RectTransform rect,RectTransform target,float padding)
        {
            target.GetWorldCorners(corners);var parent=(RectTransform)transform;
            var lo=parent.InverseTransformPoint(corners[0]);var hi=parent.InverseTransformPoint(corners[2]);
            rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.pivot=Vector2.one*.5f;
            rect.anchoredPosition=(Vector2)((lo+hi)*.5f)-parent.rect.center;
            rect.sizeDelta=new Vector2(hi.x-lo.x+padding,hi.y-lo.y+padding);
        }
        private void LateUpdate()
        {
            if(panel==null||panel.mapArtwork==null)return;
            bool active=panel.CanNavigateMap&&panel.mapArtwork.isActiveAndEnabled;
            bool reduced=panel.MapReducedMotion;
            int selected=active?panel.SelectedMapNode:-1,hovered=active?panel.HoveredMapNode:-1;
            float dt=Time.unscaledDeltaTime,blend=reduced?1:1-Mathf.Exp(-18*dt);
            pulseAge=Mathf.Min(1,pulseAge+dt/.3f);
            for(int node=0;node<9;node++)
            {
                var target=Visual(node);if(target==null)continue;
                if(!originalScales.TryGetValue(target,out var original)){original=target.localScale;originalScales.Add(target,original);}
                float scale=1;
                if(!reduced&&active)
                {
                    scale+=hovered==node?.065f:selected==node?.025f:0;
                    if(node==pulseNode)scale+=Mathf.Sin(pulseAge*Mathf.PI)*(1-pulseAge)*.09f;
                }
                target.localScale=Vector3.Lerp(target.localScale,original*scale,blend);
                if((target.localScale-original*scale).sqrMagnitude<.000001f)target.localScale=original*scale;
            }
            var selectedTarget=Visual(selected)??(selected>=9?panel.mapArtwork.NodeTarget(selected):null);
            if(selectionRing==null&&selectedTarget!=null)
            {
                var go=new GameObject("MapSelectedOutline",typeof(RectTransform),typeof(CanvasRenderer),typeof(CourierRoundedRing));
                go.transform.SetParent(transform,false);selectionRing=go.GetComponent<CourierRoundedRing>();selectionRing.raycastTarget=false;
            }
            ringAlpha=Mathf.MoveTowards(ringAlpha,selectedTarget!=null?1:0,reduced?1:dt/.14f);
            if(selectionRing!=null)
            {
                if(selectedTarget!=null)Fit(selectionRing.rectTransform,selectedTarget,12);
                selectionRing.color=CourierColors.Alpha(CourierColors.Gold,ringAlpha*.85f);
                selectionRing.enabled=ringAlpha>0;
            }
            var roads=panel.mapGraphic.PreviewRoute.Count>0?panel.mapGraphic.PreviewRoute:panel.mapGraphic.SelectedRoute;
            var path=roads.Count>0?roads[0].StreetPath:null;
            if(selected!=lastSelected||hovered!=lastHovered||path!=lastPath){routeArrival=reduced?1:0;lastSelected=selected;lastHovered=hovered;lastPath=path;}
            if(reduced)
            {
                routeArrival=1;
                if(panel.mapGraphic.highlightVisibility<1||panel.mapGraphic.highlightProgress<1)
                {panel.mapGraphic.highlightVisibility=panel.mapGraphic.highlightProgress=1;panel.mapGraphic.SetVerticesDirty();}
            }
            else if(routeArrival<1)
            {
                routeArrival=Mathf.Min(1,routeArrival+dt/(hovered>=0?.28f:.65f));
                panel.mapGraphic.highlightVisibility=Mathf.Lerp(.45f,1,routeArrival);
                panel.mapGraphic.highlightProgress=1-Mathf.Pow(1-routeArrival,2);
                panel.mapGraphic.SetVerticesDirty();
            }
            if((!active||reduced)&&burst!=null)burst.enabled=false;
        }
        private void OnDisable()
        {
            foreach(var entry in originalScales)if(entry.Key!=null)entry.Key.localScale=entry.Value;
            if(selectionRing!=null)selectionRing.enabled=false;
            if(burst!=null)burst.enabled=false;
            ringAlpha=0;pulseAge=1;lastSelected=lastHovered=-1;routeArrival=1;
            if(panel!=null&&panel.mapGraphic!=null){panel.mapGraphic.highlightVisibility=panel.mapGraphic.highlightProgress=1;panel.mapGraphic.SetVerticesDirty();}
        }
    }
}
