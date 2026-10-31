namespace NightCourier
{
    // Hints follow the player's actions; they never gate the simulation or a route.
    public sealed class CourierTutorial
    {
        public bool Finished { get; private set; }
        public bool MapOpened { get; private set; }
        public bool CargoLoaded { get; private set; }
        public bool RouteChosen { get; private set; }
        public bool Departed { get; private set; }
        public bool EventResolved { get; private set; }
        public bool Arrived { get; private set; }
        public void Observe(CourierRun run)
        {
            if (Finished) return;
            CargoLoaded |= System.Linq.Enumerable.Any(run.Parcels, p => p.State == ParcelState.Loaded || p.State == ParcelState.Delivered);
            Departed |= run.State == ShiftState.Driving || run.State == ShiftState.Encounter || run.State == ShiftState.Pushing;
            Arrived |= run.Node != 0;
            if (run.Finished) Finished = true;
        }
        public void OpenMap() { if (Arrived) Finished = true; MapOpened = true; }
        public void ChooseRoute() { RouteChosen = true; }
        public void Leave() { if (Arrived) Finished = true; Departed = true; }
        public void ResolveEvent() { EventResolved = true; }
        public void Dismiss() { Finished = true; }
        public int Step(CourierRun run, bool conversation)
        {
            Observe(run);
            if (Arrived) return 6;
            if (Departed) return run.State == ShiftState.Encounter || EventResolved ? 5 : 4;
            if (!MapOpened) return conversation ? 0 : 1;
            if (!CargoLoaded) return 2;
            return RouteChosen ? 4 : 3;
        }
    }
}
