using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // ★ 씬 전환을 위해 필수 추가!

public class DialogueManager : MonoBehaviour
{
    private DialogueDataRoot dialogueData;

    [Header("UI References")]
    public TextMeshProUGUI txtSpeaker;
    public TextMeshProUGUI txtDialogue;
    public Image bgImage;
    public Image standingImage;
    public Button clickPanel;            // 전체화면 클릭 패널 버튼
    public Image fadePanel;             // 화면 연출용 가림막 패널

    [Header("Audio References")]
    public AudioSource jsonBgmSource;
    public AudioSource ambientSource;
    public AudioSource sfxSource;

    [Header("Direct Audio Settings")]
    public AudioClip backgroundAmbientClip;
    public AudioClip clickSfxClip;          // 오직 순수 대사창을 클릭해서 넘길 때만 재생!
    public AudioClip choiceSfxClip;         // 선택지 버튼 결정용 효과음

    [Header("Scene Transition Settings")]
    // ★ 팀원이 만든 인게임 씬 이름을 유니티 인스펙터에서 칼같이 타이핑해 넣을 수 있도록 슬롯 개설!
    public string nextSceneName = "InGameScene";

    [Header("Choice System")]
    public GameObject choiceButtonPrefab;
    public Transform choiceGroupParent;
    private List<GameObject> activeButtons = new List<GameObject>();

    private int currentNodeIndex = 0;
    private int currentActionIndex = 0;
    private bool isChoiceMode = false;

    // 타이핑 연출 제어용 변수
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string currentFullText = "";

    // 연출 제어 변수 (연출 중 클릭 차단)
    private bool isEffectPlaying = false;

    void Start()
    {
        choiceGroupParent.gameObject.SetActive(false);
        standingImage.gameObject.SetActive(false);

        if (fadePanel != null)
        {
            Color c = fadePanel.color;
            c.a = 0f;
            fadePanel.color = c;
            fadePanel.gameObject.SetActive(true);
        }

        if (jsonBgmSource != null)
        {
            jsonBgmSource.loop = true;
            jsonBgmSource.playOnAwake = false;
            jsonBgmSource.volume = 0.4f;
        }

        if (ambientSource != null && backgroundAmbientClip != null)
        {
            ambientSource.clip = backgroundAmbientClip;
            ambientSource.loop = true;
            ambientSource.playOnAwake = true;
            ambientSource.volume = 0.2f;
            ambientSource.Play();
            Debug.Log($"[BGM 환경음 엔진] 상시 루프 재생 가동 : {backgroundAmbientClip.name}");
        }

        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 1.0f;
        }

        if (clickPanel != null)
        {
            clickPanel.onClick.AddListener(NextAction);
        }

        LoadDialogueJSON("Json_Prologue");

        if (dialogueData != null && dialogueData.nodes.Count > 0)
        {
            StartNode(0, 1);
        }
    }

    void LoadDialogueJSON(string fileName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(fileName);
        if (jsonFile != null)
        {
            dialogueData = Newtonsoft.Json.JsonConvert.DeserializeObject<DialogueDataRoot>(jsonFile.text);
            Debug.Log($"[DialogueManager] Newtonsoft JSON 로드 전면 성공!");
        }
    }

    void StartNode(int nodeIndex, int targetOrder = 1)
    {
        currentNodeIndex = nodeIndex;
        currentActionIndex = targetOrder - 1;
        if (currentActionIndex < 0) currentActionIndex = 0;

        ExecuteAction();
    }

    void ExecuteAction()
    {
        GameNode currentNode = dialogueData.nodes[currentNodeIndex];
        if (currentActionIndex >= currentNode.actions.Count) return;

        NodeAction currentAction = currentNode.actions[currentActionIndex];

        if (currentAction.type == "dialogue")
        {
            txtSpeaker.text = currentAction.speaker;

            if (currentAction.effects != null && currentAction.effects.Count > 0)
            {
                foreach (var effect in currentAction.effects)
                {
                    if (effect.type == "image") ApplyImageEffect(effect.value);
                }
            }

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(CoTypeText(currentAction));
        }
        else if (currentAction.type == "choice")
        {
            ShowChoices(currentAction.choices);
        }
        else if (currentAction.type == "jump")
        {
            HandleJumpAction(currentAction.to, currentAction.to_order);
        }
        else if (currentAction.type == "call" && currentAction.fn == "PlayBgm")
        {
            if (currentAction.args != null && !string.IsNullOrEmpty(currentAction.args.name))
            {
                PlayJsonBgmDirect(currentAction.args.name);
            }
            currentActionIndex++;
            ExecuteAction();
        }
        else if (currentAction.type == "effect" || currentAction.type == "fade" || currentAction.type == "audio" || currentAction.type == "bgm")
        {
            string effectName = currentAction.effect;

            if (effectName == "play_bgm" || effectName == "stop_bgm")
            {
                string legacyName = !string.IsNullOrEmpty(currentAction.src) ? currentAction.src : currentAction.text;
                PlayJsonBgmDirect(legacyName);
                currentActionIndex++;
                ExecuteAction();
            }
            else
            {
                StartCoroutine(CoPlayEffectRoutine(effectName));
            }
        }
        else
        {
            currentActionIndex++;
            ExecuteAction();
        }
    }

    void PlayJsonBgmDirect(string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || jsonBgmSource == null) return;

        fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
        AudioClip clip = Resources.Load<AudioClip>(fileName);

        if (clip != null && jsonBgmSource.clip != clip)
        {
            jsonBgmSource.clip = clip;
            jsonBgmSource.Play();
            Debug.Log($"[JSON 연출 BGM 재생] : {fileName}");
        }
    }

    void PlayClickSfx()
    {
        if (isChoiceMode) return;

        if (sfxSource != null && clickSfxClip != null)
        {
            sfxSource.PlayOneShot(clickSfxClip);
        }
    }

    void PlayChoiceSfx()
    {
        if (sfxSource != null && choiceSfxClip != null)
        {
            sfxSource.PlayOneShot(choiceSfxClip);
        }
    }

    System.Collections.IEnumerator CoPlayEffectRoutine(string effectName)
    {
        isEffectPlaying = true;
        if (clickPanel != null) clickPanel.interactable = false;

        if (effectName == "fade_out" || effectName == "white_out")
        {
            yield return StartCoroutine(CoTriggerScreenEffect(effectName));
            ForceApplyNextImageAction();
        }
        else if (effectName == "dissolve")
        {
            yield return StartCoroutine(CoDissolveRoutine(0.5f));
        }
        else if (effectName == "fade_in" || effectName == "white_in")
        {
            ForceApplyNextImageAction();
            yield return StartCoroutine(CoTriggerScreenEffect(effectName));
        }

        isEffectPlaying = false;
        if (clickPanel != null) clickPanel.interactable = true;

        currentActionIndex++;
        ExecuteAction();
    }

    void ForceApplyNextImageAction()
    {
        GameNode currentNode = dialogueData.nodes[currentNodeIndex];
        for (int i = currentActionIndex + 1; i < currentNode.actions.Count; i++)
        {
            NodeAction nextAction = currentNode.actions[i];
            if (nextAction.type == "effect" || nextAction.type == "fade") break;

            if (nextAction.type == "dialogue" && nextAction.effects != null)
            {
                foreach (var effect in nextAction.effects)
                {
                    if (effect.type == "image")
                    {
                        ApplyImageEffect(effect.value);
                    }
                }
                break;
            }
        }
    }

    System.Collections.IEnumerator CoTypeText(NodeAction action)
    {
        isTyping = true;
        currentFullText = action.text;
        txtDialogue.text = "";

        float speedDelay = 0.03f;
        if (action.speedEnabled && action.speed > 0)
        {
            speedDelay = 1f / action.speed;
        }

        for (int i = 0; i < currentFullText.Length; i++)
        {
            txtDialogue.text += currentFullText[i];

            if (action.pauseEnabled && action.pause > 0f)
            {
                char currentChar = currentFullText[i];
                if ((currentChar == ',' || currentChar == '.' || currentChar == '!') && i < currentFullText.Length - 1)
                {
                    yield return new WaitForSeconds(action.pause);
                }
            }

            yield return new WaitForSeconds(speedDelay);
        }

        isTyping = false;
    }

    System.Collections.IEnumerator CoTriggerScreenEffect(string name)
    {
        if (fadePanel == null || string.IsNullOrEmpty(name)) yield break;

        switch (name)
        {
            case "fade_out":
                yield return StartCoroutine(CoFadeScreen(Color.black, 0f, 1f, 0.5f));
                break;
            case "fade_in":
                yield return StartCoroutine(CoFadeScreen(Color.black, 1f, 0f, 0.5f));
                break;
            case "white_out":
                yield return StartCoroutine(CoFadeScreen(Color.white, 0f, 1f, 1.0f));
                break;
            case "white_in":
                yield return StartCoroutine(CoFadeScreen(Color.white, 1f, 0f, 1.0f));
                break;
        }
    }

    System.Collections.IEnumerator CoFadeScreen(Color targetColor, float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        targetColor.a = startAlpha;
        fadePanel.color = targetColor;
        fadePanel.sprite = null;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            targetColor.a = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            fadePanel.color = targetColor;
            yield return null;
        }

        targetColor.a = endAlpha;
        fadePanel.color = targetColor;
    }

    System.Collections.IEnumerator CoDissolveRoutine(float duration)
    {
        if (bgImage != null && bgImage.sprite != null)
        {
            fadePanel.sprite = bgImage.sprite;
            fadePanel.color = Color.white;
        }
        else
        {
            fadePanel.sprite = null;
            fadePanel.color = Color.black;
        }

        Color c = fadePanel.color;
        c.a = 1f;
        fadePanel.color = c;

        ForceApplyNextImageAction();
        yield return new WaitForSeconds(0.02f);

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, timer / duration);
            fadePanel.color = c;
            yield return null;
        }

        c.a = 0f;
        fadePanel.color = c;
        fadePanel.sprite = null;
    }

    void ShowChoices(List<ChoiceData> choices)
    {
        isChoiceMode = true;
        choiceGroupParent.gameObject.SetActive(true);

        if (clickPanel != null) clickPanel.interactable = false;

        ClearChoices();
        foreach (var choice in choices)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceGroupParent);
            activeButtons.Add(btnObj);
            if (btnObj.GetComponentInChildren<CanvasRenderer>() == null) btnObj.AddComponent<CanvasRenderer>();

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = choice.text;

            Button btn = btnObj.GetComponent<Button>();

            btn.onClick.AddListener(() => {
                PlayChoiceSfx();
                OnChoiceSelected(choice);
            });
        }
    }

    void OnChoiceSelected(ChoiceData selectedChoice)
    {
        choiceGroupParent.gameObject.SetActive(false);
        if (clickPanel != null) clickPanel.interactable = true;
        ClearChoices();

        if (selectedChoice.actions != null && selectedChoice.actions.Count > 0)
        {
            NodeAction choiceAction = selectedChoice.actions[0];
            if (choiceAction.type == "jump")
            {
                isChoiceMode = false;
                HandleJumpAction(choiceAction.to, choiceAction.to_order);
                return;
            }
        }

        NextAction();
        isChoiceMode = false;
    }

    void ClearChoices()
    {
        foreach (var btn in activeButtons) Destroy(btn);
        activeButtons.Clear();
    }

    void HandleJumpAction(string targetNodeId, int targetOrder = 1)
    {
        for (int i = 0; i < dialogueData.nodes.Count; i++)
        {
            if (dialogueData.nodes[i].id == targetNodeId)
            {
                StartNode(i, targetOrder);
                return;
            }
        }
    }

    void ApplyImageEffect(EffectValue value)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(value.url);
        Sprite nextSprite = Resources.Load<Sprite>(fileName);

        if (nextSprite == null)
        {
            if (value.layer == "bg") bgImage.gameObject.SetActive(false);
            else standingImage.gameObject.SetActive(false);
            return;
        }

        if (value.layer == "bg")
        {
            bgImage.sprite = nextSprite;
            bgImage.gameObject.SetActive(true);
        }
        else
        {
            standingImage.sprite = nextSprite;
            standingImage.gameObject.SetActive(true);
        }
    }

    void NextAction()
    {
        if (isEffectPlaying) return;

        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            txtDialogue.text = currentFullText;
            isTyping = false;
            return;
        }

        PlayClickSfx();

        GameNode currentNode = dialogueData.nodes[currentNodeIndex];
        currentActionIndex++;

        if (currentActionIndex < currentNode.actions.Count)
        {
            ExecuteAction();
        }
        else
        {
            // ★ [씬 전환 트리거 핵심 로직] 
            // 현재 노드의 모든 대사 액션이 끝났고, 더 이상 넘어갈 대사 분기가 없을 때 발동합니다.
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log($"[스토리 파트 종료] {nextSceneName} 씬으로 전격 전환합니다.");
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("다음 인게임 씬 이름이 인스펙터에 등록되지 않았습니다.");
            }
        }
    }
}