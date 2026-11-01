using System;
using System.IO;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierFeedbackRevisionSmokeTest
{
    const string Flag="/tmp/courier-feedback-revision-run",Result="/tmp/courier-feedback-revision-result.txt",Key="CourierFeedbackRevision";
    static int phase;static double next;static string previousScene;
    static CourierFeedbackRevisionSmokeTest(){EditorApplication.update+=Poll;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static CourierRun Configured(){var r=new CourierRun(31);CourierContentOverrides.Apply(r);return r;}
    static void Drain(NightCourierPrototype owner)
    {int guard=0;while(owner.ConversationActive){owner.AdvanceDialogue();Check(++guard<500,"Dialogue stuck");}}
    [MenuItem("Tools/Night Courier/Checks/Feedback Revision")]
    public static void Run()
    {
        try
        {
            File.WriteAllText(Result,"Running\n");
            Check(!EditorApplication.isPlaying,"Exit Play mode before running revision checks");
            Debug.Log(CourierChecks.RunAll());
            CheckTaskTable();
            var r=Configured();r.EventProbability=0;r.Node=CourierStreetMap.Node("S").StopId;
            Check(!r.Story.HasPending(r),"Intro started at depot");r.Node=0;
            Check(r.Story.Read(r)[0].Contains("Ashpine Vale"),"Source letter missing");
            Check(!r.Story.HasPending(r)&&r.Story.Read(r).Length==0,"Uncollected outgoing dialogue played");
            r.Node=1;Check(!r.Story.HasPending(r),"Arrival played send dialogue");
            Check(r.LoadParcel(r.Parcels[1]),"Rosin pickup failed");var rosin=r.Story.ReadPickup(r,r.Parcels[1]);
            Check(rosin.Length==3&&rosin[0].Contains("Right on time"),"Wrong pickup scene");
            Check(!r.Story.HasPending(r),"Another item auto-started");
            Check(r.LoadParcel(r.Parcels[2]),"Second pickup rejected");
            Check(r.Parcels[2].State==ParcelState.Loaded&&r.Parcels[3].State==ParcelState.Loaded,"Woods pickup must include both items");
            var woodsDialogue=r.Story.ReadPickup(r,r.Parcels[2]);
            Check(woodsDialogue.Length==3&&woodsDialogue[0].Contains("Normally this time of year")&&woodsDialogue[0].Contains("jam"),"Combined source dialogue missing");
            Check(!r.LoadParcel(r.Parcels[3]),"Jam can be collected separately after combined pickup");
            Check(r.Story.ReadPickup(r,r.Parcels[3]).Length==0,"Combined dialogue played twice");
            Check(r.ActiveOrders.Count()==3,"Multiple orders missing");
            Check(r.Story.ReadPickup(r,r.Parcels[2]).Length==0,"Send scene repeated");
            r=Configured();r.EventProbability=0;r.Node=5;r.LoadParcel(r.Parcels[7]);
            CourierChecks.Go(r,3);CourierChecks.Go(r,5);int old=r.TurnsFor(r.Parcels[7]);
            Check(old>0&&r.LoadParcel(r.Parcels[8]),"Later pickup failed");
            Check(r.TurnsFor(r.Parcels[7])==old&&r.TurnsFor(r.Parcels[8])==0,"Pickup reset another order's clock");
            CourierChecks.Go(r,3);
            Check(r.Parcels[8].State==ParcelState.Delivered&&r.Parcels[9].State==ParcelState.Delivered&&r.Parcels[7].State==ParcelState.Loaded,"Delivery did not select matching destination");
            Check(r.Results.Single().Turns==r.TurnsFor(r.Parcels[7])-old,"Delivery used another order's clock");
            Check(r.Story.HasPending(r)&&r.Story.Read(r).Any(l=>l.Contains("cooling unit")),"Receipt missing after delivery");
            foreach(var scene in Configured().Story.Data.scenes.Where(s=>s.trigger=="receive"))
            {
                r=Configured();r.Node=scene.node;r.TalkToNpc();Check(!r.Story.HasPending(r),"Receipt before delivery");
                foreach(var id in scene.parcels)r.Parcels[id].State=ParcelState.Delivered;
                var receipt=r.TalkToNpc();Check(scene.lines.All(l=>receipt.Contains(l.Render(false))),"Receipt missing: "+scene.id);
                Check(!r.Story.HasPending(r),"Receipt repeated: "+scene.id);
            }
            for(int node=0;node<6;node++)
            {
                r=Configured();r.Node=node;r.TalkToNpc();foreach(var parcel in r.Parcels)parcel.State=ParcelState.Delivered;
                var ending=r.TalkToNpc();Check(ending.Last().Contains("tomorrow is another night"),"Epilogue missing at "+node);
                Check(!r.Story.HasPending(r),"Epilogue repeated");
            }
            var characters=Resources.Load<CourierCharacterCatalog>("NightVoyage/Characters");
            Check(Enumerable.Range(0,6).All(id=>characters.PortraitFor(characters.ForStop(id))!=null),"Character portrait binding missing");
            r=Configured();r.Node=5;r.EventProbability=1;r.LoadParcel(r.Parcels[7]);r.Parcels[7].EventTriggered=true;r.LoadParcel(r.Parcels[8]);
            var blockedEnd=CourierStreetMap.Node("C");
            r.Roads[0]=new Road(5,blockedEnd.StopId,6,0){StreetPath=new[]{new StreetEdge(CourierStreetMap.Nodes.First(n=>n.StopId==5),blockedEnd,StreetSurface.Normal,null)}};
            r.Depart(r.Roads[0]);r.FastForwardTravel();Check(r.State==ShiftState.Docked,"New pickup caused a second event for old cargo");
            var config=Configured();Check(Math.Abs(config.EventProbability-.65f)<.001f,"Event probability wrong");
            Check(config.Parcels.Select(p=>p.TimeLimit).SequenceEqual(new float[]{7,10,7,13,10,7,10,7,8,8,9,8}),"Event turn allowance missing");
            foreach(var id in Enumerable.Range(0,12).Where(id=>config.Parcels[id].Enabled))
            {
                r=Configured();r.Node=r.Parcels[id].Origin;r.EventProbability=0;r.LoadParcel(r.Parcels[id]);CourierChecks.Go(r,r.Parcels[id].Destination);
                Check(r.Results.Last().TargetTurns-r.Results.Last().Turns>=3,"No event margin for ghost order "+id);
                Debug.Log("COURIER_FEEDBACK_BALANCE "+id+" "+r.Results.Last().Turns+"/"+r.Results.Last().TargetTurns+" ghost safe");
            }
            int encounters=0;
            for(int seed=0;seed<1000;seed++)
            {
                r=new CourierRun(seed);r.LoadParcel(r.Parcels[0]);var end=CourierStreetMap.Node("C");
                r.Roads[0]=new Road(0,end.StopId,6,0){StreetPath=new[]{new StreetEdge(CourierStreetMap.Nodes[0],end,StreetSurface.Normal,null)}};
                r.Depart(r.Roads[0]);r.FastForwardTravel();if(r.State==ShiftState.Encounter)encounters++;
            }
            Check(encounters>600&&encounters<700,"Unexpected event frequency: "+encounters);
            foreach(int vehicle in new[]{-1,0,1,2})
            {r=Configured();r.VehicleIndex=vehicle;r.EventProbability=1;CourierChecks.CompleteAll(r);Check(r.Results.Count==8&&r.Delivered==9,"Configured full run failed");}
            Debug.Log("COURIER_FEEDBACK_MODEL PASS: source letter, explicit pickups, independent clocks, selective receipts, all orders completed, "+encounters+"/1000 events.");
            previousScene=EditorSceneManager.GetActiveScene().path;SessionState.SetString(Key+"Scene",previousScene);
            EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");phase=0;next=0;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        catch(Exception e){Finish(false,e.ToString());}
    }
    static void CheckTaskTable()
    {
        var r=Configured();
        int[] active={0,1,2,3,4,5,7,8,9};
        Check(r.Parcels.Select((p,id)=>new{p,id}).Where(x=>x.p.Enabled).Select(x=>x.id).SequenceEqual(active),"Task table contains phantom or missing items");
        int[] origins={0,1,1,1,2,3,-1,5,5,5};int[] destinations={3,0,2,4,5,1,-1,4,3,3};
        foreach(int id in active)Check(r.Parcels[id].Origin==origins[id]&&r.Parcels[id].Destination==destinations[id],"Wrong task route for "+id);
        var pickups=active.Select(id=>r.Parcels[id]).Where(p=>r.Pickup(p).First()==p).ToArray();
        Check(pickups.Length==7&&r.BatchCount==8,"Pickup count confused with delivery count");
        foreach(var p in pickups)
        {
            var ids=r.Pickup(p).Select(item=>r.Parcels.IndexOf(item)).ToArray();
            Check(r.Story.Data.scenes.Count(scene=>scene.trigger=="send"&&scene.node==p.Origin&&scene.parcels.OrderBy(id=>id).SequenceEqual(ids.OrderBy(id=>id)))==1,"Pickup missing its exact dialogue group");
        }
        foreach(var p in r.Parcels.Where(p=>p.Enabled&&r.Batch(p).First()==p))
        {
            var ids=r.Batch(p).Select(item=>r.Parcels.IndexOf(item));
            Check(r.Story.Data.scenes.Count(scene=>scene.trigger=="receive"&&scene.node==p.Destination&&scene.parcels.SequenceEqual(ids))==1,"Delivery missing matching receipt");
        }
        foreach(int id in new[]{6,10,11})
        {r.Node=r.Parcels[id].Origin;Check(!r.CanLoad(r.Parcels[id])&&!r.LoadParcel(r.Parcels[id])&&!r.IsCargoRow(r.Parcels[id]),"Phantom task still accessible");}
        foreach(int first in new[]{2,3})
        {
            r=Configured();r.Node=1;r.EventProbability=0;
            Check(r.Story.ReadPickup(r,r.Parcels[first]).Length==0,"Pickup dialogue before collection");
            Check(r.LoadParcel(r.Parcels[first]),"Cannot collect combined group by either item ID");
            Check(r.Parcels[2].State==ParcelState.Loaded&&r.Parcels[3].State==ParcelState.Loaded,"Pickup loaded only one Woods item");
            var dialogue=r.Story.ReadPickup(r,r.Parcels[first]);Check(dialogue.Length==3&&dialogue[0].Contains("jam"),"Combined dialogue incomplete");
            Check(r.Story.ReadPickup(r,r.Parcels[first==2?3:2]).Length==0,"Combined dialogue repeated");
            CourierChecks.Go(r,2);Check(r.Parcels[2].State==ParcelState.Delivered&&r.Parcels[3].State==ParcelState.Loaded,"Cellar accepted jam");
            Check(r.TalkToNpc().Any(line=>line.Contains("piney")),"Cellar receipt missing");
            CourierChecks.Go(r,4);Check(r.Parcels[3].State==ParcelState.Delivered&&r.Results.Count==2,"Museum did not receive jam separately");
            Check(r.TalkToNpc().Any(line=>line.Contains("Out-of-season roses")),"Museum receipt missing");
            Check(r.Results.All(result=>result.TargetTurns-result.Turns>=3),"Grouped delivery omitted event allowance");
            Debug.Log("COURIER_WOODS_ROUTE cellar then museum: "+string.Join(", ",r.Results.Select(result=>result.Cargo+" "+result.Turns+"/"+result.TargetTurns)));
        }
    }
    static void Poll()
    {
        if(File.Exists(Flag)&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating){File.Delete(Flag);Run();}
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        var owner=NightCourierPrototype.Current;if(owner==null||!owner.HasPlanner)return;
        var pump=owner.GetComponent<CourierStreetMapCheckPump>()??owner.gameObject.AddComponent<CourierStreetMapCheckPump>();pump.Check=Tick;
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next||NightCourierPrototype.Current==null||!NightCourierPrototype.Current.Active)return;next=EditorApplication.timeSinceStartup+.6;
        try
        {
            var owner=NightCourierPrototype.Current;var r=owner.Run;
            var page=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            var guide=UIManager.Instance.GetPanel("CourierTutorialPanel") as CourierTutorialPanel;
            switch(phase++)
            {
                case 0:
                    Check(!owner.ConversationActive&&!owner.GuideOpen,"Depot starts with letter or full guide");
                    r.Node=0;owner.BeginConversation();Check(owner.DialogueSpeaker=="Gooy","Letter not at subway");
                    Drain(owner);Check(owner.GuideOpen&&!owner.ConversationActive,"Guide not after letter");break;
                case 1:
                    Check(guide.view.instruction.enabled&&guide.view.strip.rect.width>1000,"Guide not large and readable");
                    Check(!owner.DepartTo(1)&&!owner.AcceptParcel(r.Parcels[0]),"Guide did not block input");
                    Canvas.ForceUpdateCanvases();Check(guide.view.instruction.preferredHeight<=guide.view.instruction.rectTransform.rect.height,"Full guide clipped");
                    ScreenCapture.CaptureScreenshot("/tmp/courier-feedback-guide.png");break;
                case 2:
                    guide.view.skipButton.onClick.Invoke();break;
                case 3:
                    Check(!owner.GuideOpen,"Guide close failed");
                    var info=page.transform.Find("Window/InformationButton").GetComponent<Button>();info.onClick.Invoke();
                    Check(owner.GuideOpen,"Info button did not reopen guide");break;
                case 4:
                    owner.Tutorial.Dismiss();r.Node=1;owner.BeginConversation();Check(!owner.ConversationActive,"Arrival played pickup dialogue");
                    owner.InteractAtNode();Check(page.CargoOpen,"Main-screen collect action failed");
                    var rows=page.GetComponentsInChildren<CourierCargoRow>(true);rows.Single(row=>row.Item==r.Parcels[1]).actionButton.onClick.Invoke();
                    Check(owner.ConversationActive&&!page.CargoOpen&&owner.DialogueSpeaker=="Wolfwoman","Pickup did not play its dialogue");
                    Check(!owner.AcceptParcel(r.Parcels[2]),"Second pickup interrupted conversation");Drain(owner);break;
                case 5:
                    Check(!owner.ConversationActive,"Uncollected item dialogue auto-started");
                    owner.InteractAtNode();Check(page.CargoOpen,"Cannot collect again at same stop");
                    var waitingRows=page.waitingContent.GetComponentsInChildren<CourierCargoRow>().Where(row=>row.gameObject.activeInHierarchy).ToArray();
                    Check(waitingRows.Count(row=>row.Item==r.Parcels[2])==1&&!waitingRows.Any(row=>row.Item==r.Parcels[3]),"Woods pickup should have one row");
                    var combinedRow=waitingRows.Single(row=>row.Item==r.Parcels[2]);
                    Check(combinedRow.nameText.text.Contains("Honey")&&combinedRow.nameText.text.Contains("Jam")&&combinedRow.secondaryArtwork.gameObject.activeSelf,"Combined pickup artwork/title missing");
                    Check(combinedRow.destinationText.text.Contains("Brewing Cellar")&&combinedRow.destinationText.text.Contains("Museum Ruins"),"Pickup omitted one destination");
                    ScreenCapture.CaptureScreenshot("/tmp/courier-task-flow-pickup.png");break;
                case 6:
                    var pickupRow=page.waitingContent.GetComponentsInChildren<CourierCargoRow>().Single(row=>row.Item==r.Parcels[2]);
                    Check(pickupRow.LoadByDrop(),"Combined pickup drop failed");
                    Check(owner.ConversationActive&&r.Parcels[2].State==ParcelState.Loaded&&r.Parcels[3].State==ParcelState.Loaded,"Drop was not atomic");
                    Drain(owner);Check(!owner.ConversationActive&&!owner.AcceptParcel(r.Parcels[3]),"Duplicate pickup/dialogue after group collection");
                    page.SetCargoOpen(true);break;
                case 7:
                    Check(r.ActiveOrders.Count()==3,"On-board orders missing");
                    var onboard=page.loadedContent.GetComponentsInChildren<CourierCargoRow>().Where(row=>row.gameObject.activeInHierarchy).ToArray();
                    Check(onboard.Length==3,"UI omitted on-board orders");
                    foreach(var label in page.stopLabels)Check(label.preferredHeight<=label.rectTransform.rect.height+.5f,"Map label clipped: "+label.text);
                    ScreenCapture.CaptureScreenshot("/tmp/courier-feedback-orders.png");break;
                case 8:
                    page.SetCargoOpen(false);
                    // Verify the longest source lines in the real dialogue layout.
                    var hud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
                    foreach(var scene in r.Story.Data.scenes)foreach(var line in scene.lines)
                    {
                        hud.subtitleText.text=line.text;hud.subtitleText.rectTransform.sizeDelta=new Vector2(1224,1000);
                        Check(hud.subtitleText.preferredHeight<600,"Source dialogue exceeds screen: "+scene.id);
                    }
                    owner.RestartShift();Check(!owner.GuideOpen&&!owner.ConversationActive,"Restart did not reset intro/guide");
                    Finish(true,"Rules and UI passed: exact source task table, seven pickup groups, eight deliveries, nine items, combined Woods pickup/dialogue, separate destination rows, independent receipts/clocks, source English and full runs.");return;
            }
        }
        catch(Exception e){Finish(false,e.ToString());}
    }
    static void Finish(bool ok,string text)
    {
        SessionState.SetBool(Key,false);File.WriteAllText(Result,(ok?"PASS\n":"FAIL\n")+text);Debug.Log("COURIER_FEEDBACK_REVISION "+(ok?"PASS ":"FAIL ")+text);
        if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>{var path=SessionState.GetString(Key+"Scene","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);};}
    }
}
