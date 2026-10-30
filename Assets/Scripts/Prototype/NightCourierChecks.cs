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
            Require(r.Depart(To(r,node)),"Departure rejected: "+r.Node+">"+node);
            r.Elapsed+=delay;
            for(int i=0;i<100&&(r.State==ShiftState.Driving||r.State==ShiftState.Pushing||r.State==ShiftState.Encounter);i++)
            {if(r.State==ShiftState.Encounter)r.ResolveEncounter(safe);else r.Tick(10000);}
            Require(r.Node==node,"Did not reach node "+node+": "+r.State);
            if(!r.Finished)r.TalkToNpc();
        }
        private static CourierRun PerfectRoute(bool lateFinal=false)
        {
            var r=new CourierRun();r.TalkToNpc();Require(r.LoadParcel(r.Parcels[3]),"Engine cannot fit starting vehicle");
            Travel(r,1);Require(r.Parcels[1].State==ParcelState.Loaded,"Tape was not handed over");Require(r.LoadParcel(r.Parcels[5]),"Coffee load rejected");
            Travel(r,2);Require(r.UnlockedVehicles[1]&&r.Possess(1),"Bicycle unlock failed");Require(r.LoadParcel(r.Parcels[6]),"Heavy farewell load rejected");
            Travel(r,3);Require(r.Parcels[2].State==ParcelState.Loaded,"Lighthouse key missing");Require(r.LoadParcel(r.Parcels[4]),"Watch load rejected");
            Travel(r,1);Require(r.Parcels[4].State==ParcelState.Delivered,"Watch did not arrive dry");
            Travel(r,4);Require(r.TruthRevealed&&r.UnlockedVehicles[2],"DJ reveal did not unlock pickup");Require(r.Possess(2),"Pickup possession failed");
            Travel(r,5,true,lateFinal?31:0);Require(r.State==(lateFinal?ShiftState.EndingB:ShiftState.EndingA),"Final deadline / ending mismatch");return r;
        }
        private static void CheckJourneys()
        {
            var r=new CourierRun();
            for(int from=0;from<5;from++)for(int to=0;to<5;to++)
            {
                r.Node=from;var route=r.FindRoute(to);int at=from;
                foreach(var road in route){Require(road.Connects(at),"Route contains disconnected leg");at=road.Other(at);}
                Require(at==to,"Open destination unreachable");
            }
            r=new CourierRun();var planned=r.FindRoute(4);
            Require(planned.Count==2&&planned[0]==r.Roads[0]&&planned[1]==r.Roads[5],"Fastest route not selected");
            Require(r.FindRoute(5).Count==0&&!r.StartJourney(5)&&r.PlannedDestination==-1,"Story lock bypassed by route planner");
            Require(r.FindRoute(-1).Count==0&&!r.StartJourney(99)&&!r.StartJourney(0),"Invalid/current destination accepted");
            Require(r.StartJourney(4)&&r.PlannedDestination==4,"Cross-map journey rejected");
            while(r.State!=ShiftState.Docked){if(r.State==ShiftState.Encounter)r.ResolveEncounter(true);else r.Tick(10000);}
            Require(r.Node==1&&r.HasJourney&&r.Elapsed>=6&&r.Durability<50,"Intermediate stop skipped costs or lost destination");
            float clock=r.Elapsed;r.Tick(10000);Require(r.Elapsed==clock,"Intermediate stop did not wait for player");
            r.TalkToNpc();Require(r.ContinueJourney(),"Continuation failed after dialogue/cargo handoff");
            while(r.State!=ShiftState.Docked){if(r.State==ShiftState.Encounter)r.ResolveEncounter(true);else r.Tick(10000);}
            Require(r.Node==4&&!r.HasJourney&&r.Parcels[1].State==ParcelState.Delivered,"Final arrival did not finish journey/deliver tape");
            r.TalkToNpc();Require(!r.StartJourney(5),"Key/vehicle gate bypassed");
            Require(r.StartJourney(0),"Return journey rejected");
            while(r.State!=ShiftState.Docked){if(r.State==ShiftState.Encounter)r.ResolveEncounter(true);else r.Tick(10000);}
            Require(r.Node==1&&r.HasJourney&&r.StartJourney(2)&&r.PlannedDestination==2,"Midway change of destination failed");
            r=new CourierRun();r.Durabilities[0]=0;Require(r.StartJourney(4)&&r.State==ShiftState.Pushing,"Journey breakdown recovery failed");r.Tick(10000);
            Require(r.Node==1&&r.HasJourney&&r.ContinueJourney(),"Push recovery lost remaining journey");
        }
        public static string RunAll()
        {
            assertions=0;CheckJourneys();var delayed=PerfectRoute(true);Require(delayed.FinalLegLate&&delayed.State==ShiftState.EndingB,"Final storm timeout blocked or incorrectly produced ending A");
            var r=PerfectRoute();
            Require(r.Stops.Length==6&&r.Parcels.Count==7&&r.Events.Length==10,"Content counts differ from 2.0");
            Require(r.Delivered==7,"Seven parcels were not delivered");
            Require(r.Vehicles[0].Capacity==3&&r.Vehicles[1].Capacity==8&&r.Vehicles[2].Capacity==4,"Vehicle table mismatch");
            Require(r.Vehicles[0].MaxDurability==50&&r.Vehicles[1].MaxDurability==120&&r.Vehicles[2].MaxDurability==80,"Durability table mismatch");
            float clock=r.Elapsed;r.Tick(10000);Require(r.Elapsed==clock,"Finished ending still ticks");
            r=new CourierRun();Require(!r.Unload(r.Parcels[0]),"Main parcel can be discarded");
            Require(!r.Depart(r.Roads[7]),"Disconnected storm road allowed");
            Require(r.LoadParcel(r.Parcels[3])&&!r.LoadParcel(r.Parcels[3]),"Duplicate load accepted");
            Require(r.Overloaded&&Math.Abs(r.TimeCost(r.Roads[0])-8.1f)<.001,"Full-load speed penalty missing");
            Travel(r,2);Require(r.Parcels[0].Damaged,"Gravel did not affect fragile parcel");
            Require(r.Possess(1),"Bike unavailable after parts delivered");r.Durabilities[1]=42;
            Require(r.Repair()&&r.Durability==120,"Repair failed");clock=r.Elapsed;Require(!r.Repair()&&r.Elapsed==clock,"Repair at full condition charged time");
            Require(r.LoadParcel(r.Parcels[6]),"Bike rejected heavy cargo");Require(r.Unload(r.Parcels[6])&&r.Parcels[6].Origin==2,"Local storage failed");
            r=new CourierRun();r.Elapsed=300;Travel(r,1);Travel(r,3);Travel(r,4);Require(r.Possess(2),"Late main route blocked at pickup");Travel(r,5);
            Require(r.State==ShiftState.EndingB,"Dawn/side-skipping did not produce ending B");
            r=new CourierRun();r.Parcels[5].Origin=0;Require(r.LoadParcel(r.Parcels[5]),"Timed parcel rejected");
            r.Durabilities[0]=0;Require(r.Depart(r.Roads[0])&&r.State==ShiftState.Pushing,"Breakdown did not start pushing");
            Require(r.Parcels[5].State==ParcelState.Failed&&r.Parcels[0].State==ParcelState.Loaded,"Pushing failed the wrong parcels");
            r.Tick(10000);Require(r.Node==1&&r.State==ShiftState.Docked&&r.Durability>0,"Push arrival or temporary repair failed");
            r.TalkToNpc();Travel(r,3);Travel(r,4);Require(r.Possess(2),"Recovery blocked pickup");Travel(r,5);Require(r.Finished,"Breakdown route cannot reach an ending");
            r=new CourierRun();Travel(r,1);Travel(r,3);r.Node=4;Require(!r.Depart(r.Roads[7]),"Unrevealed storm road opened");
            r.Node=3;r.Parcels[4].State=ParcelState.Loaded;r.Depart(To(r,1));r.Tick(10000);
            Require(r.State==ShiftState.Encounter&&r.Progress==.5f,"Long frame skipped encounter");clock=r.Elapsed;r.Tick(10000);Require(r.Elapsed==clock,"Event choice consumed time");
            r.EncounterIndex=2;r.ResolveEncounter(false);Require(r.Parcels[4].Damaged&&r.Parcels[2].Damaged,"River event ignored dry requirement");
            Require(r.Parcels[2].State==ParcelState.Loaded,"Damaged main key vanished");
            r=new CourierRun();r.LoadParcel(r.Parcels[3]);var extra=new Parcel("Storage test",0,3,2);r.Parcels.Add(extra);r.LoadParcel(extra);Travel(r,1);
            Require(r.Parcels[1].State==ParcelState.Loaded&&extra.State==ParcelState.Waiting&&extra.Origin==1,"Critical handoff overflow softlocked");
            // Different unsafe event sequences, repeated breakdowns and dawn must still permit a main-story ending.
            for(int seed=0;seed<50;seed++)
            {
                r=new CourierRun(seed);r.TalkToNpc();r.Elapsed=seed%2==0?300:0;r.Durabilities[0]=seed%3==0?0:50;
                Travel(r,1,false);Travel(r,3,false);Travel(r,4,false);Require(r.Possess(2),"Fuzz route pickup blocked");Travel(r,5,false);
                Require(r.State==ShiftState.EndingB,"Unsafe/skip route lacks ending B, seed "+seed);
            }
            return assertions+" assertions passed: 2.0 content, A/B endings, unlocks, critical protection, deadlines, push recovery, dry/fragile events, overflow storage and 50 unsafe main routes.";
        }
    }
}
