using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CourierRouteGraphic : MaskableGraphic
    {
        public NightCourierPrototype Owner;
        public bool drawRoads=true,drawTravel=true;
        public bool drawRegions=true;
        public bool drawBaseRoads=true,drawBaseNodes=true;
        public bool drawTravelDot=true;
        [Min(1)] public float travelHaloRadius=13;
        public Color idleRoadColor=new Color(.18f,.25f,.3f);
        public Color adjacentRoadColor=new Color(.38f,.7f,.6f);
        public Color selectedRoadColor=new Color(1,.66f,.34f);
        public System.Collections.Generic.List<Road> SelectedRoute=new System.Collections.Generic.List<Road>();
        public Color previewRoadColor=new Color(.45f,.94f,.77f);
        [Range(0,1)] public float highlightVisibility=1;
        [Range(0,1)] public float highlightProgress=1;
        public System.Collections.Generic.List<Road> PreviewRoute=new System.Collections.Generic.List<Road>();
        private static readonly CourierRun Preview = new CourierRun();
        public static readonly Vector2 MapCanvasSize=new Vector2(1200,820);
        public static Rect MapBounds(Vector2 size)
        {
            float scale=Mathf.Min(size.x/MapCanvasSize.x,size.y/MapCanvasSize.y);
            var fitted=MapCanvasSize*scale;
            return new Rect((size-fitted)*.5f,fitted);
        }
        public static Vector2 MapPoint(StreetPoint point,Vector2 size)
        {
            var bounds=MapBounds(size);
            return bounds.position+new Vector2(point.X,point.Y)*(bounds.width/MapCanvasSize.x);
        }
        public static Vector2 NodePoint(Stop stop) { return NodePoint(stop,new Vector2(690,400)); }
        public static Vector2 NodePoint(Stop stop,Vector2 size) { return MapPoint(new StreetPoint(stop.X*1200,stop.Y*820),size); }
        public static Vector2 TravelPoint(CourierRun run,Vector2 size)
        {
            if(run.ActiveRoad==null||run.State==ShiftState.Docked||run.Finished)return NodePoint(run.Stops[run.Node],size);
            if(run.ActiveRoad.StreetPath!=null)return MapPoint(CourierStreetMap.Position(run.ActiveRoad,run.Node,run.DisplayProgress),size);
            return Vector2.Lerp(NodePoint(run.Stops[run.Node],size),NodePoint(run.Stops[run.ActiveRoad.Other(run.Node)],size),run.DisplayProgress);
        }
        private bool Includes(System.Collections.Generic.List<Road> roads,StreetEdge edge)
        {return roads.Exists(r=>r.StreetPath!=null&&System.Array.IndexOf(r.StreetPath,edge)>=0);}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var run=Owner!=null?Owner.Run:Preview;
            if(drawRoads)
            {
                if(drawRegions)
                {
                    Region(vh,CourierColors.Alpha(Color.Lerp(CourierColors.Night,CourierColors.Heat,.30f),.85f),new[]{new Vector2(300,170),new Vector2(510,100),new Vector2(900,200),new Vector2(750,425),new Vector2(430,410)});
                    Region(vh,CourierColors.Alpha(Color.Lerp(CourierColors.Night,CourierColors.Water,.30f),.85f),new[]{new Vector2(820,245),new Vector2(1010,350),new Vector2(1010,555),new Vector2(940,685),new Vector2(650,625)});
                    Region(vh,CourierColors.Alpha(Color.Lerp(CourierColors.Night,CourierColors.Rough,.30f),.85f),new[]{new Vector2(270,450),new Vector2(460,450),new Vector2(710,600),new Vector2(630,795),new Vector2(260,720)});
                }
                foreach(var edge in CourierStreetMap.Edges)
                {
                    bool active=run.ActiveRoad!=null&&run.State!=ShiftState.Docked&&!run.Finished&&run.ActiveRoad.StreetPath!=null&&System.Array.IndexOf(run.ActiveRoad.StreetPath,edge)>=0;
                    if(!drawBaseRoads&&!active)continue;
                    Color surface=edge.Surface==StreetSurface.Heat?CourierColors.Heat:edge.Surface==StreetSurface.Water?CourierColors.Water:edge.Surface==StreetSurface.Bumpy?CourierColors.Rough:idleRoadColor;
                    Color c=active?selectedRoadColor:surface;
                    for(int i=1;i<edge.Points.Length;i++)
                    {
                        var a=MapPoint(edge.Points[i-1],rectTransform.rect.size);var b=MapPoint(edge.Points[i],rectTransform.rect.size);
                        Segment(vh,a,b,CourierColors.Night,4.5f);
                        if(edge.Surface!=StreetSurface.Bumpy||i%2==1)Segment(vh,a,b,c,2.2f);
                    }
                }
                if(run.State==ShiftState.Docked)
                    DrawRoute(vh,run,PreviewRoute.Count>0?PreviewRoute:SelectedRoute,PreviewRoute.Count>0?previewRoadColor:selectedRoadColor);
                foreach(var node in CourierStreetMap.Nodes)
                {
                    var point=MapPoint(node.Point,rectTransform.rect.size);
                    if(System.Array.IndexOf(run.DelayEventNodes,node.Id)>=0||System.Array.IndexOf(run.RetreatEventNodes,node.Id)>=0)
                    {
                        var color=System.Array.IndexOf(run.DelayEventNodes,node.Id)>=0?CourierColors.Lavender:CourierColors.Water;
                        for(int i=0;i<12;i++)
                        {
                            float a=i*Mathf.PI/6,b=a+.15f;
                            Segment(vh,point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*14,point+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*14,color,1.5f);
                        }
                    }
                    if(drawBaseNodes)
                    {
                        Circle(vh,point,node.StopId<9?9:6,CourierColors.Night);
                        Circle(vh,point,node.StopId<9?6:3.5f,CourierColors.Road);
                    }
                }
            }
            if(drawTravel&&Owner!=null&&run.ActiveRoad!=null&&(run.State==ShiftState.Driving||run.State==ShiftState.Pushing||run.State==ShiftState.Encounter))
            {
                var position=TravelPoint(run,rectTransform.rect.size);
                Circle(vh,position,travelHaloRadius+Mathf.Sin(Owner.VisualTime*3)*1.5f,CourierColors.Alpha(CourierColors.Mint,.18f));
                if(drawTravelDot)Circle(vh,position,7,CourierColors.Mint);
            }
        }
        private void DrawRoute(VertexHelper mesh,CourierRun run,System.Collections.Generic.List<Road> roads,Color tint)
        {
            if(roads.Count==0||highlightProgress<=0)return;
            float length=0;foreach(var road in roads)if(road.StreetPath!=null)foreach(var edge in road.StreetPath)length+=edge.Length;
            float budget=length*highlightProgress;
            var node=System.Array.Find(CourierStreetMap.Nodes,n=>n.StopId==run.Node);
            if(node==null)return;
            tint.a*=highlightVisibility;
            foreach(var road in roads)
            {
                var path=road.StreetPath;if(path==null)continue;
                bool forward=node.StopId==road.A;
                for(int e=0;e<path.Length;e++)
                {
                    var edge=path[forward?e:path.Length-1-e];bool fromA=edge.A==node;
                    for(int i=1;i<edge.Points.Length;i++)
                    {
                        var start=edge.Points[fromA?i-1:edge.Points.Length-i];var end=edge.Points[fromA?i:edge.Points.Length-1-i];
                        float distance=StreetPoint.Distance(start,end);if(distance<=.0001f)continue;
                        float fraction=Mathf.Clamp01(budget/distance);
                        var a=MapPoint(start,rectTransform.rect.size);var b=MapPoint(StreetPoint.Lerp(start,end,fraction),rectTransform.rect.size);
                        Segment(mesh,a,b,CourierColors.Alpha(tint,tint.a*.16f),5);
                        Segment(mesh,a,b,tint,2.3f);
                        budget-=distance;
                        if(budget<=0){if(highlightProgress<1)Circle(mesh,b,3.2f,CourierColors.Paper);return;}
                    }
                    node=edge.Other(node);
                }
            }
        }
        private void Region(VertexHelper vh,Color color,Vector2[] points)
        {
            int start=vh.currentVertCount;
            foreach(var p in points)vh.AddVert(Point(MapPoint(new StreetPoint(p.x,p.y),rectTransform.rect.size)),color,Vector2.zero);
            for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);
        }
        private void Circle(VertexHelper vh,Vector2 center,float radius,Color color)
        {
            int start=vh.currentVertCount;vh.AddVert(Point(center),color,Vector2.zero);
            for(int i=0;i<=24;i++){float angle=i*Mathf.PI*2/24;vh.AddVert(Point(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius),color,Vector2.zero);}
            for(int i=0;i<24;i++)vh.AddTriangle(start,start+i+1,start+i+2);
        }
        private void Segment(VertexHelper vh,Vector2 a,Vector2 b,Color c,float width=2)
        {
            Vector2 offset=new Vector2(-(b-a).y,(b-a).x).normalized*width;
            int n=vh.currentVertCount;
            vh.AddVert(Point(a+offset),c,Vector2.zero);vh.AddVert(Point(a-offset),c,Vector2.zero);
            vh.AddVert(Point(b-offset),c,Vector2.zero);vh.AddVert(Point(b+offset),c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        private Vector2 Point(Vector2 p) { return new Vector2(rectTransform.rect.xMin+p.x,rectTransform.rect.yMax-p.y); }
    }
}
