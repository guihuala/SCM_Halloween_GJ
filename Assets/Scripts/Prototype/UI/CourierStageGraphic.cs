using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // Geometry is a normal Canvas Graphic: modal prefabs render and block input above it.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CourierStageGraphic : MaskableGraphic
    {
        public NightCourierPrototype Owner;
        private VertexHelper mesh;
        private readonly Color sky = new Color(.035f,.055f,.095f), mint = new Color(.45f,.94f,.77f), orange = new Color(1,.66f,.34f);
        private void Update() { if (Owner != null && Owner.Active) SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); mesh = vh;
            Rect(0,0,1440,440,sky);
            float time = Owner != null ? Owner.VisualTime : 0;
            bool driving = Owner != null && (Owner.Run.State == ShiftState.Driving || Owner.Run.State == ShiftState.Encounter);
            float motion = driving ? time : 0;
            Circle(1210,83,58,new Color(.86f,.84f,.66f)); Circle(1186,65,55,sky);
            for (int i=0;i<50;i++) Rect(20+(i*173)%1380,18+(i*47)%160,2,2,new Color(.37f,.46f,.54f));
            for (int i=0;i<20;i++)
            {
                float x=Mathf.Repeat(i*89-motion*13,1440), h=45+(i*37)%95;
                Rect(x,294-h,65,h,new Color(.07f,.13f,.18f));
                for (int j=0;j<3;j++) Rect(x+10+j*16,310-h,5,11,new Color(.24f,.32f,.30f));
            }
            Rect(0,294,1440,82,new Color(.065f,.13f,.15f));
            if (!driving)
            {
                Rect(610,184,350,150,new Color(.09f,.19f,.20f));Rect(597,178,376,8,mint);
                Rect(637,218,117,77,new Color(.17f,.35f,.33f));Rect(820,232,90,102,new Color(.03f,.065f,.085f));
                Ghost(790,320,.7f,orange);
                int available=Owner != null ? Owner.Run.Stops[Owner.Run.Node].Vehicle : -1;
                if(available>=0) Car(1080,348,available,.75f,new Color(.40f,.46f,.41f));
            }
            else for(int i=0;i<9;i++)
            { float x=Mathf.Repeat(i*180-motion*220,1440);Rect(x,285,7,65,new Color(.18f,.25f,.25f));Rect(x-12,285,31,4,orange); }
            Rect(0,376,1440,64,new Color(.025f,.04f,.06f));
            for(int i=0;i<14;i++) Rect(Mathf.Repeat(i*120-motion*340,1440),408,65,3,new Color(.36f,.40f,.38f));
            Car(395,352+(driving?Mathf.Sin(time*12)*2:0),Owner!=null?Owner.Run.VehicleIndex:0,1,mint);
            if(Owner!=null && !Owner.InCar && !driving) Ghost(Owner.GhostPosition.x,Owner.GhostPosition.y+Mathf.Sin(time*3)*5,1,mint);
        }
        private Vector2 Point(float x,float y)
        {
            var rect=rectTransform.rect;
            float scale=Mathf.Min(rect.width/1440,rect.height/440);
            return new Vector2(rect.xMin+(rect.width-1440*scale)/2+x*scale,rect.yMax-(rect.height-440*scale)/2-y*scale);
        }
        private void Rect(float x,float y,float w,float h,Color c)
        {
            int n=mesh.currentVertCount;
            mesh.AddVert(Point(x,y),c,Vector2.zero);mesh.AddVert(Point(x+w,y),c,Vector2.zero);
            mesh.AddVert(Point(x+w,y+h),c,Vector2.zero);mesh.AddVert(Point(x,y+h),c,Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
        private void Circle(float x,float y,float r,Color c)
        {
            int n=mesh.currentVertCount;mesh.AddVert(Point(x,y),c,Vector2.zero);
            const int segments=24;
            for(int i=0;i<=segments;i++) {float a=i*Mathf.PI*2/segments;mesh.AddVert(Point(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r),c,Vector2.zero);}
            for(int i=0;i<segments;i++) mesh.AddTriangle(n,n+i+1,n+i+2);
        }
        private void Ghost(float x,float y,float scale,Color c)
        {
            Circle(x,y-14*scale,25*scale,c);Rect(x-25*scale,y-14*scale,50*scale,36*scale,c);
            for(int i=0;i<3;i++) Circle(x-17*scale+i*17*scale,y+22*scale,8*scale,c);
            Circle(x-9*scale,y-14*scale,3*scale,sky);Circle(x+9*scale,y-14*scale,3*scale,sky);
        }
        private void Car(float x,float y,int type,float s,Color c)
        {
            Rect(x-91*s,y-40*s,(type==2?215:182)*s,37*s,c);Rect(x-45*s,y-72*s,(type==0?95:65)*s,35*s,c);
            Rect(x-34*s,y-66*s,42*s,24*s,sky);if(type==2) Rect(x+25*s,y-85*s,99*s,43*s,c);
            Circle(x-53*s,y,19*s,sky);Circle(x+65*s,y,19*s,sky);
            Circle(x-53*s,y,9*s,new Color(.5f,.6f,.6f));Circle(x+65*s,y,9*s,new Color(.5f,.6f,.6f));
            Rect(x+88*s,y-30*s,6*s,12*s,orange);
            if(Owner==null || Owner.InCar) Ghost(x-12*s,y-53*s,.28f,Color.white);
        }
    }
}
