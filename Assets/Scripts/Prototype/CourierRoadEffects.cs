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
        public string RouteWarnings(IEnumerable<Road> route)
        {
            var roads=route.ToArray();var edges=roads.Where(r=>r.StreetPath!=null).SelectMany(r=>r.A==Node?r.StreetPath:r.StreetPath.Reverse()).ToArray();
            if(edges.Length==0)return "";
            var warnings=new List<string>();var cargo=Parcels.Where(p=>p.State==ParcelState.Loaded).ToArray();
            var damage=new HashSet<string>();var node=CourierStreetMap.Nodes.First(n=>n.StopId==Node);
            int fuel=IsGhost?0:(int)Durability,used=0,turns=0,eventStops=0;bool vehicleTurn=false;
            foreach(var edge in edges)
            {
                if(used==0){vehicleTurn=!IsGhost&&fuel>0;if(vehicleTurn)fuel--;turns++;}
                foreach(var p in cargo)
                    if(!p.Damaged&&SurfaceDamages(edge.Surface,(p.HeatSensitive?1:0)|(p.Dry?2:0)|(p.Fragile?4:0),vehicleTurn?VehicleIndex:-1))
                        damage.Add(p.Name+" ("+SurfaceReason(edge.Surface)+")");
                node=edge.Other(node);used++;
                if(used>=(vehicleTurn?(int)Car.Speed:1)||node.StopId<9){used=0;if(DelayEventNodes.Contains(node.Id)||RetreatEventNodes.Contains(node.Id))eventStops++;}
            }
            var order=ActiveOrder;
            warnings.Add(order==null?turns+" travel turns; no order clock":"Estimated order: "+(OrderTurns+turns)+"/"+TargetTurns(order)+" turns"+(node.StopId==order.Destination?" at delivery":" at waypoint"));
            if(order!=null&&OrderTurns+turns>TargetTurns(order))warnings.Add("Will exceed the order target");
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
            var order=ActiveOrder;
            // Delivery has priority; failed probability rolls do not spend the order's event opportunity.
            var end=CourierStreetMap.Nodes.First(n=>n.StopId==destination);
            bool delay=DelayEventNodes.Contains(end.Id);
            bool retreat=RetreatEventNodes.Contains(end.Id);
            if(order!=null&&order.Destination!=destination&&!OrderEventTriggered&&(delay||retreat)&&random.NextDouble()<EventProbability)
            {
                var pool=Events.Select((e,i)=>new {e,i}).Where(x=>x.e.Effect==(delay?RoadEventEffect.Wait:RoadEventEffect.Retreat)).ToArray();
                if(pool.Length>0){EncounterIndex=pool[random.Next(pool.Length)].i;OrderEventTriggered=true;State=ShiftState.Encounter;EmitAudio(Events[EncounterIndex].SoundCue);return;}
            }
            CompleteStreetTurn();
        }
    }
}
