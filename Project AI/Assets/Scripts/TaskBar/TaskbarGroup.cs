using UnityEngine;
using System.Collections.Generic;

public class TaskbarGroup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;       // 이 그룹의 팝업 패널
    [SerializeField] private GameObject buttonPrefab;     // 생성할 Item 버튼 프리팹
    [SerializeField] private Transform buttonContainer;   // 팝업 패널 내부의 LayoutGroup이 있는 변수

    private List<GameObject> activeItems = new List<GameObject>();

    void Start()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    // 메인 버튼 클릭 시 호출 (인스펙터에서 해당 메인 버튼의 OnClick에 연결)
    public void TogglePopup()
    {
        if (popupPanel == null) return;

        bool isActive = !popupPanel.activeSelf;
        popupPanel.SetActive(isActive);

        // 팝업이 열릴 때 최상단 레이어로 오도록 설정 (다른 팝업에 가려지지 않게)
        if (isActive)
        {
            transform.SetAsLastSibling();
        }
    }

    // 이 기능(아카이브 혹은 뉴스)의 창이 새로 켜질 때 호출될 함수
    public void AddWindowItem(string windowName, GameObject windowObject)
    {
        if (buttonPrefab == null || buttonContainer == null) return;

        GameObject newButton = Instantiate(buttonPrefab, buttonContainer);

        TaskbarItem itemScript = newButton.GetComponent<TaskbarItem>();
        if (itemScript != null)
        {
            itemScript.Setup(windowName, windowObject);
        }

        // 최신 창이 아래에 쌓이도록 Hierarchy 최상단으로 이동
        newButton.transform.SetAsFirstSibling();
        activeItems.Add(newButton);
    }
}