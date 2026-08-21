// C2NpcUnknown3ThreadSetupTool.cs
// -----------------------------------------------------------------------------
// 사용법
// 1) 이 파일은 Assets/Editor 폴더 안에 있어야 합니다.
// 2) MainScene_2를 열어둔 상태에서 상단 메뉴
//    Tools > Promes > Setup C2 NPC Unknown3 Thread 클릭
// 3) 씬에 있는 기존 C2_NPC_HAN(한서아) 채팅 스레드 오브젝트를 통째로 복제해서
//    contactID = C2_NPC_UNKNOWN_3, csvFile = Assets/Resources/C2_Chat_UNKNOWN_3.csv
//    로 다시 연결하고, ChatCoordinator의 contacts / threadControllers 리스트에도
//    자동으로 등록합니다.
// 4) 이 NPC(???)와의 세 번째 대화입니다. 이정완(C2_NPC_JW)의 대화가 끝나면
//    (id 102) 이 스레드가 열립니다. 이 대화가 끝나면(id 40) 현재는 다음으로
//    열리는 방이 따로 지정되어 있지 않습니다 - 이후 이어지는 스토리가 정해지면
//    C2_Chat_UNKNOWN_3.csv의 id 40 행에 isOpenNextRoomTrigger/nextRoomContactID를
//    채워 넣으면 됩니다.
// 5) 초상화(portraitSprite)와 Display Name은 앞선 두 대화(C2_NPC_UNKNOWN,
//    C2_NPC_UNKNOWN_2)와 동일한 "???" 캐릭터이므로, 둘 중 이미 등록된 게 있으면
//    그 초상화를 그대로 재사용합니다.
// 6) 이미 C2_NPC_UNKNOWN_3 스레드가 존재하면 중복 생성하지 않고 경고만 남깁니다.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

public static class C2NpcUnknown3ThreadSetupTool
{
    private const string SOURCE_CONTACT_ID = "C2_NPC_HAN";
    private const string NEW_CONTACT_ID = "C2_NPC_UNKNOWN_3";
    private const string NEW_DISPLAY_NAME = "???";
    private const string NEW_CSV_PATH = "Assets/Resources/C2_Chat_UNKNOWN_3.csv";

    [MenuItem("Tools/Promes/Setup C2 NPC Unknown3 Thread")]
    public static void Setup()
    {
        ChatCoordinator coordinator = Object.FindAnyObjectByType<ChatCoordinator>();
        if (coordinator == null)
        {
            Debug.LogError("[C2NpcUnknown3ThreadSetupTool] 씬에서 ChatCoordinator를 찾지 못했습니다. MainScene_2가 열려 있는지 확인하세요.");
            return;
        }

        ChatThreadController[] allThreads = Object.FindObjectsByType<ChatThreadController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (System.Array.Exists(allThreads, t => t.contactID == NEW_CONTACT_ID))
        {
            Debug.LogWarning($"[C2NpcUnknown3ThreadSetupTool] contactID '{NEW_CONTACT_ID}' 스레드가 이미 존재해서 건너뜁니다.");
            return;
        }

        ChatThreadController source = System.Array.Find(allThreads, t => t.contactID == SOURCE_CONTACT_ID);
        if (source == null)
        {
            Debug.LogError($"[C2NpcUnknown3ThreadSetupTool] 복제할 원본(contactID '{SOURCE_CONTACT_ID}') 스레드를 찾지 못했습니다.");
            return;
        }

        TextAsset newCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(NEW_CSV_PATH);
        if (newCsv == null)
        {
            Debug.LogError($"[C2NpcUnknown3ThreadSetupTool] CSV를 찾지 못했습니다: {NEW_CSV_PATH}");
            return;
        }

        // ------------------------------------------------------------------
        // 1) 한서아 스레드 오브젝트 전체(ScrollView/Viewport/Content 포함)를 복제
        // ------------------------------------------------------------------
        GameObject dupGo = (GameObject)Object.Instantiate(source.gameObject, source.transform.parent);
        dupGo.name = "ChatThread_" + NEW_CONTACT_ID;

        ChatThreadController dupThread = dupGo.GetComponent<ChatThreadController>();
        SerializedObject soThread = new SerializedObject(dupThread);
        soThread.FindProperty("contactID").stringValue = NEW_CONTACT_ID;
        soThread.FindProperty("csvFile").objectReferenceValue = newCsv;
        soThread.ApplyModifiedProperties();

        // ------------------------------------------------------------------
        // 2) ChatCoordinator.contacts 리스트에 새 연락처 등록
        //    ("???"와의 세 번째 대화도 같은 인물이므로, 앞서 등록된 UNKNOWN/UNKNOWN_2 중
        //    있는 쪽의 초상화를 우선 재사용합니다.)
        // ------------------------------------------------------------------
        NPCContactData unknown1Contact = coordinator.contacts.Find(c => c.contactID == "C2_NPC_UNKNOWN");
        NPCContactData unknown2Contact = coordinator.contacts.Find(c => c.contactID == "C2_NPC_UNKNOWN_2");
        NPCContactData sourceContact = coordinator.contacts.Find(c => c.contactID == SOURCE_CONTACT_ID);
        NPCContactData portraitSource = unknown1Contact != null ? unknown1Contact
            : (unknown2Contact != null ? unknown2Contact : sourceContact);

        SerializedObject soCoordinator = new SerializedObject(coordinator);
        SerializedProperty contactsProp = soCoordinator.FindProperty("contacts");
        int newContactIndex = contactsProp.arraySize;
        contactsProp.InsertArrayElementAtIndex(newContactIndex);
        SerializedProperty newContact = contactsProp.GetArrayElementAtIndex(newContactIndex);
        newContact.FindPropertyRelative("contactID").stringValue = NEW_CONTACT_ID;
        newContact.FindPropertyRelative("displayName").stringValue = NEW_DISPLAY_NAME;
        newContact.FindPropertyRelative("csvFile").objectReferenceValue = newCsv;
        newContact.FindPropertyRelative("startDialogueID").intValue = 1;
        newContact.FindPropertyRelative("isSystemContact").boolValue = false;
        newContact.FindPropertyRelative("portraitSprite").objectReferenceValue =
            portraitSource != null ? portraitSource.portraitSprite : null;

        // ------------------------------------------------------------------
        // 3) ChatCoordinator.threadControllers 리스트에 새 스레드 컨트롤러 등록
        // ------------------------------------------------------------------
        SerializedProperty threadsProp = soCoordinator.FindProperty("threadControllers");
        int newThreadIndex = threadsProp.arraySize;
        threadsProp.InsertArrayElementAtIndex(newThreadIndex);
        threadsProp.GetArrayElementAtIndex(newThreadIndex).objectReferenceValue = dupThread;

        soCoordinator.ApplyModifiedProperties();

        EditorUtility.SetDirty(dupGo);
        EditorUtility.SetDirty(coordinator);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dupGo.scene);

        Selection.activeGameObject = dupGo;
        Debug.Log($"[C2NpcUnknown3ThreadSetupTool] '{NEW_CONTACT_ID}' 스레드를 생성하고 ChatCoordinator에 등록했습니다. " +
            $"초상화(portraitSprite)는 '{(portraitSource != null ? portraitSource.contactID : SOURCE_CONTACT_ID)}' 것을 임시로 복사해뒀으니 필요하면 교체해 주세요.");
    }
}
