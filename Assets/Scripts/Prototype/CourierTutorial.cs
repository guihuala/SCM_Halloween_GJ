namespace NightCourier
{
    // Hints follow the player's actions; they never gate the simulation or a route.
    public sealed class CourierTutorial
    {
        public bool Finished { get; private set; }
        public bool GuideOpen { get; private set; }
        public void AfterLetter() { MapOpened = true; }
        public void CloseGuide() { GuideOpen = false; }
        public void ShowGuide() { GuideOpen=true; }
        public bool MapOpened { get; private set; }
        public bool CargoLoaded { get; private set; }
        public bool RouteChosen { get; private set; }
        public bool Departed { get; private set; }
        public bool EventResolved { get; private set; }
        public bool Arrived { get; private set; }
        public void Observe(CourierRun run)
        {
            if (Finished) return;
            bool hasCargo = System.Linq.Enumerable.Any(run.Parcels, p => p.State == ParcelState.Loaded || p.State == ParcelState.Delivered);
            // Empty exploration can select routes too; start the delivery hints at pickup.
            if (!CargoLoaded && hasCargo) { RouteChosen = false; Departed = false; }
            CargoLoaded |= hasCargo;
            Departed |= CargoLoaded && (run.State == ShiftState.Driving || run.State == ShiftState.Encounter || run.State == ShiftState.Pushing);
            // Junctions and vehicle stations are waypoints, not the first delivery.
            Arrived |= run.CompletedBatches > 0;
            if (run.Finished) Finished = true;
        }
        public void OpenMap() { if (Arrived) Finished = true; MapOpened = true; }
        public void ChooseRoute() { RouteChosen = true; }
        public void Leave() { if (Arrived) Finished = true; Departed |= CargoLoaded; }
        public void ResolveEvent() { EventResolved = true; }
        public void Dismiss() { GuideOpen=false; Finished = true; }
        public int Step(CourierRun run, bool conversation)
        {
            Observe(run);
            if (Arrived) return 6;
            if (!CargoLoaded) return !MapOpened ? (conversation ? 0 : 1) : 2;
            if (Departed) return run.State == ShiftState.Encounter || EventResolved ? 5 : 4;
            return RouteChosen ? 4 : 3;
        }
    }
}
