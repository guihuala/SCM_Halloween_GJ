using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace NightCourier
{
    // Geometry is a normal Canvas Graphic: modal prefabs render and block input above it.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CourierStageGraphic : MaskableGraphic, IPointerClickHandler
    {
        public NightCourierPrototype Owner;
        private VertexHelper mesh;
        private readonly Color sky = new Color(.035f,.055f,.095f), mint = new Color(.45f,.94f,.77f), orange = new Color(1,.66f,.34f);
        public string HoverHint
        {
            get
            {
                int target = TargetAt(Input.mousePosition);
                return target == 1 ? "Click to talk" : target == 2 ? "Click vehicle to plan a route" : "Tab: Map  /  E: Interact";
            }
        }
        private int TargetAt(Vector2 screen)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, null, out local)) return 0;
            var rect = rectTransform.rect;
            float scale = Mathf.Min(rect.width / 1440, rect.height / 440);
            float x = (local.x - rect.xMin - (rect.width - 1440 * scale) / 2) / scale;
            float y = (rect.yMax - (rect.height - 440 * scale) / 2 - local.y) / scale;
            if (Owner != null && Owner.Run.Stops[Owner.Run.Node].Vehicle >= 0 && x > 990 && x < 1200 && y > 260 && y < 380) return 3;
            if (x > 730 && x < 850 && y > 260 && y < 360) return 1;
            if (x > 285 && x < 515 && y > 255 && y < 380) return 2;
            return 0;
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || Owner == null || !Owner.Active || Owner.HasPlanner || Owner.Run.State != ShiftState.Docked) return;
            int target = TargetAt(data.position);
            if (target == 1) { if (Owner.ConversationActive) Owner.AdvanceDialogue(); else Owner.BeginConversation(); }
            else if (target == 2 || target == 3) Owner.ShowPlanner("CourierRoutePanel");
        }
        private void Update() { if (Owner != null && Owner.Active) SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); mesh = vh;
            Rect(0,0,1440,440,sky);
            float time = Owner != null ? Owner.VisualTime : 0;
            bool driving = Owner != null && (Owner.Run.State == ShiftState.Driving || Owner.Run.State == ShiftState.Encounter || Owner.Run.State == ShiftState.Pushing);
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
                Landmark(Owner != null ? Owner.Run.Node : 0);
                Ghost(790,320,.7f,orange);
                int available=Owner != null ? Owner.Run.Stops[Owner.Run.Node].Vehicle : -1;
                if(available>=0) Car(1080,348,available,.75f,new Color(.40f,.46f,.41f));
            }
            else for(int i=0;i<9;i++)
            { float x=Mathf.Repeat(i*180-motion*220,1440);Rect(x,285,7,65,new Color(.18f,.25f,.25f));Rect(x-12,285,31,4,orange); }
            Rect(0,376,1440,64,new Color(.025f,.04f,.06f));
            for(int i=0;i<14;i++) Rect(Mathf.Repeat(i*120-motion*340,1440),408,65,3,new Color(.36f,.40f,.38f));
            Triangle(480,315,750,260,750,368,new Color(1,.76f,.36f,.10f));
            if (Owner != null && driving && (Owner.Run.ActiveRoad.Kind==RoadKind.Fog || Owner.Run.ActiveRoad.Kind==RoadKind.Storm))
            {
                for(int i=0;i<5;i++)Rect(Mathf.Repeat(i*330-time*30,1440),180+i*27,400,35,new Color(.57f,.65f,.67f,.055f));
                if(Owner.Run.ActiveRoad.Kind==RoadKind.Storm)
                    for(int i=0;i<65;i++)Line(20+(i*71)%1420,Mathf.Repeat(i*37+time*220,370),30+(i*71)%1420,Mathf.Repeat(i*37+time*220,370)+24,new Color(.6f,.69f,.77f,.25f),1);
            }
            Car(395,352+(driving?Mathf.Sin(time*12)*2:0),Owner!=null?Owner.Run.VehicleIndex:0,1,mint);
            if(Owner!=null && Owner.Run.State==ShiftState.Pushing)Ghost(510,330,1,mint);
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
        private void Triangle(float ax,float ay,float bx,float by,float cx,float cy,Color c)
        {int n=mesh.currentVertCount;mesh.AddVert(Point(ax,ay),c,Vector2.zero);mesh.AddVert(Point(bx,by),c,Vector2.zero);mesh.AddVert(Point(cx,cy),c,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);}
        private void Line(float ax,float ay,float bx,float by,Color c,float width=4)
        {
            Vector2 a=new Vector2(ax,ay),b=new Vector2(bx,by),d=new Vector2(-(b-a).y,(b-a).x).normalized*width/2;int n=mesh.currentVertCount;
            mesh.AddVert(Point(a.x+d.x,a.y+d.y),c,Vector2.zero);mesh.AddVert(Point(a.x-d.x,a.y-d.y),c,Vector2.zero);
            mesh.AddVert(Point(b.x-d.x,b.y-d.y),c,Vector2.zero);mesh.AddVert(Point(b.x+d.x,b.y+d.y),c,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
        private void Landmark(int node)
        {
            Color dark=new Color(.045f,.085f,.10f),warm=new Color(.86f,.58f,.28f);
            switch(node)
            {
                case 0:
                    Rect(720,170,105,164,dark);Rect(712,161,121,11,mint);Rect(737,190,72,85,new Color(.15f,.3f,.3f));Rect(738,285,12,32,mint);break;
                case 1:
                    Rect(610,210,365,124,dark);Rect(596,202,393,9,mint);
                    for(int i=0;i<4;i++)Rect(633+i*76,234,55,58,new Color(.25f,.19f,.11f));
                    Rect(725,165,160,35,new Color(.17f,.10f,.12f));Line(742,180,866,180,orange,3);break;
                case 2:
                    for(int i=0;i<6;i++)Rect(610+i*42,300-(i%3)*24,75,34,new Color(.08f,.12f,.13f));
                    Line(945,328,945,130,mint,8);Line(945,130,770,130,mint,7);Line(795,130,795,250,mint,2);Rect(780,250,30,16,orange);break;
                case 3:
                    Rect(600,317,420,16,dark);for(int i=0;i<7;i++)Rect(610+i*62,260,5,63,mint);
                    Line(610,272,810,272,mint);Line(865,272,1010,272,mint);Rect(820,316,40,22,sky);break;
                case 4:
                    Line(785,333,845,105,mint,5);Line(905,333,845,105,mint,5);
                    for(int i=0;i<5;i++)Line(800+i*9,305-i*34,891-i*9,305-i*34,mint,2);
                    Circle(845,100,6,orange);Rect(720,286,85,48,dark);break;
                case 5:
                    Rect(780,160,95,174,dark);Rect(767,148,121,16,mint);Rect(783,113,90,35,new Color(.3f,.31f,.15f));
                    Triangle(770,113,885,113,827,85,dark);Triangle(870,125,1250,50,1250,205,new Color(1,.85f,.45f,.12f));break;
            }
        }
        private void Car(float x,float y,int type,float s,Color c)
        {
            Color body=new Color(.035f,.09f,.10f);
            if(type==0)
            {
                Circle(x-48*s,y,13*s,body);Circle(x+50*s,y,13*s,body);Line(x-48*s,y-14*s,x+48*s,y-14*s,c,5*s);
                Line(x+40*s,y-14*s,x+29*s,y-68*s,c,4*s);Line(x+13*s,y-68*s,x+43*s,y-68*s,c,4*s);
            }
            else if(type==1)
            {
                Circle(x-55*s,y,23*s,body);Circle(x+60*s,y,23*s,body);
                Circle(x-55*s,y,18*s,sky);Circle(x+60*s,y,18*s,sky);
                Line(x-55*s,y,x-10*s,y-39*s,c,3*s);Line(x-10*s,y-39*s,x+15*s,y,c,3*s);
                Line(x-55*s,y,x+15*s,y,c,3*s);Line(x+15*s,y,x+46*s,y-43*s,c,3*s);
                Line(x+46*s,y-43*s,x-10*s,y-39*s,c,3*s);Line(x+46*s,y-43*s,x+60*s,y,c,3*s);Line(x+40*s,y-55*s,x+65*s,y-55*s,c,4*s);
            }
            else
            {
                Rect(x-91*s,y-40*s,182*s,37*s,body);Rect(x-45*s,y-72*s,65*s,35*s,body);
                Rect(x-34*s,y-66*s,42*s,24*s,new Color(.12f,.22f,.23f));Line(x-91*s,y-40*s,x+91*s,y-40*s,c,3*s);
                Circle(x-53*s,y,19*s,body);Circle(x+65*s,y,19*s,body);Circle(x-53*s,y,7*s,c);Circle(x+65*s,y,7*s,c);
            }
            Rect(x+(type==2?88:47)*s,y-30*s,6*s,10*s,orange);
            if((Owner==null||Owner.InCar)&&(Owner==null||Owner.Run.State!=ShiftState.Pushing))Ghost(x-12*s,y-54*s,type==2?.3f:.55f,mint);
        }
    }
}
