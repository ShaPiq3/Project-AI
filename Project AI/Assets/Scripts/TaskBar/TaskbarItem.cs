using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TaskbarItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject highlightImage;

    public GameObject TargetWindow { get; private set; }

    public void Setup(string windowName, GameObject window)
    {
        if (titleText != null) titleText.text = windowName;
        TargetWindow = window;

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnButtonClick);
        }

        // 시작할 때는 하이라이트 이미지를 꺼둡니다.
        UpdateHighlightState(false);
    }

    // ❌ 실시간으로 창 상태를 체크해서 하이라이트를 강제하던 Update 로직 삭제

    // 🌟 오직 마우스가 들어왔을 때만 하이라이트 켬
    public void OnPointerEnter(PointerEventData eventData)
    {
        UpdateHighlightState(true);
    }

    // 🌟 마우스가 나가는 순간 예외 없이 무조건 하이라이트 끔
    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateHighlightState(false);
    }

    // 🌟 버튼을 클릭했을 때 창만 띄우고, 하이라이트는 마우스가 올라가 있으니 유지됨
    private void OnButtonClick()
    {
        if (TargetWindow != null)
        {
            TargetWindow.SetActive(true);
            TargetWindow.transform.SetAsLastSibling();
        }
    }

    // 하이라이트 이미지를 안전하게 껐다 켜는 함수
    private void UpdateHighlightState(bool show)
    {
        if (highlightImage != null)
        {
            highlightImage.SetActive(show);
        }
    }
}