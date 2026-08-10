// Chapter2ChatSceneBuilder.cs
// -----------------------------------------------------------------------------
// 챕터2용 멀티 NPC 연락처 채팅 시스템을 씬에 자동으로 배치해주는 도구입니다.
//
// [중요] 이 도구를 이미 한 번 실행해본 적이 있다면, 다시 실행하기 전에
// Window > General > Undo History (또는 Ctrl+Z 반복) 로 "Build Multi-NPC Chat
// System" 실행 이전 상태로 완전히 되돌려주세요. 이 도구는 재실행 시 이전
// 결과물을 자동으로 치우지 않습니다 (실수로 다른 걸 같이 지우는 걸 막기 위해).
//
// 사용법
// 1) MainScene을 복제해서 만든 MainScene_2를 열어둡니다.
//    (기존 ChatManager 오브젝트를 아직 지우지 마세요 - 이 도구가 참조값을 읽어갑니다)
//    Chat_Panel 밑에 ChatTopBar(대화방 탭이 모이는 곳, 그 밑에 예시로
//    Chat_Button_1 하나), ChatProfileBar(NPC 이름/사진 카드) 가 있어야 합니다.
// 2) Assets/Resources 에 C2_Chat_KIM.csv / C2_Chat_LEE.csv / GameOverDialogue_C2.csv
//    가 있는지 확인합니다.
// 3) 상단 메뉴 Tools > Chapter2 > Build Multi-NPC Chat System 클릭
// 4) 콘솔 로그를 확인하고, 문제 없으면 Ctrl+S로 씬을 저장합니다.
//
// 이 도구가 자동으로 하는 일
// - 기존 ChatManager 오브젝트는 지우지 않고 이름을 바꾸고 비활성화만 합니다.
// - Chat_Button_1을 프리팹 에셋으로 저장하고(런타임에 복제해서 씀), 원본은 씬에서
//   제거합니다. ChatTopBar에는 ToggleGroup을 붙여서 탭들이 하나만 선택되게 합니다.
// - ChatCoordinator 오브젝트를 새로 만들고 채팅 패널 슬라이드 관련 값을 그대로 복사합니다.
// - 기존에 쓰던 ScrollView(대화 내용 영역)를 김민준 연락처용으로 재사용하고,
//   이서연/시스템 연락처용으로 2개를 더 복제해 각각 ChatThreadController를 붙입니다.
// - ChatProfileBar에 ChatProfileBarController를 붙입니다 (Portrait Image / Name Text
//   필드는 자동으로 못 찾으므로 인스펙터에서 직접 연결해야 합니다 - 잘못 추측해서
//   엉뚱한 오브젝트에 연결하는 것보다 비워두는 게 안전합니다).
//
// 씬 시작 시점에는 ChatTopBar에 탭이 하나도 없는 게 정상입니다. 대화방은 전부
// 런타임에 CSV 트리거(자동 시작/isOpenNextRoomTrigger)로 열립니다.
//
// 이미 위 과정을 한 번 완료해서 ChatCoordinator가 세팅되어 있는 씬에 "메시지 도착"
// 알림 팝업만 나중에 추가로 붙이고 싶다면, 전체를 다시 만들 필요 없이
// Tools > Chapter2 > Add Incoming Message Popup To Existing ChatCoordinator 를 쓰세요.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class Chapter2ChatSceneBuilder
{
    private const string OldManagerName = "ChatManager";
    private const string ChatTopBarName = "ChatTopBar";
    private const string ChatProfileBarName = "ChatProfileBar";
    private const string TemplateTabButtonName = "Chat_Button_1";
    private const string GeneratedPrefabDir = "Assets/Prefabs/UI/Generated";
    private const string TabButtonPrefabPath = GeneratedPrefabDir + "/ChatTabButton_Generated.prefab";

    private struct ContactDef
    {
        public string id;
        public string displayName;
        public string csvName;
        public bool isSystem;

        public ContactDef(string id, string displayName, string csvName, bool isSystem)
        {
            this.id = id;
            this.displayName = displayName;
            this.csvName = csvName;
            this.isSystem = isSystem;
        }
    }

    [MenuItem("Tools/Chapter2/Build Multi-NPC Chat System")]
    private static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder", "플레이 모드에서는 실행할 수 없습니다. 플레이를 멈추고 다시 시도해주세요.", "확인");
            return;
        }

        if (Object.FindAnyObjectByType<ChatCoordinator>() != null)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder",
                "이 씬에 이미 ChatCoordinator가 있습니다.\n" +
                "Window > General > Undo History (또는 Ctrl+Z 반복)로 이전 실행 결과를 되돌린 뒤 다시 시도해주세요.",
                "확인");
            return;
        }

        GameObject oldManagerGO = GameObject.Find(OldManagerName);
        if (oldManagerGO == null)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder", $"현재 씬에서 '{OldManagerName}' 오브젝트를 찾지 못했습니다.\nMainScene_2를 열어둔 상태에서 실행해주세요.", "확인");
            return;
        }

        ChatDialogueManager oldManager = oldManagerGO.GetComponent<ChatDialogueManager>();
        if (oldManager == null)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder", $"'{OldManagerName}' 오브젝트에 ChatDialogueManager 컴포넌트가 없습니다.", "확인");
            return;
        }

        SerializedObject oldSO = new SerializedObject(oldManager);

        GameObject npcPrefab = GetObjRef<GameObject>(oldSO, "npcPrefab");
        GameObject userPrefab = GetObjRef<GameObject>(oldSO, "userPrefab");
        ScrollRect oldChatScrollRect = GetObjRef<ScrollRect>(oldSO, "chatScrollRect");
        Object topIPText = GetObjRefRaw(oldSO, "topIPText");
        Button closeButton = GetObjRef<Button>(oldSO, "closeButton");
        Button showButton = GetObjRef<Button>(oldSO, "showButton");
        RectTransform dialoguePanelRect = GetObjRef<RectTransform>(oldSO, "dialoguePanelRect");
        AudioSource chatPanelAudioSource = GetObjRef<AudioSource>(oldSO, "chatPanelAudioSource");
        AudioSource branchClickAudioSource = GetObjRef<AudioSource>(oldSO, "branchClickAudioSource");
        GameObject branchGroupPrefab = GetObjRef<GameObject>(oldSO, "branchGroupPrefab");
        GameObject documentBubblePrefab = GetObjRef<GameObject>(oldSO, "documentBubblePrefab");
        WindowManager windowManager = GetObjRef<WindowManager>(oldSO, "windowManager");

        float tweenDuration = GetFloatOrDefault(oldSO, "tweenDuration", 0.5f);
        float targetPositionX = GetFloatOrDefault(oldSO, "targetPositionX", 0f);
        float hidePositionX = GetFloatOrDefault(oldSO, "hidePositionX", 600f);

        if (dialoguePanelRect == null || oldChatScrollRect == null)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder", "dialoguePanelRect 또는 chatScrollRect 참조를 읽지 못했습니다.\n콘솔 경고를 확인해주세요.", "확인");
            return;
        }

        // 1) ChatTopBar / Chat_Button_1 찾기
        Transform chatTopBarTransform = dialoguePanelRect.Find(ChatTopBarName);
        if (chatTopBarTransform == null)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder", $"'{dialoguePanelRect.name}' 밑에서 '{ChatTopBarName}' 오브젝트를 찾지 못했습니다.", "확인");
            return;
        }

        Transform templateTabTransform = chatTopBarTransform.Find(TemplateTabButtonName);
        GameObject tabButtonPrefab = BuildTabButtonPrefabFromTemplate(templateTabTransform);
        if (tabButtonPrefab == null)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder", $"'{ChatTopBarName}' 밑에서 '{TemplateTabButtonName}' 을 찾지 못해 탭 버튼 프리팹을 만들지 못했습니다. 직접 만들어서 ChatCoordinator에 연결해주세요.", "확인");
        }

        ToggleGroup chatTabToggleGroup = chatTopBarTransform.GetComponent<ToggleGroup>();
        if (chatTabToggleGroup == null)
        {
            chatTabToggleGroup = Undo.AddComponent<ToggleGroup>(chatTopBarTransform.gameObject);
        }

        // 2) 기존 ChatManager는 지우지 않고 비활성화 + 이름 변경만
        Undo.RegisterCompleteObjectUndo(oldManagerGO, "Disable old ChatManager");
        oldManagerGO.name = OldManagerName + "_OLD_참고용_직접확인후삭제";
        oldManagerGO.SetActive(false);

        // 3) ChatCoordinator 생성
        GameObject coordinatorGO = new GameObject("ChatCoordinator");
        Undo.RegisterCreatedObjectUndo(coordinatorGO, "Create ChatCoordinator");
        ChatCoordinator coordinator = coordinatorGO.AddComponent<ChatCoordinator>();
        SerializedObject coordSO = new SerializedObject(coordinator);

        SetObjRef(coordSO, "dialoguePanelRect", dialoguePanelRect);
        SetFloat(coordSO, "tweenDuration", tweenDuration);
        SetFloat(coordSO, "targetPositionX", targetPositionX);
        SetFloat(coordSO, "hidePositionX", hidePositionX);
        SetObjRef(coordSO, "chatPanelAudioSource", chatPanelAudioSource);
        SetObjRef(coordSO, "windowManager", windowManager);
        SetObjRef(coordSO, "closeButton", closeButton);
        SetObjRef(coordSO, "showButton", showButton);
        SetObjRef(coordSO, "chatTopBarContainer", chatTopBarTransform);
        SetObjRef(coordSO, "chatTabButtonPrefab", tabButtonPrefab);
        SetObjRef(coordSO, "chatTabToggleGroup", chatTabToggleGroup);
        SetFloat(coordSO, "autoOpenDelay", 3f);
        SetString(coordSO, "autoOpenContactID", "C2_NPC_KIM");

        // 4) 연락처 3개 정의 (김민준: 자동 시작 / 이서연·시스템: CSV 트리거로 열림)
        List<ContactDef> contactDefs = new List<ContactDef>
        {
            new ContactDef("C2_NPC_KIM", "김민준", "C2_Chat_KIM", false),
            new ContactDef("C2_NPC_LEE", "이서연", "C2_Chat_LEE", false),
            new ContactDef("C2_SYS", "SYSTEM", "GameOverDialogue_C2", true),
        };

        SerializedProperty contactsProp = coordSO.FindProperty("contacts");
        SerializedProperty threadsProp = coordSO.FindProperty("threadControllers");
        contactsProp.arraySize = contactDefs.Count;
        threadsProp.arraySize = contactDefs.Count;

        Transform panelParent = dialoguePanelRect;
        int missingCsvCount = 0;

        for (int i = 0; i < contactDefs.Count; i++)
        {
            ContactDef def = contactDefs[i];

            GameObject threadGO;
            if (i == 0)
            {
                // 첫 번째 연락처는 기존 ScrollView를 그대로 재사용 (오브젝트 중복 방지)
                threadGO = oldChatScrollRect.gameObject;
                Undo.RegisterCompleteObjectUndo(threadGO, "Repurpose ScrollView for contact");
                threadGO.name = $"ScrollView_{def.id}";
            }
            else
            {
                threadGO = Object.Instantiate(oldChatScrollRect.gameObject, panelParent);
                threadGO.name = $"ScrollView_{def.id}";
                Undo.RegisterCreatedObjectUndo(threadGO, "Create thread scroll view");
            }

            ScrollRect threadScrollRect = threadGO.GetComponent<ScrollRect>();
            Transform threadContent = threadScrollRect != null ? threadScrollRect.content : null;

            if (threadContent != null)
            {
                for (int c = threadContent.childCount - 1; c >= 0; c--)
                {
                    Object.DestroyImmediate(threadContent.GetChild(c).gameObject);
                }
            }

            ChatThreadController thread = threadGO.AddComponent<ChatThreadController>();
            SerializedObject threadSO = new SerializedObject(thread);

            SetString(threadSO, "contactID", def.id);
            TextAsset csv = FindCsvAsset(def.csvName);
            if (csv == null) missingCsvCount++;
            SetObjRef(threadSO, "csvFile", csv);
            SetObjRef(threadSO, "npcPrefab", npcPrefab);
            SetObjRef(threadSO, "userPrefab", userPrefab);
            SetObjRef(threadSO, "chatContent", threadContent);
            SetObjRefRaw(threadSO, "topIPText", topIPText);
            SetObjRef(threadSO, "chatScrollRect", threadScrollRect);
            SetObjRef(threadSO, "branchGroupPrefab", branchGroupPrefab);
            SetObjRef(threadSO, "branchClickAudioSource", branchClickAudioSource);
            SetObjRef(threadSO, "documentBubblePrefab", documentBubblePrefab);

            threadSO.ApplyModifiedPropertiesWithoutUndo();

            SerializedProperty contactEntry = contactsProp.GetArrayElementAtIndex(i);
            contactEntry.FindPropertyRelative("contactID").stringValue = def.id;
            contactEntry.FindPropertyRelative("displayName").stringValue = def.displayName;
            contactEntry.FindPropertyRelative("csvFile").objectReferenceValue = csv;
            contactEntry.FindPropertyRelative("startDialogueID").intValue = 1;
            contactEntry.FindPropertyRelative("isSystemContact").boolValue = def.isSystem;

            threadsProp.GetArrayElementAtIndex(i).objectReferenceValue = thread;
        }

        coordSO.ApplyModifiedPropertiesWithoutUndo();

        // 5) ChatProfileBar에 컨트롤러 부착 (이미지/이름 텍스트는 수동 연결 필요)
        Transform chatProfileBarTransform = dialoguePanelRect.Find(ChatProfileBarName);
        bool profileBarNeedsManualWiring = false;
        if (chatProfileBarTransform != null)
        {
            ChatProfileBarController profileController = chatProfileBarTransform.GetComponent<ChatProfileBarController>();
            if (profileController == null)
            {
                profileController = Undo.AddComponent<ChatProfileBarController>(chatProfileBarTransform.gameObject);
            }
            SerializedObject profileSO = new SerializedObject(profileController);
            SetObjRef(profileSO, "chatCoordinator", coordinator);
            profileSO.ApplyModifiedPropertiesWithoutUndo();
            profileBarNeedsManualWiring = true;
        }
        else
        {
            Debug.LogWarning($"[Chapter2ChatSceneBuilder] '{ChatProfileBarName}' 오브젝트를 찾지 못해 ChatProfileBarController를 붙이지 못했습니다.");
        }

        // 6) "메시지 도착" 알림 팝업을 ChatProfileBar를 덮는 오버레이로 자동 생성
        IncomingMessagePopup incomingPopup = chatProfileBarTransform != null
            ? BuildIncomingMessagePopup(chatProfileBarTransform)
            : null;
        if (incomingPopup != null)
        {
            SetObjRef(coordSO, "incomingMessagePopup", incomingPopup);
            coordSO.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[Chapter2ChatSceneBuilder] 메시지 도착 알림 팝업을 만들지 못했습니다 (ChatProfileBar를 못 찾음). 알림 없이 바로 방이 열립니다.");
        }

        EditorUtility.SetDirty(coordinatorGO);
        EditorSceneManager.MarkSceneDirty(oldManagerGO.scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string resultMsg = "완료되었습니다.\n\n" +
            "- ChatCoordinator 생성 (진행중 2 / 시스템 1 / 이전대화 1 슬롯)\n" +
            "- 연락처 3개(김민준/이서연/시스템) + ScrollView 3개 배치\n" +
            $"- '{TemplateTabButtonName}' → 프리팹으로 전환, ChatTopBar에 ToggleGroup 부착\n" +
            $"- 기존 '{OldManagerName}'는 '{oldManagerGO.name}'로 이름 변경 후 비활성화됨 (확인 후 직접 삭제)\n\n" +
            (missingCsvCount > 0 ? $"⚠ CSV를 찾지 못한 연락처가 {missingCsvCount}개 있습니다. 콘솔 경고를 확인해주세요.\n\n" : "") +
            (profileBarNeedsManualWiring ? "⚠ ChatProfileBarController의 Portrait Image / Name Text 필드는 인스펙터에서 직접 연결해주세요 (자동 추측은 하지 않았습니다).\n\n" : "") +
            (incomingPopup != null ? "- '메시지 도착' 알림 팝업을 ChatProfileBar 위에 자동 생성함 (기본 디자인 - 나중에 아트 입혀야 함)\n\n" : "") +
            "Ctrl+S로 씬을 저장해주세요.";

        Debug.Log("[Chapter2ChatSceneBuilder] " + resultMsg.Replace("\n", " "));
        EditorUtility.DisplayDialog("Chapter2 Chat Builder", resultMsg, "확인");
    }

    /// <summary>
    /// 이미 Build Multi-NPC Chat System을 실행해서 ChatCoordinator가 세팅되어 있는 씬에,
    /// 전체를 다시 만들지 않고 "메시지 도착" 알림 팝업만 추가로 붙여준다.
    /// </summary>
    [MenuItem("Tools/Chapter2/Add Incoming Message Popup To Existing ChatCoordinator")]
    private static void AddIncomingMessagePopupToExisting()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder", "플레이 모드에서는 실행할 수 없습니다. 플레이를 멈추고 다시 시도해주세요.", "확인");
            return;
        }

        ChatCoordinator coordinator = Object.FindAnyObjectByType<ChatCoordinator>();
        if (coordinator == null)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder", "씬에서 ChatCoordinator를 찾지 못했습니다.\n먼저 'Build Multi-NPC Chat System'을 실행해주세요.", "확인");
            return;
        }

        SerializedObject coordSO = new SerializedObject(coordinator);

        SerializedProperty existingPopupProp = coordSO.FindProperty("incomingMessagePopup");
        if (existingPopupProp != null && existingPopupProp.objectReferenceValue != null)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder", "이미 Incoming Message Popup이 연결되어 있습니다. 중복 생성을 막기 위해 중단합니다.", "확인");
            return;
        }

        RectTransform dialoguePanelRect = GetObjRef<RectTransform>(coordSO, "dialoguePanelRect");
        if (dialoguePanelRect == null)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder", "ChatCoordinator의 dialoguePanelRect를 읽지 못했습니다.", "확인");
            return;
        }

        Transform chatProfileBarTransform = dialoguePanelRect.Find(ChatProfileBarName);
        if (chatProfileBarTransform == null)
        {
            EditorUtility.DisplayDialog("Chapter2 Chat Builder", $"'{ChatProfileBarName}' 오브젝트를 찾지 못했습니다.", "확인");
            return;
        }

        IncomingMessagePopup popup = BuildIncomingMessagePopup(chatProfileBarTransform);
        SetObjRef(coordSO, "incomingMessagePopup", popup);
        coordSO.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(coordinator);
        EditorSceneManager.MarkSceneDirty(coordinator.gameObject.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Chapter2ChatSceneBuilder] IncomingMessagePopup을 기존 ChatCoordinator에 추가했습니다.");
        EditorUtility.DisplayDialog("Chapter2 Chat Builder", "'메시지 도착' 알림 팝업을 추가했습니다 (기본 디자인).\nCtrl+S로 씬을 저장해주세요.", "확인");
    }

    /// <summary>
    /// ChatProfileBar 자식으로 "메시지 도착" 알림 오버레이를 만든다. 부모(ChatProfileBar) 전체를
    /// 덮도록 앵커를 꽉 채우고, 배경/문구/확인 버튼을 아주 기본적인 모양으로 생성한다.
    /// (디자인은 임시 - 나중에 아트에 맞게 새로 입혀야 함)
    /// </summary>
    private static IncomingMessagePopup BuildIncomingMessagePopup(Transform chatProfileBarTransform)
    {
        GameObject popupGO = new GameObject("IncomingMessagePopup", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        Undo.RegisterCreatedObjectUndo(popupGO, "Create IncomingMessagePopup");
        popupGO.transform.SetParent(chatProfileBarTransform, false);
        popupGO.transform.SetAsLastSibling();

        // 💡 ChatProfileBar 자체에 HorizontalLayoutGroup 등이 붙어있을 수 있는데, 그러면
        // 부모 레이아웃 그룹이 이 오버레이도 "줄 안의 아이템 하나"로 취급해서 크기를
        // 멋대로 0으로 찌그러뜨린다. LayoutElement.ignoreLayout으로 그걸 무시하게 한다.
        popupGO.GetComponent<LayoutElement>().ignoreLayout = true;

        RectTransform popupRT = popupGO.GetComponent<RectTransform>();
        popupRT.anchorMin = Vector2.zero;
        popupRT.anchorMax = Vector2.one;
        popupRT.offsetMin = Vector2.zero;
        popupRT.offsetMax = Vector2.zero;

        Image bg = popupGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject textGO = new GameObject("MessageText", typeof(RectTransform));
        textGO.transform.SetParent(popupGO.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "메시지가 도착했습니다.";
        text.fontSize = 20f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0.35f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.offsetMin = new Vector2(8f, 0f);
        textRT.offsetMax = new Vector2(-8f, -8f);

        GameObject btnGO = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(popupGO.transform, false);
        btnGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0f);
        btnRT.anchorMax = new Vector2(0.5f, 0f);
        btnRT.pivot = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = new Vector2(0f, 10f);
        btnRT.sizeDelta = new Vector2(88f, 32f);

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnText.text = "확인";
        btnText.fontSize = 18f;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.black;
        RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = Vector2.zero;
        btnTextRT.offsetMax = Vector2.zero;

        IncomingMessagePopup popup = popupGO.AddComponent<IncomingMessagePopup>();
        SerializedObject popupSO = new SerializedObject(popup);
        SetObjRef(popupSO, "root", popupGO);
        SetObjRef(popupSO, "messageText", text);
        SetObjRef(popupSO, "confirmButton", btnGO.GetComponent<Button>());
        popupSO.ApplyModifiedPropertiesWithoutUndo();

        return popup;
    }

    private static GameObject BuildTabButtonPrefabFromTemplate(Transform templateTransform)
    {
        if (templateTransform == null) return null;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }
        if (!AssetDatabase.IsValidFolder(GeneratedPrefabDir))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs/UI", "Generated");
        }

        // 원본을 건드리지 않기 위해 복제본으로 프리팹을 만든다
        GameObject clone = Object.Instantiate(templateTransform.gameObject);
        clone.name = "ChatTabButton_Generated";

        TMP_Text nameText = clone.GetComponentInChildren<TMP_Text>(true);
        ChatTabButtonUI tabButtonUI = clone.GetComponent<ChatTabButtonUI>();
        if (tabButtonUI == null) tabButtonUI = clone.AddComponent<ChatTabButtonUI>();

        SerializedObject tabSO = new SerializedObject(tabButtonUI);
        SetObjRef(tabSO, "nameText", nameText);
        SetObjRef(tabSO, "toggle", clone.GetComponent<Toggle>());
        SetObjRef(tabSO, "button", clone.GetComponent<Button>());
        tabSO.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(clone, TabButtonPrefabPath);
        Object.DestroyImmediate(clone);

        // 원본 예시 버튼은 런타임에 복제해서 쓰는 프리팹으로 대체됐으니 씬에서는 제거
        Undo.DestroyObjectImmediate(templateTransform.gameObject);

        return prefab;
    }

    private static T GetObjRef<T>(SerializedObject so, string propName) where T : Object
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p == null)
        {
            Debug.LogWarning($"[Chapter2ChatSceneBuilder] 필드 '{propName}' 을 찾지 못했습니다 (원본 스크립트의 필드 이름이 바뀌었을 수 있습니다).");
            return null;
        }
        return p.objectReferenceValue as T;
    }

    private static Object GetObjRefRaw(SerializedObject so, string propName)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p == null)
        {
            Debug.LogWarning($"[Chapter2ChatSceneBuilder] 필드 '{propName}' 을 찾지 못했습니다.");
            return null;
        }
        return p.objectReferenceValue;
    }

    private static float GetFloatOrDefault(SerializedObject so, string propName, float defaultValue)
    {
        SerializedProperty p = so.FindProperty(propName);
        return p != null ? p.floatValue : defaultValue;
    }

    private static void SetObjRef(SerializedObject so, string propName, Object value)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p == null)
        {
            Debug.LogWarning($"[Chapter2ChatSceneBuilder] 대상 필드 '{propName}' 을 찾지 못해 값을 설정하지 못했습니다.");
            return;
        }
        p.objectReferenceValue = value;
    }

    private static void SetObjRefRaw(SerializedObject so, string propName, Object value)
    {
        SetObjRef(so, propName, value);
    }

    private static void SetFloat(SerializedObject so, string propName, float value)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p == null) return;
        p.floatValue = value;
    }

    private static void SetString(SerializedObject so, string propName, string value)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p == null) return;
        p.stringValue = value;
    }

    private static TextAsset FindCsvAsset(string csvNameWithoutExt)
    {
        string[] guids = AssetDatabase.FindAssets($"{csvNameWithoutExt} t:TextAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == csvNameWithoutExt)
            {
                return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            }
        }
        Debug.LogWarning($"[Chapter2ChatSceneBuilder] '{csvNameWithoutExt}' CSV를 프로젝트에서 찾지 못했습니다.");
        return null;
    }
}
