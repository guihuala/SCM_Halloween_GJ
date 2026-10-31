using System;
using System.Linq;

namespace NightCourier
{
    public static class CourierChecks
    {
        private static int assertions;
        private static void Require(bool ok,string message){assertions++;if(!ok)throw new Exception(message);}
        public static Road To(CourierRun run,int node){return run.Roads.First(r=>r.Connects(run.Node)&&r.Other(run.Node)==node);}
        public static void Travel(CourierRun r,int node,bool safe=true,float delay=0)
        {
            for(int attempt=0;attempt<100&&r.Node!=node;attempt++)
            {
                Require(r.Depart(To(r,node)),"Departure rejected: "+r.Node+">"+node);
                for(int i=0;i<100&&!r.Finished&&r.State!=ShiftState.Docked;i++)
                {if(r.State==ShiftState.Encounter)r.ResolveEncounter(safe);else r.Tick(10000);}
            }
            Require(r.Node==node,"Did not reach node "+node);
        }
        public static void Go(CourierRun r,int target)
        {
            foreach(var road in r.FindRoute(target).ToArray())
            {Travel(r,road.Other(r.Node));if(r.Finished)break;}
        }
        public static void CompleteAll(CourierRun r)
        {
            while(!r.Finished)
            {
                var p=r.ActiveOrder??r.Parcels.FirstOrDefault(item=>item.State==ParcelState.Waiting);
                Require(p!=null,"Unfinished run has no deliverable order");
                if(r.ActiveOrder==null){Go(r,p.Origin);Require(r.LoadParcel(p),"Cannot accept order "+p.Name);}
                Go(r,p.Destination);
                Require(p.State==ParcelState.Delivered,"Order did not deliver");
            }
        }
        private static void CheckAnimationAndExhaustion(int vehicle, int remaining)
        {
            var fast=new CourierRun(31);var normal=new CourierRun(31);
            foreach(var r in new[]{fast,normal}){r.VehicleIndex=vehicle;if(vehicle>=0)r.Durabilities[vehicle]=remaining;r.LoadParcel(r.Parcels[0]);r.StartJourney(3);}
            while(fast.State!=ShiftState.Docked)
            {
                if(fast.State==ShiftState.Encounter){Require(!fast.FastForwardTravel(),"Skip chose an event");fast.ResolveEncounter(false);}
                else fast.FastForwardTravel();
            }
            for(int i=0;i<100000&&normal.State!=ShiftState.Docked;i++)
            {if(normal.State==ShiftState.Encounter)normal.ResolveEncounter(false);else normal.Tick(.013f);}
            Require(normal.Node==fast.Node&&normal.OrderTurns==fast.OrderTurns&&normal.Elapsed==fast.Elapsed&&normal.Durability==fast.Durability&&normal.VehicleIndex==fast.VehicleIndex,"Animation frame size or skip changed rule results");
            Require(fast.Elapsed==(int)fast.Elapsed,"Fractional turn recorded");
            if(vehicle<0||remaining==0)Require(fast.IsGhost,"Ghost fallback missing");
        }
        private static void CheckRoadEvents()
        {
            for(int id=0;id<12;id++)foreach(bool ghost in new[]{false,true})foreach(bool confirm in new[]{false,true})
            {
                var r=new CourierRun();if(ghost)r.UseGhost();r.LoadParcel(r.Parcels[0]);r.StartJourney(3);r.FastForwardTravel();
                Require(r.State==ShiftState.Encounter,"Event not reached");
                r.EncounterIndex=id;var effect=r.Events[id].Effect;
                int turns=r.OrderTurns,remaining=r.RemainingTravelTurns;float fuel=r.Durability;
                r.ResolveEncounter(confirm);r.ResolveEncounter(confirm);
                Require(r.State==ShiftState.Driving,"Event did not resume movement");
                Require(r.OrderTurns==turns+(effect==RoadEventEffect.Wait?1:0),"Wait cost not exactly one turn, or duplicate resolution counted");
                Require(r.Durability==fuel&&!r.ActiveOrder.Damaged,"Event directly consumed fuel or damaged cargo");
                if(effect==RoadEventEffect.Detour)Require(r.RemainingTravelTurns==remaining+2,"Detour distance missing");
                float progress=r.Progress;
                r.Tick(8);
                Require(effect==RoadEventEffect.Retreat?r.Progress<progress:r.Progress>=progress,"Travel marker moved the wrong way");
                if(r.State==ShiftState.Driving)r.FastForwardTravel();
                Require(r.State==ShiftState.Docked&&r.Node==(effect==RoadEventEffect.Retreat?0:1),"Wrong event destination");
                Require(r.ActiveOrder!=null&&r.Results.Count==0&&r.PlannedDestination==3,"Event lost cargo or planned destination");
                if(effect==RoadEventEffect.Retreat)Require(r.ContinueJourney(),"Cannot resume after retreat");
            }
            // Every event must be reachable; terrain no longer pins a road to one old event.
            var seen=new bool[12];
            for(int seed=0;seed<200;seed++){var r=new CourierRun(seed);r.StartJourney(3);r.FastForwardTravel();seen[r.EncounterIndex]=true;}
            Require(seen.All(value=>value),"Some random events are unreachable");
        }
        public static string RunAll()
        {
            assertions=0;CheckRoadEvents();var r=new CourierRun();
            Require(r.BatchCount==11&&r.Parcels.Count==12,"Expected 11 orders / 12 items");
            for(int from=0;from<6;from++)for(int to=0;to<6;to++)
            {r.Node=from;int at=from;foreach(var road in r.FindRoute(to)){Require(road.Connects(at),"Disconnected route");at=road.Other(at);}Require(at==to,"Unreachable node");}
            r=new CourierRun();r.Node=5;
            Require(!r.LoadParcel(r.Parcels[0]),"Remote pickup allowed");
            Require(r.LoadParcel(r.Parcels[10]),"Batch acceptance failed");
            Require(r.Parcels[9].State==ParcelState.Loaded&&r.Parcels[10].State==ParcelState.Loaded,"Same pair not loaded as a batch");
            Require(!r.LoadParcel(r.Parcels[8])&&!r.Unload(r.Parcels[9]),"Active order split, stored or second order accepted");
            int origin=r.Parcels[9].Origin;r.Tick(10000);Require(r.OrderTurns==0,"Idle time counted");
            Go(r,3);Require(r.Results.Count==1&&r.CompletedBatches==1&&r.Parcels[9].Origin==origin,"Batch delivery or origin changed");
            Require(!r.CanLoad(r.Parcels[9]),"Completed order still available");
            Go(r,1);Require(r.LoadParcel(r.Parcels[1])&&r.OrderTurns==0,"Next order clock not reset");
            var carried=r.ActiveOrder;r.Durabilities[r.VehicleIndex]=0;Travel(r,0);
            Require(r.IsGhost&&carried.State==ParcelState.Delivered,"Exhaustion prevents delivery");
            r=new CourierRun();r.UseGhost();Go(r,2);Require(r.OrderTurns==0&&r.Elapsed>0,"Empty travel counted in order time");
            Require(r.LoadParcel(r.Parcels[4]),"Ghost cannot carry heavy batch");
            r.Parcels[4].TimeLimit=1;r.Parcels[4].Damaged=true;Go(r,5);
            Require(r.Parcels[4].State==ParcelState.Delivered&&r.Results.Last().Late&&r.Results.Last().Damaged,"Late / damaged order rejected instead of evaluated");
            Require(!r.Finished,"Early completion at bar");
            float clock=r.Elapsed;r.Tick(100000);Require(r.Elapsed==clock,"Idle animation advanced turns");
            r=new CourierRun();r.Elapsed=10000;foreach(int node in new[]{0,1,2,3,4,5}){Go(r,node);r.TalkToNpc();}
            Require(!r.Finished&&r.CompletedBatches==0,"Global clock or conversations ended run early");
            foreach(var p in r.Parcels)p.State=ParcelState.Failed;r.TalkToNpc();Require(!r.Finished,"Failed items counted as delivered");
            for(int v=-1;v<3;v++)foreach(int turns in new[]{0,1,2,50})CheckAnimationAndExhaustion(v,turns);
            for(int seed=0;seed<20;seed++)
            {
                r=new CourierRun(seed);r.UseGhost();CompleteAll(r);
                Require(r.Results.Count==11&&r.CompletedBatches==11&&r.Delivered==12,"Ghost circuit cannot deliver every order");
                Require(r.Parcels.All(p=>p.State==ParcelState.Delivered)&&r.State==ShiftState.EndingA,"All delivered did not clear game");
            }
            return assertions+" assertions passed: batch orders, discrete turns, evaluation, ghost fallback, frame-independent travel, all-delivered completion, 12 random events and retreat recovery.";
        }
    }
}
