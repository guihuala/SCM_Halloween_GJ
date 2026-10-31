using System.Linq;
namespace NightCourier
{
    public static class CourierInformation
    {
        public static string Order(CourierRun run,Parcel parcel)
        {
            var batch=run.Batch(parcel).ToArray();
            var requirements=new System.Collections.Generic.List<string>();
            if(batch.Any(p=>p.HeatSensitive))requirements.Add("Flame: heat-sensitive · use H");
            if(batch.Any(p=>p.Dry))requirements.Add("Umbrella: keep dry · use W");
            if(batch.Any(p=>p.Fragile))requirements.Add("Glass: fragile · use Q");
            if(requirements.Count==0)requirements.Add("No special cargo requirements");
            string status=batch.Any(p=>p.Damaged)?"Damaged · delivery failed; still accepted":run.ActiveOrder!=null&&run.ActiveOrder.Origin==parcel.Origin&&run.ActiveOrder.Destination==parcel.Destination&&run.Late?"Late · delivery failed; still accepted":"Damage cannot be repaired. Late or damaged orders can still be delivered.";
            return string.Join(" + ",batch.Select(p=>p.Name))+"\n"+run.Stops[parcel.Origin].Name+" > "+run.Stops[parcel.Destination].Name+"\n"+string.Join("\n",requirements)+"\nClock: "+(parcel.State==ParcelState.Loaded?run.OrderTurns:0)+"/"+run.TargetTurns(parcel)+" order turns\n"+status;
        }
        public static string Vehicle(CourierRun run)
        {
            return run.Car.Name+"\n"+run.ProtectionSummary+"\n"+(run.IsGhost?"1 edge per turn · unlimited use":"Up to "+run.Car.Speed+" edges per turn · "+(int)run.Durability+" turns left\nExpires to ghost; protection ends")+"\nEmpty travel does not advance the order clock.";
        }
        public static string Station(CourierRun run,int id)
        {
            return run.Stops[id].Name+"\n"+run.StationDescription(id).Replace(". ",".\n")+"\nUp to "+run.Vehicles[id-6].Speed+" edges per turn.\nProtection ends when the vehicle expires.";
        }
        public static string Rules(CourierRun run)
        {
            return "DELIVERY GUIDE\nBox: Orders · Tab. Visit the sender to accept one batch; no cancellation; finish before another.\nPin: recipient · automatic delivery on arrival.\nClock: used / target turns. Empty travel has no order clock.\nFlame / umbrella / glass: heat / water / bumps; H / W / Q protect matching cargo.\nVehicles: up to 2 edges/turn, 5 turns. Exchange costs +1 order turn; expires to ghost. Ghost: 1 edge/turn, unlimited.\nEvents: "+System.Math.Round(run.EventProbability*100)+"% at marked stops, carrying cargo only; max 1/order. +1 wait or retreat 1 node. Route estimates exclude events.\nLate or damaged: failed rating, still deliverable.\nWASD/arrows: move. E: talk/continue. F: skip travel. Esc: pause. Hover icons for details.";
        }
    }
}
