using System;
using UnityEngine;

namespace NightCourier
{
    [Serializable]
    public sealed class CourierCargoIcon
    {
        [Tooltip("Stable parcel ID (row ID in Parcels.csv).")]
        public int parcelId;
        public string displayName;
        public Sprite sprite;
    }

    [CreateAssetMenu(menuName = "Night Courier/Cargo Icon Catalog")]
    public sealed class CourierCargoIconCatalog : ScriptableObject
    {
        public CourierCargoIcon[] icons = new CourierCargoIcon[0];
        public Sprite ForParcel(int parcelId)
        {
            if(icons!=null)foreach(var icon in icons)
                if(icon!=null&&icon.parcelId==parcelId)return icon.sprite;
            return null;
        }
    }
}
