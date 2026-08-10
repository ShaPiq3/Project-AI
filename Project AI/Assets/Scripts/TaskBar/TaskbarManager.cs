using UnityEngine;

public class TaskbarManager : MonoBehaviour
{
    // --- TaskbarManager.cs 내부에 추가할 코드 ---
    public bool IsArchivePanelActive() => archiveSet.popupPanel != null && archiveSet.popupPanel.activeSelf;
    public bool IsNewsPanelActive() => newsSet.popupPanel != null && newsSet.popupPanel.activeSelf;
    // 💡 [추가]
    public bool IsCommunityPanelActive() => communitySet.popupPanel != null && communitySet.popupPanel.activeSelf;

    public static TaskbarManager Instance { get; private set; }

    // 🌟 [수정] struct를 class로 변경하여 참조형으로 관리합니다. (인스펙터 꼬임 원천 차단)
    [System.Serializable]
    public class TaskbarSet
    {
        public string groupName;
        public GameObject mainButton;
        public GameObject popupPanel;
        public GameObject buttonPrefab;
        public Transform buttonContainer;
    }

    [Header("기능별 세팅")]
    [SerializeField] private TaskbarSet archiveSet = new TaskbarSet();
    [SerializeField] private TaskbarSet newsSet = new TaskbarSet();
    // 💡 [추가]
    [SerializeField] private TaskbarSet communitySet = new TaskbarSet();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitSet(archiveSet);
        InitSet(newsSet);
        // 💡 [추가]
        InitSet(communitySet);
    }

    private void InitSet(TaskbarSet set)
    {
        if (set == null) return;
        if (set.mainButton != null) set.mainButton.SetActive(false);
        if (set.popupPanel != null) set.popupPanel.SetActive(false);
    }

    public void ToggleArchivePopup() => TogglePopup(archiveSet.popupPanel);
    public void ToggleNewsPopup() => TogglePopup(newsSet.popupPanel);
    // 💡 [추가]
    public void ToggleCommunityPopup() => TogglePopup(communitySet.popupPanel);

    private void TogglePopup(GameObject panel)
    {
        if (panel != null) panel.SetActive(!panel.activeSelf);
    }

    public void AddArchiveWindow(string windowName, GameObject windowObject) => SpawnItemButton(archiveSet, windowName, windowObject);
    public void AddNewsWindow(string windowName, GameObject windowObject) => SpawnItemButton(newsSet, windowName, windowObject);
    // 💡 [추가]
    public void AddCommunityWindow(string windowName, GameObject windowObject) => SpawnItemButton(communitySet, windowName, windowObject);

    public void RemoveArchiveWindow(GameObject windowObject) => DestroyItemButton(archiveSet, windowObject);
    public void RemoveNewsWindow(GameObject windowObject) => DestroyItemButton(newsSet, windowObject);
    // 💡 [추가]
    public void RemoveCommunityWindow(GameObject windowObject) => DestroyItemButton(communitySet, windowObject);

    private void SpawnItemButton(TaskbarSet set, string name, GameObject winObj)
    {
        if (set == null || set.mainButton == null || set.buttonContainer == null || set.buttonPrefab == null) return;

        if (!set.mainButton.activeSelf) set.mainButton.SetActive(true);

        GameObject newBtn = Instantiate(set.buttonPrefab, set.buttonContainer);
        TaskbarItem item = newBtn.GetComponent<TaskbarItem>();
        if (item != null)
        {
            // 진짜 창 이름을 버튼 내 TMP 텍스트에 꽂아주는 핵심 함수 정상 도달 유도
            item.Setup(name, winObj);
        }
        else
        {
            Debug.LogError($"{set.buttonPrefab.name} 프리팹에 TaskbarItem 스크립트가 누락되었습니다!");
        }

        newBtn.transform.SetAsFirstSibling();
    }

    private void DestroyItemButton(TaskbarSet set, GameObject winObj)
    {
        if (set == null || set.buttonContainer == null) return;

        foreach (Transform child in set.buttonContainer)
        {
            TaskbarItem item = child.GetComponent<TaskbarItem>();
            if (item != null && item.TargetWindow == winObj)
            {
                Destroy(child.gameObject);
                break;
            }
        }

        if (set.buttonContainer.childCount <= 1)
        {
            if (set.mainButton != null) set.mainButton.SetActive(false);
            if (set.popupPanel != null) set.popupPanel.SetActive(false);
        }
    }
}