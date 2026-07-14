using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueRow
{
    public string id;
    public string speaker;
    public string dialogue;
    public string background;
    public string standingLeft;
    public string standingMid;
    public string standingRight;
    public string sfx;
    public string bgm;
    public string ambience; // 앰비언스는 유지
    public string effect;

    public string choice1;
    public string nextId1;
    public string choice2;
    public string nextId2;
    public string choice3;
    public string nextId3;

    public string nextId;
}

public class VNDialogueData : ScriptableObject
{
    public List<DialogueRow> rows = new List<DialogueRow>();
}