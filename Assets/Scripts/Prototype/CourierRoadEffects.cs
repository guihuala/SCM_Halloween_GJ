using System;
using System.Collections.Generic;
using System.Linq;

namespace NightCourier
{
    public sealed partial class CourierRun
    {
        public string[] DelayEventNodes={"C","J"},RetreatEventNodes={"N","P","E"};
        public bool HeatShield {get{return !IsGhost&&VehicleIndex==0;}}
        public bool Waterproof {get{return !IsGhost&&VehicleIndex==1;}}
        public bool Suspension {get{return !IsGhost&&VehicleIndex==2;}}
        public bool CargoSuspension {get{return Suspension;}}
        public bool HasProtection(int station){return station==6?HeatShield:station==7?Waterproof:station==8&&Suspension;}
        public string ProtectionSummary {get{return IsGhost?"Ghost · no protection":Car.Ability;}}
        public string StationDescription(int station)
        {
            if(station<6||station>8)return "Choose a connected node or continue your planned journey.";
            return Vehicles[station-6].Ability+". Exchange here: +1 order turn, fresh 5 vehicle turns. Replaces the old vehicle.";
        }
        public int EdgeTurns(StreetEdge edge){return 1;}
        private readonly Dictionary<string,Road> streetRoutes=new Dictionary<string,Road>();
        private int CargoRiskMask
        {
            get { int mask=0;foreach(var p in Parcels)if(p.State==ParcelState.Loaded&&!p.Damaged)mask|=(p.HeatSensitive?1:0)|(p.Dry?2:0)|(p.Fragile?4:0);return mask; }
        }
        private static bool SurfaceDamages(StreetSurface surface,int mask,int vehicle)
        {return surface==StreetSurface.Heat&&(mask&1)!=0&&vehicle!=0||surface==StreetSurface.Water&&(mask&2)!=0&&vehicle!=1||surface==StreetSurface.Bumpy&&(mask&4)!=0&&vehicle!=2;}
        private Road StreetRoute(int target)
        {
            string key=Node+":"+target+":"+VehicleIndex+":"+Durability+":"+PreferSafeRoutes+":"+CargoRiskMask;
            Road route;if(streetRoutes.TryGetValue(key,out route))return route;
            int mask=CargoRiskMask;
            var path=PreferSafeRoutes&&mask!=0?CourierStreetMap.FindMovementPath(Node,target,IsGhost?1:(int)Car.Speed,(int)Durability,
                (edge,protectedTurn)=>!SurfaceDamages(edge.Surface,mask,protectedTurn?VehicleIndex:-1)):null;
            // Keep all destinations reachable even when cargo cannot be protected.
            if(path==null||path.Count==0)path=CourierStreetMap.FindMovementPath(Node,target,IsGhost?1:(int)Car.Speed,(int)Durability);
            if(path.Count==0)return null;
            route=new Road(Node,target,path.Count*6,0){StreetPath=path.ToArray()};
            streetRoutes[key]=route;return route;
        }
        // One committed movement turn: up to two edges, stopping early at NPCs and stations.
        private Road MovementLeg(Road route)
        {
            var edges=Node==route.A?route.StreetPath:route.StreetPath.Reverse().ToArray();
            var node=CourierStreetMap.Nodes.First(n=>n.StopId==Node);var path=new List<StreetEdge>();
            int speed=IsGhost||Durability<=0?1:(int)Car.Speed;
            foreach(var edge in edges)
            {
                path.Add(edge);node=edge.Other(node);
                if(path.Count>=speed||node.StopId<9)break;
            }
            return new Road(Node,node.StopId,path.Count*6,0){StreetPath=path.ToArray()};
        }
        private int EstimateStreetTurns(Road road)
        {
            var edges=Node==road.A?road.StreetPath:road.StreetPath.Reverse().ToArray();
            var node=CourierStreetMap.Nodes.First(n=>n.StopId==Node);
            int turns=0,used=0;float fuel=Durability;
            foreach(var edge in edges)
            {
                node=edge.Other(node);used++;
                int speed=IsGhost||fuel<=0?1:(int)Car.Speed;
                if(used>=speed||node.StopId<9){turns++;used=0;fuel--;}
            }
            return turns+(used>0?1:0);
        }
        // Same turn/protection rules as the route forecast, including vehicle exhaustion.
        public Dictionary<StreetEdge,List<Parcel>> RouteSurfaceRisks(IEnumerable<Road> route)
        {
            var risks=new Dictionary<StreetEdge,List<Parcel>>();
            VisitRouteSurfaces(route,(edge,cargo,vehicle)=>
            {
                var affected=cargo.Where(p=>SurfaceDamages(edge.Surface,ParcelRiskMask(p),vehicle)).ToList();
                if(affected.Count>0)risks[edge]=affected;
            });
            return risks;
        }
        private static int ParcelRiskMask(Parcel p)
        {return (p.HeatSensitive?1:0)|(p.Dry?2:0)|(p.Fragile?4:0);}
        public static string SurfaceLabel(StreetSurface surface)
        {return surface==StreetSurface.Heat?"Heat":surface==StreetSurface.Water?"Water":surface==StreetSurface.Bumpy?"Bumps":"Normal road";}
        public static string SurfaceRule(StreetSurface surface)
        {return surface==StreetSurface.Heat?"Damages heat-sensitive cargo without heat protection.":surface==StreetSurface.Water?"Damages dry cargo without waterproofing.":"Damages fragile cargo without suspension.";}
        // Shared traversal keeps both the visible forecast and map warnings aligned with
        // movement turns, deliveries en route, and protection on the last vehicle turn.
        private void VisitRouteSurfaces(IEnumerable<Road> route,Action<StreetEdge,List<Parcel>,int> visit)
        {
            var cargo=Parcels.Where(p=>p.State==ParcelState.Loaded&&!p.Damaged).ToList();
            var node=CourierStreetMap.Nodes.First(n=>n.StopId==Node);
            int fuel=IsGhost?0:(int)Durability,used=0;bool protectedTurn=false;
            foreach(var road in route)
            {
                if(road.StreetPath==null)continue;
                var edges=node.StopId==road.A?road.StreetPath:road.StreetPath.Reverse();
                foreach(var edge in edges)
                {
                    if(used==0){protectedTurn=!IsGhost&&fuel>0;if(protectedTurn)fuel--;}
                    visit(edge,cargo,protectedTurn?VehicleIndex:-1);
                    node=edge.Other(node);used++;
                    cargo.RemoveAll(p=>p.Destination==node.StopId);
                    if(used>=(protectedTurn?(int)Car.Speed:1)||node.StopId<9)used=0;
                }
            }
        }
        public string RouteSurfaceDetails(IEnumerable<Road> route)
        {
            var surfaces=new List<StreetSurface>();
            var damaged=new Dictionary<StreetSurface,HashSet<Parcel>>();
            var protectedCargo=new Dictionary<StreetSurface,HashSet<Parcel>>();
            VisitRouteSurfaces(route,(edge,cargo,vehicle)=>
            {
                var surface=edge.Surface;if(surface==StreetSurface.Normal)return;
                if(!surfaces.Contains(surface))
                {surfaces.Add(surface);damaged[surface]=new HashSet<Parcel>();protectedCargo[surface]=new HashSet<Parcel>();}
                foreach(var parcel in cargo.Where(p=>SurfaceDamages(surface,ParcelRiskMask(p),-1)))
                    (SurfaceDamages(surface,ParcelRiskMask(parcel),vehicle)?damaged[surface]:protectedCargo[surface]).Add(parcel);
            });
            if(surfaces.Count==0)return "Road conditions: Normal road";
            var lines=new List<string>{"Road conditions: "+string.Join(" / ",surfaces.Select(SurfaceLabel))};
            foreach(var surface in surfaces)
            {
                if(damaged[surface].Count>0)lines.Add(SurfaceLabel(surface)+" — Will damage: "+string.Join(", ",Parcels.Where(p=>damaged[surface].Contains(p)).Select(p=>p.Name)));
                // Do not claim protection for a parcel that later loses it on this surface.
                var safe=Parcels.Where(p=>protectedCargo[surface].Contains(p)&&!damaged[surface].Contains(p)).ToArray();
                if(safe.Length>0)lines.Add(SurfaceLabel(surface)+" — Vehicle protects: "+string.Join(", ",safe.Select(p=>p.Name)));
                if(damaged[surface].Count==0&&safe.Length==0)lines.Add(SurfaceLabel(surface)+" — No vulnerable cargo on board");
            }
            return string.Join("\n",lines);
        }
        public string RouteWarnings(IEnumerable<Road> route)
        {
            var roads=route.ToArray();var edges=roads.Where(r=>r.StreetPath!=null).SelectMany(r=>r.A==Node?r.StreetPath:r.StreetPath.Reverse()).ToArray();
            if(edges.Length==0)return "";
            var warnings=new List<string>();var cargo=Parcels.Where(p=>p.State==ParcelState.Loaded).ToArray();
            var damage=new HashSet<string>();var node=CourierStreetMap.Nodes.First(n=>n.StopId==Node);
            foreach(var entry in RouteSurfaceRisks(roads))
                foreach(var parcel in entry.Value)damage.Add(parcel.Name+" ("+SurfaceReason(entry.Key.Surface)+")");
            int fuel=IsGhost?0:(int)Durability,used=0,turns=0,eventStops=0;bool vehicleTurn=false;
            foreach(var edge in edges)
            {
                if(used==0){vehicleTurn=!IsGhost&&fuel>0;if(vehicleTurn)fuel--;turns++;}
                node=edge.Other(node);used++;
                if(used>=(vehicleTurn?(int)Car.Speed:1)||node.StopId<9){used=0;if(DelayEventNodes.Contains(node.Id)||RetreatEventNodes.Contains(node.Id))eventStops++;}
            }
            var order=ActiveOrder;
            if(order==null)warnings.Add(turns+" travel turns; no order clock");
            foreach(var carried in ActiveOrders)
            {
                int projected=TurnsFor(carried)+turns;
                warnings.Add(carried.Name+": "+projected+"/"+TargetTurns(carried)+" turns"+(node.StopId==carried.Destination?" at delivery":" at waypoint")+(projected>TargetTurns(carried)?" · will exceed target":""));
            }
            if(damage.Count>0)warnings.Add((PreferSafeRoutes?"No intact route available. ":"")+"Will damage: "+string.Join(", ",damage));
            else if(cargo.Length>0)warnings.Add(cargo.Any(p=>p.Damaged)?"Cargo already damaged; damage cannot be repaired":"Cargo intact on this route");
            if(!IsGhost&&turns>Durability)warnings.Add("Vehicle expires after "+(int)Durability+" turn(s); then ghost, without protection");
            if(order!=null&&!OrderEventTriggered&&eventStops>0&&EventProbability>0)warnings.Add("Event risk: "+Math.Round(EventProbability*100)+"% per eligible stop; may add turns");
            warnings.Add("Estimate excludes random events");
            return string.Join("\n",warnings);
        }
        private static string SurfaceReason(StreetSurface surface)
        {return surface==StreetSurface.Heat?"heat":surface==StreetSurface.Water?"water":"bumps";}
        private StreetEdge[] travelEdges;
        private void ResetStreetTravel()
        {
            travelEdges=Node==ActiveRoad.A?ActiveRoad.StreetPath:ActiveRoad.StreetPath.Reverse().ToArray();
        }
        private int StreetRemainingTurns(){return State==ShiftState.Driving?1:0;}
        private float StreetDisplayProgress()
        {return State==ShiftState.Driving?Math.Min(1,animationSeconds/SecondsPerTurn):Progress;}
        private int ApplyStreetSurface(int index,bool rollback=false)
        {
            var surface=travelEdges[index].Surface;
            foreach(var p in Parcels.Where(p=>p.State==ParcelState.Loaded&&!p.Damaged))
                if(surface==StreetSurface.Heat&&p.HeatSensitive&&!HeatShield||surface==StreetSurface.Water&&p.Dry&&!Waterproof||surface==StreetSurface.Bumpy&&p.Fragile&&!Suspension)
                {p.Damaged=true;p.DamageReason=SurfaceReason(surface);EmitAudio("damage");Log(p.Name+": damaged by "+p.DamageReason+".");}
            return 0;
        }
        private void CompleteStreetTurn()
        {
            // A final vehicle turn protects forward movement AND event rollback.
            if(!IsGhost&&Durability<=0)BreakDown();
            Arrive();
        }
        private void TickStreet(float seconds)
        {
            if(seconds<=0||State!=ShiftState.Driving)return;
            animationSeconds+=seconds;
            if(animationSeconds<SecondsPerTurn)return;
            for(int i=0;i<travelEdges.Length;i++)ApplyStreetSurface(i);
            AdvanceClock(1);Progress=1;animationSeconds=0;
            if(!IsGhost)Durabilities[VehicleIndex]=Math.Max(0,Durability-1);
            var orders=ActiveOrders.ToArray();
            // Delivery has priority; failed probability rolls do not spend the order's event opportunity.
            var end=CourierStreetMap.Nodes.First(n=>n.StopId==destination);
            bool delay=DelayEventNodes.Contains(end.Id);
            bool retreat=RetreatEventNodes.Contains(end.Id);
            if(orders.Length>0&&orders.All(p=>!p.EventTriggered)&&!ActiveOrders.Any(p=>p.Destination==destination)&&(delay||retreat)&&NextEventRoll()<EventProbability)
            {
                var pool=Events.Select((e,i)=>new {e,i}).Where(x=>x.e.Effect==(delay?RoadEventEffect.Wait:RoadEventEffect.Retreat)).ToArray();
                if(pool.Length>0){EncounterIndex=pool[(int)(NextEventRoll()*pool.Length)].i;foreach(var p in Parcels.Where(p=>p.State==ParcelState.Loaded))p.EventTriggered=true;State=ShiftState.Encounter;EmitAudio(Events[EncounterIndex].SoundCue);return;}
            }
            CompleteStreetTurn();
        }
    }
}
