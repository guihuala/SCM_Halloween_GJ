using System;
using System.IO;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierMapPageSmokeTest
{
    private const string Key="CourierMapPage.Active";
    private const string LegendKey="CourierMapPage.LegendOnly";
    private static int step;
    private static double next,deadline;
    private static Vector2 dialogueMapSize;
    static CourierMapPageSmokeTest(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    public static void RunLegend()
    {
        SessionState.SetBool(LegendKey,true);Run();
    }
    private static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
    private static Rect Bounds(RectTransform rect)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
    }
    private static void CheckLayout(CourierRoutePanel planner,CourierHudPanel hud)
    {
        Canvas.ForceUpdateCanvases();
        var customGhost=Resources.Load<Sprite>("NightVoyage/VehicleIcons/Ghost_");
        Require(planner.mapArtwork.travelerIcon.ghostArtwork==customGhost&&customGhost!=null,"Map ghost does not use requested custom art");
        var screen=(RectTransform)planner.transform;
        Require(!planner.mapHeading.gameObject.activeSelf,"Retired map heading remains visible");
        foreach(var name in new[]{"PageHeaderRule","SummaryRule"})Require(!planner.transform.Find("Window/"+name).gameObject.activeSelf,"Retired divider remains visible");
        Require(planner.mapGraphic.rectTransform.rect.height>=screen.rect.height-220,"Map did not fill reclaimed vertical space");
        Require(planner.destinationText.transform.parent==planner.destinationTooltip&&planner.departButton.transform.parent==planner.destinationTooltip,"Destination controls remain in fixed bottom bar");
        Require(planner.mapGraphic.rectTransform.rect.width>=screen.rect.width*.9f,"Map still reserves space for a sidebar");
        Require(planner.transform.Find("Window/GoolieIllustration")==null,"Removed portrait was restored");
        if(!planner.vehicleArtwork.ghost.gameObject.activeSelf)Require(planner.vehicleArtwork.artwork.enabled&&planner.vehicleArtwork.artwork.sprite!=null,"Current vehicle icon is missing");
        Require(!Bounds(planner.vehicleText.rectTransform).Overlaps(Bounds(hud.timeText.rectTransform)),"Vehicle and clock overlap");
        foreach(var text in new[]{planner.objectiveText,planner.destinationText,planner.vehicleText,planner.cargoToggleText})
            if(text!=null)Require(text.preferredHeight<=text.rectTransform.rect.height,"Clipped map text: "+text.name);
        if(planner.CargoOpen)
        {
            Require(!Bounds(planner.waitingSection).Overlaps(Bounds(planner.loadedSection)),"Cargo columns overlap");
            var bar=hud.subtitleText.transform.parent.Find("SubtitleBar") as RectTransform;
            if(bar.gameObject.activeSelf)Require(Bounds(planner.cargoDrawer).yMin>=Bounds(bar).yMax,"Cargo drawer overlaps dialogue");
        }
    }
    private static void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
        var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);
        Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Button blocked: "+button.name);
        UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
    }
    private static void CheckLegend(CourierRoutePanel planner)
    {
        var legend=planner.GetComponentInChildren<CourierMapLegend>(true);
        Require(legend!=null,"Map legend missing");
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        legend.OnPointerEnter(pointer);legend.SendMessage("Update");
        Require(legend.IsExpanded,"Hover did not expand map legend");
        var card=(RectTransform)legend.transform.Find("LegendCard");
        Require(Bounds((RectTransform)planner.transform).Contains(Bounds(card).min)&&
            Bounds((RectTransform)planner.transform).Contains(Bounds(card).max),"Map legend extends beyond screen");
        Require(card.GetComponentsInChildren<CourierVehicleIconView>().Length==3,"Legend station icons missing");
        foreach(var label in card.GetComponentsInChildren<Text>())
            Require(label.preferredHeight<=label.rectTransform.rect.height,"Clipped legend label: "+label.text);
        for(int i=0;i<6;i++)Require(card.Find("StopEntry_"+i+"/Landmark_"+i)!=null,"Legend landmark missing: "+i);
        int selected=planner.SelectedMapNode,hovered=planner.HoveredMapNode;
        var run=planner.mapGraphic.Owner.Run;
        foreach(var entry in card.GetComponentsInChildren<CourierMapLegendEntry>())
        {
            var rect=(RectTransform)entry.transform;
            pointer.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);
            Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<CourierMapLegendEntry>()==entry,"Legend row hover is blocked: "+entry.name);
            entry.OnPointerEnter(pointer);
            foreach(var node in CourierStreetMap.Nodes)
            {
                bool expected=entry.target>=0?node.StopId==entry.target:
                    entry.target==CourierMapLegend.Junctions?node.StopId>=9&&node.Id!="S":
                    entry.target==CourierMapLegend.DelayEvents?run.DelayEventNodes.Contains(node.Id):run.RetreatEventNodes.Contains(node.Id);
                Require(legend.HighlightsNode(node.StopId)==expected,"Incorrect legend highlight: "+entry.name+" / "+node.Id);
            }
            entry.OnPointerExit(pointer);
            Require(CourierStreetMap.Nodes.All(n=>!legend.HighlightsNode(n.StopId)),"Legend highlight remains after exit");
        }
        Require(planner.SelectedMapNode==selected&&planner.HoveredMapNode==hovered,"Legend hover changed destination or route preview");
        var forest=card.Find("StopEntry_1").GetComponent<CourierMapLegendEntry>();forest.OnPointerEnter(pointer);
        planner.mapArtwork.GetComponent<CourierMapMotion>().SendMessage("LateUpdate");
        foreach(var glow in planner.mapArtwork.GetComponentsInChildren<CourierLandmarkGlow>())glow.SendMessage("LateUpdate");
        Canvas.ForceUpdateCanvases();
        var mesh=planner.mapArtwork.travelOverlay.canvasRenderer.GetMesh();
        Require(mesh!=null&&mesh.vertexCount>0,"Legend highlight overlay is empty");
        Capture("/tmp/courier-map-legend.png",planner);
        legend.OnPointerExit(pointer);legend.SendMessage("Update");
        Require(!legend.IsExpanded,"Legend still captures input after pointer exit");
        Require(CourierStreetMap.Nodes.All(n=>!legend.HighlightsNode(n.StopId)),"Closing legend leaves map highlighted");
        legend.OnSelect(new UnityEngine.EventSystems.AxisEventData(UnityEngine.EventSystems.EventSystem.current));legend.SendMessage("Update");
        Require(legend.IsExpanded,"Keyboard focus did not expand legend");
        planner.SetCargoOpen(true);legend.SendMessage("Update");
        Require(!legend.IsExpanded,"Legend overlaps open cargo controls");
        Require(CourierStreetMap.Nodes.All(n=>!legend.HighlightsNode(n.StopId)),"Cargo opening leaves legend highlight");
        planner.SetCargoOpen(false);legend.OnDeselect(pointer);legend.SendMessage("Update");
    }
    private static void Capture(string path,CourierRoutePanel planner)
    {
        var canvas=planner.GetComponentInParent<Canvas>().rootCanvas;
        var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float distance=canvas.planeDistance;
        var cameraObject=new GameObject("MapPreviewCamera");var camera=cameraObject.AddComponent<Camera>();
        var target=new RenderTexture(1920,1080,24);var active=RenderTexture.active;
        var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try
        {
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.085f);camera.orthographic=true;camera.targetTexture=target;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
            Canvas.ForceUpdateCanvases();planner.ApplyFullscreenLayout();Canvas.ForceUpdateCanvases();
            // Capture resizes the canvas outside the normal player frame; resync artwork halos.
            foreach(var glow in planner.mapArtwork.GetComponentsInChildren<CourierLandmarkGlow>())
            {
                glow.SendMessage("LateUpdate");
                Require(Vector2.Distance(Bounds(glow.rectTransform).center,Bounds(glow.source.rectTransform).center)<1,"Legend glow is offset from its landmark");
            }
            foreach(var guide in UnityEngine.Object.FindObjectsOfType<CourierTutorialView>())guide.Refresh();
            if(planner.destinationTooltip.gameObject.activeInHierarchy)
            {
                var card=Bounds(planner.destinationTooltip);var mapBounds=Bounds(planner.mapGraphic.rectTransform);
                Require(card.xMin>=mapBounds.xMin&&card.xMax<=mapBounds.xMax&&card.yMin>=mapBounds.yMin&&card.yMax<=mapBounds.yMax,"Destination tooltip extends beyond map after resize");
            }
            if(planner.nodePromptButton.gameObject.activeInHierarchy&&!planner.CargoOpen)
            {
                var prompt=Bounds((RectTransform)planner.nodePromptButton.transform);
                foreach(var label in planner.stopLabels)Require(!prompt.Overlaps(Bounds(label.rectTransform)),"Node prompt overlaps station name: "+label.text);
            }
            camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;
            RenderTexture.active=active;camera.targetTexture=null;target.Release();
            UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);
            Canvas.ForceUpdateCanvases();
        }
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        if(deadline==0){deadline=EditorApplication.timeSinceStartup+60;next=EditorApplication.timeSinceStartup+3;}
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Map layout test timed out at "+step);
            if(EditorApplication.timeSinceStartup<next)return;
            var planner=UIManager.Instance.GetPanel("CourierRoutePanel") as CourierRoutePanel;
            var hud=UIManager.Instance.GetPanel("CourierHudPanel") as CourierHudPanel;
            var owner=UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            Require(planner!=null&&hud!=null&&owner!=null,"Gameplay missing");
            if(SessionState.GetBool(LegendKey,false))
            {
                owner.Tutorial.Dismiss();while(owner.ConversationActive)owner.AdvanceDialogue();
                hud.CompleteSubtitle();planner.SetCargoOpen(false);planner.ApplyFullscreenLayout();
                if(step==0)
                {
                    var legend=planner.GetComponentInChildren<CourierMapLegend>(true);
                    var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                    legend.OnPointerEnter(pointer);legend.SendMessage("Update");
                    legend.transform.Find("LegendCard/StopEntry_1").GetComponent<CourierMapLegendEntry>().OnPointerEnter(pointer);
                    step=1;next=EditorApplication.timeSinceStartup+.5;return;
                }
                CheckLegend(planner);SessionState.SetBool(LegendKey,false);SessionState.SetBool(Key,false);
                Debug.Log("COURIER_MAP_LEGEND PASS: all row raycasts, per-node hover matching, highlight mesh, unchanged route selection, exit/close/cargo cleanup, keyboard focus, text fit and screen bounds.");
                EditorApplication.Exit(0);return;
            }
            CheckLayout(planner,hud);
            switch(step)
            {
                case 0:
                    // Use a physical vehicle to cover vehicle-icon and resource presentation.
                    owner.Run.VehicleIndex=0;owner.Run.Durabilities[0]=owner.Run.Vehicles[0].MaxDurability;
                    Require(!planner.CargoOpen&&!planner.cargoDrawer.gameObject.activeSelf,"Cargo should start closed");
                    Require(owner.ConversationActive,"Initial conversation missing");
                    var group=planner.mapGraphic.GetComponentInParent<CanvasGroup>();
                    Require(group.alpha<.5f&&!group.interactable&&!group.blocksRaycasts,"Dialogue must dim and block the map");
                    Require(planner.stopButtons.All(b=>!b.interactable)&&!planner.cargoToggleButton.interactable,"Dialogue leaves map controls active");
                    planner.stopButtons[3].onClick.Invoke();planner.ToggleCargoDrawer();
                    Require(!planner.CargoOpen&&!planner.destinationTooltip.gameObject.activeSelf,"Dialogue accepts map selection or cargo input");
                    dialogueMapSize=planner.mapGraphic.rectTransform.rect.size;
                    owner.Tutorial.Dismiss();hud.CompleteSubtitle();Capture("/tmp/courier-map-dialogue-focus.png",planner);
                    owner.Tutorial.Dismiss();while(owner.ConversationActive)owner.AdvanceDialogue();
                    planner.stopButtons[3].onClick.Invoke();break;
                case 1:
                    CheckLegend(planner);
                    Require(planner.mapGraphic.rectTransform.rect.size==dialogueMapSize,"Dialogue completion moved the map");
                    var restored=planner.mapGraphic.GetComponentInParent<CanvasGroup>();
                    Require(restored.alpha==1&&restored.interactable&&restored.blocksRaycasts,"Map did not unlock after dialogue");
                    Require(planner.destinationTooltip.gameObject.activeInHierarchy,"Selecting a destination did not open tooltip");
                    for(int i=0;i<planner.stopButtons.Length;i++)
                    {
                        var feedback=planner.stopButtons[i].GetComponent<CourierButtonFeedback>();
                        Require(feedback.feedbackTarget==planner.mapArtwork.landmarkImages[i].rectTransform,"Landmark focus follows old button bounds");
                        Require(planner.stopButtons[i].colors.highlightedColor.a==0,"Legacy button highlight remains offset from landmark");
                        var frame=Bounds((RectTransform)feedback.Effect.transform);var art=Bounds(feedback.feedbackTarget);
                        Require(Vector2.Distance(frame.center,art.center)<1&&frame.width>=art.width&&frame.height>=art.height,"Landmark focus frame is offset");
                    }
                    Capture("/tmp/courier-map-destination-tooltip.png",planner);
                    Require(!hud.subtitleText.gameObject.activeSelf&&!hud.subtitleText.transform.parent.Find("SubtitleBar").gameObject.activeSelf,"Idle bottom prompt is still visible");
                    Require(planner.nodePromptButton.gameObject.activeInHierarchy&&planner.nodePromptText.text=="Talk  [E]","Current node talk prompt missing");
                    Capture("/tmp/courier-map-node-prompt.png",planner);Click(planner.cargoToggleButton);break;
                case 2:
                    Require(planner.CargoOpen,"Cargo button did not open drawer");
                    Require(!planner.destinationTooltip.gameObject.activeSelf,"Destination tooltip overlaps cargo drawer");
                    Capture("/tmp/courier-map-expanded-cargo.png",planner);
                    var row=planner.GetComponentsInChildren<CourierCargoRow>().First(x=>x.Item==owner.Run.Parcels[0]);Click(row.actionButton);break;
                case 3:
                    Require(owner.Run.Load==1,"Loading through cargo drawer failed");
                    Require(planner.GetComponentsInChildren<CourierCargoRow>().Any(x=>x.Item==owner.Run.Parcels[0]&&x.transform.parent==planner.loadedContent),"Loaded parcel did not move columns");
                    Click(planner.cargoCloseButton);break;
                case 4:
                    Require(!planner.CargoOpen,"Close did not return to map");
                    var mapSize=planner.mapGraphic.rectTransform.rect.size;planner.SetCargoOpen(true);
                    Require(planner.mapGraphic.rectTransform.rect.size==mapSize,"Drawer opening moved the road network");
                    planner.SetCargoOpen(false);Click(planner.nodePromptButton);
                    Require(owner.ConversationActive,"Node talk action failed");
                    while(owner.ConversationActive)owner.AdvanceDialogue();break;
                case 5:Click(planner.departButton);break;
                case 6:
                    Require(owner.Run.HasJourney,"Departure failed after layout change");
                    Require(planner.nodePromptText.text=="Heading to this stop"&&!planner.nodePromptButton.interactable,"Travel prompt is missing or clickable");
                    planner.SetCargoOpen(true);GameManager.Instance.PauseGame();break;
                case 7:
                    planner.ToggleCargoDrawer();Require(planner.CargoOpen,"Cargo toggles while paused");GameManager.Instance.ResumeGame();owner.Run.Tick(10000);break;
                case 8:
                    // Encounters are probabilistic in the current rules; either outcome is valid.
                    if(owner.Run.State==ShiftState.Encounter)
                    {
                        Require(!planner.nodePromptButton.gameObject.activeSelf,"Node prompt remains under encounter");
                        owner.ResolveEncounter(true);owner.Run.Tick(10000);
                    }
                    else Require(owner.Run.State==ShiftState.Docked,"Travel neither stopped nor opened an encounter");
                    break;
                case 9:
                    Require(owner.Run.State==ShiftState.Docked&&owner.Run.HasJourney,"Intermediate arrival lost journey");
                    while(owner.ConversationActive)owner.AdvanceDialogue();planner.SetCargoOpen(false);break;
                case 10:
                    Require(planner.nodePromptText.text.StartsWith("Continue to "),"Continue prompt missing at waypoint");
                    Require(planner.nodePromptText.preferredHeight<=planner.nodePromptText.rectTransform.rect.height,"Waypoint prompt text clipped");
                    Capture("/tmp/courier-map-node-continue.png",planner);Click(planner.nodePromptButton);
                    Require(owner.Run.State==ShiftState.Driving,"Node continue action failed");owner.RestartShift();break;
                case 11:
                    Require(!planner.CargoOpen,"Restart did not reset drawer");
                    Require(hud.subtitleText.gameObject.activeSelf&&!planner.nodePromptButton.gameObject.activeSelf,"Restart did not restore dialogue presentation");
                    SessionState.SetBool(Key,false);Debug.Log("COURIER_MAP_PAGE PASS: reclaimed map space, destination tooltip, landmark focus bounds, dialogue dim/input lock and unlock, node clicks, cargo, departure, waypoints, pause and restart.");EditorApplication.Exit(0);return;
            }

            step++;next=EditorApplication.timeSinceStartup+.5;
        }
        catch(Exception error){SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
