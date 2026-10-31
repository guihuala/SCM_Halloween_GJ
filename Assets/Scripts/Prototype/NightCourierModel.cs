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
        public float Deadline,TimeLimit;
        public ParcelState State;
        public Parcel(string name,int origin,int destination,int weight,int reward=0,bool critical=false,bool fragile=false,bool heavy=false,bool dry=false,float timeLimit=0)
        {Name=name;Origin=origin;Destination=destination;Weight=weight;Reward=reward;Critical=critical;Fragile=fragile;Heavy=heavy;Dry=dry;TimeLimit=timeLimit;}
        public string Tags {get{return (Critical?"Story ":"")+(TimeLimit>0?"Timed ":"")+(Fragile?"Fragile ":"")+(Heavy?"Heavy ":"")+(Dry?"Keep dry":"");}}
    }
    public sealed class RoadEvent
    {
        public string Title,Description,Safe,Risky;
        public float SafeMinutes,RiskDamage;
        public bool BreakFragile,WetDry;
        public RoadEvent(string title,string description,string safe,string risky,float time,float damage,bool fragile=false,bool wet=false)
        {Title=title;Description=description;Safe=safe;Risky=risky;SafeMinutes=time;RiskDamage=damage;BreakFragile=fragile;WetDry=wet;}
    }
    public sealed class CourierRun
    {
        public readonly Stop[] Stops={
            new Stop("Abandoned Subway","DROW","Drow: Keep our world a secret. Please take my party theme tape to the vampire DJ.",0.07f,0.5f,0,false),
            new Stop("Forest Campsite","WOLF","Wolfwoman: Humans must never learn about us. There is honey, rosin and flowers to deliver.",0.31f,0.2f,-1,true),
            new Stop("Mead Cellar","MEAD","Bear: Too warm to sleep. Damp labels, spoiled mead... I need autumn honey and fresh supplies.",0.31f,0.8f,1,true),
            new Stop("Abandoned Radio Tower","FM","Vampire: Tonight I play the host! If only this overheated radio would cooperate.",0.6f,0.75f,2,true),
            new Stop("Museum Ruins","MUSEUM","Zombie: I repair antiques. Flowers for cakes. Moisture repellent for me. Please.",0.69f,0.2f,-1,true),
            new Stop("Settlement Bar","BAR","Witch: The human parade is outside. Keep the Silence Pact, and bring the party supplies inside.",0.94f,0.5f,-1,true)
        };
        // 3.4.1 is authoritative; heavy eligibility follows unlock tier, not vehicle name.
        public readonly Vehicle[] Vehicles={
            new Vehicle("Scooter","Lightweight; cannot carry heavy parcels",3,50,1,1),
            new Vehicle("Bicycle","Carries heavy parcels; largest capacity",8,120,.9f,.7f),
            new Vehicle("Old Pickup","Carries heavy parcels; protects fragile cargo on gravel",4,80,1.25f,.75f)
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
            new RoadEvent("Trick-or-Treaters","Children gather in the headlights, demanding candy.","Share your candy · +4 min","Brake and swerve · -7 durability / fragile cargo damaged",4,7,true),
            new RoadEvent("Strangers in the Fog","Dark elves peer through the fog, trying to read your license plate.","Dim the lights and wait · +6 min","Push through the fog · -12 durability",6,12),
            new RoadEvent("Waves on the River","The ferry rocks. Waves threaten to soak the trunk.","Secure the tarp · +5 min","Ride through the waves · -8 durability / dry cargo soaked",5,8,false,true),
            new RoadEvent("Falling Rocks","Rocks tumble down the slope, blocking the shortcut.","Pick a careful path · +5 min","Drive over the rocks · -14 durability / fragile cargo damaged",5,14,true),
            new RoadEvent("Werewolf Roadblock","A werewolf family has closed the road for their Halloween celebrations.","Stop and ask for a way through · +4 min","Break through the fence · -10 durability",4,10),
            new RoadEvent("The Bear's Honey Cart","A little bear's cart, laden with honey, blocks the lane.","Help push the cart aside · +5 min","Squeeze past the guardrail · -6 durability",5,6),
            new RoadEvent("A Vampire Hitchhiker","He asks: Could you take me a little farther before sunrise?","Give him a ride · +4 min","Swerve away · -8 durability / fragile cargo damaged",4,8,true),
            new RoadEvent("The Zombie's Old Map","A zombie newcomer holds an old map upside down.","Mark the way for him · +3 min","Take the muddy shoulder · -9 durability / dry cargo soaked",3,9,false,true),
            new RoadEvent("A Witch's Crosswind","A passing broomstick's wake pushes your vehicle toward the shoulder.","Pull over and wait · +5 min","Accelerate into the wind · -13 durability",5,13),
            new RoadEvent("Old Headlights in the Storm","Lightning reveals last night's tire tracks. You recognize the skid marks.","Hold the vehicle steady · +6 min","Race after the headlights · -18 durability / fragile cargo damaged",6,18,true)
        };
        public readonly float[] Durabilities={50,120,80};
        public readonly bool[] Talked=new bool[6];
        public readonly bool[] UnlockedVehicles={true,false,false};
        public readonly List<string> Journal=new List<string>();
        public const float ShiftMinutes=240;
        public float NightMinutes=ShiftMinutes;
        public const float CruiseMinutesPerSecond=.3f;
        public int Node,VehicleIndex,Delivered,Earnings,EncounterIndex;
        public float Elapsed,Progress;
        public bool UsedPush;
        public ShiftState State=ShiftState.Docked;
        public Road ActiveRoad;
        public int PlannedDestination {get;private set;} = -1;
        public bool HasJourney {get{return PlannedDestination>=0&&PlannedDestination!=Node&&!Finished;}}
        public string Notice="";
        private int destination;
        private bool eventTriggered,pushedThisLeg;
        private readonly Random random;
        public CourierRun(int seed=31)
        {random=new Random(seed);Log("Halloween Eve. Keep the Silence Pact and deliver the party supplies.");}
        public Vehicle Car {get{return Vehicles[VehicleIndex];}}
        public float Durability {get{return Durabilities[VehicleIndex];}}
        public int Load {get{return Parcels.Where(p=>p.State==ParcelState.Loaded).Sum(p=>p.Weight);}}
        public bool Overloaded {get{return Load>=Car.Capacity;}}
        public bool Late {get{return Elapsed>=NightMinutes;}}
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
        public float TimeCost(Road r){return r.Minutes/Car.Speed*(Overloaded?1.35f:1);}
        public float WearCost(Road r){return r.Damage*Car.Wear*(Overloaded?1.65f:1);}
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
        {return State==ShiftState.Docked&&Parcels.Contains(p)&&p.State==ParcelState.Waiting&&p.Origin==Node&&(!p.Heavy||VehicleIndex>0)&&Load+p.Weight<=Car.Capacity+2;}
        public bool LoadParcel(Parcel p)
        {
            if(!CanLoad(p))return false;
            p.State=ParcelState.Loaded;if(p.TimeLimit>0&&p.Deadline==0)p.Deadline=Elapsed+p.TimeLimit;
            Log("Loaded: "+p.Name+" > "+Stops[p.Destination].Name);return true;
        }
        public bool Unload(Parcel p)
        {
            if(State!=ShiftState.Docked||p.State!=ParcelState.Loaded||p.Critical)return false;
            p.State=ParcelState.Waiting;p.Origin=Node;Log("Stored: "+p.Name);return true;
        }
        public bool CanPossess(int v)
        {return State==ShiftState.Docked&&v>=0&&v<Vehicles.Length&&UnlockedVehicles[v]&&Stops[Node].Vehicle==v&&Load<=Vehicles[v].Capacity+2;}
        public bool Possess(int v)
        {if(!CanPossess(v))return false;VehicleIndex=v;Log("Possessed: "+Car.Name);return true;}
        public bool Repair()
        {
            if(State!=ShiftState.Docked||!Stops[Node].Repair||Durability>=Car.MaxDurability)return false;
            Durabilities[VehicleIndex]=Car.MaxDurability;AdvanceClock(12);Log("Vehicle repaired. 12 minutes elapsed.");return true;
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
                case 0:lines.Add("Drow: I write and play the violin. The ranger supplies my rosin; my new tune is for tomorrow's party.");
                    lines.Add("Halloween Eve is your busiest shift. Tab opens the map and trunk; load the tape, choose a stop, then depart.");break;
                case 1:lines.Add("Wolfwoman: I patrol the foggy forest and lead stray children home. Tonight I could eat three suppers.");
                    lines.Add("Wolfwoman: Fewer bees each year. Take autumn honey to the bear, rosin to the drow, and flowers to the museum.");break;
                case 2:lines.Add("Bear: Spring mead takes months! Now the cellar is too warm, and the labels are damp. Are we going to have to move?");
                    lines.Add("Bear: Take the mead to the witch. You may possess my bicycle; it can carry the heavy crate.");break;
                case 3:lines.Add("Vampire: A theme tune for our grand celebration! My radiator is overloaded; the witch has the repair equipment.");
                    lines.Add(UnlockedVehicles[2]?"Vampire: Repairs delivered! The old pickup is yours to possess. It protects glass on gravel.":"Vampire: Bring the repair equipment from the bar to unlock my old pickup. I have a hot supper for the ranger.");break;
                case 4:lines.Add("Zombie: Came here a century ago. Still here. Still damp. The witch has moisture repellent.");
                    lines.Add("Zombie: Forest flowers make good cakes. Take these to the ranger. Before they go stale.");break;
                case 5:lines.Add("Witch: I trade with the humans. Blood and repair equipment for the DJ, labels for the bear, lotion for our old friend.");
                    lines.Add("Witch: The humans parade tonight. Tomorrow is our party. Deliver what you can; we will celebrate together.");break;
            }
            return lines.ToArray();
        }
        private void CheckEnding()
        {
            bool resolved=Parcels.All(p=>p.State==ParcelState.Delivered||p.State==ParcelState.Failed);
            if(!resolved&&!(Late&&Talked.All(t=>t)))return;
            bool perfect=!Late&&Parcels.All(p=>p.State==ParcelState.Delivered&&!p.Damaged);
            State=perfect?ShiftState.EndingA:ShiftState.EndingB;PlannedDestination=-1;
            Log(perfect?"Party Ready! Every delivery arrived safely. Ghost Courier is always with you.":"Together at Dawn. The night was difficult, but everyone is welcome at the party.");
        }
        public bool Depart(Road r)
        {
            if(State!=ShiftState.Docked||RoadBlockReason(r)!="")return false;
            PlannedDestination=-1;return BeginLeg(r);
        }
        private bool BeginLeg(Road r)
        {
            ActiveRoad=r;destination=r.Other(Node);Progress=0;eventTriggered=false;pushedThisLeg=false;
            State=ShiftState.Driving;Log("Departing for "+Stops[destination].Name);
            if(Durability<=0)BreakDown();return true;
        }
        // Fast-forward presentation only; never choose an event for the player.
        public bool FastForwardTravel()
        {
            if(State!=ShiftState.Driving&&State!=ShiftState.Pushing)return false;
            for(int i=0;i<100000&&(State==ShiftState.Driving||State==ShiftState.Pushing);i++)Tick(.1f);
            return true;
        }
        public void Tick(float seconds)
        {
            if(seconds<=0||(State!=ShiftState.Driving&&State!=ShiftState.Pushing))return;
            bool pushing=State==ShiftState.Pushing;
            float limit=pushing||eventTriggered?1:.5f;
            float delta=Math.Min(seconds*CruiseMinutesPerSecond*(pushing?.45f:1)/TimeCost(ActiveRoad),limit-Progress);
            Progress+=delta;AdvanceClock(TimeCost(ActiveRoad)*delta*(pushing?1.8f:1));
            if(!pushing)
            {
                Durabilities[VehicleIndex]=Math.Max(0,Durability-WearCost(ActiveRoad)*delta);
                if(Durability<=0){BreakDown();return;}
                if(!eventTriggered&&Progress>=.5f)
                {
                    eventTriggered=true;EncounterIndex=ActiveRoad.Kind==RoadKind.River?2:ActiveRoad.Kind==RoadKind.Fog?1:ActiveRoad.Kind==RoadKind.Storm?9:random.Next(Events.Length);
                    State=ShiftState.Encounter;return;
                }
            }
            if(Progress>=1)Arrive();
        }
        private void BreakDown()
        {
            State=ShiftState.Pushing;pushedThisLeg=true;UsedPush=true;
            foreach(var p in Parcels.Where(p=>p.State==ParcelState.Loaded&&p.TimeLimit>0&&!p.Critical))p.State=ParcelState.Failed;
            Log("Broken down. You push on to the next stop. Timed side parcels expire; your story continues.");
        }
        public string EncounterTitle{get{return Events[EncounterIndex].Title;}}
        public string EncounterDescription{get{return Events[EncounterIndex].Description;}}
        public string SafeChoice{get{var e=Events[EncounterIndex];return e.Safe.Split('·')[0].Trim()+" · +"+e.SafeMinutes+" min";}}
        public string RiskChoice{get{var e=Events[EncounterIndex];return e.Risky.Split('·')[0].Trim()+" · -"+e.RiskDamage+" durability"+(e.BreakFragile?" / fragile cargo damaged":"")+(e.WetDry?" / dry cargo soaked":"");}}
        public void ResolveEncounter(bool safe)
        {
            if(State!=ShiftState.Encounter)return;
            var e=Events[EncounterIndex];State=ShiftState.Driving;
            if(safe)AdvanceClock(e.SafeMinutes);
            else
            {
                Durabilities[VehicleIndex]=Math.Max(0,Durability-e.RiskDamage);
                foreach(var p in Parcels.Where(p=>p.State==ParcelState.Loaded&&((e.BreakFragile&&p.Fragile)||(e.WetDry&&p.Dry))))p.Damaged=true;
                if(Durability<=0)BreakDown();
            }
            Log(safe?"Safe passage: "+e.Title:"Risky passage: "+e.Title);
        }
        private void Arrive()
        {
            if(!pushedThisLeg&&ActiveRoad.Gravel&&VehicleIndex!=2)
                foreach(var p in Parcels.Where(p=>p.State==ParcelState.Loaded&&p.Fragile))p.Damaged=true;
            Node=destination;State=ShiftState.Docked;
            if(Node==PlannedDestination)PlannedDestination=-1;
            foreach(var p in Parcels.Where(p=>p.State==ParcelState.Loaded&&p.Destination==Node))
            {
                if(p.Damaged&&!p.Critical){p.State=ParcelState.Failed;Log("Refused, damaged: "+p.Name);}
                else {p.State=ParcelState.Delivered;Delivered++;Earnings+=p.Reward;Log("Delivered: "+p.Name);}
            }
            if(Parcels[9].State==ParcelState.Delivered)UnlockedVehicles[2]=true;
            if(pushedThisLeg){Durabilities[VehicleIndex]=Car.MaxDurability*.35f;Log("Pushed to the stop. Emergency repairs restore 35% durability. You can continue.");}
            CheckEnding();
        }
        private void AdvanceClock(float minutes)
        {
            Elapsed+=minutes;
            foreach(var p in Parcels.Where(p=>!p.Critical&&p.TimeLimit>0&&p.Deadline>0&&(p.State==ParcelState.Loaded||p.State==ParcelState.Waiting)&&Elapsed>p.Deadline))
            {p.State=ParcelState.Failed;Log("Timed delivery expired: "+p.Name);}
        }
        private void Log(string text){Notice=text;Journal.Insert(0,text);if(Journal.Count>50)Journal.RemoveAt(50);}
    }
}
