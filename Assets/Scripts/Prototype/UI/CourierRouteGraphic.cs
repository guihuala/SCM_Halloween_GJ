using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CourierRouteGraphic : MaskableGraphic
    {
        public NightCourierPrototype Owner;
        public Road Selected;
        private static readonly CourierRun Preview = new CourierRun();
        public static Vector2 NodePoint(Stop stop) { return new Vector2(65 + stop.X * 690, 70 + stop.Y * 470); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var run=Owner!=null?Owner.Run:Preview;
            foreach(var road in run.Roads)
            {
                var a=NodePoint(run.Stops[road.A]);var b=NodePoint(run.Stops[road.B]);
                Color c=road==Selected?new Color(1,.66f,.34f):road.Connects(run.Node)?new Color(.38f,.7f,.6f):new Color(.18f,.25f,.3f);
                if(road.Gravel) for(int i=0;i<20;i+=2) Segment(vh,Vector2.Lerp(a,b,i/20f),Vector2.Lerp(a,b,(i+1)/20f),c);
                else Segment(vh,a,b,c);
            }
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
