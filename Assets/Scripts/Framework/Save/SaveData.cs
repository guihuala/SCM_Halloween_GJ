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
    public DialogueSaveData dialogue = new DialogueSaveData();

    public SaveData()
    {
        createdTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastSaveTime = createdTime;
        slotName = "New Save";
    }
}

[Serializable]
public class DialogueSaveData
{
    public List<string> flags = new List<string>();
    public List<string> items = new List<string>();
    public List<DialogueQuestSaveData> quests = new List<DialogueQuestSaveData>();
    public List<DialogueNumberSaveData> numbers = new List<DialogueNumberSaveData>();
}

[Serializable]
public class DialogueQuestSaveData
{
    public string questId;
    public string state;
}

[Serializable]
public class DialogueNumberSaveData
{
    public string key;
    public float value;
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
