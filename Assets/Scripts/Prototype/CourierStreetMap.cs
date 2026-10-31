using System;
using System.Collections.Generic;
using System.Linq;

namespace NightCourier
{
    // Reference coordinates are traced from the V2.4 shared-junction map (1200 x 820).
    // Only these explicit vertices connect roads. Geometry intersections never create edges.
    public struct StreetPoint
    {
        public float X, Y;
        public StreetPoint(float x,float y){X=x;Y=y;}
        public static StreetPoint Lerp(StreetPoint a,StreetPoint b,float t)
        {return new StreetPoint(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);}
        public static float Distance(StreetPoint a,StreetPoint b)
        {return (float)Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
    }
    public sealed class StreetNode
    {
        public readonly string Id;
        public readonly StreetPoint Point;
        public readonly int StopId;
        public StreetNode(string id,float x,float y,int stop=-1){Id=id;Point=new StreetPoint(x,y);StopId=stop;}
    }
    public enum StreetSurface { Normal, Heat, Water, Bumpy }
    public sealed class StreetEdge
    {
        public readonly StreetNode A,B;
        public readonly StreetSurface Surface;
        public readonly StreetPoint[] Points;
        private readonly float[] lengths;
        public float Length {get{return lengths[lengths.Length-1];}}
        public StreetEdge(StreetNode a,StreetNode b,StreetSurface surface,StreetPoint? control)
        {
            A=a;B=b;Surface=surface;
            Points=new StreetPoint[17];lengths=new float[17];
            for(int i=0;i<Points.Length;i++)
            {
                float t=i/16f;
                Points[i]=control.HasValue?StreetPoint.Lerp(StreetPoint.Lerp(a.Point,control.Value,t),StreetPoint.Lerp(control.Value,b.Point,t),t):StreetPoint.Lerp(a.Point,b.Point,t);
                if(i>0)lengths[i]=lengths[i-1]+StreetPoint.Distance(Points[i-1],Points[i]);
            }
        }
        public StreetNode Other(StreetNode n){return n==A?B:A;}
        public StreetPoint Sample(StreetNode from,float progress)
        {
            float d=Math.Max(0,Math.Min(1,from==A?progress:1-progress))*lengths[lengths.Length-1];
            for(int i=1;i<lengths.Length;i++)if(d<=lengths[i])return StreetPoint.Lerp(Points[i-1],Points[i],(d-lengths[i-1])/Math.Max(.001f,lengths[i]-lengths[i-1]));
            return B.Point;
        }
    }
    public static class CourierStreetMap
    {
        public static readonly StreetNode[] Nodes={
            new StreetNode("1",390,100,0),new StreetNode("2",180,370,1),new StreetNode("6",420,750,2),
            new StreetNode("3",800,105,3),new StreetNode("4",970,690,4),new StreetNode("5",1050,360,5),
            new StreetNode("1a",350,195,9),new StreetNode("1b",500,130,10),new StreetNode("2a",250,270,11),new StreetNode("2b",170,480,12),
            new StreetNode("3a",610,220,13),new StreetNode("3b",790,165,14),new StreetNode("4a",970,560,15),new StreetNode("4b",1050,660,16),
            new StreetNode("5a",930,270,17),new StreetNode("5b",1100,455,18),new StreetNode("6a",300,650,19),new StreetNode("6b",300,730,20),
            new StreetNode("N",670,165,21),new StreetNode("U",420,280,22),new StreetNode("V",760,280,23),new StreetNode("C",550,420,24),
            new StreetNode("J",690,470,25),new StreetNode("R",850,490,26),new StreetNode("B",650,610,27),new StreetNode("L",350,530,28),
            new StreetNode("P",230,590,29),new StreetNode("E",1060,560,30),new StreetNode("S",90,520,31),new StreetNode("H",440,375,6),
            new StreetNode("W",900,340,7),new StreetNode("Q",440,640,8)
        };
        public static StreetNode Node(string id){return Nodes.First(n=>n.Id==id);}
        private static StreetEdge Edge(string a,string b,StreetSurface surface=StreetSurface.Normal,float x=float.NaN,float y=0)
        {return new StreetEdge(Node(a),Node(b),surface,float.IsNaN(x)?(StreetPoint?)null:new StreetPoint(x,y));}
        public static readonly StreetEdge[] Edges={
            Edge("1","1a",x:390,y:155),Edge("1","1b",x:450,y:100),Edge("1a","U",StreetSurface.Heat,370,245),
            Edge("1b","N",x:580,y:170),Edge("N","3b",x:730,y:145),Edge("3b","3"),
            Edge("3","3a",x:735,y:175),Edge("3a","N"),Edge("3a","H",x:535,y:310),Edge("U","3a",StreetSurface.Heat,505,230),
            Edge("N","V",x:695,y:235),Edge("U","V",StreetSurface.Heat,590,255),Edge("U","C",StreetSurface.Heat,475,370),
            Edge("C","V",StreetSurface.Heat,670,365),Edge("U","H",x:415,y:335),Edge("H","C",x:505,y:380),
            Edge("U","2a",StreetSurface.Bumpy,325,300),Edge("2a","2",x:190,y:320),Edge("2","2b",x:195,y:425),
            Edge("2b","S"),Edge("2b","U"),Edge("2b","P",x:185,y:540),Edge("S","L",x:220,y:500),
            Edge("U","L",x:370,y:410),Edge("L","P",x:280,y:545),Edge("P","Q",x:320,y:635),
            Edge("L","Q",x:375,y:600),Edge("Q","B",x:535,y:605),Edge("L","B",StreetSurface.Bumpy,510,545),
            Edge("L","C",StreetSurface.Bumpy,455,490),Edge("C","B",StreetSurface.Bumpy,580,530),
            Edge("C","J",x:640,y:425),Edge("C","J",StreetSurface.Water,610,445),Edge("J","R",StreetSurface.Water,770,485),
            Edge("V","J",x:750,y:385),Edge("J","B",x:690,y:550),Edge("V","R",StreetSurface.Water,785,400),
            Edge("V","W",x:820,y:325),Edge("W","R",x:900,y:420),Edge("W","E",x:965,y:460),
            Edge("R","E",x:965,y:510),Edge("R","4a",StreetSurface.Water,925,500),Edge("4a","4",x:950,y:625),
            Edge("4","4b",x:1005,y:665),Edge("4b","E",x:1075,y:605),Edge("E","5b",x:1060,y:505),
            Edge("5b","5",x:1090,y:400),Edge("5","5a",x:975,y:335),Edge("5a","V",StreetSurface.Water,845,250),
            Edge("R","B",StreetSurface.Water,735,530),Edge("L","6a",StreetSurface.Bumpy,345,600),
            Edge("P","6a"),Edge("6a","6",x:350,y:715),Edge("P","6b",x:280,y:650),Edge("6b","6",x:360,y:760)
        };
        public static List<StreetEdge> FindPath(int from,int to,Func<StreetEdge,float> cost=null)
        {
            var start=Nodes.First(n=>n.StopId==from);var goal=Nodes.First(n=>n.StopId==to);
            var distance=Nodes.ToDictionary(n=>n,n=>float.PositiveInfinity);distance[start]=0;
            var length=Nodes.ToDictionary(n=>n,n=>float.PositiveInfinity);length[start]=0;
            var previous=new Dictionary<StreetNode,StreetEdge>();var visited=new HashSet<StreetNode>();
            while(visited.Count<Nodes.Length)
            {
                var n=Nodes.Where(v=>!visited.Contains(v)).OrderBy(v=>distance[v]).ThenBy(v=>length[v]).First();
                if(float.IsPositiveInfinity(distance[n])||n==goal)break;visited.Add(n);
                foreach(var edge in Edges.Where(e=>e.A==n||e.B==n))
                {
                    var next=edge.Other(n);
                    // Customers are destinations, never shortcuts between their two entrances.
                    if((next.StopId>=0&&next.StopId<6&&next!=goal)||visited.Contains(next))continue;
                    float hops=distance[n]+(cost!=null?cost(edge):1);float meters=length[n]+edge.Length;
                    if(hops<distance[next]||(hops==distance[next]&&meters<length[next]))
                    {distance[next]=hops;length[next]=meters;previous[next]=edge;}
                }
            }
            var path=new List<StreetEdge>();if(!previous.ContainsKey(goal))return path;
            for(var n=goal;n!=start;){var e=previous[n];path.Insert(0,e);n=e.Other(n);}return path;
        }
        private sealed class RouteState
        {
            public StreetNode Node;public int Used,Fuel,Turns;public float Length;
            public RouteState Previous;public StreetEdge Edge;
            public string Key {get{return Node.Id+":"+Used+":"+Fuel;}}
        }
        // Dijkstra over turn position and remaining vehicle uses, including early station stops.
        public static List<StreetEdge> FindMovementPath(int from,int to,int speed,int fuel,Func<StreetEdge,bool,bool> canTraverse=null)
        {
            var start=Nodes.First(n=>n.StopId==from);var goal=Nodes.First(n=>n.StopId==to);
            var initial=new RouteState{Node=start,Fuel=fuel};
            var open=new List<RouteState>{initial};var best=new Dictionary<string,RouteState>{{initial.Key,initial}};
            while(open.Count>0)
            {
                var state=open.OrderBy(x=>x.Turns).ThenBy(x=>x.Length).First();open.Remove(state);
                if(best[state.Key]!=state)continue;
                if(state.Node==goal)
                {
                    var path=new List<StreetEdge>();for(var cursor=state;cursor.Previous!=null;cursor=cursor.Previous)path.Insert(0,cursor.Edge);
                    return path;
                }
                foreach(var edge in Edges.Where(e=>e.A==state.Node||e.B==state.Node))
                {
                    var next=edge.Other(state.Node);if(next.StopId<6&&next!=goal)continue;
                    // Fuel is consumed when a turn starts; its protection lasts both edges.
                    bool protectedTurn=state.Fuel>0||state.Used>0;
                    if(canTraverse!=null&&!canTraverse(edge,protectedTurn))continue;
                    bool newTurn=state.Used==0;
                    int effectiveSpeed=newTurn&&state.Fuel<=0?1:speed;
                    int used=state.Used+1;if(used>=effectiveSpeed||next.StopId<9)used=0;
                    var candidate=new RouteState{Node=next,Used=used,Fuel=Math.Max(0,state.Fuel-(newTurn?1:0)),
                        Turns=state.Turns+(newTurn?1:0),Length=state.Length+edge.Length,Previous=state,Edge=edge};
                    RouteState old;
                    if(best.TryGetValue(candidate.Key,out old)&&(old.Turns<candidate.Turns||old.Turns==candidate.Turns&&old.Length<=candidate.Length))continue;
                    best[candidate.Key]=candidate;open.Add(candidate);
                }
            }
            return new List<StreetEdge>();
        }
        // Composite legs preserve stable customer IDs for orders/story while routing over the explicit street graph.
        public static Road[] BuildRoads()
        {
            var roads=new List<Road>();for(int a=0;a<9;a++)for(int b=a+1;b<9;b++)
            {
                var path=FindPath(a,b);if(path.Count==0)continue;
                roads.Add(new Road(a,b,path.Count*6,0,path.Any(e=>e.Surface==StreetSurface.Bumpy)?RoadKind.Gravel:RoadKind.Asphalt){StreetPath=path.ToArray()});
            }
            return roads.ToArray();
        }
        public static StreetPoint Position(Road road,int from,float progress)
        {
            var path=road.StreetPath;bool forward=from==road.A;
            float p=Math.Max(0,Math.Min(1,progress))*path.Length;int index=Math.Min(path.Length-1,(int)p);
            var node=Nodes.First(n=>n.StopId==from);
            for(int i=0;i<index;i++)node=path[forward?i:path.Length-1-i].Other(node);
            return path[forward?index:path.Length-1-index].Sample(node,p-index);
        }
    }
}
