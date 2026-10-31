using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
namespace NightCourier
{
    // Stable IDs preserve story logic; CSVs expose text and balance fields to designers.
    [Serializable] public sealed class CourierEventRules { public float probability=.35f; public string[] delayNodes,retreatNodes; }
    public static class CourierContentOverrides
    {
        public static void Apply(CourierRun run)
        {
            var rules=Resources.Load<TextAsset>("NightVoyage/EventRules");
            if(rules!=null){var config=JsonUtility.FromJson<CourierEventRules>(rules.text);run.EventProbability=Mathf.Clamp01(config.probability);if(config.delayNodes!=null)run.DelayEventNodes=config.delayNodes;if(config.retreatNodes!=null)run.RetreatEventNodes=config.retreatNodes;}
            var story = Resources.Load<TextAsset>("NightVoyage/Story");
            if(story!=null)run.Story=new CourierStory(JsonUtility.FromJson<CourierStoryData>(story.text));
            Apply("Stops",(id,row)=>{
                if(id<0||id>=6)return;var stop=run.Stops[id];
                stop.Name=row[1];stop.Sign=row[2];stop.Voice=row[3];
                stop.X=Number(row[4],stop.X,0,1);stop.Y=Number(row[5],stop.Y,0,1);
                stop.Vehicle=Int(row[6],stop.Vehicle,-1,run.Vehicles.Length-1);stop.Repair=row[7]=="1";
            });
            Apply("Vehicles",(id,row)=>{
                if(id<0||id>=run.Vehicles.Length)return;var v=run.Vehicles[id];
                v.Name=row[1];v.Ability=row[2];v.Capacity=Int(row[3],v.Capacity,1,12);v.MaxDurability=(float)Math.Ceiling(Number(row[4],v.MaxDurability,1,200));
                v.Speed=Number(row[5],v.Speed,.25f,3);v.Wear=Number(row[6],v.Wear,.1f,3);run.Durabilities[id]=v.MaxDurability;
            });
            Apply("Parcels",(id,row)=>{
                if(id<0||id>=run.Parcels.Count)return;var p=run.Parcels[id];
                p.Name=row[1];p.Weight=Int(row[2],p.Weight,1,4);p.Reward=Int(row[3],p.Reward,0,1000);
                p.HeatSensitive=row.Count>10&&row[10]=="1";p.Fragile=row[4]=="1";p.Heavy=row[5]=="1";p.Dry=row[6]=="1";
                if(row.Count>=10){p.Origin=Int(row[8],p.Origin,0,5);p.Destination=Int(row[9],p.Destination,0,5);}
                p.TimeLimit=(float)Math.Ceiling(Number(row[7],p.TimeLimit,1,240));
            });
            Apply("Events",(id,row)=>{
                if(id<0||id>=run.Events.Length)return;var e=run.Events[id];
                if(row.Count>11)e.SoundCue=row[11];
                if(row.Count>10)e.Illustration=row[10];e.Title=row[1];e.Description=row[2];e.Safe=row[3];e.Risky=row[4];e.SafeMinutes=(float)Math.Ceiling(Number(row[5],e.SafeMinutes,0,30));
                RoadEventEffect effect;if(row.Count>9&&Enum.TryParse(row[9],true,out effect)&&Enum.IsDefined(typeof(RoadEventEffect),effect))e.Effect=effect;
                e.RiskDamage=(float)Math.Ceiling(Number(row[6],e.RiskDamage,0,100));e.BreakFragile=row[7]=="1";e.WetDry=row[8]=="1";
            });
        }
        private static void Apply(string name,Action<int,List<string>> apply)
        {
            var asset=Resources.Load<TextAsset>("NightVoyage/"+name);if(asset==null)return;
            var rows=Parse(asset.text);
            for(int i=1;i<rows.Count;i++)
            {
                int id;if(rows[i].Count==0||!int.TryParse(rows[i][0],out id))continue;
                try{apply(id,rows[i]);}catch(ArgumentOutOfRangeException){Debug.LogWarning("Ignored incomplete row in "+name+": "+id);}
            }
        }
        private static float Number(string text,float fallback,float min,float max)
        {float value;return float.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&!float.IsNaN(value)&&!float.IsInfinity(value)?Mathf.Clamp(value,min,max):fallback;}
        private static int Int(string text,int fallback,int min,int max){int value;return int.TryParse(text,out value)?Mathf.Clamp(value,min,max):fallback;}
        public static List<List<string>> Parse(string csv)
        {
            var rows=new List<List<string>>();var row=new List<string>();var cell=new StringBuilder();bool quoted=false;
            for(int i=0;i<csv.Length;i++)
            {
                char c=csv[i];if(c=='\uFEFF'&&i==0)continue;
                if(c=='"'){if(quoted&&i+1<csv.Length&&csv[i+1]=='"'){cell.Append('"');i++;}else quoted=!quoted;}
                else if(!quoted&&(c==','||c=='\n')){row.Add(cell.ToString());cell.Length=0;if(c=='\n'){rows.Add(row);row=new List<string>();}}
                else if(c!='\r')cell.Append(c);
            }
            if(cell.Length>0||row.Count>0){row.Add(cell.ToString());rows.Add(row);}return rows;
        }
    }
}
