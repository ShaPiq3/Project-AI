using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 💡 DataLog 여닫기 버튼(>>/<< 겸용, 채팅창-패널 경계에 고정된 버튼)에 붙이는 스크립트입니다.
///
/// 역할 두 가지:
/// 1) WindowManager.IsDatalogOpen 상태를 감시해서 스프라이트(>>/<<)를 자동으로 바꿔줍니다.
/// 2) DataLogManager.HasActiveTrigger(트리거가 한 번이라도 발동했는지)가 false인 동안은
///    버튼 자체를 완전히 숨깁니다. 즉 게임 시작 직후, 아직 isTrigger 대화가 재생되기 전에는
///    이 버튼이 화면에 전혀 보이지 않다가, 트리거가 발동하는 순간부터 나타납니다.
///
/// 사용법:
/// 1. 여닫기 버튼 오브젝트에 이 스크립트를 추가로 붙입니다.
/// 2. buttonImage 에 이 버튼의 Image 컴포넌트를 연결합니다.
/// 3. closedSprite(패널 닫힘 상태, >> 모양), openSprite(패널 열림 상태, << 모양)를 각각 연결합니다.
/// 4. 이 오브젝트는 반드시 활성화(체크) 상태로 씬에 저장해두세요.
///    (비활성화된 채로 시작하면 Update() 자체가 실행되지 않아 영원히 안 나타납니다.
///     숨김 처리는 이 스크립트가 런타임에 알아서 SetActive(false)로 처리합니다.)
/// </summary>
public class DatalogButtonSpriteSwapper : MonoBehaviour
{
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite closedSprite; // >> (패널 닫혀있을 때)
    [SerializeField] private Sprite openSprite;   // << (패널 열려있을 때)

    private bool? lastState = null; // 매 프레임 불필요한 대입을 피하기 위한 캐시

    void Update()
    {
        if (buttonImage == null || WindowManager.Instance == null || DataLogManager.Instance == null) return;

        // 💡 트리거가 한 번도 발동하지 않았다면(퀘스트 시작 전) 버튼 자체를 숨김
        bool shouldBeVisible = DataLogManager.Instance.HasActiveTrigger;

        if (gameObject.activeSelf != shouldBeVisible)
        {
            gameObject.SetActive(shouldBeVisible);
        }

        if (!shouldBeVisible) return; // 숨겨진 상태면 스프라이트 갱신도 필요 없음

        bool isOpen = WindowManager.Instance.IsDatalogOpen;

        if (lastState.HasValue && lastState.Value == isOpen) return; // 상태 변화 없으면 아무것도 안 함

        lastState = isOpen;
        buttonImage.sprite = isOpen ? openSprite : closedSprite;
    }
}