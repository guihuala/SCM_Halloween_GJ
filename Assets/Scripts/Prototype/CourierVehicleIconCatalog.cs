using System;
using UnityEngine;
namespace NightCourier
{
    [Serializable]
    public sealed class CourierVehicleIcon
    {
        [Tooltip("Stable Vehicles.csv ID: 0 scooter, 1 bicycle, 2 pickup.")]
        public int vehicleId;
        public Sprite sprite;
    }
    [CreateAssetMenu(menuName="Night Courier/Vehicle Icon Catalog")]
    public sealed class CourierVehicleIconCatalog : ScriptableObject
    {
        public CourierVehicleIcon[] icons=new CourierVehicleIcon[0];
        public Sprite ForVehicle(int id)
        {
            if(icons!=null)foreach(var entry in icons)if(entry!=null&&entry.vehicleId==id)return entry.sprite;
            return null;
        }
    }
}
