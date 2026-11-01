using System;
using System.Linq;

namespace NightCourier
{
    public static class CourierMovementUndoChecks
    {
        private static int assertions;
        private static void Require(bool valid,string message)
        { assertions++;if(!valid)throw new Exception("Movement undo: "+message); }

        private static Road RoadTo(CourierRun run,int target,StreetSurface surface=StreetSurface.Normal)
        {
            var a=CourierStreetMap.Nodes.First(n=>n.StopId==run.Node);
            var b=CourierStreetMap.Nodes.First(n=>n.StopId==target);
            var road=new Road(run.Node,target,6,0){StreetPath=new[]{new StreetEdge(a,b,surface,null)}};
            run.Roads[0]=road;return road;
        }

        public static string RunAll()
        {
            assertions=0;
            var run=new CourierRun();
            Require(!run.CanUndoMovement&&!run.UndoMovement(),"new shift has undo history");
            Require(!run.StartJourney(-1)&&!run.StartJourney(run.Node)&&run.UndoMovementCount==0,"invalid departure records history");
            Require(run.StartJourney(3),"could not start journey");
            Require(!run.StartJourney(2)&&run.UndoMovementCount==1,"in-transit departure records history");
            run.Tick(2);
            Require(run.DisplayProgress>0&&run.UndoMovement(),"partial journey could not be undone");
            Require(run.Node==0&&run.State==ShiftState.Docked&&run.Elapsed==0&&run.PlannedDestination==-1&&!run.CanUndoMovement,"partial journey did not restore departure");
            run.Tick(10000);
            Require(run.Node==0&&run.Elapsed==0,"undone animation still advances simulation");

            run=new CourierRun();run.EventProbability=0;var parcel=run.Parcels[0];
            Require(run.LoadParcel(parcel),"pickup failed");
            string notice=run.Notice;var journal=run.Journal.ToArray();
            run.Depart(RoadTo(run,3,StreetSurface.Water));run.FastForwardTravel();
            Require(parcel.Damaged&&parcel.State==ParcelState.Delivered&&run.Results.Count==1&&run.Earnings>0,"delivery fixture did not change cargo and rewards");
            Require(run.UndoMovement(),"delivery could not be undone");
            Require(ReferenceEquals(parcel,run.Parcels[0])&&parcel.State==ParcelState.Loaded&&!parcel.Damaged&&parcel.DamageReason==null,"cargo state or identity lost");
            Require(parcel.CarriedTurns==0&&parcel.DeliveryTurns==0&&!parcel.DeliveredLate&&run.Delivered==0&&run.Earnings==0&&run.Results.Count==0,"delivery clocks, rewards or result retained");
            Require(run.Notice==notice&&run.Journal.SequenceEqual(journal)&&run.AudioCues.Count==0,"journal or queued audio retained");

            run=new CourierRun();run.EventProbability=0;run.VehicleIndex=0;run.Durabilities[0]=1;
            run.Depart(RoadTo(run,1));run.FastForwardTravel();
            Require(run.IsGhost&&run.UsedPush,"vehicle fixture did not exhaust");
            Require(run.UndoMovement()&&run.VehicleIndex==0&&run.Durability==1&&!run.UsedPush,"vehicle life not restored");

            foreach(string nodeId in new[]{"C","N"})
            {
                run=new CourierRun(31);run.LoadParcel(run.Parcels[0]);run.EventProbability=1;
                var road=RoadTo(run,CourierStreetMap.Node(nodeId).StopId);
                run.Depart(road);run.FastForwardTravel();int encounter=run.EncounterIndex;
                Require(run.State==ShiftState.Encounter&&run.OrderEventTriggered,"event fixture failed");
                Require(run.UndoMovement()&&run.State==ShiftState.Docked&&!run.OrderEventTriggered&&run.OrderTurns==0,"pending event not restored");
                run.Depart(road);run.FastForwardTravel();
                Require(run.State==ShiftState.Encounter&&run.EncounterIndex==encounter,"undo rerolled the event");
                run.ResolveEncounter(true);
                Require(run.UndoMovement()&&run.Node==0&&run.OrderTurns==0&&!run.OrderEventTriggered,"resolved wait/retreat not restored");
            }

            run=new CourierRun();run.EventProbability=0;
            run.Story=new CourierStory(new CourierStoryData{scenes=new[]{
                new CourierStoryScene{id="arrival",trigger="intro",node=1,lines=new[]{new CourierStoryLine{speaker="Wolfwoman",text="Hello"}}}
            }});
            run.Depart(RoadTo(run,1));run.FastForwardTravel();run.TalkToNpc();
            Require(run.Story.WasPlayed("arrival")&&run.Talked[1],"story fixture failed");
            Require(run.LoadParcel(run.Parcels[1]),"post-arrival pickup failed");
            Require(run.UndoMovement()&&!run.Story.WasPlayed("arrival")&&!run.Talked[1]&&run.Parcels[1].State==ParcelState.Waiting,"story or post-arrival pickup retained");
            run.Depart(RoadTo(run,1));run.FastForwardTravel();
            Require(run.Story.HasPending(run),"arrival dialogue cannot replay after undo");

            run=new CourierRun();run.EventProbability=0;
            run.Depart(RoadTo(run,1));run.FastForwardTravel();
            run.Depart(RoadTo(run,2));run.FastForwardTravel();
            Require(run.UndoMovementCount==2&&run.UndoMovement()&&run.Node==1&&run.Elapsed==1,"first consecutive undo failed");
            Require(run.UndoMovement()&&run.Node==0&&run.Elapsed==0&&!run.UndoMovement(),"second consecutive undo or empty history failed");
            run.Depart(RoadTo(run,4));run.FastForwardTravel();
            Require(run.Node==4&&run.UndoMovementCount==1&&run.UndoMovement()&&run.Node==0,"new route after undo failed");

            run=new CourierRun();run.EventProbability=0;
            foreach(var p in run.Parcels.Where(p=>p.Enabled))p.State=ParcelState.Delivered;
            parcel=run.Parcels[0];parcel.State=ParcelState.Loaded;
            run.Story=new CourierStory(new CourierStoryData{scenes=new[]{
                new CourierStoryScene{id="ending",trigger="ending",lines=new[]{new CourierStoryLine{speaker="Narrator",text="End"}}}
            }});
            run.Depart(RoadTo(run,3));run.FastForwardTravel();run.TalkToNpc();
            Require(run.Finished&&run.Story.WasPlayed("ending"),"ending fixture failed");
            Require(run.UndoMovement()&&!run.Finished&&parcel.State==ParcelState.Loaded&&!run.Story.WasPlayed("ending"),"ending could not be undone");
            run.Depart(RoadTo(run,3));run.FastForwardTravel();
            Require(run.Finished&&run.Story.HasPending(run),"ending cannot replay after undo");
            return assertions+" movement undo assertions passed";
        }
    }
}
