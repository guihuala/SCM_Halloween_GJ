using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier
{
    // Reversible component tints: the hand-drawn source pixels are never recolored.
    public static class CourierTheme
    {
        public static void Apply(GameObject root)
        {
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(image.color.a<=.001f)continue; // Hit regions and masks keep their authored transparency.
                float alpha=image.color.a;
                string sprite=image.sprite!=null?image.sprite.name:"";
                string name=image.name;
                Color color;
                if(IsIllustration(image)){image.color=Color.white;continue;}
                bool paper=IsPaper(image);
                var button=image.GetComponent<Button>();
                if(paper)color=CourierColors.Paper;
                else if(button!=null&&(button.name=="back"||button.name=="BackButton"))color=CourierColors.Paper;
                else if(button!=null)color=IsPrimary(button.name)?CourierColors.Mint:CourierColors.Raised;
                else if(name=="HandDrawnCloseIcon"||name=="Handle"&&image.GetComponentInParent<Slider>()!=null)color=CourierColors.Night;
                else if(sprite=="ProgressFill"||name=="Content"&&root.name.StartsWith("LoadingScreen",StringComparison.Ordinal))color=CourierColors.Mint;
                else if(sprite=="ProgressTrack"||name=="Scrollbar"||name=="Progress")color=CourierColors.Raised;
                else if(sprite=="Card")color=CourierColors.Raised;
                else if(sprite=="Dialogue")color=CourierColors.Night;
                else if(name=="Window"&&root.name.StartsWith("CourierRoutePanel",StringComparison.Ordinal))color=CourierColors.Night;
                else if(sprite=="Panel"||name=="ContentPanel"||name=="Dialog"||name=="Window")color=CourierColors.Panel;
                else if(name=="Bg"||name=="Loading"||image.transform==root.transform)color=CourierColors.Night;
                else if(name=="SpeakerAccent")continue;
                else color=Tone(image.color);
                image.color=CourierColors.Alpha(color,alpha);
            }
            foreach(var icon in root.GetComponentsInChildren<CourierIconGraphic>(true))
                icon.color=CourierColors.Alpha(Tone(icon.color),icon.color.a);
            foreach(var label in root.GetComponentsInChildren<Text>(true))
            {
                var button=label.GetComponentInParent<Button>();
                bool ink=HasPaperAncestor(label.transform)||(button!=null&&(IsPrimary(button.name)||button.name=="back"||button.name=="BackButton"));
                Color text=button!=null?(ink?CourierColors.Night:CourierColors.Paper):(ink?CourierColors.Night:Tone(label.color));
                label.color=CourierColors.Alpha(text,label.color.a);
            }
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            {
                // Invisible node hit surfaces remain invisible in every interaction state.
                var image=button.targetGraphic as Image;
                if(image!=null&&image.color.a<=.001f)continue;
                var colors=button.colors;
                colors.normalColor=Color.white;colors.highlightedColor=Color.Lerp(Color.white,CourierColors.Mint,.18f);
                colors.selectedColor=colors.highlightedColor;colors.pressedColor=CourierColors.Alpha(Color.white*.8f,1);
                colors.disabledColor=new Color(.55f,.55f,.55f,.65f);button.colors=colors;
            }
            foreach(var animation in root.GetComponentsInChildren<ButtonHoverAnimation>(true))
            {
                var image=animation.GetComponent<Image>();if(image==null)continue;
                animation.hoverColor=Color.Lerp(image.color,CourierColors.Paper,.15f);
                animation.clickColor=Color.Lerp(image.color,CourierColors.Night,.16f);
            }
            foreach(var outline in root.GetComponentsInChildren<Outline>(true))
                outline.effectColor=CourierColors.Alpha(outline.GetComponent<Text>()!=null?CourierColors.Night:CourierColors.Gold,outline.effectColor.a);
            foreach(var map in root.GetComponentsInChildren<CourierRouteGraphic>(true))
            {map.idleRoadColor=map.adjacentRoadColor=CourierColors.Road;map.selectedRoadColor=CourierColors.Gold;map.previewRoadColor=CourierColors.Mint;map.SetVerticesDirty();}
            foreach(var effect in root.GetComponentsInChildren<CourierInteractionEffect>(true))
            {effect.focusColor=CourierColors.Mint;effect.blockedColor=CourierColors.Gold;}
            foreach(var hud in root.GetComponentsInChildren<CourierHudPanel>(true))hud.ApplyReadingColors();
            foreach(var guide in root.GetComponentsInChildren<CourierTutorialView>(true))guide.ApplyReadingColors();
        }
        private static bool IsPrimary(string name)=>name=="ResumeButton"||name=="ConfirmButton"||name=="DepartButton"||name=="LoadButton"||name=="SafeButton"||name=="NewGame"||name=="AdvanceButton";
        private static bool IsPaper(Image image)
        {
            string sprite=image.sprite!=null?image.sprite.name:"";
            return sprite.StartsWith("UI_atlas_1_",StringComparison.Ordinal)||sprite=="Credits_panel"||sprite.StartsWith("mainmenu_",StringComparison.Ordinal);
        }
        private static bool IsIllustration(Image image)
        {
            string name=image.name;
            return name.Contains("Portrait")||name.Contains("VehicleArtwork")||name=="TravelerIcon"||name.Contains("CargoArtwork")||name.StartsWith("Landmark_",StringComparison.Ordinal)||name=="TerrainArtwork";
        }
        private static bool HasPaperAncestor(Transform target)
        {
            for(var parent=target.parent;parent!=null;parent=parent.parent)
            {var image=parent.GetComponent<Image>();if(image!=null&&IsPaper(image))return true;}
            return false;
        }
        private static Color Tone(Color old)
        {
            if(Mathf.Max(old.r,old.g,old.b)<.23f)return CourierColors.Night;
            if(old.r>old.g*1.18f&&old.g>old.b*1.12f)return CourierColors.Gold;
            if(old.g>old.r*1.16f&&old.g>old.b*1.05f)return CourierColors.Mint;
            return Mathf.Max(old.r,old.g,old.b)<.85f?CourierColors.Muted:CourierColors.Paper;
        }
    }
}
