using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ChatProfileBar(상단 프로필 카드)를 현재 포커스된 연락처에 맞춰 갱신한다.
/// 원래도 AD-001 하나만 있던 챕터1에서는 굳이 갈아끼울 일이 없어서 연결이 안 되어 있었음.
///
/// portraitSprite가 아직 없는 연락처(NPCContactData.portraitSprite == null)는 사진을
/// 바꾸지 않고 그대로 둔다 - 초상화 아트가 준비되기 전까지 자리만 잡아두는 용도.
///
/// 포커스된 방이 없어지면(OnFocusCleared) 예전 NPC 정보가 그대로 남아있지 않도록
/// 자기 자식들(단, IncomingMessagePopup은 제외)을 자동으로 껐다 켠다. IncomingMessagePopup이
/// 같은 오브젝트의 자식으로 붙어있어도(화면을 덮어야 해서) 이 처리 때문에 같이 꺼지지 않는다.
/// </summary>
public class ChatProfileBarController : MonoBehaviour
{
    [SerializeField] private ChatCoordinator chatCoordinator;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;

    private readonly List<GameObject> contentRoots = new List<GameObject>();

    void Awake()
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponent<IncomingMessagePopup>() != null) continue;
            contentRoots.Add(child.gameObject);
        }

        // 씬 시작 직후, 아직 아무 연락처도 포커스되지 않은 초기 상태도 "포커스 없음"과
        // 동일하게 취급 - 에디터에서 남겨둔 예전 플레이스홀더(예: AD-001)가 잠깐이라도
        // 보이지 않게 미리 꺼둔다.
        SetContentVisible(false);
    }

    void OnEnable()
    {
        if (chatCoordinator == null) chatCoordinator = ChatCoordinator.Instance;
        if (chatCoordinator == null)
        {
            Debug.LogWarning("[ChatProfileBarController] ChatCoordinator를 찾지 못했습니다.");
            return;
        }
        chatCoordinator.OnContactFocused += HandleContactFocused;
        chatCoordinator.OnFocusCleared += HandleFocusCleared;
    }

    void OnDisable()
    {
        if (chatCoordinator == null) return;
        chatCoordinator.OnContactFocused -= HandleContactFocused;
        chatCoordinator.OnFocusCleared -= HandleFocusCleared;
    }

    private void HandleContactFocused(string contactID)
    {
        NPCContactData data = chatCoordinator.GetContactData(contactID);
        if (data == null) return;

        SetContentVisible(true);
        if (nameText != null) nameText.text = data.displayName;
        if (portraitImage != null && data.portraitSprite != null) portraitImage.sprite = data.portraitSprite;
    }

    private void HandleFocusCleared()
    {
        SetContentVisible(false);
    }

    private void SetContentVisible(bool visible)
    {
        foreach (GameObject go in contentRoots)
        {
            if (go != null) go.SetActive(visible);
        }
    }
}
