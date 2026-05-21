using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TMP_LinkClickHandler : MonoBehaviour, IPointerClickHandler
{
    private TextMeshProUGUI pText;
    private string targetPanelName = ""; // 💡 엑셀에서 받아올 패널 이름을 저장할 변수

    void Awake()
    {
        // 이 스크립트가 붙은 오브젝트에서 TextMeshProUGUI 컴포넌트를 가져옵니다.
        pText = GetComponent<TextMeshProUGUI>();

        // 💡 만약 자기 자신에게 없다면 자식 오브젝트에서도 찾아봅니다 (Null 에러 방지)
        if (pText == null)
        {
            pText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    // 💡 엑셀 매니저가 말풍선을 만들 때 패널 이름을 넣어줄 함수
    public void SetTargetPanel(string panelName)
    {
        targetPanelName = panelName;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 🛠️ 안전장치 1: 만약 텍스트 컴포넌트를 여전히 찾지 못했다면 에러를 내지 않고 안전하게 종료합니다.
        if (pText == null)
        {
            Debug.LogWarning($"[{gameObject.name}] TMP_LinkClickHandler: TextMeshProUGUI 컴포넌트가 누락되었습니다! 스크립트 위치를 확인해주세요.");
            return;
        }

        // 🛠️ 안전장치 2: Canvas가 Screen Space - Overlay 모드일 때 세 번째 인자인 카메라가 null이 되어 터지는 현상 방지
        Camera eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : null;

        // 클릭한 위치가 텍스트 내의 링크(<link>) 위인지 확인 (이제 절대 TMP_TextUtilities 내부에서 Null 에러가 안 터집니다.)
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(pText, eventData.position, eventCamera);

        if (linkIndex != -1) // 링크를 정확히 클릭했다면
        {
            TMP_LinkInfo linkInfo = pText.textInfo.linkInfo[linkIndex];
            string linkId = linkInfo.GetLinkID();

            // 💡 최신 유니티 권장 방식: FindAnyObjectByType 사용
            NewChatSystem chatManager = Object.FindAnyObjectByType<NewChatSystem>();
            if (chatManager != null)
            {
                // 원래 쓰시던 함수에 'linkId'와 함께 엑셀의 'targetPanelName'도 같이 넘겨줍니다.
                chatManager.OnTextLinkClick(linkId, targetPanelName);
            }
            else
            {
                Debug.LogWarning("씬에서 NewChatSystem 오브젝트를 찾을 수 없습니다.");
            }
        }
    }
}