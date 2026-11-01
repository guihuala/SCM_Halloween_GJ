using System;
using System.Collections.Generic;
using System.Linq;

namespace NightCourier
{
    public enum ParcelState { Waiting, Loaded, Delivered, Failed, Unavailable }
    public enum ShiftState { Docked, Driving, Encounter, Pushing, EndingA, EndingB }
    public enum RoadKind { Asphalt, Gravel, Fog, River, Storm }
    public sealed class Stop
    {
        public string Name, Sign, Voice;
        public float X, Y;
        public int Vehicle;
        public bool Repair;
        public Stop(string name, string sign, string voice, float x, float y, int vehicle = -1, bool repair = false)
        { Name=name;Sign=sign;Voice=voice;X=x;Y=y;Vehicle=vehicle;Repair=repair; }
    }
    public sealed class Vehicle
    {
        public string Name, Ability;
        public int Capacity;
        public float MaxDurability, Speed, Wear;
        public Vehicle(string name,string ability,int capacity,float durability,float speed,float wear)
        {Name=name;Ability=ability;Capacity=capacity;MaxDurability=durability;Speed=speed;Wear=wear;}
    }
    public sealed class Road
    {
        public int A,B;
        public StreetEdge[] StreetPath;
        public float Minutes,Damage;
        public RoadKind Kind;
        public bool Gravel {get{return Kind==RoadKind.Gravel;}}
        public Road(int a,int b,float minutes,float damage,RoadKind kind=RoadKind.Asphalt)
        {A=a;B=b;Minutes=minutes;Damage=damage;Kind=kind;}
        public int Other(int node){return node==A?B:A;}
        public bool Connects(int node){return node==A||node==B;}
        public string Label {get{return new[]{"Asphalt Road","Gravel Shortcut","Foggy Forest","River Crossing","Storm Road"}[(int)Kind];}}
    }
    public sealed class Parcel
    {
        public string Name;
        public int Origin,Destination,Weight,Reward;
        public bool Fragile,Heavy,Dry,HeatSensitive,Critical,Damaged;
        // Legacy field names remain for prefab/editor compatibility; values are turns.
        public float Deadline,TimeLimit;
        public int DeliveryTurns, CarriedTurns;
        public bool Enabled=true;
        public int PickupGroup=-1;
        public bool EventTriggered;
        public bool DeliveredLate;
        public string DamageReason;
        public string Evaluation { get { return (DeliveredLate || Damaged ? "Delivery failed" : "Delivered successfully"); } }
        public ParcelState State;
        public Parcel(string name,int origin,int destination,int weight,int reward=0,bool critical=false,bool fragile=false,bool heavy=false,bool dry=false,float timeLimit=0)
        {Name=name;Origin=origin;Destination=destination;Weight=weight;Reward=reward;Critical=critical;Fragile=fragile;Heavy=heavy;Dry=dry;TimeLimit=timeLimit;}
        public string Tags {get{return (Critical?"Story ":"")+(TimeLimit>0?"Timed ":"")+(Fragile?"Fragile ":"")+(Heavy?"Heavy ":"")+(Dry?"Keep dry ":"")+(HeatSensitive?"Keep cool":"");}}
    }
    public enum RoadEventEffect { Wait, Detour, Retreat }
    public sealed class RoadEvent
    {
        public string Title,Description,Safe,Risky;
        public string Illustration="social",SoundCue="encounter";
        public float SafeMinutes,RiskDamage;
        public bool BreakFragile,WetDry;
        public RoadEventEffect Effect;
        public RoadEvent(string title,string description,string action,RoadEventEffect effect,int turns)
        {Title=title;Description=description;Safe=action;Risky=action;Effect=effect;SafeMinutes=turns;}
        public RoadEvent(string title,string description,string safe,string risky,float time,float damage,bool fragile=false,bool wet=false)
        {Title=title;Description=description;Safe=safe;Risky=risky;SafeMinutes=time;RiskDamage=damage;BreakFragile=fragile;WetDry=wet;}
    }
    public sealed class DeliveryResult
    {
        public int Origin, Destination, Turns, TargetTurns;
        public string Cargo;
        public bool Late, Damaged;
        public string DamageReason;
        public string OutcomeDetail { get { return string.Join("; ",new[]{Late?"Late by "+Math.Max(0,Turns-TargetTurns)+" turn(s)":null,Damaged?"Damaged: "+(string.IsNullOrEmpty(DamageReason)?"cargo conditions violated":DamageReason):null}.Where(s=>s!=null).DefaultIfEmpty("On time; cargo intact")); } }
        public string Evaluation { get { return (Late || Damaged ? "Delivery failed" : "Delivered successfully"); } }
    }
    public sealed partial class CourierRun
    {
        public CourierStory Story;
        // Committed actions survive fast-forward and multiple transitions within one frame.
        public readonly Queue<string> AudioCues=new Queue<string>();
        private void EmitAudio(string cue){if(!string.IsNullOrEmpty(cue))AudioCues.Enqueue(cue);}

        public readonly Stop[] Stops={
            new Stop("Tunnel","Tunnel","Drow: ...Hello. I hope the violin sounds all right in this damp tunnel.",0.07f,0.5f,0,false),
            new Stop("Woods","Woods","Wolfwoman: Good to see you, little ghost! Take care out there in the fog.",0.31f,0.2f,-1,true),
            new Stop("Cellar","Cellar","Little Werebear: The cellar feels warm again... I hope I can sleep this winter.",0.31f,0.8f,1,true),
            new Stop("Tower","Tower","Vampire: Hello, little ghost. Come in and rest your ears a moment.",0.6f,0.75f,2,true),
            new Stop("Museum","Museum","Immigrant Jiangshi: Come in, dear. I am looking after my antiques.",0.69f,0.2f,-1,true),
            new Stop("Tavern","Tavern","Witch: One thing at a time, darling. I have the list.",0.94f,0.5f,-1,true),
            new Stop("Heat Shield Station","H","",0,0),
            new Stop("Waterproofing Station","W","",0,0),
            new Stop("Suspension Station","Q","",0,0)
        };
        // Capacity and wear fields remain for existing assets; they no longer gate order transport.
        public readonly Vehicle[] Vehicles={
            new Vehicle("H Heatproof","Protects heat-sensitive cargo",3,5,2,1),
            new Vehicle("W Amphibious","Protects cargo from water",3,5,2,1),
            new Vehicle("Q Suspension","Protects fragile cargo",3,5,2,1)
        };
        public readonly Road[] Roads=CourierStreetMap.BuildRoads();
        public readonly List<Parcel> Parcels=new List<Parcel>{
            new Parcel("Party Theme Tape",0,3,1,25,false,false,false,true,0),
            new Parcel("Violin Rosin",1,0,1,20,false,false,false,true,0),
            new Parcel("Gray-Pine Autumn Honey",1,2,2,25,false,true,false,false,0),
            new Parcel("Flower Jam",1,4,1,20,false,false,false,false,0),
            new Parcel("Spring Mead",2,5,4,35,false,true,true,false,0),
            new Parcel("Spicy Blood Stew",3,1,1,25,false,false,false,false,60),
            new Parcel("Flower Cakes",4,1,1,20,false,false,false,false,60),
            new Parcel("Moisture Repellent",5,4,1,20,false,false,false,false,0),
            new Parcel("Cooling Unit",5,3,2,30,false,false,false,false,0),
            new Parcel("Livestock Blood",5,3,1,20,false,false,false,false,0)
        };
        public readonly RoadEvent[] Events={
            new RoadEvent("Trick-or-Treaters","A troupe of kiddos in quirky costumes blocks the way: \"Trick or treat!\"","Spend 1 turn handing out candy.",RoadEventEffect.Wait,1),
            new RoadEvent("A Skeleton Dog","A little skeleton pup wants a Halloween treat too, wagging its bony tail!","Spend 1 turn rummaging for dog-safe snacks.",RoadEventEffect.Wait,1),
            new RoadEvent("The Hologram Ghost","Teenagers in bedsheet costumes come charging over with selfie sticks: \"Whoa! This roadside hologram ghost looks so real!\"","Spend 1 turn holding your breath and pretending to be a plastic prop until they leave.",RoadEventEffect.Wait,1),
            new RoadEvent("A Bat in the Postal Cap","Splashed by a warm geyser, a dizzy little lost bat tumbles right into Goolie's postman cap!","Spend 1 turn fanning the little bat to cool it down.",RoadEventEffect.Wait,1),
            new RoadEvent("Halloween Parade","A bustling Halloween parade is marching right across the road ahead!","Wait 1 turn.",RoadEventEffect.Wait,1),
            new RoadEvent("Erupting Geyser","Whoosh! A roadside geyser suddenly erupts in a tall plume of steam!","Wait 1 turn.",RoadEventEffect.Wait,1),
            new RoadEvent("Sudden Fog","A thick blanket of fog rolls in out of nowhere, nudging the little ghost back!","Move back 1 node.",RoadEventEffect.Retreat,0),
            new RoadEvent("Ghost Hunters","Two mischievous kids in cheap werewolf masks are waving bright flashlights at the crossroads ahead, playing \"Ghost Hunters\"!","Wait 1 turn.",RoadEventEffect.Wait,1),
            new RoadEvent("Thawing Hillside","The thawing permafrost on the hillside comes tumbling down in a warm mudslide!","Move back 1 node.",RoadEventEffect.Retreat,0),
            new RoadEvent("A Sudden Gust","Whoosh! A sudden gust of wind nearly blows the weightless little ghost away!","Move back 1 node.",RoadEventEffect.Retreat,0),
            new RoadEvent("Sulfur Steam","A geyser suddenly puffs out a cloud of piping-hot steam, leaving weightless little Goolie too dizzy to open their eyes!","Move back 1 node to dodge the hot steam.",RoadEventEffect.Retreat,0),
            new RoadEvent("An Unexpected Float","A parade of glowing floats suddenly turns onto the street in a whirlwind of confetti and flashing cameras!","Move back 1 node to duck away from the human crowd.",RoadEventEffect.Retreat,0)
        };
        public readonly float[] Durabilities={5,5,5};
        public readonly bool[] Talked=new bool[6];
        public readonly bool[] UnlockedVehicles={true,true,true};
        public readonly List<string> Journal=new List<string>();
        // Legacy editor API only; no global deadline or early ending uses these values.
        public const float ShiftMinutes=240;
        public float NightMinutes=ShiftMinutes;
        public const float CruiseMinutesPerSecond=.3f;
        public int Node,VehicleIndex=-1,Delivered,Earnings,EncounterIndex;
        public float EventProbability=.65f;
        public bool OrderEventTriggered {get{return Parcels.Any(p=>p.State==ParcelState.Loaded&&p.EventTriggered);}}
        // Elapsed stores total integer turns for legacy callers; OrderTurns is the rating clock.
        public float Elapsed,Progress;
        public bool UsedPush;
        public ShiftState State=ShiftState.Docked;
        public Road ActiveRoad;
        public int PlannedDestination {get;private set;} = -1;
        public bool HasJourney {get{return PlannedDestination>=0&&PlannedDestination!=Node&&!Finished;}}
        public string Notice="";
        private int destination;
        private bool returning;
        private float animationSeconds;
        private const float SecondsPerTurn = 8f;
        private readonly Vehicle ghost = new Vehicle("Ghost", "Carries any order; slower travel; no vehicle turns consumed", 0, 0, 1, 0);
        public bool IsGhost { get { return VehicleIndex < 0; } }
        public readonly List<DeliveryResult> Results = new List<DeliveryResult>();
        private int lastOrderTurns;
        public int OrderTurns { get { return ActiveOrder!=null?ActiveOrder.CarriedTurns:lastOrderTurns; } }
        public int TurnsFor(Parcel parcel) { return parcel.State==ParcelState.Delivered?parcel.DeliveryTurns:parcel.CarriedTurns; }
        public IEnumerable<Parcel> ActiveOrders { get { return Parcels.Where(p=>p.State==ParcelState.Loaded&&Batch(p).First()==p); } }
        public bool PreferSafeRoutes = true;
        public int BatchCount { get { return Parcels.Where(p=>p.Enabled).GroupBy(p => new { p.Origin, p.Destination }).Count(); } }
        public int CompletedBatches { get { return Parcels.Where(p=>p.Enabled).GroupBy(p => new { p.Origin, p.Destination }).Count(g => g.All(p => p.State == ParcelState.Delivered)); } }
        public Parcel ActiveOrder { get { return Parcels.Where(p=>p.State==ParcelState.Loaded).OrderBy(p=>TargetTurns(p)-p.CarriedTurns).FirstOrDefault(); } }
        public IEnumerable<Parcel> Batch(Parcel parcel) { return Parcels.Where(p => p.Origin == parcel.Origin && p.Destination == parcel.Destination); }
        // Pickup groups are independent of delivery batches: Woods gives two destinations at once.
        public IEnumerable<Parcel> Pickup(Parcel parcel)
        { return parcel.PickupGroup>=0?Parcels.Where(p=>p.Enabled&&p.PickupGroup==parcel.PickupGroup):Batch(parcel); }
        public IEnumerable<Parcel> CargoRowItems(Parcel parcel)
        { return parcel.State==ParcelState.Waiting&&parcel.Enabled?Pickup(parcel):Batch(parcel); }
        public bool IsCargoRow(Parcel parcel)
        { return parcel.Enabled&&(parcel.State==ParcelState.Waiting||parcel.State==ParcelState.Loaded)&&CargoRowItems(parcel).First()==parcel; }
        public int TargetTurns(Parcel parcel) { return (int)Batch(parcel).Min(p => p.TimeLimit > 0 ? p.TimeLimit : 12); }
        public bool IsReturning { get { return returning; } }
        public int TravelDestination { get { return destination; } }
        public int RemainingTravelTurns { get { return ActiveRoad!=null&&ActiveRoad.StreetPath!=null?StreetRemainingTurns():0; } }
        // Fractional animation is read-only: pausing/events cannot advance clocks or consume durability.
        public float DisplayProgress {get{return ActiveRoad!=null&&ActiveRoad.StreetPath!=null?StreetDisplayProgress():Progress;}}
        private readonly Random random;
        public CourierRun(int seed=31)
        {
            random=new Random(seed);
            Stops=Stops.Concat(CourierStreetMap.Nodes.Where(n=>n.StopId>=9).OrderBy(n=>n.StopId)
                .Select(n=>new Stop(n.Id,"JUNCTION","",n.Point.X/1200,n.Point.Y/820))).ToArray();
            foreach(var n in CourierStreetMap.Nodes){Stops[n.StopId].X=n.Point.X/1200;Stops[n.StopId].Y=n.Point.Y/820;}
            Stops[CourierStreetMap.Node("S").StopId].Name="Parcel Depot";
            Stops[CourierStreetMap.Node("S").StopId].Sign="DEPOT";
            foreach(var stop in Stops){stop.Vehicle=-1;stop.Repair=false;}
            Stops[6].Vehicle=0;Stops[7].Vehicle=1;Stops[8].Vehicle=2;
            int[] limits={7,10,7,13,10,7,10,7,8,8};
            for(int i=0;i<Parcels.Count;i++){var p=Parcels[i];p.TimeLimit=limits[i];p.Heavy=false;p.Dry=i==0||i==6||i==7;p.HeatSensitive=i==1||i==3||i==8||i==9;}
            Parcels[0].Name="Cassette Tape";Parcels[2].Name="Ashpine Autumn Honey";Parcels[3].Name="Flower Jam";Parcels[7].Name="Moisture Absorber";Parcels[8].Name="Radiator";
            Parcels.Add(new Parcel("Desiccant, Labels and Sundries",5,2,1,timeLimit:9));
            Parcels.Add(new Parcel("Late-night Snack",5,1,1,timeLimit:8));
            // Retain legacy IDs for artwork and story references; these three are not tasks in the source flow.
            foreach(int id in new[]{6,10,11}){Parcels[id].Enabled=false;Parcels[id].State=ParcelState.Unavailable;}
            Parcels[2].PickupGroup=Parcels[3].PickupGroup=2;
            Parcels[8].PickupGroup=Parcels[9].PickupGroup=8;
            string[] eventArt={"event_00","event_01","event_00","event_02","event_03","event_04","event_05","event_00","event_05","event_05","event_04","event_03"};
            for(int i=0;i<Events.Length;i++)Events[i].Illustration=eventArt[i];
            Log("Halloween Eve. Choose an order at its sender and plan your route.");
        }
        public Vehicle Car {get{return IsGhost?ghost:Vehicles[VehicleIndex];}}
        public float Durability {get{return IsGhost?0:Durabilities[VehicleIndex];}}
        public int Load {get{return Parcels.Where(p=>p.State==ParcelState.Loaded).Sum(p=>p.Weight);}}
        public bool Overloaded {get{return false;}}
        public bool Late {get{return ActiveOrder!=null&&OrderTurns>TargetTurns(ActiveOrder);}}
        public bool Finished {get{return State==ShiftState.EndingA||State==ShiftState.EndingB;}}
        public string ObjectiveText
        {
            get
            {
                var cargo=Parcels.FirstOrDefault(p=>p.State==ParcelState.Loaded);
                if(cargo!=null)return "Deliver "+cargo.Name+" to "+Stops[cargo.Destination].Name+".";
                var waiting=Parcels.FirstOrDefault(p=>p.State==ParcelState.Waiting&&p.Origin==Node)
                    ??Parcels.FirstOrDefault(p=>p.State==ParcelState.Waiting);
                if(waiting!=null)return waiting.Origin==Node?"Orders available here. Open Orders [Tab] to choose a batch.":"Choose any order in Orders [Tab], then visit its sender.";
                return "Speak to the locals. The Halloween party is almost ready.";
            }
        }
        public float TimeCost(Road r){return r.StreetPath!=null?EstimateStreetTurns(r):Math.Max(1,(float)Math.Ceiling(r.Minutes/6/Car.Speed));}
        public float WearCost(Road r){return IsGhost?0:Math.Min(Durability,TimeCost(r));}
        public string RoadBlockReason(Road r)
        {
            if(r==null||(!Roads.Contains(r)&&!streetRoutes.Values.Contains(r))||!r.Connects(Node))return "Choose a connected stop";
            return RoadAccessReason(r);
        }
        private string RoadAccessReason(Road r)
        {
            return "";
        }
        // Lowest estimated driving time; event delays remain unknown until encountered.
        public List<Road> FindRoute(int target)
        {
            var result=new List<Road>();
            if(target<0||target>=Stops.Length||target==Node)return result;
            // Each leg was found on the shared street graph, with either entrance eligible.
            var leg=StreetRoute(target);
            if(leg!=null)result.Add(leg);
            return result;
        }
        public string DestinationBlockReason(int target)
        {
            if(target<0||target>=Stops.Length)return "Choose a destination";
            if(target==Node)return "You are already here";
            return FindRoute(target).Count>0?"":"No open route is available";
        }
        public bool StartJourney(int target)
        {
            if(State!=ShiftState.Docked||DestinationBlockReason(target)!="")return false;
            var route=FindRoute(target);RememberMovement();PlannedDestination=target;return BeginLeg(MovementLeg(route[0]));
        }
        public bool ContinueJourney(){return HasJourney&&StartJourney(PlannedDestination);}
        public bool CanLoad(Parcel p)
        {return State==ShiftState.Docked && p!=null && p.Enabled && Parcels.Contains(p) && p.State==ParcelState.Waiting && p.Origin==Node && Pickup(p).All(item=>item.Enabled&&item.State==ParcelState.Waiting&&item.Origin==Node);}
        public bool LoadParcel(Parcel p)
        {
            if(!CanLoad(p))return false;
            EmitAudio("cargo");
            foreach(var item in Pickup(p)){item.State=ParcelState.Loaded;item.Deadline=0;item.CarriedTurns=0;item.EventTriggered=false;}
            Log("Order accepted: "+string.Join(" + ",Pickup(p).Select(item=>item.Name))+" > "+string.Join(" / ",Pickup(p).Select(item=>Stops[item.Destination].Name).Distinct())+". Target: "+TargetTurns(p)+" turns.");return true;
        }
        // Active orders cannot be cancelled, split or stored at another node.
        public bool Unload(Parcel p){return false;}
        public bool CanPossess(int v)
        {return State==ShiftState.Docked&&v>=0&&v<Vehicles.Length&&UnlockedVehicles[v]&&Stops[Node].Vehicle==v;}
        public bool Possess(int v)
        {if(!CanPossess(v))return false;if(!IsGhost)Durabilities[VehicleIndex]=0;VehicleIndex=v;Durabilities[v]=Vehicles[v].MaxDurability;AdvanceClock(1);EmitAudio("possession");Log("Vehicle exchanged: "+Car.Name+". 5 vehicle turns; exchange costs 1 order turn.");return true;}
        public bool Repair(){return false;}
        public bool UseGhost()
        {
            if(State!=ShiftState.Docked)return false;
            if(!IsGhost){Durabilities[VehicleIndex]=0;EmitAudio("possession");}VehicleIndex=-1;Log("Ghost transport: all cargo can still be delivered.");return true;
        }
        public string[] TalkToNpc()
        {
            if(State!=ShiftState.Docked&&!Finished)return new string[0];
            // The depot can host the opening letter without being an NPC stop.
            if(Node>=6)return Story != null ? Story.Read(this) : new string[0];
            Talked[Node]=true;
            CheckEnding();
            return Story != null ? Story.Read(this) : new[] { Stops[Node].Voice };
        }
        private void CheckEnding()
        {
            if(!Parcels.Where(p=>p.Enabled).All(p=>p.State==ParcelState.Delivered))return;
            State=ShiftState.EndingA;PlannedDestination=-1;EmitAudio("ending");
            Log("All "+BatchCount+" orders delivered. Halloween deliveries complete!");
        }
        public bool Depart(Road r)
        {
            if(State!=ShiftState.Docked||RoadBlockReason(r)!="")return false;
            RememberMovement();PlannedDestination=r.Other(Node);return BeginLeg(MovementLeg(r));
        }
        private bool BeginLeg(Road r)
        {
            if(!IsGhost&&Durability<=0)BreakDown();
            ActiveRoad=r;destination=r.Other(Node);Progress=0;returning=false;animationSeconds=0;
            State=ShiftState.Driving;EmitAudio("departure");ResetStreetTravel();Log("Departing for "+Stops[destination].Name);return true;
        }
        // Animation only: skips stop at each event and do not change turn costs.
        public bool FastForwardTravel()
        {
            if(State!=ShiftState.Driving)return false;
            Tick(100000);return true;
        }
        public void Tick(float seconds)
        {
            if(ActiveRoad!=null&&ActiveRoad.StreetPath!=null)TickStreet(seconds);
        }
        private void BreakDown()
        {
            VehicleIndex=-1;UsedPush=true;EmitAudio("exhausted");
            Log("Vehicle exhausted. Continue carrying the complete order as a ghost.");
        }
        public string EncounterTitle{get{return Events[EncounterIndex].Title;}}
        public string EncounterDescription{get{return Events[EncounterIndex].Description;}}
        public string EventCost
        {
            get
            {
                var e=Events[EncounterIndex];
                if(e.Effect==RoadEventEffect.Retreat)return "Fall back one node; no extra turn or vehicle use";
                return "+1 order turn; no vehicle use";
            }
        }
        public string SafeChoice{get{return Events[EncounterIndex].Safe+" · "+EventCost;}}
        // Retained for existing callers; these events have one mandatory consequence.
        public string RiskChoice{get{return SafeChoice;}}
        public void ResolveEncounter(bool safe)
        {
            if(State!=ShiftState.Encounter)return;
            var e=Events[EncounterIndex];
            EmitAudio(e.Effect==RoadEventEffect.Wait?"event_wait":"event_retreat");
            if(e.Effect==RoadEventEffect.Wait)AdvanceClock(1);
            else if(travelEdges!=null&&travelEdges.Length>0)
            {
                // Revisit only the final edge, with the protection of the completed movement turn.
                ApplyStreetSurface(travelEdges.Length-1,true);
                var end=CourierStreetMap.Nodes.First(n=>n.StopId==destination);
                destination=travelEdges[travelEdges.Length-1].Other(end).StopId;
                returning=true;
            }
            Log(e.Title+": "+SafeChoice);
            CompleteStreetTurn();
        }

        private void Arrive()
        {
            Node=destination;State=ShiftState.Docked;animationSeconds=0;

            if(Node==PlannedDestination)PlannedDestination=-1;
            var deliveries=ActiveOrders.Where(p=>p.Destination==Node).ToArray();
            EmitAudio(deliveries.Length>0?"delivered":"arrival");
            foreach(var order in deliveries)
            {
                var items=Batch(order).ToArray();int target=TargetTurns(order);int turns=TurnsFor(order);lastOrderTurns=turns;
                var result=new DeliveryResult{Origin=order.Origin,Destination=Node,Turns=turns,TargetTurns=target,
                    Cargo=string.Join(" + ",items.Select(p=>p.Name)),Late=turns>target,Damaged=items.Any(p=>p.Damaged),
                    DamageReason=string.Join(", ",items.Where(p=>p.Damaged).Select(p=>p.Name+" ("+(p.DamageReason??"cargo conditions violated")+")"))};
                foreach(var p in items){p.State=ParcelState.Delivered;p.DeliveryTurns=turns;p.DeliveredLate=result.Late;Delivered++;Earnings+=p.Reward;}
                Results.Add(result);Log("Order delivered: "+result.Cargo+". "+turns+"/"+target+" turns - "+result.Evaluation);
            }
            if(Parcels[8].State==ParcelState.Delivered)UnlockedVehicles[2]=true;
            CheckEnding();
        }
        private void AdvanceClock(float turns)
        {
            int wholeTurns=(int)Math.Ceiling(turns);
            Elapsed+=wholeTurns;
            foreach(var parcel in Parcels.Where(p=>p.State==ParcelState.Loaded))parcel.CarriedTurns+=wholeTurns;
        }
        private void Log(string text){Notice=text;Journal.Insert(0,text);if(Journal.Count>50)Journal.RemoveAt(50);}
    }
}
