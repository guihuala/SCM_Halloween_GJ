using System;
using System.Collections.Generic;
using System.Linq;

namespace NightCourier
{
    public sealed partial class CourierRun
    {
        // Checkpoints are made only before valid departures, never on animation frames.
        private readonly Stack<MovementCheckpoint> movementHistory = new Stack<MovementCheckpoint>();
        private readonly List<double> eventRolls = new List<double>();
        private int eventRollIndex;
        public bool CanUndoMovement => movementHistory.Count > 0;
        public int UndoMovementCount => movementHistory.Count;

        private double NextEventRoll()
        {
            // Repeating a movement after undo reuses its rolls instead of rerolling events.
            if (eventRollIndex == eventRolls.Count) eventRolls.Add(random.NextDouble());
            return eventRolls[eventRollIndex++];
        }

        private sealed class ParcelCheckpoint
        {
            public ParcelState State;
            public int DeliveryTurns, CarriedTurns;
            public float Deadline;
            public bool Damaged, EventTriggered, DeliveredLate;
            public string DamageReason;
            public ParcelCheckpoint(Parcel p)
            {
                State=p.State;DeliveryTurns=p.DeliveryTurns;CarriedTurns=p.CarriedTurns;Deadline=p.Deadline;
                Damaged=p.Damaged;EventTriggered=p.EventTriggered;DeliveredLate=p.DeliveredLate;DamageReason=p.DamageReason;
            }
            public void Restore(Parcel p)
            {
                p.State=State;p.DeliveryTurns=DeliveryTurns;p.CarriedTurns=CarriedTurns;p.Deadline=Deadline;
                p.Damaged=Damaged;p.EventTriggered=EventTriggered;p.DeliveredLate=DeliveredLate;p.DamageReason=DamageReason;
            }
        }

        private sealed class MovementCheckpoint
        {
            public CourierRun Values;
            public ParcelCheckpoint[] Parcels;
            public float[] Durabilities;
            public bool[] Talked, UnlockedVehicles;
            public DeliveryResult[] Results;
            public string[] Journal, StoryProgress;
        }

        private void RememberMovement()
        {
            movementHistory.Push(new MovementCheckpoint {
                Values=(CourierRun)MemberwiseClone(),
                Parcels=Parcels.Select(p=>new ParcelCheckpoint(p)).ToArray(),
                Durabilities=(float[])Durabilities.Clone(),Talked=(bool[])Talked.Clone(),
                UnlockedVehicles=(bool[])UnlockedVehicles.Clone(),Results=Results.ToArray(),
                Journal=Journal.ToArray(),StoryProgress=Story?.CaptureProgress()
            });
        }

        public bool UndoMovement()
        {
            if (!CanUndoMovement) return false;
            var checkpoint=movementHistory.Pop();var previous=checkpoint.Values;
            Node=previous.Node;VehicleIndex=previous.VehicleIndex;Delivered=previous.Delivered;Earnings=previous.Earnings;
            EncounterIndex=previous.EncounterIndex;Elapsed=previous.Elapsed;Progress=previous.Progress;
            NightMinutes=previous.NightMinutes;UsedPush=previous.UsedPush;State=previous.State;Notice=previous.Notice;
            PlannedDestination=previous.PlannedDestination;ActiveRoad=previous.ActiveRoad;
            destination=previous.destination;returning=previous.returning;animationSeconds=previous.animationSeconds;
            travelEdges=previous.travelEdges;lastOrderTurns=previous.lastOrderTurns;eventRollIndex=previous.eventRollIndex;
            PreferSafeRoutes=previous.PreferSafeRoutes;
            Array.Copy(checkpoint.Durabilities,Durabilities,Durabilities.Length);
            Array.Copy(checkpoint.Talked,Talked,Talked.Length);
            Array.Copy(checkpoint.UnlockedVehicles,UnlockedVehicles,UnlockedVehicles.Length);
            // Preserve parcel identities: cargo rows and tutorial references remain valid.
            for(int i=0;i<Parcels.Count;i++)checkpoint.Parcels[i].Restore(Parcels[i]);
            Results.Clear();Results.AddRange(checkpoint.Results);
            Journal.Clear();Journal.AddRange(checkpoint.Journal);
            Story=previous.Story;
            if(Story!=null)Story.RestoreProgress(checkpoint.StoryProgress);
            AudioCues.Clear();streetRoutes.Clear();
            return true;
        }
    }
}
