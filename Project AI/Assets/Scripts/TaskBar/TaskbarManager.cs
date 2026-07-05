using UnityEngine;
using System.Collections.Generic;

public class TaskbarManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;       // 버튼들이 나열될 부모 패널
    [SerializeField] private GameObject buttonPrefab;     // Window_Item_Button 프리팹
    [SerializeField] private Transform buttonContainer;   // popupPanel과 같거나 그 내부의 LayoutGroup이 있는 변수

    private List<GameObject> activeItems = new List<GameObject>();

    void Start()
    {
        // 시작할 때는 팝업창을 닫아둡니다.
        popupPanel.SetActive(false);
    }

    // '아카이브' 메인 버튼을 클릭했을 때 실행할 함수 (인스펙터에서 OnClick에 연결)
    public void TogglePopupPanel()
    {
        popupPanel.SetActive(!popupPanel.activeSelf);
    }

    // 새로운 아카이브 창이 켜질 때 호출할 함수
    public void OnWindowOpened(string windowName, GameObject windowObject)
    {
        // 1. 프리팹 생성
        GameObject newButton = Instantiate(buttonPrefab, buttonContainer);

        // 2. 버튼 데이터 세팅
        TaskbarItem itemScript = newButton.GetComponent<TaskbarItem>();
        itemScript.Setup(windowName, windowObject);

        // 3. 리스트 관리 및 생성 위치 조정
        // 최근 켠 창이 아래에 오도록 하려면, 계층 구조상 가장 위(첫 번째)로 올려야 
        // Vertical Layout Group(Lower Alignment) 기준 가장 아래에 배치됩니다.
        newButton.transform.SetAsFirstSibling();

        activeItems.Add(newButton);
    }
}