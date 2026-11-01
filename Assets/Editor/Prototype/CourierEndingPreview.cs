using System.Reflection;
using NightCourier;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CourierEndingPreview
{
    const string Key = "CourierEndingPreview.Active";
    static double next;
    static int step;
    static CourierEndingPreview() { EditorApplication.update += Tick; }
    [MenuItem("Tools/Night Courier/Preview Ending CG")]
    static void Preview()
    {
        SessionState.SetBool(Key, true);
        step = 0; next = EditorApplication.timeSinceStartup + 5;
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
        var owner = NightCourierPrototype.Current;
        if (owner == null || !owner.Active) return;
        if (step == 0)
        {
            owner.Tutorial.Dismiss();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(NightCourierPrototype).GetField("dialogue", flags).SetValue(owner, new string[0]);
            typeof(NightCourierPrototype).GetField("dialogueIndex", flags).SetValue(owner, 0);
            typeof(NightCourierPrototype).GetField("dialogueNode", flags).SetValue(owner, -1);
            owner.Run.State = ShiftState.EndingA;
            step = 1; next = EditorApplication.timeSinceStartup + 5;
        }
        else
        {
            ScreenCapture.CaptureScreenshot("/tmp/courier-ending-preview.png");
            SessionState.SetBool(Key, false);
            Debug.Log("Ending CG preview captured.");
        }
    }
}
