using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // Animate artwork, leaving the landmark hit regions and map graph fixed.
    public sealed class CourierMapMotion : MonoBehaviour
    {
        public CourierRoutePanel panel;
        public Image TutorialTarget { get; set; }
        private readonly Dictionary<RectTransform,Vector3> originalScales=new Dictionary<RectTransform,Vector3>();
        private readonly Dictionary<Image,CourierLandmarkGlow> glows=new Dictionary<Image,CourierLandmarkGlow>();
        private readonly HashSet<Image> legendImages=new HashSet<Image>();
        private int pulseNode=-1,lastSelected=-1,lastHovered=-1;
        private float pulseAge=1,routeArrival=1;
        private StreetEdge[] lastPath;
        private void OnEnable()
        {
            var legacy=transform.Find("MapSelectedOutline");
            if(legacy!=null)legacy.gameObject.SetActive(false);
        }
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
        }
        private void LateUpdate()
        {
            if(panel==null||panel.mapArtwork==null)return;
            bool active=panel.CanNavigateMap&&panel.mapArtwork.isActiveAndEnabled;
            bool reduced=panel.MapReducedMotion;
            int selected=active?panel.SelectedMapNode:-1,hovered=active?panel.HoveredMapNode:-1;
            float dt=Time.unscaledDeltaTime,blend=reduced?1:1-Mathf.Exp(-18*dt);
            pulseAge=Mathf.Min(1,pulseAge+dt/.3f);
            legendImages.Clear();
            for(int node=0;node<9;node++)
            {
                var target=Visual(node);if(target==null)continue;
                if(!originalScales.TryGetValue(target,out var original)){original=target.localScale;originalScales.Add(target,original);}
                float scale=1;
                if(!reduced&&active)
                {
                    scale+=hovered==node||panel.LegendHighlightsNode(node)?.065f:selected==node?.025f:0;
                    if(node==pulseNode)scale+=Mathf.Sin(pulseAge*Mathf.PI)*(1-pulseAge)*.09f;
                }
                target.localScale=Vector3.Lerp(target.localScale,original*scale,blend);
                if((target.localScale-original*scale).sqrMagnitude<.000001f)target.localScale=original*scale;
                var image=target.GetComponent<Image>();
                if(active&&panel.LegendHighlightsNode(node)&&image!=null&&image.sprite!=null)
                {
                    legendImages.Add(image);
                    if(!glows.ContainsKey(image))glows.Add(image,CourierLandmarkGlow.Create(image));
                }
            }
            var selectedTarget=Visual(selected)??(selected>=9?panel.mapArtwork.NodeTarget(selected):null);
            var selectedImage=selectedTarget!=null?selectedTarget.GetComponent<Image>():null;
            if(selectedImage!=null&&selectedImage.sprite!=null&&!glows.ContainsKey(selectedImage))glows.Add(selectedImage,CourierLandmarkGlow.Create(selectedImage));
            var tutorialImage=active&&TutorialTarget!=null&&TutorialTarget.isActiveAndEnabled?TutorialTarget:null;
            if(tutorialImage!=null&&tutorialImage.sprite!=null&&!glows.ContainsKey(tutorialImage))glows.Add(tutorialImage,CourierLandmarkGlow.Create(tutorialImage));
            foreach(var entry in glows)if(entry.Value!=null)
                entry.Value.SetSelected(entry.Key==selectedImage||legendImages.Contains(entry.Key)||entry.Key==tutorialImage,reduced,entry.Key==tutorialImage);
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
        }
        private void OnDisable()
        {
            foreach(var entry in originalScales)if(entry.Key!=null)entry.Key.localScale=entry.Value;
            foreach(var glow in glows.Values)if(glow!=null)glow.ResetGlow();
            pulseAge=1;lastSelected=lastHovered=-1;routeArrival=1;
            if(panel!=null&&panel.mapGraphic!=null){panel.mapGraphic.highlightVisibility=panel.mapGraphic.highlightProgress=1;panel.mapGraphic.SetVerticesDirty();}
        }
    }
}
