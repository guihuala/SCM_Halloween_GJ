using System;
using System.Collections.Generic;
using System.Linq;

namespace NightCourier
{
    [Serializable] public sealed class CourierStoryLine
    {
        public string speaker, text, sourceChinese;
        public string Render(bool chinese) { return speaker + ": " + (chinese ? sourceChinese : text); }
    }
    [Serializable] public sealed class CourierStoryScene
    {
        public string id, trigger;
        public int node;
        public int[] parcels;
        public CourierStoryLine[] lines;
    }
    [Serializable] public sealed class CourierStoryData
    {
        public string language, worldBackgroundChinese;
        public CourierStoryScene[] scenes;
    }
    // Playback belongs to one shift. Reading a scene never changes cargo or unlocks.
    public sealed class CourierStory
    {
        public readonly CourierStoryData Data;
        private readonly HashSet<string> played = new HashSet<string>();
        public CourierStory(CourierStoryData data) { Data = data; }
        public static int SpeakerStop(string speaker)
        {
            switch(speaker)
            {
                case "Drow":return 0;case "Wolfwoman":return 1;case "Little Werebear":return 2;
                case "Vampire":return 3;case "Immigrant Jiangshi":return 4;case "Witch":return 5;
                default:return -1;
            }
        }
        private bool Ready(CourierStoryScene scene, CourierRun run)
        {
            if (scene.node != run.Node || played.Contains(scene.id)) return false;
            if (scene.trigger == "intro") return true;
            if (scene.parcels == null || scene.parcels.Length == 0) return false;
            if (scene.trigger == "receive")
                return scene.parcels.All(id => run.Parcels[id].State == ParcelState.Delivered);
            return scene.trigger == "send" && scene.parcels.Any(id => run.Parcels[id].State == ParcelState.Waiting);
        }
        public bool HasPending(CourierRun run) { return Data.scenes.Any(scene => Ready(scene, run)); }
        public string[] Read(CourierRun run)
        {
            // The letter is its own scene; delivery reactions precede outgoing parcels.
            var available = Data.scenes.Where(scene => Ready(scene, run))
                .OrderBy(scene => scene.trigger == "intro" ? 0 : scene.trigger == "receive" ? 1 : 2).ToArray();
            if (available.Length == 0) return new[] { run.Stops[run.Node].Voice };
            var selected = available[0].trigger == "intro" ? available.Take(1) : available;
            var lines = new List<string>();
            foreach (var scene in selected)
            {
                played.Add(scene.id);
                lines.AddRange(scene.lines.Select(line => line.Render(Data.language == "zh")));
            }
            return lines.ToArray();
        }
    }
}
