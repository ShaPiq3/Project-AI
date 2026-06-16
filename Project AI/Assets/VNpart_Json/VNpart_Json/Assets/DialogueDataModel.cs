using System;
using System.Collections.Generic;
using UnityEngine;

// JSON 전체를 담을 루트 클래스
[Serializable]
public class DialogueDataRoot
{
    public MetaData meta;
    public List<GameNode> nodes;
}

// 상단의 메타 정보
[Serializable]
public class MetaData
{
    public string game;
    public int version;
    public GlobalSettings globalSettings;
    public List<Speaker> speakers;
}

[Serializable]
public class GlobalSettings
{
    public NodeDefaults nodeDefaults;
    public ActionDefaults actionDefaults;
}

[Serializable]
public class NodeDefaults
{
    public bool requireInput;
    public bool allowSkip;
    public float defaultOutputWait;
    public int defaultTypingSpeed;
    public string dialoguePosition;
}

[Serializable]
public class ActionDefaults
{
    public bool speedEnabled;
    public bool pauseEnabled;
}

[Serializable]
public class Speaker
{
    public string name;
    public string voice;
    public float pitch;
}

// 개별 노드 정보
[Serializable]
public class GameNode
{
    public int index;
    public string id;
    public NodeDefaults defaults;
    public List<NodeAction> actions;
}

// ★ [Args 구조체 추가] PlaySfx나 PlayBgm 내부의 인자값(name 등)을 받아줄 바구니
[Serializable]
public class ActionArgs
{
    public string name;
}

// 노드 안에서 순서대로 실행될 액션 단위
[Serializable]
public class NodeAction
{
    public int order;
    public string type; // "dialogue", "choice", "effect", "jump", "call" 등

    // dialogue 타입용
    public string speaker;
    public string text;
    public int speed;
    public float pause;
    public bool speedEnabled;
    public bool pauseEnabled;
    public List<EffectData> effects;

    // choice 타입용
    public List<ChoiceData> choices;

    // effect 타입용
    public string effect;
    public string src;
    public string target;
    public float duration;

    // jump 타입용
    public string to;
    public int to_order;

    // ★ [새 JSON 연동용 변수 추가] call 타입용 (PlayBgm, PlaySfx 함수 제어)
    public string fn;
    public ActionArgs args;

    // 공통 흐름 제어
    public FlowData flow;
}

// dialogue 내부의 이미지 변경 등의 이펙트
[Serializable]
public class EffectData
{
    public string id;
    public string type;
    public int[] range;
    public EffectValue value;
}

[Serializable]
public class EffectValue
{
    public string url;
    public string align;
    public string valign;
    public OffsetData offset;
    public string layer;
    public float scale;
    public int dim;
}

[Serializable]
public class OffsetData
{
    public float x;
    public float y;
}

// 선택지 데이터
[Serializable]
public class ChoiceData
{
    public string text;
    public List<NodeAction> actions;
}

// 흐름 제어 세부 설정
[Serializable]
public class FlowData
{
    public bool requireInput;
    public bool allowSkip;
    public List<NodeAction> onSkip;
}