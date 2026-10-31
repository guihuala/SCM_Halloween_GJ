using System;
using System.Linq;

namespace NightCourier
{
    public static class CourierChecks
    {
        private static int assertions;
        private static void Require(bool ok,string message){assertions++;if(!ok)throw new Exception(message);}
        public static Road To(CourierRun run,int node){return run.FindRoute(node).Single();}
        public static void Travel(CourierRun r,int node,bool safe=true,float delay=0){Go(r,node);}
        public static void Go(CourierRun r,int target)
        {
            for(int i=0;i<200&&r.Node!=target&&!r.Finished;i++)
            {
                Require(r.StartJourney(target),"Cannot continue from "+r.Node+" to "+target);
                r.FastForwardTravel();if(r.State==ShiftState.Encounter)r.ResolveEncounter(true);
                Require(r.State==ShiftState.Docked||r.Finished,"Movement did not end at a node");
            }
            Require(r.Node==target,"Destination not reached");
        }
        public static void CompleteAll(CourierRun r)
        {
            while(!r.Finished)
            {
                var p=r.ActiveOrder??r.Parcels.First(x=>x.State==ParcelState.Waiting);
                if(r.ActiveOrder==null){Go(r,p.Origin);Require(r.LoadParcel(p),"Order rejected");}
                Go(r,p.Destination);Require(p.State==ParcelState.Delivered,"Delivery blocked by failure");
            }
        }
        private static CourierRun EncounterFixture(string endId,int vehicle=-1,int fuel=5,StreetSurface surface=StreetSurface.Normal)
        {
            var r=new CourierRun(31);r.LoadParcel(r.Parcels[0]);r.VehicleIndex=vehicle;
            if(vehicle>=0)r.Durabilities[vehicle]=fuel;
            r.EventProbability=1;
            var start=CourierStreetMap.Nodes.First(n=>n.StopId==0);var end=CourierStreetMap.Node(endId);
            var edge=new StreetEdge(start,end,surface,null);
            r.Roads[0]=new Road(0,end.StopId,6,0){StreetPath=new[]{edge}};
            Require(r.Depart(r.Roads[0]),"Fixture departure failed");r.FastForwardTravel();return r;
        }
        private static void CheckEvents()
        {
            foreach(string id in new[]{"C","J","N","P","E"})foreach(int vehicle in new[]{-1,0,1,2})
            {
                var r=EncounterFixture(id,vehicle,1);
                Require(r.State==ShiftState.Encounter&&r.OrderEventTriggered,"Event missing at "+id);
                bool wait=id=="C"||id=="J";
                Require(r.Events[r.EncounterIndex].Effect==(wait?RoadEventEffect.Wait:RoadEventEffect.Retreat),"Wrong event pool");
                int turns=r.OrderTurns;float fuel=r.Durability;float frozen=r.DisplayProgress;
                r.Tick(10000);Require(r.OrderTurns==turns&&r.DisplayProgress==frozen,"Pending event advanced");
                r.ResolveEncounter(false);r.ResolveEncounter(true);
                Require(r.OrderTurns==turns+(wait?1:0)&&r.Durability==fuel,"Event charged extra vehicle use / duplicate resolution");
                Require(r.Node==(wait?CourierStreetMap.Node(id).StopId:0),"Retreat did not return exactly one node");
                Require(vehicle<0||r.IsGhost,"Final vehicle turn did not expire after event");
                Go(r,3);Require(r.CompletedBatches==1,"Event lost order or prevented continuation");
            }
            for(int vehicle=0;vehicle<3;vehicle++)
            {
                var surface=vehicle==0?StreetSurface.Heat:vehicle==1?StreetSurface.Water:StreetSurface.Bumpy;
                var r=new CourierRun();r.Parcels[0].HeatSensitive=r.Parcels[0].Dry=r.Parcels[0].Fragile=true;
                r.LoadParcel(r.Parcels[0]);r.VehicleIndex=vehicle;r.Durabilities[vehicle]=1;r.EventProbability=1;
                var edge=new StreetEdge(CourierStreetMap.Nodes[0],CourierStreetMap.Node("N"),surface,null);
                r.Roads[0]=new Road(0,edge.B.StopId,6,0){StreetPath=new[]{edge}};r.Depart(r.Roads[0]);r.FastForwardTravel();r.ResolveEncounter(true);
                Require(!r.ActiveOrder.Damaged&&r.IsGhost,"Rollback lost last-turn protection");
            }
            // Empty travel, non-event nodes, failed rolls, and the destination must not trigger.
            var empty=new CourierRun();empty.EventProbability=1;Go(empty,CourierStreetMap.Node("N").StopId);
            Require(!empty.OrderEventTriggered&&empty.OrderTurns==0,"Empty travel triggered an event or order clock");
            var noRoll=EncounterFixture("C");noRoll.ResolveEncounter(true);
            var once=noRoll.OrderTurns;Go(noRoll,CourierStreetMap.Node("J").StopId);
            Require(noRoll.OrderEventTriggered&&noRoll.State==ShiftState.Docked,"Same order triggered a second event");
            var misses=new CourierRun();misses.EventProbability=0;misses.LoadParcel(misses.Parcels[0]);Go(misses,CourierStreetMap.Node("C").StopId);
            Require(!misses.OrderEventTriggered,"Failed probability consumed the opportunity");
            misses.EventProbability=1;misses.StartJourney(CourierStreetMap.Node("J").StopId);misses.FastForwardTravel();
            Require(misses.State==ShiftState.Encounter,"Failed roll blocked later event");
            var destination=new CourierRun();destination.LoadParcel(destination.Parcels[0]);destination.Parcels[0].Destination=CourierStreetMap.Node("N").StopId;destination.EventProbability=1;Go(destination,destination.Parcels[0].Destination);
            Require(destination.CompletedBatches==1&&!destination.OrderEventTriggered,"Event took priority over delivery");
            var seen=new bool[12];
            for(int seed=0;seed<200;seed++)foreach(string endpoint in new[]{"C","N"})
            {
                var r=new CourierRun(seed);r.EventProbability=1;r.LoadParcel(r.Parcels[0]);
                var end=CourierStreetMap.Node(endpoint);r.Roads[0]=new Road(0,end.StopId,6,0){StreetPath=new[]{new StreetEdge(CourierStreetMap.Nodes[0],end,StreetSurface.Normal,null)}};
                r.Depart(r.Roads[0]);r.FastForwardTravel();seen[r.EncounterIndex]=true;
            }
            Require(seen.All(x=>x),"An event is unreachable");
        }
        public static string RunAll()
        {
            assertions=0;var r=new CourierRun();
            Require(r.BatchCount==11&&r.Parcels.Count==12,"Expected 11 orders / 12 cargo entries");
            Require(r.Events.Count(e=>e.Effect==RoadEventEffect.Wait)==7&&r.Events.Count(e=>e.Effect==RoadEventEffect.Retreat)==5,"Expected 7 delays and 5 retreats");
            Require(CourierStreetMap.Nodes.Length==32&&CourierStreetMap.Nodes.Select(n=>n.StopId).Distinct().Count()==32,"Street IDs invalid");
            foreach(var n in CourierStreetMap.Nodes)foreach(var to in CourierStreetMap.Nodes)
                if(n!=to)Require(CourierStreetMap.FindPath(n.StopId,to.StopId).Count>0,"Disconnected street node");
            CheckEvents();
            CheckPlanningAndTutorial();
            r=new CourierRun();r.Node=5;r.LoadParcel(r.Parcels[9]);
            Require(r.Parcels[8].State==ParcelState.Loaded&&!r.LoadParcel(r.Parcels[10])&&!r.Unload(r.Parcels[9]),"Batch exclusivity violated");
            Go(r,3);Require(r.Results.Count==1,"Combined batch not delivered");
            r=new CourierRun();r.LoadParcel(r.Parcels[0]);r.Parcels[0].Damaged=true;r.Parcels[0].TimeLimit=1;Go(r,3);
            Require(r.Results[0].Late&&r.Results[0].Damaged&&r.Results[0].Evaluation=="Delivery failed","Binary evaluation wrong");
            for(int v=-1;v<3;v++)
            {
                var fast=new CourierRun();var slow=new CourierRun();
                foreach(var run in new[]{fast,slow}){run.VehicleIndex=v;if(v>=0)run.Durabilities[v]=1;run.LoadParcel(run.Parcels[0]);run.StartJourney(3);}
                fast.Tick(10000);for(int i=0;i<1000&&slow.State==ShiftState.Driving;i++)slow.Tick(.013f);
                Require(fast.Node==slow.Node&&fast.OrderTurns==slow.OrderTurns&&fast.VehicleIndex==slow.VehicleIndex,"Frame size changed turn outcome");
            }
            for(int seed=0;seed<10;seed++)
            {r=new CourierRun(seed);CompleteAll(r);Require(r.Results.Count==11&&r.Delivered==12&&r.CompletedBatches==11,"Not all orders completable as ghost");}
            return assertions+" assertions passed; "+CourierRoadEffectsChecks.RunAll();
        }
        private static void CheckPlanningAndTutorial()
        {
            var run=new CourierRun();run.Node=2;run.VehicleIndex=0;run.EventProbability=0;
            Require(run.LoadParcel(run.Parcels[4]),"Cannot load route comparison fixture");
            var safe=run.FindRoute(5).Single();
            Require(run.TimeCost(safe)==4&&!safe.StreetPath.Any(e=>e.Surface==StreetSurface.Bumpy),"Safe route ignored fragile cargo");
            run.PreferSafeRoutes=false;var fast=run.FindRoute(5).Single();
            Require(run.TimeCost(fast)==3&&fast.StreetPath.Any(e=>e.Surface==StreetSurface.Bumpy),"Fastest route unavailable after switching modes");
            Require(run.RouteWarnings(new[]{fast}).Contains("Will damage: Spring Mead"),"Cargo-specific warning missing");
            run.PreferSafeRoutes=true;Go(run,5);
            Require(!run.Results.Single().Damaged&&!run.Results.Single().Late,"Safe route cannot be followed to a successful delivery");

            // Filtering must use the vehicle for each turn, not its starting protection.
            foreach(int fuel in new[]{1,2,5})
            {
                run=new CourierRun();run.Node=1;run.VehicleIndex=0;run.Durabilities[0]=fuel;run.EventProbability=0;run.LoadParcel(run.Parcels[1]);
                var route=run.FindRoute(0);string forecast=run.RouteWarnings(route);Go(run,0);
                Require(forecast.Contains("Will damage:")==run.Results.Single().Damaged,"Forecast disagrees with protection after exhaustion");
            }
            run=new CourierRun();run.Node=2;run.VehicleIndex=0;run.EventProbability=0;run.PreferSafeRoutes=false;run.LoadParcel(run.Parcels[4]);Go(run,5);
            Require(run.Results.Single().OutcomeDetail.Contains("bumps"),"Result lost cargo damage cause");

            var guide=new CourierTutorial();run=new CourierRun();run.EventProbability=0;
            guide.OpenMap();run.LoadParcel(run.Parcels[0]);guide.ChooseRoute();run.StartJourney(3);guide.Observe(run);run.FastForwardTravel();guide.Observe(run);
            Require(run.Node!=0&&run.Node!=3&&!guide.Arrived&&guide.Step(run,false)==4,"Waypoint ended first-delivery tutorial");
            guide.Leave();Require(!guide.Finished,"Continuing at waypoint dismissed tutorial");Go(run,3);guide.Observe(run);
            Require(guide.Arrived&&guide.Step(run,false)==6,"First delivery did not complete tutorial arrival step");
            guide.OpenMap();Require(guide.Finished,"Tutorial could not finish after delivery");
            run=new CourierRun();run.StartJourney(3);run.Tick(4);
            Require(Math.Abs(run.DisplayProgress-.5f)<.001f,"Continuous travel progress missing");
        }
    }
}
