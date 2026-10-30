using System;
using System.Collections.Generic;
using System.Linq;

namespace NightCourier
{
    public enum ParcelState { Waiting, Loaded, Delivered, Failed }
    public enum ShiftState { Docked, Driving, Encounter, Won, Lost }
    public sealed class Stop
    {
        public string Name, Sign, Voice;
        public float X, Y;
        public int Vehicle;
        public bool Repair;
        public Stop(string name, string sign, string voice, float x, float y, int vehicle = -1, bool repair = false)
        { Name = name; Sign = sign; Voice = voice; X = x; Y = y; Vehicle = vehicle; Repair = repair; }
    }
    public sealed class Vehicle
    {
        public string Name, Ability;
        public int Capacity;
        public float MaxDurability, Speed, Wear;
        public Vehicle(string name, string ability, int capacity, float durability, float speed, float wear)
        { Name = name; Ability = ability; Capacity = capacity; MaxDurability = durability; Speed = speed; Wear = wear; }
    }
    public sealed class Road
    {
        public int A, B;
        public float Minutes, Damage;
        public bool Gravel;
        public Road(int a, int b, float minutes, float damage, bool gravel = false)
        { A = a; B = b; Minutes = minutes; Damage = damage; Gravel = gravel; }
        public int Other(int node) { return node == A ? B : A; }
        public bool Connects(int node) { return node == A || node == B; }
    }
    public sealed class Parcel
    {
        public string Name;
        public int Origin, Destination, Weight, Reward;
        public bool Fragile, Heavy, Damaged;
        public float Deadline;
        public ParcelState State;
        public Parcel(string name, int origin, int destination, int weight, int reward, float deadline = 0, bool fragile = false, bool heavy = false)
        { Name = name; Origin = origin; Destination = destination; Weight = weight; Reward = reward; Deadline = deadline; Fragile = fragile; Heavy = heavy; }
        public string Tags { get { return (Deadline > 0 ? "【限时】" : "") + (Fragile ? "【易碎】" : "") + (Heavy ? "【重货】" : ""); } }
    }
    // All simulation state is scene-local. No save files or global random state are changed.
    public sealed class CourierRun
    {
        public readonly Stop[] Stops = {
            new Stop("午夜邮局", "DEAD LETTER / 01", "邮差，天亮前送完四件。先挑货，再规划路线；重货需要皮卡。", .10f, .50f),
            new Stop("霓虹餐厅", "OPEN ALL NIGHT", "这是给灯塔守夜人的热汤。走公路会远一点，但汤碗怕颠。", .34f, .23f),
            new Stop("废车场", "SECOND LIFE MOTORS", "那辆皮卡还会喘气。附上去，重货就能拉了。修车免费，但需要十二分钟。", .34f, .77f, 1, true),
            new Stop("十字加油站", "LAST GAS / 24H", "东边是灯塔，南边是墓园。这里可以免费维修，别把时间都耗在路上。", .58f, .50f, -1, true),
            new Stop("旧灯塔", "KEEP THE LIGHT", "灯没熄，收件人就还在。谢谢你走了这么远。", .83f, .20f),
            new Stop("南山墓园", "REST IN TRANSIT", "大卡车在门边等你。它走碎石路更稳，但很慢。", .81f, .80f, 2),
            new Stop("终点汽车旅馆", "MOTEL / VACANCY", "活人住店，幽灵赶路。这里有最后一批收件人。", .94f, .50f)
        };
        public readonly Vehicle[] Vehicles = {
            new Vehicle("轻型轿车", "轻快：公路速度最快；不能承运重货", 4, 70, 1.2f, 1),
            new Vehicle("旧皮卡", "承重：解锁重货；容量与速度均衡", 8, 100, 1, .9f),
            new Vehicle("重型货车", "减震：碎石路不伤易碎件；耐久损耗降低", 12, 140, .8f, .65f)
        };
        public readonly Road[] Roads = {
            new Road(0,1,20,5), new Road(0,2,15,12,true), new Road(1,2,16,10,true),
            new Road(1,3,21,6), new Road(2,3,14,13,true), new Road(3,4,24,7),
            new Road(3,5,16,14,true), new Road(4,6,16,5), new Road(5,6,18,6), new Road(3,6,32,8)
        };
        public readonly List<Parcel> Parcels = new List<Parcel> {
            new Parcel("余温尚存的信",0,1,1,30,65), new Parcel("守夜人的玻璃灯",0,4,2,60,0,true),
            new Parcel("废旧引擎",0,2,4,55,0,false,true), new Parcel("最后一碗热汤",1,4,1,50,125,true),
            new Parcel("石像的心脏",2,5,4,75,0,false,true), new Parcel("旅馆备用灯泡",2,6,2,55,0,true),
            new Parcel("墓园挂号信",3,5,1,35), new Parcel("不肯醒的行李",5,6,3,45)
        };
        public readonly float[] Durabilities = {70,100,140};
        public readonly List<string> Journal = new List<string>();
        public const float ShiftMinutes = 240;
        public const int Target = 4;
        public int Node, VehicleIndex, Delivered, Earnings, EncounterIndex;
        public float Elapsed, Progress;
        public ShiftState State = ShiftState.Docked;
        public Road ActiveRoad;
        private int destination;
        private bool encounterTriggered;
        private readonly Random random;
        public CourierRun(int seed = 31) { random = new Random(seed); Log("夜班开始。四件快递，天亮之前。"); }
        public Vehicle Car { get { return Vehicles[VehicleIndex]; } }
        public float Durability { get { return Durabilities[VehicleIndex]; } }
        public int Load { get { return Parcels.Where(p => p.State == ParcelState.Loaded).Sum(p => p.Weight); } }
        public bool Overloaded { get { return Load >= Car.Capacity; } }
        public float TimeCost(Road road) { return road.Minutes / Car.Speed * (Overloaded ? 1.35f : 1); }
        public float WearCost(Road road) { return road.Damage * Car.Wear * (Overloaded ? 1.65f : 1); }
        public bool CanLoad(Parcel p) { return State == ShiftState.Docked && p.State == ParcelState.Waiting && p.Origin == Node && (!p.Heavy || VehicleIndex >= 1) && Load + p.Weight <= Car.Capacity + 2; }
        public bool LoadParcel(Parcel p)
        {
            if (!CanLoad(p)) return false;
            p.State = ParcelState.Loaded; Log("装车：" + p.Name); return true;
        }
        public bool Unload(Parcel p)
        {
            if (State != ShiftState.Docked || p.State != ParcelState.Loaded || p.Origin != Node) return false;
            p.State = ParcelState.Waiting; Log("卸回寄件站：" + p.Name); return true;
        }
        public bool Possess(int vehicle)
        {
            if (State != ShiftState.Docked || vehicle < 0 || vehicle >= Vehicles.Length || (vehicle != 0 && Stops[Node].Vehicle != vehicle)) return false;
            if (Load > Vehicles[vehicle].Capacity + 2 || (vehicle == 0 && Parcels.Any(p => p.State == ParcelState.Loaded && p.Heavy))) return false;
            VehicleIndex = vehicle; Log("附身：" + Car.Name + "。货物随灵体转移。"); return true;
        }
        public bool Repair()
        {
            if (State != ShiftState.Docked || !Stops[Node].Repair || Durability >= Car.MaxDurability) return false;
            Durabilities[VehicleIndex] = Car.MaxDurability; AdvanceClock(12); Log("免费维修完成，用时12分钟。"); return true;
        }
        public bool Depart(Road road)
        {
            if (State != ShiftState.Docked || road == null || !Roads.Contains(road) || !road.Connects(Node) || Durability <= 0) return false;
            ActiveRoad = road; destination = road.Other(Node); Progress = 0; encounterTriggered = false;
            State = ShiftState.Driving; Log("出发前往" + Stops[destination].Name); return true;
        }
        public void Tick(float seconds)
        {
            if (State != ShiftState.Driving || seconds <= 0) return;
            // Clamp at the encounter and destination so costs cannot spill across state transitions.
            float limit = encounterTriggered ? 1 : .5f;
            float delta = Math.Min(seconds * 4 / TimeCost(ActiveRoad), limit - Progress);
            Progress += delta;
            Durabilities[VehicleIndex] = Math.Max(0, Durability - WearCost(ActiveRoad) * delta);
            AdvanceClock(TimeCost(ActiveRoad) * delta);
            if (State == ShiftState.Lost) return;
            if (Durability <= 0) { State = ShiftState.Lost; Log("载具在公路上熄火。夜班结束。"); return; }
            if (!encounterTriggered && Progress >= .5f)
            { encounterTriggered = true; EncounterIndex = random.Next(3); State = ShiftState.Encounter; return; }
            if (Progress >= 1) Arrive();
        }
        public string EncounterTitle { get { return new[]{"横穿公路的幽灵鹿", "雨夜里塌陷的路面", "路边求助的迷路灵魂"}[EncounterIndex]; } }
        public string EncounterDescription { get { return new[]{"鹿影停在车灯中央。急刹会摔坏所有易碎件。", "积水遮住深坑。硬闯需要消耗额外耐久。", "它挥着一张过期车票。停车指路需要时间。"}[EncounterIndex]; } }
        public string SafeChoice { get { return new[]{"放慢绕行 · +8分钟", "绕开积水 · +10分钟", "停车指路 · +6分钟"}[EncounterIndex]; } }
        public string RiskChoice { get { return new[]{"急刹穿过 · -7耐久 / 易碎损坏", "直接驶过 · -16耐久 / 易碎损坏", "继续赶路 · -4耐久"}[EncounterIndex]; } }
        public void ResolveEncounter(bool safe)
        {
            if (State != ShiftState.Encounter) return;
            State = ShiftState.Driving;
            if (safe) { AdvanceClock(new[]{8,10,6}[EncounterIndex]); Log("谨慎处理事件，付出额外时间。"); }
            else
            {
                Durabilities[VehicleIndex] = Math.Max(0, Durability - new[]{7,16,4}[EncounterIndex]);
                if (EncounterIndex != 2) DamageFragile();
                Log("冒险通过，载具受到损耗。");
                if (Durability <= 0) { State = ShiftState.Lost; Log("载具损毁。夜班结束。"); }
            }
        }
        private void Arrive()
        {
            if (ActiveRoad.Gravel && VehicleIndex != 2) DamageFragile();
            if (State == ShiftState.Lost) return;
            Node = destination; State = ShiftState.Docked;
            foreach (var p in Parcels.Where(p => p.State == ParcelState.Loaded && p.Destination == Node))
            {
                if (p.Damaged) { p.State = ParcelState.Failed; Log("收件人拒收破损件：" + p.Name); }
                else { p.State = ParcelState.Delivered; Delivered++; Earnings += p.Reward; Log("签收：" + p.Name + " / +" + p.Reward); }
            }
            Log("抵达" + Stops[Node].Name);
            if (Delivered >= Target) { State = ShiftState.Won; Log("四件签收，夜班完成。"); }
            CheckPossible();
        }
        private void AdvanceClock(float minutes)
        {
            Elapsed += minutes;
            foreach (var p in Parcels.Where(p => (p.State == ParcelState.Loaded || p.State == ParcelState.Waiting) && p.Deadline > 0 && Elapsed > p.Deadline))
            { p.State = ParcelState.Failed; Log("订单超时：" + p.Name); }
            if (Elapsed >= ShiftMinutes) { State = ShiftState.Lost; Log("天亮了。未能完成夜班。"); }
            CheckPossible();
        }
        private void CheckPossible()
        {
            if (State == ShiftState.Won || State == ShiftState.Lost) return;
            if (Delivered + Parcels.Count(p => p.State == ParcelState.Waiting || (p.State == ParcelState.Loaded && !p.Damaged)) < Target)
            { State = ShiftState.Lost; Log("剩余可送订单不足四件。夜班结束。"); }
        }
        private void DamageFragile()
        {
            foreach (var p in Parcels.Where(p => p.State == ParcelState.Loaded && p.Fragile && !p.Damaged))
            { p.Damaged = true; Log("易碎件破损：" + p.Name); }
            CheckPossible();
        }
        private void Log(string text) { Journal.Insert(0, text); if (Journal.Count > 30) Journal.RemoveAt(30); }
    }
}
