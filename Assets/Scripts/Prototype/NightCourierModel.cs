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
        public bool Fragile,Heavy,Dry,Critical,Damaged;
        // Legacy field names remain for prefab/editor compatibility; values are turns.
        public float Deadline,TimeLimit;
        public int DeliveryTurns;
        public bool DeliveredLate;
        public string Evaluation { get { return (DeliveredLate ? "Late" : "On time") + (Damaged ? " / cargo conditions violated" : " / cargo safe"); } }
        public ParcelState State;
        public Parcel(string name,int origin,int destination,int weight,int reward=0,bool critical=false,bool fragile=false,bool heavy=false,bool dry=false,float timeLimit=0)
        {Name=name;Origin=origin;Destination=destination;Weight=weight;Reward=reward;Critical=critical;Fragile=fragile;Heavy=heavy;Dry=dry;TimeLimit=timeLimit;}
        public string Tags {get{return (Critical?"Story ":"")+(TimeLimit>0?"Timed ":"")+(Fragile?"Fragile ":"")+(Heavy?"Heavy ":"")+(Dry?"Keep dry":"");}}
    }
    public enum RoadEventEffect { Wait, Detour, Retreat }
    public sealed class RoadEvent
    {
        public string Title,Description,Safe,Risky;
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
        public string Evaluation { get { return (Late ? "Late" : "On time") + (Damaged ? " / cargo conditions violated" : " / cargo safe"); } }
    }
    public sealed class CourierRun
    {
        public readonly Stop[] Stops={
            new Stop("Abandoned Subway","DROW","Drow: Oh... hello. I made a party tape for the vampire. Could you take it? Only if you are going that way...",0.07f,0.5f,0,false),
            new Stop("Forest Campsite","WOLF","Wolfwoman: There you are, little courier! Honey for the bear, rosin for the drow, flowers for granny. Leave the forest to your big sister!",0.31f,0.2f,-1,true),
            new Stop("Mead Cellar","MEAD","Bear: The honey is late, the labels are damp... Oh dear, I am rambling again. Will my mead be ready for the party?",0.31f,0.8f,1,true),
            new Stop("Abandoned Radio Tower","FM","Vampire: Hail, brave courier! By my honor, the feast shall have music. First, I must rescue this most unchivalrous radio.",0.6f,0.75f,2,true),
            new Stop("Museum Ruins","MUSEUM","Zombie: Oh, a visitor! Did I order flowers? Yes, for the cakes. Come in, dear... which century is it again?",0.69f,0.2f,-1,true),
            new Stop("Settlement Bar","BAR","Witch: Right, darling, one thing at a time. Blood and repairs for our knight, supplies for the bear, then supper. I have the list.",0.94f,0.5f,-1,true)
        };
        // Capacity and wear fields remain for existing assets; they no longer gate order transport.
        public readonly Vehicle[] Vehicles={
            new Vehicle("Scooter","Standard courier; carries any order batch",3,50,1,1),
            new Vehicle("Bicycle","Efficient courier; carries any order batch",8,120,.9f,.7f),
            new Vehicle("Old Pickup","Protects fragile cargo on gravel; carries any order batch",4,80,1.25f,.75f)
        };
        public readonly Road[] Roads={
            new Road(0,1,6,3),new Road(0,2,22,14,RoadKind.Gravel),new Road(1,2,18,8),
            new Road(1,3,16,9,RoadKind.River),new Road(2,3,14,10,RoadKind.Fog),new Road(1,4,22,10),
            new Road(3,4,20,16,RoadKind.Gravel),new Road(4,5,24,31,RoadKind.Storm)
        };
        public readonly List<Parcel> Parcels=new List<Parcel>{
            new Parcel("Party Theme Tape",0,3,1,25,false,false,false,true,0),
            new Parcel("Violin Rosin",1,0,1,20,false,false,false,true,0),
            new Parcel("Autumn Honey",1,2,2,25,false,true,false,false,0),
            new Parcel("Forest Flowers",1,4,1,20,false,false,false,false,0),
            new Parcel("Spring Mead",2,5,4,35,false,true,true,false,0),
            new Parcel("Spicy Blood Stew",3,1,1,25,false,false,false,false,60),
            new Parcel("Flower Cakes",4,1,1,20,false,false,false,false,60),
            new Parcel("Moisture Repellent",5,4,1,20,false,false,false,false,0),
            new Parcel("Desiccant and Labels",5,2,1,20,false,false,false,true,0),
            new Parcel("Repair Equipment",5,3,2,30,false,false,false,false,0),
            new Parcel("Livestock Blood",5,3,1,20,false,false,false,false,0),
            new Parcel("Late-Night Meal",5,1,1,20,false,false,false,false,90)
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
            new RoadEvent("Sulfur Steam","Scalding sulfur steam bursts from a crack in the ground. The heat leaves Goolie unable to see.","Fall back from the hot spring steam",RoadEventEffect.Retreat,0),
            new RoadEvent("An Unexpected Float","A parade float turns into the street. Confetti and camera flashes rush straight at Goolie.","Fall back from the human parade",RoadEventEffect.Retreat,0)
        };
        public readonly float[] Durabilities={50,120,80};
        public readonly bool[] Talked=new bool[6];
        public readonly bool[] UnlockedVehicles={true,false,false};
        public readonly List<string> Journal=new List<string>();
        // Legacy editor API only; no global deadline or early ending uses these values.
        public const float ShiftMinutes=240;
        public float NightMinutes=ShiftMinutes;
        public const float CruiseMinutesPerSecond=.3f;
        public int Node,VehicleIndex,Delivered,Earnings,EncounterIndex;
        // Elapsed stores total integer turns for legacy callers; OrderTurns is the rating clock.
        public float Elapsed,Progress;
        public bool UsedPush;
        public ShiftState State=ShiftState.Docked;
        public Road ActiveRoad;
        public int PlannedDestination {get;private set;} = -1;
        public bool HasJourney {get{return PlannedDestination>=0&&PlannedDestination!=Node&&!Finished;}}
        public string Notice="";
        private int destination;
        private bool eventTriggered, gravelProtected, returning;
        private float returnStartProgress;
        private float animationSeconds;
        private int legTurnsRemaining, legTurnsTotal;
        private const float SecondsPerTurn = 8f;
        private readonly Vehicle ghost = new Vehicle("Ghost", "Carries any order; slower travel; no vehicle turns consumed", 0, 0, .5f, 0);
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
        public int RemainingTravelTurns { get { return legTurnsRemaining; } }
        private readonly Random random;
        public CourierRun(int seed=31)
        {random=new Random(seed);foreach(var p in Parcels)p.TimeLimit=p.TimeLimit>0?(float)Math.Ceiling(p.TimeLimit/6):12;Log("Halloween Eve. Keep the Silence Pact and deliver the party supplies.");}
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
        public float TimeCost(Road r){return (float)Math.Ceiling(Math.Max(2,Math.Ceiling(r.Minutes/6))/Car.Speed);}
        public float WearCost(Road r){return IsGhost?0:Math.Min(Durability,TimeCost(r));}
        public string RoadBlockReason(Road r)
        {
            if(r==null||!Roads.Contains(r)||!r.Connects(Node))return "Choose a connected stop";
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
            var distance=Enumerable.Repeat(float.PositiveInfinity,Stops.Length).ToArray();
            var previous=new Road[Stops.Length];var visited=new bool[Stops.Length];distance[Node]=0;
            for(int i=0;i<Stops.Length;i++)
            {
                int next=-1;for(int j=0;j<Stops.Length;j++)if(!visited[j]&&(next<0||distance[j]<distance[next]))next=j;
                if(next<0||float.IsPositiveInfinity(distance[next]))break;
                if(next==target)break;visited[next]=true;
                foreach(var road in Roads.Where(r=>r.Connects(next)&&RoadAccessReason(r)==""))
                {int other=road.Other(next);float cost=distance[next]+TimeCost(road);if(cost<distance[other]){distance[other]=cost;previous[other]=road;}}
            }
            if(previous[target]==null)return result;
            for(int at=target;at!=Node;){var road=previous[at];result.Insert(0,road);at=road.Other(at);}
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
            var route=FindRoute(target);PlannedDestination=target;return BeginLeg(route[0]);
        }
        public bool ContinueJourney(){return HasJourney&&StartJourney(PlannedDestination);}
        public bool CanLoad(Parcel p)
        {return State==ShiftState.Docked && p!=null && Parcels.Contains(p) && p.State==ParcelState.Waiting && p.Origin==Node && ActiveOrder==null;}
        public bool LoadParcel(Parcel p)
        {
            if(!CanLoad(p))return false;
            OrderTurns=0;
            foreach(var item in Batch(p)){item.State=ParcelState.Loaded;item.Deadline=0;}
            Log("Order accepted: "+string.Join(" + ",Batch(p).Select(item=>item.Name))+" > "+Stops[p.Destination].Name+". Target: "+TargetTurns(p)+" turns.");return true;
        }
        // Active orders cannot be cancelled, split or stored at another node.
        public bool Unload(Parcel p){return false;}
        public bool CanPossess(int v)
        {return State==ShiftState.Docked&&v>=0&&v<Vehicles.Length&&UnlockedVehicles[v]&&Stops[Node].Vehicle==v;}
        public bool Possess(int v)
        {if(!CanPossess(v))return false;VehicleIndex=v;Log("Possessed: "+Car.Name);return true;}
        public bool Repair()
        {
            if(State!=ShiftState.Docked||IsGhost||!Stops[Node].Repair||Durability>=Car.MaxDurability)return false;
            Durabilities[VehicleIndex]=Car.MaxDurability;Log("Vehicle turns restored. Free repair.");return true;
        }
        public bool UseGhost()
        {
            if(State!=ShiftState.Docked)return false;
            VehicleIndex=-1;Log("Ghost transport: all cargo can still be delivered.");return true;
        }
        public string[] TalkToNpc()
        {
            if(State!=ShiftState.Docked)return new string[0];
            Talked[Node]=true;
            if(Node==2)UnlockedVehicles[1]=true;
            if(Node==3&&Parcels[9].State==ParcelState.Delivered)UnlockedVehicles[2]=true;
            CheckEnding();
            var lines=new List<string>{Stops[Node].Voice};
            switch(Node)
            {
                case 0:lines.Add("Drow: The ranger brings my violin rosin. I wrote a tune for everyone... I hope they like it. Please do not tell them I am nervous.");
                    lines.Add("Halloween Eve is your busiest shift. Tab opens cargo; load the tape, choose a stop on the map, then depart.");break;
                case 1:lines.Add("Wolfwoman: I lead lost children out of the fog. Nothing to worry about! Though I could eat three suppers: the vampire's stew, granny's cakes, and the witch's meal.");
                    lines.Add("Wolfwoman: Fewer bees each year, eh? We will manage. Take honey to our little bear, rosin to the shy drow, and flowers to granny.");break;
                case 2:lines.Add("Bear: Spring mead takes months, you know. And the cellar is so warm... I need autumn honey from the wolf, desiccant and labels from the witch. Sorry, am I fussing?");
                    lines.Add("Bear: Please take my mead to the witch. My bicycle can carry the heavy crate. Do borrow it... and mind the bottles. I would be ever so sad.");break;
                case 3:lines.Add("Vampire: A theme tune from the drow! A worthy anthem. The witch supplies blood and repair equipment; until then, I stand guard over this fallen radio.");
                    lines.Add(UnlockedVehicles[2]?"Vampire: Repairs delivered! By my oath, my trusty pickup is yours. It shall shield your glass cargo upon the gravel roads!":"Vampire: Bring repair equipment from the bar and my pickup shall serve you. Take this spicy blood stew to our valiant wolfwoman!");break;
                case 4:lines.Add("Zombie: I moved here a century ago. Or was it last Tuesday? The witch sends moisture repellent. Very kind. My old bones do get damp.");
                    lines.Add("Zombie: The wolf brings fresh flowers, and I bake her flower cakes. Take these to her, dear. Now... did I already offer you a cake?");break;
                case 5:lines.Add("Witch: Blood and repair equipment for the vampire. Desiccant and labels for the bear. Moisture repellent for granny, supper for the wolf. Everyone gets what they need.");
                    lines.Add("Witch: Bring the bear's honey mead here, darling. The humans parade tonight; our party is tomorrow. Eat something, watch the road, and leave the arrangements to me.");break;
            }
            return lines.ToArray();
        }
        private void CheckEnding()
        {
            if(!Parcels.All(p=>p.State==ParcelState.Delivered))return;
            State=ShiftState.EndingA;PlannedDestination=-1;
            Log("All "+BatchCount+" orders delivered. Halloween deliveries complete!");
        }
        public bool Depart(Road r)
        {
            if(State!=ShiftState.Docked||RoadBlockReason(r)!="")return false;
            PlannedDestination=-1;return BeginLeg(r);
        }
        private bool BeginLeg(Road r)
        {
            if(!IsGhost&&Durability<=0)BreakDown();
            ActiveRoad=r;destination=r.Other(Node);Progress=0;eventTriggered=false;returning=false;animationSeconds=0;
            gravelProtected=!IsGhost&&VehicleIndex==2;
            legTurnsRemaining=legTurnsTotal=(int)TimeCost(r);
            State=ShiftState.Driving;Log("Departing for "+Stops[destination].Name);return true;
        }
        // Animation only: skips stop at each event and do not change turn costs.
        public bool FastForwardTravel()
        {
            if(State!=ShiftState.Driving)return false;
            Tick(100000);return true;
        }
        public void Tick(float seconds)
        {
            if(seconds<=0||State!=ShiftState.Driving)return;
            animationSeconds+=seconds;
            while(animationSeconds>=SecondsPerTurn&&State==ShiftState.Driving)
            {
                animationSeconds-=SecondsPerTurn;
                AdvanceClock(1);legTurnsRemaining--;
                Progress=returning?returnStartProgress*legTurnsRemaining/legTurnsTotal:1-(float)legTurnsRemaining/legTurnsTotal;
                if(IsGhost)gravelProtected=false;
                else
                {
                    Durabilities[VehicleIndex]=Math.Max(0,Durability-1);
                    if(Durability<=0)
                    {
                        BreakDown();
                        // Remaining distance takes twice as long on foot. Completed turns stay counted once.
                        legTurnsRemaining=(int)Math.Ceiling(legTurnsRemaining*Vehicles[lastVehicle].Speed/ghost.Speed);
                        legTurnsTotal=legTurnsRemaining==0?1:(int)Math.Ceiling(legTurnsRemaining/Math.Max(.001f,returning?Progress/returnStartProgress:1-Progress));
                    }
                }
                if(legTurnsRemaining<=0){Progress=returning?0:1;Arrive();return;}
                if(!eventTriggered&&Progress>=.5f)
                {
                    eventTriggered=true;EncounterIndex=random.Next(Events.Length);
                    State=ShiftState.Encounter;animationSeconds=0;return;
                }
            }
        }
        private int lastVehicle;
        private void BreakDown()
        {
            lastVehicle=VehicleIndex;VehicleIndex=-1;UsedPush=true;
            Log("Vehicle exhausted. Continue carrying the complete order as a ghost.");
        }
        public string EncounterTitle{get{return Events[EncounterIndex].Title;}}
        public string EncounterDescription{get{return Events[EncounterIndex].Description;}}
        public string EventCost
        {
            get
            {
                var e=Events[EncounterIndex];
                if(e.Effect==RoadEventEffect.Retreat)return "Return to "+Stops[Node].Name+"; travel turns apply";
                return "+"+e.SafeMinutes+(e.Effect==RoadEventEffect.Detour?" travel turns":" order turn");
            }
        }
        public string SafeChoice{get{return Events[EncounterIndex].Safe+" · "+EventCost;}}
        // Retained for existing callers; these events have one mandatory consequence.
        public string RiskChoice{get{return SafeChoice;}}
        public void ResolveEncounter(bool safe)
        {
            if(State!=ShiftState.Encounter)return;
            var e=Events[EncounterIndex];State=ShiftState.Driving;animationSeconds=0;
            if(e.Effect==RoadEventEffect.Wait)AdvanceClock(e.SafeMinutes);
            else if(e.Effect==RoadEventEffect.Detour)
            {
                // Local bypasses also work on bridge roads with no alternate graph route.
                legTurnsRemaining+=(int)e.SafeMinutes;
                legTurnsTotal=(int)Math.Ceiling(legTurnsRemaining/Math.Max(.001f,1-Progress));
            }
            else
            {
                // Node still identifies the last reached stop while travelling.
                returning=true;returnStartProgress=Progress;destination=Node;
                legTurnsRemaining=legTurnsTotal=Math.Max(1,(int)Math.Ceiling(TimeCost(ActiveRoad)*Progress));
            }
            Log(e.Title+": "+SafeChoice);
        }
        private void Arrive()
        {
            if(!returning&&!gravelProtected&&ActiveRoad.Gravel)
                foreach(var p in Parcels.Where(p=>p.State==ParcelState.Loaded&&p.Fragile))p.Damaged=true;
            Node=destination;State=ShiftState.Docked;animationSeconds=0;
            if(Node==PlannedDestination)PlannedDestination=-1;
            var order=ActiveOrder;
            if(order!=null&&order.Destination==Node)
            {
                var items=Batch(order).ToArray();int target=TargetTurns(order);
                var result=new DeliveryResult{Origin=order.Origin,Destination=Node,Turns=OrderTurns,TargetTurns=target,
                    Cargo=string.Join(" + ",items.Select(p=>p.Name)),Late=OrderTurns>target,Damaged=items.Any(p=>p.Damaged)};
                foreach(var p in items){p.State=ParcelState.Delivered;p.DeliveryTurns=OrderTurns;p.DeliveredLate=result.Late;Delivered++;Earnings+=p.Reward;}
                Results.Add(result);Log("Order delivered: "+result.Cargo+". "+OrderTurns+"/"+target+" turns - "+result.Evaluation);
            }
            if(Parcels[9].State==ParcelState.Delivered)UnlockedVehicles[2]=true;
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
