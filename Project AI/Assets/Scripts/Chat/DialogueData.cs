[System.Serializable]
public class DialogueData
{
    public int id;
    public string speakerType;
    public string speakerName;
    public string dialogueText;
    public bool hasImage;
    public string imagePath;
    public float delayTime;
    public string ipAddress;
    public string questID;
    public int targetCount;
    // --- [필수 추가] 선택지 분기 연출을 위한 변수들 ---
    public bool isBranch;
    public string branchText1;
    public int nextId1;
    public string branchText2;
    public int nextId2;
    public string branchText3;
    public int nextId3;
    public bool isTrigger;

    // 💡 [추가] 문서 요약 패널을 여는 말풍선(로딩바 → 버튼) 관련 필드
    public bool isDocumentBubble;      // 이 대화 행에서 문서 버블을 띄울지 여부
    public string documentID;          // 어느 DocumentQuestManager(문서)를 열지 식별하는 ID
    public float bubbleLoadingDuration; // 말풍선 안 로딩바가 채워지는 시간(초). 0이면 기본값 사용

    // 💡 [추가] 서로 다른 분기(정답/오답 등)가 같은 지점으로 합류할 때 쓰는 강제 점프 ID.
    // 0이면 사용 안 함(평소처럼 다음 순번 id로 진행), 0이 아니면 이 대화 다음에 무조건 이 id로 점프.
    public int overrideNextId;

    // 💡 [추가] isTrigger 행에서만 의미가 있음. 이 퀘스트의 답변 결과에 따라
    // "답변 생성" 클릭 시 점프할 대화 ID를 같이 지정합니다.
    public int correctDialogueID;
    public int incorrectDialogueID;

    // 💡 [오류 파라미터] isTrigger 행에서만 의미가 있음. 이 퀘스트를 실패했을 때 오류 파라미터가 오르는 양.
    public float errorWeight;

    // ==========================================================
    // 💡 [이미지 생성 퀘스트 추가] DialogueData 클래스 내부에 아래 3개 필드를 추가하세요.
    // 메인 대화 CSV의 25, 26, 27번째 컬럼 (기존 24번째 컬럼 incorrectDialogueID 바로 뒤)
    // ==========================================================
    //
    public bool isImageGenTrigger;          // 이 말풍선에서 image generation 버튼 잠금 해제 + 패널 자동 오픈
    public string imageGenQuestID;          // ImageGenQuestSlots.csv / ImageGenQuestResults.csv 의 QuestID
    public int imageGenTruthDialogueID;         // 💡 [신규] 진실(정답) 판정 시 점프할 대화 ID
    public int imageGenFalseDialogueID;         // 💡 [신규] 거짓(오답) 판정 시 점프할 대화 ID
    public int imageGenMalfunctionDialogueID;   // 오작동 판정 시 점프할 대화 ID
    public bool isImageGenMalfunctionEnd;

    // ==========================================================
    // 💡 [씬 전환] 31, 32번째 컬럼
    // ==========================================================
    public bool isSceneTransition;   // 이 줄이 재생 완료된 직후 씬을 전환할지 여부
    public string nextSceneName;     // 전환할 씬 이름 (Build Settings에 등록된 정확한 이름)

    // ==========================================================
    // 💡 [타이핑 속도] 33번째 컬럼
    // ==========================================================
    public float typingSpeed;
    public bool isPanelTrigger;   // 이 대화 행에서 특정 패널을 팝업으로 열지 여부
    public string panelID;

    // ==========================================================
    // 💡 [글리치 이펙트] 36번째 컬럼 (ChatDialogueManager/PopupChatData.csv 전용)
    // "" = 없음, "Glitch" = 씬 전환 없이 화면만 잠깐 깨졌다가 복구, "Glitch_Scene" = 이 줄로 씬이 전환될 때 화면이 깨지는 연출
    // ==========================================================
    public string glitchEffect;

    // ==========================================================
    // 💡 [멀티 NPC 연락처 - 대화방 탭] 36, 37, 38번째 컬럼
    // ==========================================================
    /// <summary> 이 대화 행 처리 후 다른 연락처의 대화방을 새로 연다(탭 생성). </summary>
    public bool isOpenNextRoomTrigger;
    /// <summary> isOpenNextRoomTrigger가 true일 때 열 연락처의 contactID (NPCContactData.contactID와 매칭). </summary>
    public string nextRoomContactID;
    /// <summary> 이 대화 행 처리 후 "지금 이 CSV(자기 자신)"의 대화방을 닫는다(탭 제거). 기록은 유지됨. </summary>
    public bool isCloseRoomTrigger;
}