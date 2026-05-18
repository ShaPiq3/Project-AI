using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TMP_Text))]
public class TMP_TextEventHandler : MonoBehaviour, IPointerClickHandler
{
    // 💡 핵심: TextMeshPro 공식 예제(TMP_TextEventCheck)가 내부에서 
    // .AddListener() 등을 통해 추적하는 오리지널 '클래스 구조'와 100% 동일하게 선언합니다.
    [System.Serializable]
    public class CharacterSelectionEvent : UnityEvent<char, int> { }

    [System.Serializable]
    public class SpriteSelectionEvent : UnityEvent<char, int> { }

    [System.Serializable]
    public class WordSelectionEvent : UnityEvent<string, int, int> { }

    [System.Serializable]
    public class LineSelectionEvent : UnityEvent<string, int, int> { }

    [System.Serializable]
    public class LinkSelectionEvent : UnityEvent<string, int, int> { }

    // 예제 스크립트가 인스펙터나 코드에서 접근하는 변수명을 공식 이름 그대로 복원합니다.
    public CharacterSelectionEvent onCharacterSelection = new CharacterSelectionEvent();
    public SpriteSelectionEvent onSpriteSelection = new SpriteSelectionEvent();
    public WordSelectionEvent onWordSelection = new WordSelectionEvent();
    public LineSelectionEvent onLineSelection = new LineSelectionEvent();
    public LinkSelectionEvent onLinkSelection = new LinkSelectionEvent();

    private TMP_Text m_TextMeshPro;

    void Awake()
    {
        m_TextMeshPro = GetComponent<TMP_Text>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (m_TextMeshPro == null) return;

        // New Input System 환경에서 마우스 좌표를 안전하게 가져오는 정석 코드
        Vector2 mousePosition = Vector2.zero;
        if (Pointer.current != null)
        {
            mousePosition = Pointer.current.position.ReadValue();
        }
        else
        {
            mousePosition = eventData.position;
        }

        // 1. 링크가 걸린 텍스트 영역을 클릭했는지 검사
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, mousePosition, eventData.pressEventCamera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];

            // 예제 스크립트 및 유니티 이벤트 시스템에 규격을 맞춰 string, int, int 인자를 쏴줍니다.
            onLinkSelection.Invoke(linkInfo.GetLinkID(), linkIndex, 0);
            return;
        }

        // 2. 일반 단어 영역을 클릭했는지 검사 (예제 대응용)
        int wordIndex = TMP_TextUtilities.FindIntersectingWord(m_TextMeshPro, mousePosition, eventData.pressEventCamera);
        if (wordIndex != -1)
        {
            TMP_WordInfo wordInfo = m_TextMeshPro.textInfo.wordInfo[wordIndex];
            onWordSelection.Invoke(wordInfo.GetWord(), wordInfo.firstCharacterIndex, wordInfo.characterCount);
        }
    }
}