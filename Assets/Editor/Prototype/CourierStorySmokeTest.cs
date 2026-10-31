using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierStorySmokeTest
{
    private const string Key="CourierStorySmoke.Active";
    private static int step;
    private static double next, deadline;
    static CourierStorySmokeTest(){EditorApplication.update+=Tick;}
    private static void Require(bool value,string message){if(!value)throw new Exception(message);}
    private static CourierRun Configured(){var r=new CourierRun();CourierContentOverrides.Apply(r);return r;}
    public static void Run()
    {
        var run=Configured();
        Require(run.Parcels.Count>=10,"Story parcel IDs missing");
        var data=run.Story.Data;
        Require(data.language=="en"&&data.scenes.Length==17,"English story configuration missing");
        Require(data.scenes.Select(s=>s.id).Distinct().Count()==17,"Duplicate story ID");
        Require(data.scenes.Sum(s=>s.lines.Length)==67,"Source dialogue is incomplete");
        var first=run.TalkToNpc();Require(first[0].StartsWith("Gooy:"),"Letter does not precede Drow");
        Require(!run.TalkToNpc().Any(line=>line.StartsWith("Gooy:")),"Letter repeated");
        foreach(var scene in data.scenes.Where(s=>s.trigger=="receive"))
        {
            run=Configured();run.Node=scene.node;
            var before=run.TalkToNpc();
            Require(!before.Contains(scene.lines[0].Render(false)),"Receipt played before delivery: "+scene.id);
            foreach(var id in scene.parcels)run.Parcels[id].State=ParcelState.Delivered;
            var after=run.TalkToNpc();
            Require(scene.lines.All(line=>after.Contains(line.Render(false))),"Missing receipt: "+scene.id);
            Require(!run.TalkToNpc().Contains(scene.lines[0].Render(false)),"Receipt repeated: "+scene.id);
        }
        run=Configured();run.Node=3;run.Parcels[8].State=ParcelState.Delivered;
        Require(!run.TalkToNpc().Contains(data.scenes.First(s=>s.id=="vampire_supplies").lines[0].Render(false)),"Combined package acknowledged before both items arrived");
        var epilogue=data.scenes.Single(s=>s.trigger=="ending");
        foreach(var language in new[]{"en","zh"})
        {
            data.language=language;
            for(int node=0;node<6;node++)
            {
                run=Configured();run.Node=node;run.Story=new CourierStory(data);
                Require(!run.TalkToNpc().Contains(epilogue.lines[0].Render(language=="zh")),"Epilogue played before completion");
                foreach(var parcel in run.Parcels)parcel.State=ParcelState.Delivered;
                var final=run.TalkToNpc();
                // At the starting stop, finish the intro before reading the final receipt.
                if(!final.Contains(epilogue.lines[0].Render(language=="zh")))final=run.TalkToNpc();
                Require(final.Skip(final.Length-3).SequenceEqual(epilogue.lines.Select(line=>line.Render(language=="zh"))),"Epilogue must follow receipts: "+language+" / "+node);
                Require(!run.TalkToNpc().Contains(epilogue.lines[0].Render(language=="zh")),"Epilogue repeated");
            }
        }
        data.language="en";
        var icons=Resources.Load<CourierCargoIconCatalog>("NightVoyage/CargoIcons");
        Require(icons.icons.Length>=10&&Enumerable.Range(0,10).All(id=>icons.ForParcel(id)!=null),"Cargo icons missing after ID migration");
        Debug.Log("COURIER_STORY_CONFIG PASS: 17 scenes, 67 source/English lines, bilingual epilogue at all six stops, receive gating, deduplication, 10 story cargo icons.");
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        SessionState.SetBool(Key,true);Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    private static void Drain(NightCourierPrototype owner)
    {
        int guard=0;while(owner.ConversationActive){owner.AdvanceDialogue();Require(++guard<500,"Dialogue cannot finish");}
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+90;next=EditorApplication.timeSinceStartup+3;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Story UI timeout at "+step);
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            var hud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
            Require(owner!=null&&hud!=null,"Gameplay HUD missing");
            if(step==0)
            {
                Require(owner.DialogueSpeaker=="Gooy"&&!hud.speakerPortrait.gameObject.activeSelf,"Letter uses an NPC portrait");
                Require(hud.speakerName.text=="Gooy","Letter speaker label incorrect");
                Require(!owner.DepartTo(3),"Departure discarded active dialogue");
                foreach(var scene in owner.Run.Story.Data.scenes)foreach(var line in scene.lines)
                {
                    hud.subtitleText.text=line.Render(false);Canvas.ForceUpdateCanvases();
                    Require(hud.subtitleText.preferredHeight<=hud.subtitleText.rectTransform.rect.height,"Clipped dialogue: "+scene.id+" / "+line.speaker);
                    Require(hud.subtitleText.cachedTextGenerator.vertexCount>4,"Invisible dialogue: "+scene.id);
                }
                // Complete just the letter. The next conversation is Drow's outgoing tape.
                for(int i=0;i<7;i++){hud.CompleteSubtitle();owner.AdvanceDialogue();}
            }
            else if(step==1)
            {
                Require(owner.DialogueSpeaker=="Drow"&&hud.speakerPortrait.gameObject.activeSelf,"Drow portrait did not follow letter");
                ScreenCapture.CaptureScreenshot("/tmp/courier-story-drow.png");
                Drain(owner);owner.Run.Node=3;owner.Run.Parcels[0].State=ParcelState.Delivered;owner.BeginConversation();
            }
            else if(step==2)
            {
                Require(owner.DialogueSpeaker=="Vampire"&&hud.speakerName.text=="Vampire","Delivery speaker is wrong");
                Require(owner.Subtitle.Contains("glacial meltwater"),"Tape reaction missing");
                Drain(owner);
                foreach(var parcel in owner.Run.Parcels)parcel.State=ParcelState.Delivered;
                owner.Run.TalkToNpc(); // Recompute completion as the final delivery would.
                // Recreate pending receipt for the final package in a fresh story session.
                owner.Run.Story=new CourierStory(owner.Run.Story.Data);
                Require(owner.Run.Finished,"All delivered did not end run");
            }
            else if(step==3)
            {
                Require(owner.ConversationActive&&!UIManager.Instance.IsPanelOpen("CourierResultPanel"),"Result hid final delivery conversation");
                bool sawEpilogue=false;int guard=0;
                while(owner.ConversationActive)
                {
                    if(owner.Subtitle.Contains("As for tomorrow, tomorrow is another night."))sawEpilogue=true;
                    owner.AdvanceDialogue();Require(++guard<500,"Final dialogue cannot finish");
                }
                Require(sawEpilogue,"Epilogue missing before result");
            }
            else if(step==4)
            {
                Require(UIManager.Instance.IsPanelOpen("CourierResultPanel"),"Result missing after final dialogue");
                owner.RestartShift();
            }
            else if(step==5)
            {
                Require(owner.DialogueSpeaker=="Gooy"&&owner.Run.CompletedBatches==0,"Restart did not reset story");
                SessionState.SetBool(Key,false);Debug.Log("COURIER_STORY_UI PASS: speaker portraits, 67 English lines fit, letter to Drow, delivery dialogue before result, restart.");EditorApplication.Exit(0);return;
            }
            step++;next=EditorApplication.timeSinceStartup+.6;
        }
        catch(Exception ex){SessionState.SetBool(Key,false);Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
