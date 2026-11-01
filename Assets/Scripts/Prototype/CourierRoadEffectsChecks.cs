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
                var route=new[]{r.Roads[0]};
                bool expectedProtection=vehicle==(surface==StreetSurface.Heat?0:surface==StreetSurface.Water?1:2);
                var details=r.RouteSurfaceDetails(route);
                if(!details.Contains(CourierRun.SurfaceLabel(surface))||!details.Contains(r.Parcels[0].Name)||
                    details.Contains("Will damage:")==expectedProtection||details.Contains("Vehicle protects:")!=expectedProtection||
                    (r.RouteSurfaceRisks(route).Count==0)!=expectedProtection)
                    throw new Exception("Route forecast disagrees with surface protection");
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
            {
                var r=new CourierRun();var cargo=r.Parcels[0];r.LoadParcel(cargo);cargo.HeatSensitive=true;
                r.VehicleIndex=0;r.Durabilities[0]=1;
                var edges=new[]{
                    new StreetEdge(CourierStreetMap.Node("1"),CourierStreetMap.Node("U"),StreetSurface.Heat,null),
                    new StreetEdge(CourierStreetMap.Node("U"),CourierStreetMap.Node("V"),StreetSurface.Heat,null),
                    new StreetEdge(CourierStreetMap.Node("V"),CourierStreetMap.Node("J"),StreetSurface.Heat,null)};
                var route=new[]{new Road(0,CourierStreetMap.Node("J").StopId,18,0){StreetPath=edges}};
                var risks=r.RouteSurfaceRisks(route);var detail=r.RouteSurfaceDetails(route);
                if(risks.Count!=1||!risks.ContainsKey(edges[2])||!detail.Contains("Will damage:")||detail.Contains("Vehicle protects:")||!r.RouteWarnings(route).Contains("Vehicle expires"))
                    throw new Exception("Forecast lost final vehicle turn or protection expiry");
                cargo.Destination=CourierStreetMap.Node("V").StopId;
                if(r.RouteSurfaceRisks(route).Count!=0||!r.RouteSurfaceDetails(route).Contains("Vehicle protects:"))
                    throw new Exception("Delivered cargo warned about later surfaces");
                cargo.Damaged=true;
                if(r.RouteSurfaceDetails(route).Contains("Vehicle protects:")||r.RouteSurfaceRisks(route).Count!=0)
                    throw new Exception("Already damaged cargo presented as intact");
                r.Parcels.ForEach(p=>p.State=ParcelState.Waiting);
                if(!r.RouteSurfaceDetails(route).Contains("Heat")||!r.RouteSurfaceDetails(route).Contains("No vulnerable cargo"))
                    throw new Exception("Road conditions disappear without cargo");
                count+=4;
            }
            return count+" surface, forecast and exchange cases passed";
        }
    }
}
