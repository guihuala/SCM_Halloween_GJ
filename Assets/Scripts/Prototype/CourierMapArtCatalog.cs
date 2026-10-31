using System;
using UnityEngine;

namespace NightCourier
{
    [Serializable]
    public sealed class CourierMapLandmark
    {
        [Tooltip("Stable Stops.csv ID; array order does not affect the binding.")]
        public int stopId;
        public Sprite sprite;
        [Tooltip("Anchor inside the square source canvas, measured from the bottom-left. Placeholder foot = (0.5, 0.15625).")]
        public Vector2 anchor=new Vector2(.5f,.15625f);
        [Tooltip("Visual adjustment in map UI units: right/up. The route node itself stays in place.")]
        public Vector2 offset;
        [Min(.1f)] public float scale=1;
    }

    [CreateAssetMenu(menuName="Night Courier/Map Art Catalog")]
    public sealed class CourierMapArtCatalog : ScriptableObject
    {
        [Header("Background artwork (1200 x 820 canvas, or 2400 x 1640)")]
        [Tooltip("Replace this sprite to change the map background. Draw terrain/decorations only; roads, nodes, landmarks and the ghost are separate live layers. Use the Export Map Art Guide menu for alignment.")]
        public Sprite terrain;
        [Tooltip("White preserves the original artwork colors.")]
        public Color terrainTint=Color.white;
        [Header("Hand-drawn routes (same full canvas as the background)")]
        [Tooltip("Optional transparent PNG containing base roads and node dots. Selected/hover routes, events and the traveler remain dynamic. Clear this field to restore procedural base roads.")]
        public Sprite routeArtwork;
        [Range(.1f,.4f)] public float landmarkHeightFraction=.28f;
        [Min(32)] public float maximumLandmarkSize=160;
        public Color idleRoadColor=new Color(.40f,.51f,.48f);
        public Color adjacentRoadColor=new Color(.45f,.70f,.61f);
        public Color selectedRoadColor=new Color(1,.66f,.34f);
        public CourierMapLandmark[] landmarks=new CourierMapLandmark[0];
        public CourierMapLandmark ForStop(int id)
        {
            if(landmarks!=null)foreach(var entry in landmarks)if(entry!=null&&entry.stopId==id)return entry;
            return null;
        }
    }
}
