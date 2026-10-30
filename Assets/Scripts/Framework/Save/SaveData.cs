using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = 1;
    public int slotIndex = -1;
    public string slotName;
    public string lastSaveTime;
    public string createdTime;
    public string sceneName;
    public float playTimeSeconds;

    public int coin;
    public int currentLevel;
    public List<string> unlockedLevels = new List<string>();

    public SaveData()
    {
        createdTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastSaveTime = createdTime;
        slotName = "New Save";
    }
}

[Serializable]
public class SaveSlotMeta
{
    public int slotIndex;
    public string slotName;
    public string lastSaveTime;
    public string createdTime;
    public string sceneName;
    public float playTimeSeconds;
    public bool hasData;
    public bool isValid = true;
    public string validationSummary;
}
