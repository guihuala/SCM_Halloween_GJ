using System;
using System.IO;
using System.Linq;
using System.Text;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CourierPaletteInstaller
{
    [MenuItem("Tools/Night Courier/Apply Unified Game Palette")]
    public static void Install()
    {
        const string path="Assets/Resources/NightVoyage/Palette.asset";
        var palette=AssetDatabase.LoadAssetAtPath<CourierPalette>(path);
        if(palette==null){palette=ScriptableObject.CreateInstance<CourierPalette>();AssetDatabase.CreateAsset(palette,path);}
        CourierColors.Reload();
        CheckContrast();
        var paths=Directory.GetFiles("Assets/Resources/Panel","*.prefab",SearchOption.AllDirectories)
            .Concat(Directory.GetFiles("Assets/Prefab/UI","*.prefab",SearchOption.AllDirectories))
            .Concat(Directory.GetFiles("Assets/Resources/Feedback","*.prefab",SearchOption.AllDirectories));
        foreach(var prefab in paths)
        {
            var root=PrefabUtility.LoadPrefabContents(prefab);
            try{CourierTheme.Apply(root);CheckButtonText(root);PrefabUtility.SaveAsPrefabAsset(root,prefab);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var art=Resources.Load<CourierMapArtCatalog>("NightVoyage/MapArtwork");
        if(art!=null){art.idleRoadColor=art.adjacentRoadColor=CourierColors.Road;art.selectedRoadColor=CourierColors.Gold;EditorUtility.SetDirty(art);}
        var characters=Resources.Load<CourierCharacterCatalog>("NightVoyage/Characters");
        if(characters!=null)
        {
            var accents=new[]{CourierColors.Lavender,CourierColors.Gold,CourierColors.Rough,CourierColors.Heat,CourierColors.Mint,CourierColors.Water};
            foreach(var character in characters.characters)if(character!=null&&character.stopId>=0&&character.stopId<accents.Length)character.accent=accents[character.stopId];
            EditorUtility.SetDirty(characters);
        }
        foreach(var scenePath in Directory.GetFiles("Assets/Scenes","*.unity"))
        {
            var scene=EditorSceneManager.OpenScene(scenePath);
            foreach(var root in scene.GetRootGameObjects())
            {
                CourierTheme.Apply(root);CheckButtonText(root);
                foreach(var camera in root.GetComponentsInChildren<Camera>(true))camera.backgroundColor=CourierColors.Night;
            }
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();ExportCard();
        Debug.Log("COURIER_PALETTE PASS: shared editable palette applied to menus, panels, feedback, map and character accents; source images retained; text contrast checked.");
    }
    private static void CheckButtonText(GameObject root)
    {
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            var image=button.targetGraphic as Image;if(image==null||image.color.a<.5f)continue;
            foreach(var label in button.GetComponentsInChildren<Text>(true))
                if(label.color.a>.9f&&!string.IsNullOrWhiteSpace(label.text))Pair(root.name+"/"+button.name,label.color,image.color);
        }
    }
    private static float Luma(Color c)
    {
        Func<float,float> linear=v=>v<=.04045f?v/12.92f:Mathf.Pow((v+.055f)/1.055f,2.4f);
        return .2126f*linear(c.r)+.7152f*linear(c.g)+.0722f*linear(c.b);
    }
    private static void Pair(string name,Color text,Color background)
    {
        float a=Luma(text),b=Luma(background),ratio=(Mathf.Max(a,b)+.05f)/(Mathf.Min(a,b)+.05f);
        if(ratio<4.5f)throw new Exception("Palette contrast below 4.5:1: "+name+" "+ratio);
        Debug.Log("COURIER_PALETTE contrast "+name+" "+ratio.ToString("0.00")+":1");
    }
    private static void CheckContrast()
    {
        Pair("body / panel",CourierColors.Paper,CourierColors.Panel);
        Pair("body / raised",CourierColors.Paper,CourierColors.Raised);
        Pair("secondary / raised",CourierColors.Muted,CourierColors.Raised);
        Pair("primary button",CourierColors.Night,CourierColors.Mint);
        Pair("paper illustration labels",CourierColors.Night,CourierColors.Paper);
        Pair("warning / panel",CourierColors.Gold,CourierColors.Panel);
    }
    private static void ExportCard()
    {
        var names=new[]{"Night","Panel","Raised","Paper","Muted","Mint","Gold","Heat","Water","Rough","Lavender","Road"};
        var roles=new[]{"Main background","Dark UI dividers","Dark menu / tracks","Light dialogs / paper / text","Secondary text","Actions / hover route","Destination / notices","High heat","Water / crossing","Rough road","Character accent","Ordinary roads"};
        var colors=new[]{CourierColors.Night,CourierColors.Panel,CourierColors.Raised,CourierColors.Paper,CourierColors.Muted,CourierColors.Mint,CourierColors.Gold,CourierColors.Heat,CourierColors.Water,CourierColors.Rough,CourierColors.Lavender,CourierColors.Road};
        var s=new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' width='1200' height='690' viewBox='0 0 1200 690'>");
        s.Append("<rect width='1200' height='690' fill='#"+ColorUtility.ToHtmlStringRGB(CourierColors.Night)+"'/><g font-family='sans-serif'><text x='36' y='56' fill='#F1E7D2' font-size='28'>NIGHT COURIER / SHARED PALETTE</text><text x='36' y='88' fill='#B5C3BA' font-size='16'>Hand-drawn paper, quiet night, clear route and interaction colors</text>");
        for(int i=0;i<names.Length;i++)
        {
            int x=36+(i%4)*288,y=120+(i/4)*170;string hex=ColorUtility.ToHtmlStringRGB(colors[i]);
            s.Append($"<rect x='{x}' y='{y}' width='260' height='82' rx='10' fill='#{hex}' stroke='#7D9891'/><text x='{x}' y='{y+110}' fill='#F1E7D2' font-size='19'>{names[i]}  #{hex}</text><text x='{x}' y='{y+138}' fill='#B5C3BA' font-size='14'>{roles[i]}</text>");
        }
        s.Append("<text x='36' y='660' fill='#B5C3BA' font-size='15'>Editable source: Assets/Resources/NightVoyage/Palette.asset  /  UI text pairs verified at 4.5:1 or higher</text></g></svg>");
        Directory.CreateDirectory("ArtPreviews");File.WriteAllText("ArtPreviews/game-palette.svg",s.ToString());
    }
    public static void InstallAndPlayCheck(){Install();GameFlowSmokeTest.Run();}
}
