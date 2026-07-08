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
    public string effect;

    public string choice1;
    public string nextId1;
    public string choice2;
    public string nextId2;
    public string choice3;
    public string nextId3;

    // 🌟 [자동 이동 장치] 선택지 없이 일반 대사 넘김 시 이동할 타겟 ID
    public string nextId;
}

public class VNDialogueData : ScriptableObject
{
    public List<DialogueRow> rows = new List<DialogueRow>();
}