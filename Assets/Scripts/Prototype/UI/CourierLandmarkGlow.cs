using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // A separate alpha-sampled halo preserves the illustration and its input geometry.
    public sealed class CourierLandmarkGlow : MaskableGraphic
    {
        public Image source;
        [Min(1)] public float radius=9;
        private Material instance;
        private Sprite lastSprite;
        private Vector4 lastUV;
        private Vector2 lastSize;
        private bool selected,reduced;
        private float visibility,arrival=1;
        private static readonly int BoundsId=Shader.PropertyToID("_SpriteUVRect");
        private static readonly int RadiusId=Shader.PropertyToID("_GlowRadius");
        private Sprite Artwork => source!=null?(source.overrideSprite!=null?source.overrideSprite:source.sprite):null;
        public override Texture mainTexture => Artwork!=null?Artwork.texture:base.mainTexture;

        public static CourierLandmarkGlow Create(Image target)
        {
            var go=new GameObject("SelectedLandmarkGlow",typeof(RectTransform),typeof(CanvasRenderer),typeof(CourierLandmarkGlow));
            go.layer=target.gameObject.layer;go.transform.SetParent(target.transform.parent,false);
            go.transform.SetSiblingIndex(target.transform.GetSiblingIndex());
            var glow=go.GetComponent<CourierLandmarkGlow>();glow.source=target;glow.raycastTarget=false;
            glow.color=CourierColors.Alpha(CourierColors.Gold,0);
            var asset=Resources.Load<Material>("NightVoyage/LandmarkGlow");
            if(asset!=null){glow.instance=new Material(asset){name="Landmark Glow (Instance)",hideFlags=HideFlags.DontSave};glow.material=glow.instance;}
            else glow.enabled=false;
            return glow;
        }
        public void SetSelected(bool value,bool reducedMotion)
        {if(value&&!selected)arrival=0;selected=value;reduced=reducedMotion;}
        private Rect DrawingRect()
        {
            Rect rect=rectTransform.rect;var sprite=Artwork;
            if(source!=null&&source.preserveAspect&&sprite!=null)
            {
                float ratio=sprite.rect.width/sprite.rect.height;
                if(rect.width/Mathf.Max(1,rect.height)>ratio){float width=rect.height*ratio;rect.x+=(rect.width-width)*rectTransform.pivot.x;rect.width=width;}
                else{float height=rect.width/ratio;rect.y+=(rect.height-height)*rectTransform.pivot.y;rect.height=height;}
            }
            return rect;
        }
        private void LateUpdate()
        {
            if(source==null||!source.isActiveAndEnabled||Artwork==null){ResetGlow();return;}
            var target=source.rectTransform;var rect=rectTransform;
            rect.anchorMin=target.anchorMin;rect.anchorMax=target.anchorMax;rect.pivot=target.pivot;
            rect.sizeDelta=target.sizeDelta;rect.anchoredPosition=target.anchoredPosition;
            rect.localRotation=target.localRotation;rect.localScale=target.localScale;
            // Stay immediately behind the source, even if artwork layers are reordered.
            if(transform.GetSiblingIndex()!=target.GetSiblingIndex()-1)transform.SetSiblingIndex(target.GetSiblingIndex());
            visibility=reduced?(selected?1:0):Mathf.MoveTowards(visibility,selected?1:0,Time.unscaledDeltaTime/.18f);
            arrival=Mathf.Min(1,arrival+Time.unscaledDeltaTime/.32f);
            float entrance=reduced?0:Mathf.Sin(arrival*Mathf.PI)*(1-arrival)*.18f;
            color=CourierColors.Alpha(CourierColors.Gold,visibility*.85f*(1+entrance));
            var sprite=Artwork;var uv=UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);var bounds=DrawingRect();
            if(sprite!=lastSprite||uv!=lastUV||bounds.size!=lastSize)
            {lastSprite=sprite;lastUV=uv;lastSize=bounds.size;SetAllDirty();}
            if(instance!=null)
            {
                var step=new Vector4((uv.z-uv.x)*radius/Mathf.Max(1,bounds.width),(uv.w-uv.y)*radius/Mathf.Max(1,bounds.height),0,0);
                instance.SetVector(BoundsId,uv);instance.SetVector(RadiusId,step);
                var rendered=materialForRendering;
                if(rendered!=null&&rendered!=instance){rendered.SetVector(BoundsId,uv);rendered.SetVector(RadiusId,step);}
            }
            canvasRenderer.cull=visibility<=0;
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var sprite=Artwork;if(sprite==null||instance==null)return;
            Rect rect=DrawingRect();var uv=UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            float u=radius*(uv.z-uv.x)/Mathf.Max(1,rect.width),v=radius*(uv.w-uv.y)/Mathf.Max(1,rect.height);
            mesh.AddVert(new Vector3(rect.xMin-radius,rect.yMin-radius),color,new Vector2(uv.x-u,uv.y-v));
            mesh.AddVert(new Vector3(rect.xMin-radius,rect.yMax+radius),color,new Vector2(uv.x-u,uv.w+v));
            mesh.AddVert(new Vector3(rect.xMax+radius,rect.yMax+radius),color,new Vector2(uv.z+u,uv.w+v));
            mesh.AddVert(new Vector3(rect.xMax+radius,rect.yMin-radius),color,new Vector2(uv.z+u,uv.y-v));
            mesh.AddTriangle(0,1,2);mesh.AddTriangle(2,3,0);
        }
        public void ResetGlow(){selected=false;visibility=0;arrival=1;color=CourierColors.Alpha(CourierColors.Gold,0);canvasRenderer.cull=true;}
        protected override void OnDisable(){ResetGlow();base.OnDisable();}
        protected override void OnDestroy(){if(instance!=null){if(Application.isPlaying)Destroy(instance);else DestroyImmediate(instance);}base.OnDestroy();}
    }
}
