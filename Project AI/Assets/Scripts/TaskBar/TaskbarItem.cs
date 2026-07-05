using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskbarItem : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    private GameObject targetWindow; // 이 버튼과 연결된 실제 아카이브 창 윈도우

    // 버튼 초기화 함수
    public void Setup(string windowName, GameObject window)
    {
        titleText.text = windowName;
        targetWindow = window;

        // 버튼 클릭 이벤트 연결
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        if (targetWindow != null)
        {
            // 창을 활성화하고 UI 레이어 상 맨 앞으로 보냄
            targetWindow.SetActive(true);
            targetWindow.transform.SetAsLastSibling();
        }
    }
}