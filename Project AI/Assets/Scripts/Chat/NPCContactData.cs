using UnityEngine;

/// <summary>
/// 연락처 목록에 표시되는 NPC(또는 시스템) 채팅 상대 1명의 메타데이터.
/// TextAsset 참조를 담아야 해서 CSV가 아니라 씬 인스펙터에서 직접 구성한다.
/// </summary>
[System.Serializable]
public class NPCContactData
{
    [Tooltip("연락처 고유 ID. 예: C2_NPC_KIM, 시스템 연락처는 C2_SYS")]
    public string contactID;

    [Tooltip("연락처 목록에 표시될 이름")]
    public string displayName;

    [Tooltip("이 연락처 전용 대화 CSV")]
    public TextAsset csvFile;

    [Tooltip("처음 대화창을 열었을 때 시작할 대화 ID")]
    public int startDialogueID = 1;

    [Tooltip("게임오버 등 시스템 메시지 전용 연락처인지 여부")]
    public bool isSystemContact;

    [Tooltip("ChatProfileBar에 표시할 초상화. 아직 아트가 없으면 비워둬도 됨(플레이스홀더 처리)")]
    public Sprite portraitSprite;
}
