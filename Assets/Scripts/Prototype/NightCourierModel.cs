using System;
using System.Collections.Generic;
using System.Linq;

namespace NightCourier
{
    public enum ParcelState { Waiting, Loaded, Delivered, Failed }
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
        public int DeliveryTurns;
        public bool DeliveredLate;
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
        public string Evaluation { get { return (Late || Damaged ? "Delivery failed" : "Delivered successfully"); } }
    }
    public sealed partial class CourierRun
    {
        public CourierStory Story;
        // Committed actions survive fast-forward and multiple transitions within one frame.
        public readonly Queue<string> AudioCues=new Queue<string>();
        private void EmitAudio(string cue){if(!string.IsNullOrEmpty(cue))AudioCues.Enqueue(cue);}

        public readonly Stop[] Stops={
            new Stop("Abandoned Subway","DROW","Drow: ...Hello. I hope the violin sounds all right in this damp tunnel.",0.07f,0.5f,0,false),
            new Stop("Abandoned Campsite","WOLF","Wolfwoman: Good to see you, little ghost! Take care out there in the fog.",0.31f,0.2f,-1,true),
            new Stop("Mead Cellar","MEAD","Little Werebear: The cellar feels warm again... I hope I can sleep this winter.",0.31f,0.8f,1,true),
            new Stop("Abandoned TV Tower","FM","Vampire: Hello, little ghost. Come in and rest your ears a moment.",0.6f,0.75f,2,true),
            new Stop("Museum Ruins","MUSEUM","Immigrant Jiangshi: Come in, dear. I am looking after my antiques.",0.69f,0.2f,-1,true),
            new Stop("Settlement Bar","BAR","Witch: One thing at a time, darling. I have the list.",0.94f,0.5f,-1,true),
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
            new RoadEvent("Trick-or-Treaters","Children in all sorts of costumes stop Goolie. \"Trick or treat!\"","Share candy with the children",RoadEventEffect.Wait,1),
            new RoadEvent("A Skeleton Dog","A skeleton dog wants some candy too. Goolie needs to find a treat it can safely eat.","Find a dog-friendly treat",RoadEventEffect.Wait,1),
            new RoadEvent("The Hologram Ghost","Teenagers in white bedsheets rush over with a selfie stick. \"Wow! This hologram ghost looks so real!\"","Pose as a plastic decoration",RoadEventEffect.Wait,1),
            new RoadEvent("A Bat in the Postal Cap","A lost bat, dizzy from the unusually hot October night, crashes into Goolie's postal cap.","Fan the bat to cool it down",RoadEventEffect.Wait,1),
            new RoadEvent("Halloween Parade","A Halloween parade blocks the road ahead.","Take a side path around the parade",RoadEventEffect.Detour,2),
            new RoadEvent("Erupting Geyser","A geyser erupts across the path ahead.","Take a path around the geyser",RoadEventEffect.Detour,2),
            new RoadEvent("Sudden Fog","A sudden bank of thick fog swallows the road.","Follow a clear side path",RoadEventEffect.Detour,2),
            new RoadEvent("Ghost Hunters","Two human kids in cheap werewolf masks sweep the junction with bright flashlights on a ghost hunt.","Detour around the flashlight beams",RoadEventEffect.Detour,2),
            new RoadEvent("Thawing Hillside","Geothermal heat loosens the frozen hillside. Warm mud and dead trees slide down and block the road.","Take a side path around the mudslide",RoadEventEffect.Detour,2),
            new RoadEvent("A Sudden Gust","A sudden gust nearly blows the little ghost away!","Fall back to the previous stop",RoadEventEffect.Retreat,0),
            new RoadEvent("Sulfur Steam","A geyser suddenly blasts scalding steam. The heat leaves light-as-air Goolie unable to keep their eyes open.","Fall back from the hot spring steam",RoadEventEffect.Retreat,0),
            new RoadEvent("An Unexpected Float","A parade float turns into the street. Confetti and camera flashes rush straight at Goolie.","Fall back from the human parade",RoadEventEffect.Retreat,0)
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
        public float EventProbability=.35f;
        public bool OrderEventTriggered {get;private set;}
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
        public int OrderTurns { get; private set; }
        public int BatchCount { get { return Parcels.GroupBy(p => new { p.Origin, p.Destination }).Count(); } }
        public int CompletedBatches { get { return Parcels.GroupBy(p => new { p.Origin, p.Destination }).Count(g => g.All(p => p.State == ParcelState.Delivered)); } }
        public Parcel ActiveOrder { get { return Parcels.FirstOrDefault(p => p.State == ParcelState.Loaded); } }
        public IEnumerable<Parcel> Batch(Parcel parcel) { return Parcels.Where(p => p.Origin == parcel.Origin && p.Destination == parcel.Destination); }
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
            foreach(var stop in Stops){stop.Vehicle=-1;stop.Repair=false;}
            Stops[6].Vehicle=0;Stops[7].Vehicle=1;Stops[8].Vehicle=2;
            int[] limits={4,3,4,5,4,4,4,4,4,4};
            for(int i=0;i<Parcels.Count;i++){var p=Parcels[i];p.TimeLimit=limits[i];p.Heavy=false;p.Dry=i==0||i==6||i==7;p.HeatSensitive=i==1||i==3||i==8||i==9;}
            Parcels[3].Name="Flowers";Parcels[8].Name="Repair Device";
            Parcels.Add(new Parcel("Desiccant, Labels and Sundries",5,2,1,timeLimit:4));
            Parcels.Add(new Parcel("Late-night Snack",5,1,1,timeLimit:4));
            foreach(int id in new[]{4,5,7}){Events[id].Effect=RoadEventEffect.Wait;Events[id].SafeMinutes=1;Events[id].Safe="Wait for the road to clear";}
            foreach(int id in new[]{6,8}){Events[id].Effect=RoadEventEffect.Retreat;Events[id].SafeMinutes=0;Events[id].Safe="Fall back one node";}
            Events[3].Description="A lost bat is splashed by a geyser and crashes, dizzy, into Goolie's postal cap.";
            foreach(int id in new[]{3,5,8,9,10})Events[id].Illustration="weather";
            Events[6].Illustration="weather";
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
                if(waiting!=null)return "Collect "+waiting.Name+" at "+Stops[waiting.Origin].Name+". Keep the Silence Pact.";
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
            var route=FindRoute(target);PlannedDestination=target;return BeginLeg(MovementLeg(route[0]));
        }
        public bool ContinueJourney(){return HasJourney&&StartJourney(PlannedDestination);}
        public bool CanLoad(Parcel p)
        {return State==ShiftState.Docked && p!=null && Parcels.Contains(p) && p.State==ParcelState.Waiting && p.Origin==Node && ActiveOrder==null;}
        public bool LoadParcel(Parcel p)
        {
            if(!CanLoad(p))return false;
            OrderTurns=0;OrderEventTriggered=false;EmitAudio("cargo");
            foreach(var item in Batch(p)){item.State=ParcelState.Loaded;item.Deadline=0;}
            Log("Order accepted: "+string.Join(" + ",Batch(p).Select(item=>item.Name))+" > "+Stops[p.Destination].Name+". Target: "+TargetTurns(p)+" turns.");return true;
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
            if((State!=ShiftState.Docked&&!Finished)||Node>=6)return new string[0];
            Talked[Node]=true;
            CheckEnding();
            return Story != null ? Story.Read(this) : new[] { Stops[Node].Voice };
        }
        private void CheckEnding()
        {
            if(!Parcels.All(p=>p.State==ParcelState.Delivered))return;
            State=ShiftState.EndingA;PlannedDestination=-1;EmitAudio("ending");
            Log("All "+BatchCount+" orders delivered. Halloween deliveries complete!");
        }
        public bool Depart(Road r)
        {
            if(State!=ShiftState.Docked||RoadBlockReason(r)!="")return false;
            PlannedDestination=r.Other(Node);return BeginLeg(MovementLeg(r));
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
            var order=ActiveOrder;
            EmitAudio(order!=null&&order.Destination==Node?"delivered":"arrival");
            if(order!=null&&order.Destination==Node)
            {
                var items=Batch(order).ToArray();int target=TargetTurns(order);
                var result=new DeliveryResult{Origin=order.Origin,Destination=Node,Turns=OrderTurns,TargetTurns=target,
                    Cargo=string.Join(" + ",items.Select(p=>p.Name)),Late=OrderTurns>target,Damaged=items.Any(p=>p.Damaged)};
                foreach(var p in items){p.State=ParcelState.Delivered;p.DeliveryTurns=OrderTurns;p.DeliveredLate=result.Late;Delivered++;Earnings+=p.Reward;}
                Results.Add(result);Log("Order delivered: "+result.Cargo+". "+OrderTurns+"/"+target+" turns - "+result.Evaluation);
            }
            if(Parcels[8].State==ParcelState.Delivered)UnlockedVehicles[2]=true;
            CheckEnding();
        }
        private void AdvanceClock(float turns)
        {
            int wholeTurns=(int)Math.Ceiling(turns);
            Elapsed+=wholeTurns;
            if(ActiveOrder!=null)OrderTurns+=wholeTurns;
        }
        private void Log(string text){Notice=text;Journal.Insert(0,text);if(Journal.Count>50)Journal.RemoveAt(50);}
    }
}
