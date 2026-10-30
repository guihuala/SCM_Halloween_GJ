using System;
using System.Linq;

namespace NightCourier
{
    // Can run independently of Unity; the editor smoke runner also executes these checks.
    public static class CourierChecks
    {
        private static int assertions;
        private static void Require(bool value, string message) { assertions++; if(!value) throw new Exception(message); }
        private static Road To(CourierRun r,int node) { return r.Roads.First(x=>x.Connects(r.Node)&&x.Other(r.Node)==node); }
        private static void Travel(CourierRun r,int node)
        {
            Require(r.Depart(To(r,node)),"Departure rejected");
            for(int i=0;i<2000 && (r.State==ShiftState.Driving||r.State==ShiftState.Encounter);i++)
            { if(r.State==ShiftState.Encounter) r.ResolveEncounter(true); else r.Tick(.1f); }
            Require(r.Node==node,"Did not arrive at "+node+": "+r.State);
        }
        public static string RunAll()
        {
            assertions=0;
            var r=new CourierRun();
            Require(!r.LoadParcel(r.Parcels[2]),"Car accepted heavy parcel");
            Require(r.LoadParcel(r.Parcels[0])&&r.LoadParcel(r.Parcels[1]),"Starting load failed");
            Require(!r.LoadParcel(r.Parcels[0]),"Duplicate loading");
            Require(!r.Depart(r.Roads[9]),"Disconnected travel allowed");
            Travel(r,1); Require(r.Delivered==1,"Letter delivery failed");
            Require(r.LoadParcel(r.Parcels[3]),"Soup loading failed");
            Travel(r,3); Require(!r.Repair() || r.Durability==r.Car.MaxDurability,"Repair did not restore durability");
            Travel(r,4); Require(r.Delivered==3,"Fragile highway deliveries failed");
            Travel(r,6); Travel(r,5);
            Require(r.Possess(2),"Truck possession failed");
            Require(r.LoadParcel(r.Parcels[7]),"Luggage loading failed");
            Travel(r,6); Require(r.State==ShiftState.Won&&r.Delivered==4&&r.Earnings==185,"Full shift win failed");
            float end=r.Elapsed; r.Tick(10); Require(r.Elapsed==end,"Finished shift still ticking");

            r=new CourierRun(); r.LoadParcel(r.Parcels[1]); Travel(r,2);
            Require(r.Parcels[1].Damaged,"Gravel did not damage fragile parcel");
            Require(r.Possess(1),"Pickup possession failed");
            Require(!r.Possess(2),"Remote truck possession allowed");
            r.Durabilities[1]=45; Require(r.Possess(0)&&r.Possess(1)&&r.Durability==45,"Possession reset durability");
            float clock=r.Elapsed; Require(r.Repair()&&r.Elapsed==clock+12&&r.Durability==100,"Repair cost incorrect");
            Require(!r.Repair(),"Full durability repair consumed time");
            Travel(r,0); Require(r.LoadParcel(r.Parcels[2]),"Pickup rejected heavy parcel");
            Require(!r.Possess(0),"Heavy cargo moved to sedan");

            r=new CourierRun(); var p=new Parcel("Load test",0,1,6,1);r.Parcels.Add(p);
            Require(r.LoadParcel(p)&&r.Overloaded,"Overload allowance missing");
            Require(!r.LoadParcel(r.Parcels[0]),"Overload cap exceeded");
            Require(Math.Abs(r.TimeCost(r.Roads[0])-22.5f)<.001f,"Overload speed penalty incorrect");
            Require(Math.Abs(r.WearCost(r.Roads[0])-8.25f)<.001f,"Overload wear penalty incorrect");
            Require(r.Unload(p)&&r.Load==0,"Unload did not return capacity");

            r=new CourierRun();r.LoadParcel(r.Parcels[0]);r.Elapsed=64;Travel(r,1);
            Require(r.Parcels[0].State==ParcelState.Failed&&r.Delivered==0,"Late parcel signed");
            r=new CourierRun();r.Elapsed=239;r.Depart(r.Roads[0]);r.Tick(1);
            Require(r.State==ShiftState.Lost,"Dawn did not end shift");
            r=new CourierRun();r.Durabilities[0]=.01f;r.Depart(r.Roads[0]);r.Tick(1);
            Require(r.State==ShiftState.Lost,"Breakdown did not end shift");

            r=new CourierRun();r.LoadParcel(r.Parcels[1]);r.Depart(r.Roads[0]);r.Tick(100);
            Require(r.State==ShiftState.Encounter&&r.Progress==.5f,"Long frame skipped encounter");
            clock=r.Elapsed;r.Tick(100);Require(r.Elapsed==clock,"Event clock not paused");
            r.EncounterIndex=0;r.ResolveEncounter(false);
            Require(r.Parcels[1].Damaged,"Unsafe event did not damage cargo");
            r=new CourierRun();Travel(r,2);r.Possess(1);Travel(r,3);Travel(r,5);r.Possess(2);
            var glass=new Parcel("Truck glass",5,3,1,1,0,true);r.Parcels.Add(glass);r.LoadParcel(glass);Travel(r,3);
            Require(!glass.Damaged&&glass.State==ParcelState.Delivered,"Truck suspension did not protect gravel cargo");
            Require(r.Node==3,"Graph return trip failed");
            return assertions+" assertions passed: full shift, heavy gates, fragile handling, overload, deadlines, repair, vehicle persistence, events, dawn, breakdown, return travel.";
        }
    }
}
