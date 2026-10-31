using System;
using System.Linq;
namespace NightCourier
{
    public static class CourierRoadEffectsChecks
    {
        public static string RunAll()
        {
            int count=0;
            foreach(var surface in new[]{StreetSurface.Heat,StreetSurface.Water,StreetSurface.Bumpy})foreach(int vehicle in new[]{-1,0,1,2})
            {
                var r=new CourierRun();r.LoadParcel(r.Parcels[0]);r.Parcels[0].HeatSensitive=r.Parcels[0].Dry=r.Parcels[0].Fragile=true;r.VehicleIndex=vehicle;
                var edge=new StreetEdge(CourierStreetMap.Nodes[0],CourierStreetMap.Nodes[3],surface,null);
                r.Roads[0]=new Road(0,3,6,0){StreetPath=new[]{edge}};
                r.Depart(r.Roads[0]);r.FastForwardTravel();
                bool protectedRoad=vehicle==(surface==StreetSurface.Heat?0:surface==StreetSurface.Water?1:2);
                if(r.Results.Single().Damaged==protectedRoad||r.OrderTurns!=1)throw new Exception("Wrong surface protection or movement cost");count++;
            }
            for(int station=6;station<=8;station++)
            {
                var r=new CourierRun();r.LoadParcel(r.Parcels[0]);r.Node=station;
                if(r.HasProtection(station)||!r.Possess(station-6)||r.OrderTurns!=1||r.Durability!=5)throw new Exception("Station exchange rules wrong");
                r.Node=6+(station-5)%3;int next=r.Node-6;int old=r.VehicleIndex;
                if(!r.Possess(next)||r.Durabilities[old]!=0||r.OrderTurns!=2||r.HasProtection(station))throw new Exception("Old vehicle/protection retained");
                r.UseGhost();if(r.HeatShield||r.Waterproof||r.Suspension)throw new Exception("Ghost retained protection");count++;
            }
            return count+" surface and exchange cases passed";
        }
    }
}
