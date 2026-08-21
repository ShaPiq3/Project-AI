// C2NpcUnknownThreadSetupTool.cs
// -----------------------------------------------------------------------------
// 사용법
// 1) 이 파일은 Assets/Editor 폴더 안에 있어야 합니다.
// 2) MainScene_2를 열어둔 상태에서 상단 메뉴
//    Tools > Promes > Setup C2 NPC Unknown Thread 클릭
// 3) 씬에 있는 기존 C2_NPC_HAN(한서아) 채팅 스레드 오브젝트를 통째로 복제해서
//    contactID = C2_NPC_UNKNOWN, csvFile = Assets/Resources/C2_Chat_UNKNOWN.csv
//    로 다시 연결하고, ChatCoordinator의 contacts / threadControllers 리스트에도
//    자동으로 등록합니다.
// 4) 초상화(portraitSprite)는 일단 한서아 것을 임시로 복사해둡니다 —
//    Hierarchy에서 ChatCoordinator를 선택하고 Inspector의 Contacts 리스트에서
//    C2_NPC_UNKNOWN 항목의 Portrait Sprite / Display Name을 원하는 걸로 바꿔주세요.
// 5) 이미 C2_NPC_UNKNOWN 스레드가 존재하면 중복 생성하지 않고 경고만 남깁니다.
//    다시 만들고 싶으면 씬에서 기존 복제 오브젝트를 지우고 ChatCoordinator의
//    두 리스트에서도 항목을 지운 뒤 다시 실행하세요.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

public static class C2NpcUnknownThreadSetupTool
{
    private const string SOURCE_CONTACT_ID = "C2_NPC_HAN";
    private const string NEW_CONTACT_ID = "C2_NPC_UNKNOWN";
    private const string NEW_DISPLAY_NAME = "???";
    private const string NEW_CSV_PATH = "Assets/Resources/C2_Chat_UNKNOWN.csv";

    [MenuItem("Tools/Promes/Setup C2 NPC Unknown Thread")]
    public static void Setup()
    {
        ChatCoordinator coordinator = Object.FindAnyObjectByType<ChatCoordinator>();
        if (coordinator == null)
        {
            Debug.LogError("[C2NpcUnknownThreadSetupTool] 씬에서 ChatCoordinator를 찾지 못했습니다. MainScene_2가 열려 있는지 확인하세요.");
            return;
        }

        ChatThreadController[] allThreads = Object.FindObjectsByType<ChatThreadController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (System.Array.Exists(allThreads, t => t.contactID == NEW_CONTACT_ID))
        {
            Debug.LogWarning($"[C2NpcUnknownThreadSetupTool] contactID '{NEW_CONTACT_ID}' 스레드가 이미 존재해서 건너뜁니다.");
            return;
        }

        ChatThreadController source = System.Array.Find(allThreads, t => t.contactID == SOURCE_CONTACT_ID);
        if (source == null)
        {
            Debug.LogError($"[C2NpcUnknownThreadSetupTool] 복제할 원본(contactID '{SOURCE_CONTACT_ID}') 스레드를 찾지 못했습니다.");
            return;
        }

        TextAsset newCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(NEW_CSV_PATH);
        if (newCsv == null)
        {
            Debug.LogError($"[C2NpcUnknownThreadSetupTool] CSV를 찾지 못했습니다: {NEW_CSV_PATH}");
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
        // ------------------------------------------------------------------
        NPCContactData sourceContact = coordinator.contacts.Find(c => c.contactID == SOURCE_CONTACT_ID);

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
        // 초상화는 일단 원본(한서아) 것을 임시로 복사 - 나중에 Inspector에서 교체 필요
        newContact.FindPropertyRelative("portraitSprite").objectReferenceValue =
            sourceContact != null ? sourceContact.portraitSprite : null;

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
        Debug.Log($"[C2NpcUnknownThreadSetupTool] '{NEW_CONTACT_ID}' 스레드를 생성하고 ChatCoordinator에 등록했습니다. " +
            $"초상화(portraitSprite)는 '{SOURCE_CONTACT_ID}' 것을 임시로 복사해뒀으니 ChatCoordinator Inspector의 Contacts 리스트에서 교체해 주세요.");
    }
}
