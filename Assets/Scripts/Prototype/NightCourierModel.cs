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
            new Stop("Route Zero Depot","00","Dispatcher: Halloween again. Head to Rust Anchor Diner, and keep your own parcel close.",.07f,.5f),
            new Stop("Rust Anchor Diner","24H","Waitress: Would you take this tape to the DJ at Midnight Radio Tower?",.31f,.20f),
            new Stop("Echo Junkyard","ECHO","Mechanic: Bring me the engine parts, and that bicycle is yours to possess.",.31f,.80f,1,true),
            new Stop("Sunken Bridge Ferry","FERRY","Sailor: A driver swerved off the cliff last night. Take this key to the lighthouse.",.60f,.75f,-1,true),
            new Stop("Midnight Radio Tower","FM 00","DJ: I've been waiting for that tape. And for you.",.69f,.20f,2),
            new Stop("Cliffside Lighthouse","DAWN","The lighthouse shines toward a home still awake.",.94f,.5f)
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
            new Parcel("Unmarked Parcel",0,5,1,0,true,true),
            new Parcel("Lost Love Tape",1,4,1,0,true),
            new Parcel("Lighthouse Key",3,5,1,0,true,false,false,true),
            new Parcel("Engine Parts",0,2,2,25),
            new Parcel("Sailor's Watch",3,1,1,30,false,true,false,true),
            new Parcel("Hot Coffee",1,2,1,20,false,false,false,false,60),
            new Parcel("Farewell Letters",2,4,4,35,false,false,true)
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
        public const float FinalTimeLimit=30;
        public const float CruiseMinutesPerSecond=.3f;
        public int Node,VehicleIndex,Delivered,Earnings,EncounterIndex;
        public float Elapsed,Progress,FinalDeadline;
        public bool TruthRevealed,UsedPush;
        public ShiftState State=ShiftState.Docked;
        public Road ActiveRoad;
        public int PlannedDestination {get;private set;} = -1;
        public bool HasJourney {get{return PlannedDestination>=0&&PlannedDestination!=Node&&!Finished;}}
        public string Notice="";
        private int destination;
        private bool eventTriggered,pushedThisLeg;
        private readonly Random random;
        public CourierRun(int seed=31)
        {random=new Random(seed);Parcels[0].State=ParcelState.Loaded;Log("Your unmarked parcel is in the trunk. The night shift begins.");}
        public Vehicle Car {get{return Vehicles[VehicleIndex];}}
        public float Durability {get{return Durabilities[VehicleIndex];}}
        public int Load {get{return Parcels.Where(p=>p.State==ParcelState.Loaded).Sum(p=>p.Weight);}}
        public bool Overloaded {get{return Load>=Car.Capacity;}}
        public bool Late {get{return Elapsed>=ShiftMinutes;}}
        public bool FinalLegLate {get{return FinalDeadline>0&&Elapsed>FinalDeadline;}}
        public bool Finished {get{return State==ShiftState.EndingA||State==ShiftState.EndingB;}}
        public bool FinalRoadUnlocked {get{return TruthRevealed&&Parcels[2].State==ParcelState.Loaded;}}
        public string ObjectiveText
        {
            get
            {
                if(!Talked[1])return "Visit Rust Anchor Diner and speak to the waitress.";
                if(Parcels[3].State==ParcelState.Loaded)return "Deliver the engine parts to Echo Junkyard.";
                if(Parcels[2].State==ParcelState.Waiting)return "Visit the sailor at Sunken Bridge Ferry for the lighthouse key.";
                if(!TruthRevealed)return "Deliver the tape to Midnight Radio Tower and speak to the DJ.";
                if(VehicleIndex!=2)return "Possess the old pickup at the tower, then head to the lighthouse.";
                return "Reach Cliffside Lighthouse with your parcel and the key.";
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
            if(r.Kind!=RoadKind.Storm)return "";
            if(!TruthRevealed)return "Deliver the tape to the tower and speak to the DJ first";
            if(Parcels[2].State!=ParcelState.Loaded)return "You need the lighthouse key from the ferry's sailor";
            if(VehicleIndex!=2)return "Possess the old pickup at the tower first";
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
            if(target==5){string reason=RoadAccessReason(Roads.First(r=>r.Kind==RoadKind.Storm));if(reason!="")return reason;}
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
            if(Node==1)GiveCritical(Parcels[1]);
            if(Node==3)GiveCritical(Parcels[2]);
            if(Node==4&&Parcels[1].State==ParcelState.Delivered){TruthRevealed=true;UnlockedVehicles[2]=true;}
            switch(Node)
            {
                case 0:return new[]{"Dispatcher: Welcome to Route Zero. It's Halloween. The roads will be busy tonight.","Dispatcher: That parcel in your trunk has no name on either side. Don't leave it behind.","Dispatcher: Try Rust Anchor Diner first. Tab opens your map and trunk. Drag parcels to load them; press E to continue."};
                case 1:return Parcels[4].State==ParcelState.Delivered?new[]{"Waitress: His watch. So he did remember the time we agreed on.","The scent of hot coffee brings back your last stop among the living."}:new[]{"Waitress: He promised to play a song for me on the radio this Halloween.","Waitress: Take this tape to the DJ at Midnight Radio Tower. I've put it in your trunk.","You remember sitting by this window, drinking a cup of coffee that would never be refilled."};
                case 2:return UnlockedVehicles[1]?new[]{"Mechanic: The parts are here. An old friend can finally ride again.","Mechanic: That bicycle is yours. Plenty of room, even for heavy cargo. Check your load before possessing it.","Mechanic: There's a box of unsent farewell letters. Could you take it to the radio tower?"}:new[]{"Mechanic: I'm waiting for engine parts from Route Zero Depot. Bring them here, and the bicycle is yours.","Mechanic: Repairs are on the house. Money is for the living. All I ask is a little time."};
                case 3:return new[]{"Sailor: There was a pileup on the broken bridge last night.","Sailor: One driver swerved to spare the others and went over the cliff. Parcels scattered everywhere.","Sailor: Take this key to the lighthouse. And return my watch to Rust Anchor Diner. Keep it dry."};
                case 4:return TruthRevealed?new[]{"DJ: I played her tape for everyone on the road. Did you hear it?","DJ: You were the driver who went over the cliff. That unmarked parcel is what you never got to send home.","DJ: The old pickup is below the tower. Take the sailor's key and your parcel. The storm road is open."}:new[]{"DJ: I'm waiting for a tape from Rust Anchor Diner. That song will tell me where you came from.","DJ: The storm has cut off the lighthouse. Ask the sailor at the ferry first."};
                default:return new[]{"The lighthouse shines through the night toward a home still awake."};
            }
        }
        private void GiveCritical(Parcel p)
        {
            if(p.State!=ParcelState.Waiting)return;
            // Automatically store side parcels locally rather than blocking a main-story handoff.
            foreach(var side in Parcels.Where(x=>x.State==ParcelState.Loaded&&!x.Critical).Reverse())
            {if(Load+p.Weight<=Car.Capacity+2)break;side.State=ParcelState.Waiting;side.Origin=Node;Log("Stored to make room for a story parcel: "+side.Name);}
            p.State=ParcelState.Loaded;Log("Story parcel received: "+p.Name);
        }
        public bool Depart(Road r)
        {
            if(State!=ShiftState.Docked||RoadBlockReason(r)!="")return false;
            PlannedDestination=-1;return BeginLeg(r);
        }
        private bool BeginLeg(Road r)
        {
            ActiveRoad=r;destination=r.Other(Node);Progress=0;eventTriggered=false;pushedThisLeg=false;
            State=ShiftState.Driving;if(r.Kind==RoadKind.Storm)FinalDeadline=Elapsed+FinalTimeLimit;Log("Departing for "+Stops[destination].Name);
            if(Durability<=0)BreakDown();return true;
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
            if(Parcels[3].State==ParcelState.Delivered)UnlockedVehicles[1]=true;
            if(pushedThisLeg){Durabilities[VehicleIndex]=Car.MaxDurability*.35f;Log("Pushed to the stop. Emergency repairs restore 35% durability. You can continue.");}
            if(Node==5)
            {
                bool perfect=!Late&&!FinalLegLate&&!Parcels[0].Damaged&&Parcels.Where(p=>!p.Critical).All(p=>p.State==ParcelState.Delivered);
                State=perfect?ShiftState.EndingA:ShiftState.EndingB;
                Log(perfect?"Ending A - Perfect Delivery. Your parcel becomes light, bound for the living.":"Ending B - Road Keeper. You remain as the next midnight courier.");
            }
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
