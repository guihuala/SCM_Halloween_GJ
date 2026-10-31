using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CourierRouteGraphic : MaskableGraphic
    {
        public NightCourierPrototype Owner;
        public System.Collections.Generic.List<Road> SelectedRoute=new System.Collections.Generic.List<Road>();
        private static readonly CourierRun Preview = new CourierRun();
        public static Vector2 NodePoint(Stop stop) { return new Vector2(50 + stop.X * 550, 35 + stop.Y * 340); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var run=Owner!=null?Owner.Run:Preview;
            foreach(var road in run.Roads)
            {
                var a=NodePoint(run.Stops[road.A]);var b=NodePoint(run.Stops[road.B]);
                var direction=(b-a).normalized;a+=direction*23;b-=direction*23;
                Color c=road==run.ActiveRoad&&run.State!=ShiftState.Docked||SelectedRoute.Contains(road)?new Color(1,.66f,.34f):road.Connects(run.Node)?new Color(.38f,.7f,.6f):new Color(.18f,.25f,.3f);
                if(road.Gravel) for(int i=0;i<20;i+=2) Segment(vh,Vector2.Lerp(a,b,i/20f),Vector2.Lerp(a,b,(i+1)/20f),c);
                else Segment(vh,a,b,c);
            }
            if(Owner!=null&&run.ActiveRoad!=null&&(run.State==ShiftState.Driving||run.State==ShiftState.Pushing||run.State==ShiftState.Encounter))
            {
                var a=NodePoint(run.Stops[run.Node]);var b=NodePoint(run.Stops[run.ActiveRoad.Other(run.Node)]);var position=Vector2.Lerp(a,b,run.Progress);
                Segment(vh,a,position,new Color(.45f,.94f,.77f));
                Circle(vh,position,13+Mathf.Sin(Owner.VisualTime*3)*1.5f,new Color(.45f,.94f,.77f,.18f));
                Circle(vh,position,7,new Color(.45f,.94f,.77f));
            }
        }
        private void Circle(VertexHelper vh,Vector2 center,float radius,Color color)
        {
            int start=vh.currentVertCount;vh.AddVert(Point(center),color,Vector2.zero);
            for(int i=0;i<=24;i++){float angle=i*Mathf.PI*2/24;vh.AddVert(Point(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius),color,Vector2.zero);}
            for(int i=0;i<24;i++)vh.AddTriangle(start,start+i+1,start+i+2);
        }
        private void Segment(VertexHelper vh,Vector2 a,Vector2 b,Color c)
        {
            Vector2 offset=new Vector2(-(b-a).y,(b-a).x).normalized*2;
            int n=vh.currentVertCount;
            vh.AddVert(Point(a+offset),c,Vector2.zero);vh.AddVert(Point(a-offset),c,Vector2.zero);
            vh.AddVert(Point(b-offset),c,Vector2.zero);vh.AddVert(Point(b+offset),c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        private Vector2 Point(Vector2 p) { return new Vector2(rectTransform.rect.xMin+p.x,rectTransform.rect.yMax-p.y); }
    }
}
