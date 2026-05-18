using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ChatManager : MonoBehaviour
{
    [Header("Chat UI Panel")]
    public GameObject popupChatPanel;        // 최상위 부모 대화창 패널 전체
    public Transform chatContent;            // 말풍선 부모 (Content)

    [Header("프리팹들")]
    public GameObject npcMessagePrefab;
    public GameObject userMessagePrefab;
    public GameObject typingMessagePrefab;   // 🔥 [새로 추가] 카톡 "..." 생각중 말풍선 프리팹
    public GameObject choiceButtonContainer;
    public GameObject choiceButtonPrefab;

    [Header("단서 제어")]
    public List<GameObject> clueObjectsSequence = new List<GameObject>();

    [Header("열어줄 단서 패널들 (자유롭게 추가/삭제 가능)")]
    public List<GameObject> dbWikiPanels = new List<GameObject>();

    private int currentClueIndex = 0;

    // 💡 [1] 대화창 본문의 [파란 글씨]를 클릭했을 때 호출되는 함수
    public void OnTextLinkClick(string linkId)
    {
        Debug.Log($"[클릭 감지] 파란 글씨 링크가 클릭되었습니다! ID: {linkId}");

        if (linkId == "q1_trigger")
        {
            OpenWikiPanelIndependent(0); // 첫 번째(0번) 위키 패널 열기
        }
    }

    // 💡 [2] 위키 메뉴의 WikiClue_1, 2 버튼들을 클릭했을 때 호출할 함수
    public void OnWikiClueButtonClick(int index)
    {
        Debug.Log($"[클릭 감지] 위키 메뉴 버튼이 클릭되었습니다! Index: {index}");
        OpenWikiPanelIndependent(index);
    }

    // 💡 [최종 수정] RectMask2D의 강제 컬링 버그를 깨부수는 함수
    private void OpenWikiPanelIndependent(int index)
    {
        if (dbWikiPanels != null && index >= 0 && index < dbWikiPanels.Count)
        {
            GameObject targetPanel = dbWikiPanels[index];
            if (targetPanel != null)
            {
                // 1. 해당 패널이 속한 최상위 Canvas를 찾아서 독립시킵니다.
                Canvas rootCanvas = targetPanel.GetComponentInParent<Canvas>(true);
                if (rootCanvas != null)
                {
                    targetPanel.transform.SetParent(rootCanvas.transform, true);
                    targetPanel.transform.SetAsLastSibling();
                }

                // 2. 패널을 활성화합니다.
                targetPanel.SetActive(true);

                // 🌟 3. [RectMask2D 버그 해결 핵심 치트키]
                // 패널 자식에 있는 RectMask2D를 찾아 강제로 껐다 켜서 
                // 유니티 UI 엔진이 자식 버튼들을 다시 정상적으로 그리도록 강제 명령합니다.
                RectMask2D rectMask = targetPanel.GetComponentInChildren<RectMask2D>(true);
                if (rectMask != null)
                {
                    rectMask.enabled = false;
                    rectMask.enabled = true;
                }

                // 🌟 4. Content 레이아웃도 한 프레임 강제로 갱신합니다.
                ScrollRect scrollRect = targetPanel.GetComponentInChildren<ScrollRect>(true);
                if (scrollRect != null && scrollRect.content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[ChatManager] {index}번째 단서 패널이 리스트에 등록되지 않았거나 비어있습니다!");
        }
    }

    // 💡 OpenButton_1 버튼을 클릭했을 때 대화창이 열리며 말풍선 딜레이 연출 시작
    public void OnOpenButtonClick()
    {
        if (popupChatPanel != null)
        {
            popupChatPanel.SetActive(true);
        }

        choiceButtonContainer.SetActive(false);
        StartCoroutine(InitChatRoutine());
    }

    // 하이어라키의 Content 자식 오브젝트들을 애니메이션 딜레이 연출하는 루틴
    IEnumerator InitChatRoutine()
    {
        // 처음에 모든 말풍선 꺼두기
        foreach (Transform child in chatContent) child.gameObject.SetActive(false);

        // 순서대로 켜주기
        foreach (Transform child in chatContent)
        {
            child.gameObject.SetActive(true);
            UpdateChatLayout();
            yield return new WaitForSeconds(1.5f);
        }
    }

    // 💡 패널 내부에서 [진짜 노란색 단서 글씨]를 찾아 클릭했을 때 호출할 함수
    public void OnRealClueClick()
    {
        // NPC 대사에 카톡 "..." 애니메이션 효과 적용
        StartCoroutine(ShowChoiceRoutine());
    }

    IEnumerator ShowChoiceRoutine()
    {
        // 🔥 일반 SpawnMessage 대신 카톡 효과 코루틴을 호출합니다.
        yield return StartCoroutine(SpawnNPCMessageWithTyping("찾으신 항목이 'End-to-end encryption...' 이게 맞나요?"));

        ClearButtons();
        CreateChoiceButton("이 단서가 맞습니다", "clue_next");
        CreateChoiceButton("잘 모르겠습니다", "clue_unknown");
        choiceButtonContainer.SetActive(true);
    }

    public void OnChoiceSelected(string replyId, string buttonText)
    {
        // 유저는 생각할 필요 없이 바로 말풍선 생성
        SpawnMessage(buttonText, false);
        choiceButtonContainer.SetActive(false);
        StartCoroutine(ProcessChoiceRoutine(replyId));
    }

    IEnumerator ProcessChoiceRoutine(string replyId)
    {
        yield return new WaitForSeconds(0.5f); // 버튼 누르고 NPC 반응 시작 전 약간의 대기

        if (replyId == "clue_next")
        {
            if (currentClueIndex < clueObjectsSequence.Count)
            {
                clueObjectsSequence[currentClueIndex].SetActive(true);

                // 🔥 NPC 말풍선 카톡 효과 적용
                yield return StartCoroutine(SpawnNPCMessageWithTyping("기록되었습니다."));
                yield return new WaitForSeconds(0.8f);
                yield return StartCoroutine(SpawnNPCMessageWithTyping("다음 단서를 계속해서 조사해 주세요."));

                currentClueIndex++;
            }
            else
            {
                // 🔥 NPC 말풍선 카톡 효과 적용
                yield return StartCoroutine(SpawnNPCMessageWithTyping("모든 단서를 찾았습니다."));
                yield return new WaitForSeconds(0.8f);
                yield return StartCoroutine(SpawnNPCMessageWithTyping("최종 답변을 선택해 주세요."));

                ClearButtons();
                CreateChoiceButton("로지컬 모듈", "final_logical");
                CreateChoiceButton("에티컬 모듈", "final_ethical");
                choiceButtonContainer.SetActive(true);
            }
        }
        else if (replyId == "clue_unknown")
        {
            // 🔥 NPC 말풍선 카톡 효과 적용
            yield return StartCoroutine(SpawnNPCMessageWithTyping("다시 한번 단서를 꼼꼼히 확인해 보세요."));
            ClearButtons();
        }
    }

    // 🔥 [핵심 추가] 카카오톡처럼 ... 말풍선이 나오다가 NPC 대사로 교체되는 마법의 코루틴
    IEnumerator SpawnNPCMessageWithTyping(string message)
    {
        // 1. "..." 말풍선을 스폰합니다.
        GameObject typingObj = Instantiate(typingMessagePrefab, chatContent);
        UpdateChatLayout();

        // 2. 카톡처럼 입력 중인 느낌을 주기 위해 1.2초 동안 대기합니다. (원하는 시간으로 수정 가능)
        yield return new WaitForSeconds(1.2f);

        // 3. 기다린 후 "..." 말풍선을 파괴합니다.
        Destroy(typingObj);
        yield return null; // 파괴가 완전히 반영되도록 한 프레임 대기

        // 4. 진짜 NPC 대사 말풍선을 생성합니다.
        SpawnMessage(message, true);
    }

    void ClearButtons() { foreach (Transform child in choiceButtonContainer.transform) Destroy(child.gameObject); }

    void CreateChoiceButton(string buttonText, string replyId)
    {
        GameObject btnObj = Instantiate(choiceButtonPrefab, choiceButtonContainer.transform);
        btnObj.GetComponentInChildren<TextMeshProUGUI>().text = buttonText;
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => OnChoiceSelected(replyId, buttonText));
    }

    // 일반 메시지 스폰 함수
    void SpawnMessage(string message, bool isNPC)
    {
        GameObject target = isNPC ? npcMessagePrefab : userMessagePrefab;
        GameObject msgObj = Instantiate(target, chatContent);

        msgObj.GetComponentInChildren<TextMeshProUGUI>().text = message;
        UpdateChatLayout();
    }

    // 스크롤 및 레이아웃을 즉시 갱신해주는 공용 함수
    void UpdateChatLayout()
    {
        Canvas.ForceUpdateCanvases();
        RectTransform contentRect = chatContent.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }
}