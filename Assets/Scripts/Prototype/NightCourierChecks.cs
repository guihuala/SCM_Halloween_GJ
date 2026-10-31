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
            Require(r.Depart(To(r,node)),"Departure rejected: "+r.Node+">"+node);r.Elapsed+=delay;
            for(int i=0;i<100&&!r.Finished&&r.State!=ShiftState.Docked;i++)
            {if(r.State==ShiftState.Encounter)r.ResolveEncounter(safe);else r.Tick(10000);}
            Require(r.Node==node,"Did not reach node "+node);
            if(!r.Finished)r.TalkToNpc();
        }
        public static void Go(CourierRun r,int target)
        {
            foreach(var road in r.FindRoute(target).ToArray())
            {Travel(r,road.Other(r.Node));if(r.Finished)break;}
            if(!r.Finished)r.TalkToNpc();
        }
        private static void CheckFastForward()
        {
            var fast=new CourierRun(31);var normal=new CourierRun(31);
            fast.LoadParcel(fast.Parcels[0]);normal.LoadParcel(normal.Parcels[0]);
            Require(!fast.FastForwardTravel(),"Skip accepted while docked");
            fast.StartJourney(3);normal.StartJourney(3);
            Require(fast.FastForwardTravel()&&fast.State==ShiftState.Encounter,"Skip bypassed the next event");
            while(normal.State==ShiftState.Driving)normal.Tick(.07f);
            Require(fast.EncounterIndex==normal.EncounterIndex&&Math.Abs(fast.Elapsed-normal.Elapsed)<.002f&&Math.Abs(fast.Durability-normal.Durability)<.002f,"Skip changed road costs or event selection");
            float clock=fast.Elapsed;Require(!fast.FastForwardTravel()&&fast.Elapsed==clock,"Skip resolved an event automatically");
            fast.ResolveEncounter(true);normal.ResolveEncounter(true);fast.FastForwardTravel();
            while(normal.State==ShiftState.Driving||normal.State==ShiftState.Pushing)normal.Tick(.07f);
            Require(fast.Node==normal.Node&&fast.Node==1&&fast.HasJourney&&Math.Abs(fast.Elapsed-normal.Elapsed)<.002f&&Math.Abs(fast.Durability-normal.Durability)<.002f,"Skip lost intermediate stop or costs");
            fast=new CourierRun();fast.Durabilities[0]=0;fast.StartJourney(3);
            Require(fast.FastForwardTravel()&&fast.State==ShiftState.Docked&&fast.Durability>0,"Skipping push recovery failed");
        }
        public static string RunAll()
        {
            assertions=0;CheckFastForward();var r=new CourierRun();
            Require(r.Stops.Length==6&&r.Parcels.Count==12&&r.Events.Length==10,"Updated content counts");
            Require(r.Load==0&&r.Parcels.All(p=>!p.Critical),"Legacy story cargo remains");
            Require(r.Vehicles.Select(v=>v.Capacity).SequenceEqual(new[]{3,8,4}),"Vehicle capacity mismatch");
            Require(r.Vehicles.Select(v=>v.MaxDurability).SequenceEqual(new[]{50f,120f,80f}),"Vehicle durability mismatch");
            for(int from=0;from<6;from++)for(int to=0;to<6;to++)
            {r.Node=from;int at=from;foreach(var road in r.FindRoute(to)){Require(road.Connects(at),"Disconnected route");at=road.Other(at);}Require(at==to,"New node locked");}
            r=new CourierRun();var guide=new CourierTutorial();guide.OpenMap();Require(guide.Step(r,false)==2,"Initial load guide");
            Require(r.LoadParcel(r.Parcels[0]),"Initial tape unavailable");Require(guide.Step(r,false)==3,"Tape loading did not advance guide");
            Require(r.Unload(r.Parcels[0])&&r.Load==0,"Local storage failed");
            Go(r,2);Require(r.UnlockedVehicles[1]&&r.Possess(1),"Bear bicycle unavailable");
            Require(r.LoadParcel(r.Parcels[4]),"Heavy mead rejected by bicycle");Travel(r,1);Travel(r,4);Travel(r,5);
            Require(!r.Finished&&r.Parcels[4].State==ParcelState.Delivered,"Bar ends game prematurely or mead damaged");
            Require(r.LoadParcel(r.Parcels[9]),"Repair equipment unavailable");Go(r,3);
            Require(r.UnlockedVehicles[2]&&r.Possess(2),"Repair delivery did not unlock pickup");
            r=new CourierRun();r.Node=2;Require(!r.LoadParcel(r.Parcels[4]),"Scooter accepted heavy mead");
            r=new CourierRun();r.Parcels[5].Origin=0;Require(r.LoadParcel(r.Parcels[5]),"Timed meal rejected");
            r.Durabilities[0]=0;Require(r.StartJourney(3)&&r.State==ShiftState.Pushing,"Breakdown recovery unavailable");
            Require(r.Parcels[5].State==ParcelState.Failed&&r.Parcels[0].State==ParcelState.Waiting,"Breakdown failed wrong cargo");
            r.Tick(10000);Require(r.Node==1&&r.Durability>0&&r.HasJourney,"Push lost onward journey");
            r=new CourierRun();r.Node=5;r.LoadParcel(r.Parcels[8]);r.StartJourney(3);r.Tick(10000);
            float clock=r.Elapsed;r.Tick(10000);Require(r.Elapsed==clock&&r.State==ShiftState.Encounter,"Encounter did not pause time");
            r.EncounterIndex=2;r.ResolveEncounter(false);Require(r.Parcels[8].Damaged,"Dry label cargo survived river risk");
            // A complete safe delivery circuit: no clock reset, injected delivery states or cargo teleports.
            r=new CourierRun();r.NightMinutes=1440;Go(r,2);r.Possess(1);
            foreach(int id in new[]{4,9,0,1,2,3,7,8,10,11,5,6})
            {
                var parcel=r.Parcels[id];Go(r,parcel.Origin);Require(!r.Finished,"Shift ended before delivery circuit completed");
                r.Repair();Require(r.LoadParcel(parcel),"Circuit cannot load "+parcel.Name);
                if(id==4){Travel(r,1);Travel(r,4);Travel(r,5);}else if(id==11){Go(r,4);r.Repair();Go(r,1);}else Go(r,parcel.Destination);
            }
            Require(r.Finished&&r.Delivered==12,"Delivery circuit did not reach party: "+r.State+" "+r.Delivered+" "+string.Join(",",r.Parcels.Select(p=>p.Name+":"+p.State))+" time="+r.Elapsed);
            float circuitMinutes=r.Elapsed;
            // Explicit boundary conditions for both party outcomes.
            r=new CourierRun();foreach(var p in r.Parcels)p.State=ParcelState.Delivered;r.TalkToNpc();Require(r.State==ShiftState.EndingA,"Perfect party missing");
            r=new CourierRun();foreach(var p in r.Parcels)p.State=ParcelState.Failed;r.TalkToNpc();Require(r.State==ShiftState.EndingB,"Failed deliveries block party");
            for(int seed=0;seed<20;seed++)
            {
                r=new CourierRun(seed);r.Elapsed=CourierRun.ShiftMinutes;
                foreach(int node in new[]{0,1,2,3,4,5}){Go(r,node);if(r.Finished)break;}
                Require(r.State==ShiftState.EndingB,"Dawn cannot reach party: "+seed);
            }
            return assertions+" assertions passed; six open nodes, twelve delivery relations, vehicle unlocks, tags, tutorial, push recovery, party endings; safe circuit "+circuitMinutes+" min.";
        }
    }
}
