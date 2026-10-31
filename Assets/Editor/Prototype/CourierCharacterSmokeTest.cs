using System;
using System.Linq;
using NightCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CourierCharacterSmokeTest
{
    private const string Key = "CourierCharacters.Active";
    private static int step;
    private static double next, deadline;
    private static CourierCharacterCatalog runtimeCatalog;
    static CourierCharacterSmokeTest() { EditorApplication.update += Tick; }
    private static void Require(bool ok, string error) { if (!ok) throw new Exception(error); }

    public static void Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CourierCharacterCatalog>(CourierCharacterInstaller.CatalogPath);
        Require(catalog != null && catalog.characters.Length == 6, "Expected six character configurations");
        Require(catalog.characters.Select(c => c.id).Distinct().Count() == 6, "Duplicate character IDs");
        for (int i = 0; i < 6; i++)
        {
            var c = catalog.ForStop(i);
            Require(c != null && c.portrait != null && !string.IsNullOrWhiteSpace(c.personality), "Incomplete character " + i);
            Require(c.portrait.texture.width >= 256, "Portrait resolution too low");
        }
        var run = new CourierRun(); CourierContentOverrides.Apply(run);
        int[] origins = { 0, 1, 1, 1, 2, 3, 4, 5, 5, 5 };
        int[] destinations = { 3, 0, 2, 4, 5, 1, 1, 4, 3, 3 };
        Require(run.Parcels.Count == 10, "Unexpected delivery count");
        for (int i = 0; i < 10; i++)
            Require(run.Parcels[i].Origin == origins[i] && run.Parcels[i].Destination == destinations[i], "Supply relationship differs from reference: parcel " + i);
        Debug.Log("COURIER_CHARACTER_CONFIG PASS: six portraits and personalities; ten reference delivery relationships.");
        EditorSceneManager.OpenScene("Assets/Scenes/GamePlay.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        if (deadline == 0) { deadline = EditorApplication.timeSinceStartup + 60; next = EditorApplication.timeSinceStartup + 3; }
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Portrait test timeout at " + step);
            if (EditorApplication.timeSinceStartup < next) return;
            var owner = UnityEngine.Object.FindObjectOfType<NightCourierPrototype>();
            var hud = UnityEngine.Object.FindObjectOfType<CourierHudPanel>();
            Require(owner != null && hud != null && hud.characterCatalog != null, "Portrait HUD or catalog missing");
            if (step == 0)
            {
                while(owner.ConversationActive)owner.AdvanceDialogue();
                owner.Run.Story=null;
                runtimeCatalog = UnityEngine.Object.Instantiate(hud.characterCatalog);
                // Deep-copy entries before testing replacement; never mutate the asset's data.
                runtimeCatalog.characters = hud.characterCatalog.characters.Select(c => new CourierCharacter { stopId = c.stopId, id = c.id, displayName = c.displayName, personality = c.personality, portrait = c.portrait, accent = c.accent }).ToArray();
                hud.characterCatalog = runtimeCatalog;
                owner.Run.NightMinutes = 1440;
                owner.Run.Node = 0;
                owner.BeginConversation();
            }
            else if (step <= 6)
            {
                int node = step - 1;
                var c = runtimeCatalog.ForStop(node);
                Require(hud.speakerPortrait.gameObject.activeInHierarchy && hud.speakerPortrait.sprite == c.portrait, "Wrong portrait at stop " + node);
                Require(hud.speakerName.text == c.displayName, "Wrong speaker name at stop " + node);
                Require(!hud.speakerPortrait.raycastTarget && !hud.speakerName.raycastTarget, "Portrait blocks dialogue input");
                Canvas.ForceUpdateCanvases();
                Require(hud.subtitleText.preferredHeight <= hud.subtitleText.rectTransform.rect.height, "Opening dialogue clipped at stop " + node);
                while (owner.ConversationActive)
                {
                    hud.CompleteSubtitle();
                    Require(hud.subtitleText.preferredHeight <= hud.subtitleText.rectTransform.rect.height, "Dialogue clipped at stop " + node);
                    owner.AdvanceDialogue();
                }
                if (step < 6) { owner.Run.Node = step; owner.BeginConversation(); }
                else { runtimeCatalog.ForStop(5).portrait = runtimeCatalog.ForStop(0).portrait; owner.BeginConversation(); }
            }
            else if (step == 7)
            {
                Require(hud.speakerPortrait.sprite == runtimeCatalog.ForStop(0).portrait, "Custom portrait replacement failed");
                runtimeCatalog.ForStop(5).portrait = null;
            }
            else if (step == 8)
            {
                Require(hud.speakerPortrait.sprite == runtimeCatalog.fallbackPortrait, "Missing portrait fallback failed");
                Require(hud.speakerPortrait.enabled == (runtimeCatalog.fallbackPortrait != null), "Missing portrait should hide when no fallback is configured");
                while (owner.ConversationActive) owner.AdvanceDialogue();
            }
            else if (step == 9)
            {
                Require(!hud.speakerPortrait.gameObject.activeSelf && !hud.speakerName.gameObject.activeSelf, "Speaker stays visible outside dialogue");
                Require(hud.subtitleText.rectTransform.sizeDelta == new Vector2(1600, 40), "Status text width was not restored");
                owner.Run.StartJourney(4);
            }
            else if (step == 10)
            {
                Require(!hud.speakerPortrait.gameObject.activeSelf, "Portrait remains visible while driving");
                owner.RestartShift();
            }
            else if (step == 11)
            {
                Require(owner.DialogueSpeaker == "Gooy" && !hud.speakerPortrait.gameObject.activeSelf, "Restart did not restore the opening letter");
                SessionState.SetBool(Key, false);
                Debug.Log("COURIER_CHARACTER_UI PASS: six speakers, full dialogue layout, custom sprite, fallback, status, driving and restart.");
                EditorApplication.Exit(0);
                return;
            }
            step++;
            next = EditorApplication.timeSinceStartup + .3;
        }
        catch (Exception error)
        {
            SessionState.SetBool(Key, false);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
