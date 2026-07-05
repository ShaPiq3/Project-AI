using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TaskbarItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject highlightImage;

    public GameObject TargetWindow { get; private set; }

    // 🌟 [추가] 이 버튼이 클릭되어 활성화 상태인지 기억하는 변수
    private bool isWindowActive = false;

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

    private void Update()
    {
        // 🌟 [핵심] 실제 특정 창이 사용자에 의해 x 버튼 등으로 꺼졌는지 실시간 체크
        if (TargetWindow != null)
        {
            // 창의 활성화 상태가 바뀌었다면 하이라이트도 같이 갱신
            if (isWindowActive != TargetWindow.activeSelf)
            {
                isWindowActive = TargetWindow.activeSelf;
                UpdateHighlightState(isWindowActive);
            }
        }
    }

    // 🌟 마우스가 들어왔을 때 (Hover)
    public void OnPointerEnter(PointerEventData eventData)
    {
        UpdateHighlightState(true);
    }

    // 🌟 마우스가 나갔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스가 나가더라도, 이 버튼의 창이 켜져 있는 상태라면 하이라이트를 유지합니다.
        if (!isWindowActive)
        {
            UpdateHighlightState(false);
        }
    }

    // 🌟 버튼을 클릭했을 때
    private void OnButtonClick()
    {
        if (TargetWindow != null)
        {
            TargetWindow.SetActive(true);
            TargetWindow.transform.SetAsLastSibling();

            // 클릭했으므로 활성화 상태를 true로 만들고 하이라이트 유지
            isWindowActive = true;
            UpdateHighlightState(true);
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