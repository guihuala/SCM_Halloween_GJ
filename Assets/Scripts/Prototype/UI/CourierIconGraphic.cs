using UnityEngine;
using UnityEngine.UI;
namespace NightCourier
{
    public enum CourierIconKind { Close,Arrow,Load,Store,Wrench,Box,Clock,Fragile,Heavy,Dry,Story,Lock,Pin,Dot,Ghost,Home,Restart,Warning }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CourierIconGraphic : MaskableGraphic
    {
        public CourierIconKind kind;
        private VertexHelper mesh;
        public void SetKind(CourierIconKind value){if(kind==value)return;kind=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();mesh=vh;
            switch(kind)
            {
                case CourierIconKind.Close:Line(6,6,18,18);Line(18,6,6,18);break;
                case CourierIconKind.Arrow:Line(4,12,20,12);Line(14,6,20,12);Line(20,12,14,18);break;
                case CourierIconKind.Load:Box(4,11,20,22);Line(12,2,12,15);Line(8,11,12,15);Line(12,15,16,11);break;
                case CourierIconKind.Store:Box(4,11,20,22);Line(12,16,12,2);Line(8,6,12,2);Line(12,2,16,6);break;
                case CourierIconKind.Wrench:Arc(16,7,5,40,300);Line(12,11,4,20,3);Line(18,3,16,7);Line(16,7,20,8);break;
                case CourierIconKind.Box:Box(4,5,20,21);Line(4,10,20,10);Line(12,5,12,10);break;
                case CourierIconKind.Clock:Arc(12,12,9,0,360);Line(12,6,12,12);Line(12,12,16,14);break;
                case CourierIconKind.Fragile:Line(7,3,17,3);Line(7,3,8,10);Line(8,10,12,13);Line(12,13,16,10);Line(16,10,17,3);Line(12,13,12,21);Line(8,21,16,21);Line(12,4,10,7);Line(10,7,13,9);break;
                case CourierIconKind.Heavy:Arc(12,5,3,180,360);Line(6,8,18,8);Line(6,8,3,21);Line(3,21,21,21);Line(21,21,18,8);break;
                case CourierIconKind.Dry:Arc(12,12,9,180,360);Line(3,12,21,12);Line(12,3,12,18);Arc(9,18,3,0,180);break;
                case CourierIconKind.Story:Line(12,2,15,9);Line(15,9,22,12);Line(22,12,15,15);Line(15,15,12,22);Line(12,22,9,15);Line(9,15,2,12);Line(2,12,9,9);Line(9,9,12,2);break;
                case CourierIconKind.Lock:Box(5,10,19,21);Arc(12,9,5,180,360);Line(7,9,7,10);Line(17,9,17,10);Line(12,14,12,17);break;
                case CourierIconKind.Pin:Arc(12,9,6,150,390);Line(7,12,12,22);Line(12,22,17,12);Arc(12,9,2,0,360);break;
                case CourierIconKind.Dot:Arc(12,12,4,0,360);break;
                case CourierIconKind.Ghost:Arc(12,9,7,180,360);Line(5,9,5,21);Line(5,21,9,18);Line(9,18,12,21);Line(12,21,15,18);Line(15,18,19,21);Line(19,21,19,9);Line(9,10,9,12);Line(15,10,15,12);break;
                case CourierIconKind.Home:Line(2,11,12,3);Line(12,3,22,11);Line(5,9,5,21);Line(5,21,19,21);Line(19,21,19,9);Line(10,21,10,15);Line(10,15,14,15);Line(14,15,14,21);break;
                case CourierIconKind.Restart:Arc(12,12,8,-70,235);Line(3,5,3,11);Line(3,11,9,11);break;
                case CourierIconKind.Warning:Line(12,2,23,21);Line(23,21,1,21);Line(1,21,12,2);Line(12,8,12,14);Line(12,17,12,18);break;
            }
        }
        private Vector2 Point(float x,float y){var r=rectTransform.rect;float s=Mathf.Min(r.width,r.height)/24;return new Vector2(r.center.x+(x-12)*s,r.center.y+(12-y)*s);}
        private void Line(float ax,float ay,float bx,float by,float width=2.1f)
        {
            var a=new Vector2(ax,ay);var b=new Vector2(bx,by);var d=new Vector2(-(b-a).y,(b-a).x).normalized*width/2;int n=mesh.currentVertCount;
            mesh.AddVert(Point(a.x+d.x,a.y+d.y),color,Vector2.zero);mesh.AddVert(Point(a.x-d.x,a.y-d.y),color,Vector2.zero);
            mesh.AddVert(Point(b.x-d.x,b.y-d.y),color,Vector2.zero);mesh.AddVert(Point(b.x+d.x,b.y+d.y),color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
        private void Box(float left,float top,float right,float bottom){Line(left,top,right,top);Line(right,top,right,bottom);Line(right,bottom,left,bottom);Line(left,bottom,left,top);}
        private void Arc(float x,float y,float radius,float from,float to)
        {int count=Mathf.Max(3,Mathf.CeilToInt((to-from)/15));for(int i=0;i<count;i++){float a=Mathf.Lerp(from,to,i/(float)count)*Mathf.Deg2Rad,b=Mathf.Lerp(from,to,(i+1)/(float)count)*Mathf.Deg2Rad;Line(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius);}}
    }
}
