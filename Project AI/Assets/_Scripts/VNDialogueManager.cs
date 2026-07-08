using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class VNDialogueManager : MonoBehaviour
{
    [Header("CSV File Name")]
    [SerializeField] private string csvFileName = "Data/DialogueTable";

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image standingLeftImage;
    [SerializeField] private Image standingMidImage;
    [SerializeField] private Image standingRightImage;

    [Header("Audio References")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Choice Settings")]
    [SerializeField] private GameObject choiceContainer;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private TextMeshProUGUI[] choiceTexts;

    [Header("Effect Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float dissolveDuration = 1.0f;
    [SerializeField] private float dissolveStandingDuration = 0.25f;
    [SerializeField] private float whiteoutDuration = 1.5f;
    [SerializeField] private float whiteinDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 1.5f;

    private List<DialogueRow> dialogueRows;
    private int currentLineIndex = 0;
    private string currentBgmName = "";

    private Coroutine typingCoroutine;
    private Coroutine fadeCoroutine;
    private bool isTyping = false;
    private string completeDialogue = "";

    private bool isChoiceActive = false;

    private Image fullScreenFadeImage;
    private Image baseBlackImage;
    private Image bgEffectOverlayImage;

    private Image bgDissolveTemp;
    private Image leftDissolveTemp;
    private Image midDissolveTemp;
    private Image rightDissolveTemp;

    void Awake()
    {
        Transform defaultCanvasTransform = null;
        Canvas canvas = GameObject.FindAnyObjectByType<Canvas>();
        if (canvas != null) defaultCanvasTransform = canvas.transform;

        CreateFullScreenFadeObject(defaultCanvasTransform);
        CreateBaseBlackObject();
        CreateBgEffectOverlayObject();

        if (backgroundImage != null)
            bgDissolveTemp = CreateDissolveTempObject("BG_Dissolve_Temp", backgroundImage.transform.parent, backgroundImage.transform.GetSiblingIndex() + 1);

        if (standingLeftImage != null)
            leftDissolveTemp = CreateDissolveTempObject("Left_Dissolve_Temp", standingLeftImage.transform.parent, standingLeftImage.transform.GetSiblingIndex() + 1);

        if (standingMidImage != null)
            midDissolveTemp = CreateDissolveTempObject("Mid_Dissolve_Temp", standingMidImage.transform.parent, standingMidImage.transform.GetSiblingIndex() + 1);

        if (standingRightImage != null)
            rightDissolveTemp = CreateDissolveTempObject("Right_Dissolve_Temp", standingRightImage.transform.parent, standingRightImage.transform.GetSiblingIndex() + 1);
    }

    void Start()
    {
        CheckInspectorAssignments();
        dialogueRows = VNCSVParser.ParseCSV(csvFileName);

        if (dialogueRows != null)
        {
            dialogueRows.RemoveAll(row => row == null || (string.IsNullOrEmpty(row.speaker) && string.IsNullOrEmpty(row.dialogue)));
        }

        if (choiceContainer != null) choiceContainer.SetActive(false);
        BindChoiceButtonEvents();

        if (dialogueRows != null && dialogueRows.Count > 0)
        {
            PlayLine(currentLineIndex);
        }
        else
        {
            Debug.LogError($"[데이터 오류] CSV 대사 데이터를 로드하지 못했거나 비어있습니다. 경로를 확인하세요: Resources/{csvFileName}");
        }

        Button btn = GetComponent<Button>();
        if (btn == null) btn = gameObject.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnScreenClicked);
    }

    void Update()
    {
        if (isChoiceActive) return;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            HandleInput();
        }
#else
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            HandleInput();
        }
#endif
    }

    private void CheckInspectorAssignments()
    {
        if (choiceContainer == null) Debug.LogError("[인스펙터 누락] 'Choice Container' 슬롯이 비어있습니다! 오브젝트를 연결해주세요.");
        if (choiceButtons == null || choiceButtons.Length == 0) Debug.LogError("[인스펙터 누락] 'Choice Buttons' 배열이 비어있거나 세팅되지 않았습니다.");
        if (choiceTexts == null || choiceTexts.Length == 0) Debug.LogError("[인스펙터 누락] 'Choice Texts' 배열이 비어있거나 세팅되지 않았습니다.");
    }

    private void CreateFullScreenFadeObject(Transform parentCanvas)
    {
        if (parentCanvas == null || fullScreenFadeImage != null) return;
        GameObject fadeObj = new GameObject("FullScreen_Fade_Panel");
        fadeObj.transform.SetParent(parentCanvas, false);
        fadeObj.transform.SetAsLastSibling();
        fullScreenFadeImage = fadeObj.AddComponent<Image>();
        fullScreenFadeImage.color = new Color(0, 0, 0, 0f);
        fullScreenFadeImage.raycastTarget = false;
        SetStretchAnchor(fadeObj.GetComponent<RectTransform>());
    }

    private void CreateBaseBlackObject()
    {
        if (backgroundImage == null || baseBlackImage != null) return;
        GameObject bgBlackObj = new GameObject("Base_Black_Background");
        bgBlackObj.transform.SetParent(backgroundImage.transform.parent, false);
        bgBlackObj.transform.SetSiblingIndex(backgroundImage.transform.GetSiblingIndex());
        baseBlackImage = bgBlackObj.AddComponent<Image>();
        baseBlackImage.color = new Color(0, 0, 0, 1f);
        baseBlackImage.raycastTarget = false;
        SetStretchAnchor(bgBlackObj.GetComponent<RectTransform>());
    }

    private void CreateBgEffectOverlayObject()
    {
        if (backgroundImage == null || bgEffectOverlayImage != null) return;
        GameObject overlayObj = new GameObject("BG_Effect_Overlay_Panel");
        overlayObj.transform.SetParent(backgroundImage.transform.parent, false);
        overlayObj.transform.SetSiblingIndex(backgroundImage.transform.GetSiblingIndex() + 1);
        bgEffectOverlayImage = overlayObj.AddComponent<Image>();
        bgEffectOverlayImage.color = new Color(0, 0, 0, 0f);
        bgEffectOverlayImage.raycastTarget = false;
        SetStretchAnchor(overlayObj.GetComponent<RectTransform>());
    }

    private Image CreateDissolveTempObject(string name, Transform parent, int siblingIndex)
    {
        if (parent == null) return null;
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.SetSiblingIndex(siblingIndex);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0f);
        img.raycastTarget = false;
        img.gameObject.SetActive(false);
        SetStretchAnchor(obj.GetComponent<RectTransform>());
        return img;
    }

    private void SetStretchAnchor(RectTransform rect)
    {
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnScreenClicked()
    {
        if (isChoiceActive) return;
        HandleInput();
    }

    private void HandleInput()
    {
        if (isTyping)
        {
            StopTypingAndShowFullText();
        }
        else
        {
            OnNextTarget();
        }
    }

    // 🌟 [핵심 변경] 대사 넘김 버튼을 눌렀을 때 작동하는 자동 이동 및 자동 종료 판정 로직
    public void OnNextTarget()
    {
        if (currentLineIndex >= 0 && currentLineIndex < dialogueRows.Count)
        {
            DialogueRow currentRow = dialogueRows[currentLineIndex];
            string autoNextID = !string.IsNullOrEmpty(currentRow.nextId) ? currentRow.nextId.Trim() : string.Empty;

            if (!string.IsNullOrEmpty(autoNextID))
            {
                // ① 만약 next_id 칸에 QUIT 라고 적혀있다면 즉시 시스템 종료 처리
                if (autoNextID.Equals("QUIT", System.StringComparison.OrdinalIgnoreCase))
                {
                    TriggerExitVisualNovel();
                    return;
                }

                // ② 특정 ID 주소가 적혀있다면 해당 행을 찾아서 다이렉트 자동 점프(합치기)
                int targetIndex = dialogueRows.FindIndex(row => row != null && row.id.Trim() == autoNextID);
                if (targetIndex >= 0)
                {
                    Debug.Log($"[자동 분기 이동] {currentRow.id}번 대사 종료 후 {autoNextID}번 행으로 자동 워프합니다.");
                    currentLineIndex = targetIndex;
                    PlayLine(currentLineIndex);
                    return;
                }
                else
                {
                    Debug.LogError($"[자동 이동 오류] CSV 파일 내에 주소 [{autoNextID}]를 가진 대사 행이 없습니다.");
                }
            }
        }

        // 별다른 자동 이동 규칙이 없다면 평소처럼 다음 행(+1)으로 순차 진행
        currentLineIndex++;

        if (currentLineIndex < dialogueRows.Count)
        {
            PlayLine(currentLineIndex);
        }
        else
        {
            TriggerExitVisualNovel();
        }
    }

    // 🌟 비주얼 노벨 종료 연출 공통 모듈화
    private void TriggerExitVisualNovel()
    {
        Debug.Log("비주얼 노벨 시스템 연출 종료");
        if (speakerText != null) speakerText.text = "";
        if (dialogueText != null) dialogueText.text = "이야기가 끝났습니다.";

        // 💡 나레이션 멈춤 후 타이틀 화면으로 완전히 씬 전환을 시키고 싶다면 
        // 여기에 UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene"); 코드를 넣으면 됩니다.
    }

    private void PlayLine(int index)
    {
        if (index < 0 || index >= dialogueRows.Count || dialogueRows[index] == null)
        {
            OnNextTarget();
            return;
        }

        isChoiceActive = false;
        if (choiceContainer != null) choiceContainer.SetActive(false);

        string effectKey = !string.IsNullOrEmpty(dialogueRows[index].effect) ? dialogueRows[index].effect.Trim() : "";
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (!effectKey.Equals("Whitein_BG", System.StringComparison.OrdinalIgnoreCase) &&
            !effectKey.Equals("Fadein_BG", System.StringComparison.OrdinalIgnoreCase))
        {
            if (bgEffectOverlayImage != null) bgEffectOverlayImage.color = new Color(0, 0, 0, 0f);
        }

        if (backgroundImage != null) backgroundImage.color = Color.white;
        if (bgDissolveTemp != null) bgDissolveTemp.gameObject.SetActive(false);

        DialogueRow row = dialogueRows[index];

        if (speakerText != null && !string.IsNullOrEmpty(row.speaker)) speakerText.text = row.speaker;
        else if (speakerText != null) speakerText.text = "";

        completeDialogue = !string.IsNullOrEmpty(row.dialogue) ? row.dialogue : "";
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeTextCoroutine(completeDialogue, row));

        bool isFadeBG = effectKey.Equals("Fadein_BG", System.StringComparison.OrdinalIgnoreCase);
        bool isDissolveBG = effectKey.Equals("Dissolve_BG", System.StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(row.background) && backgroundImage != null)
        {
            Sprite nextBgSprite = Resources.Load<Sprite>($"Backgrounds/{row.background}");
            if (nextBgSprite != null)
            {
                if (isDissolveBG && backgroundImage.sprite != null && backgroundImage.gameObject.activeSelf)
                    fadeCoroutine = StartCoroutine(DissolveImageCoroutine(backgroundImage, bgDissolveTemp, nextBgSprite, dissolveDuration));
                else
                {
                    backgroundImage.sprite = nextBgSprite;
                    backgroundImage.color = new Color(1f, 1f, 1f, 1f);
                }
            }
        }

        bool isFadeLeft = effectKey.Equals("Fadein_Left", System.StringComparison.OrdinalIgnoreCase);
        bool isFadeMid = effectKey.Equals("Fadein_Mid", System.StringComparison.OrdinalIgnoreCase);
        bool isFadeRight = effectKey.Equals("Fadein_Right", System.StringComparison.OrdinalIgnoreCase);

        bool isDissolveLeft = effectKey.Equals("Dissolve_Left", System.StringComparison.OrdinalIgnoreCase);
        bool isDissolveMid = effectKey.Equals("Dissolve_Mid", System.StringComparison.OrdinalIgnoreCase);
        bool isDissolveRight = effectKey.Equals("Dissolve_Right", System.StringComparison.OrdinalIgnoreCase);

        UpdateStandingCharacter(row.standingLeft, standingLeftImage, leftDissolveTemp, isFadeLeft, isDissolveLeft, dissolveStandingDuration);
        UpdateStandingCharacter(row.standingMid, standingMidImage, midDissolveTemp, isFadeMid, isDissolveMid, dissolveStandingDuration);
        UpdateStandingCharacter(row.standingRight, standingRightImage, rightDissolveTemp, isFadeRight, isDissolveRight, dissolveStandingDuration);

        if (!string.IsNullOrEmpty(row.bgm) && bgmSource != null)
        {
            if (row.bgm.Equals("StopBGM", System.StringComparison.OrdinalIgnoreCase))
            {
                bgmSource.Stop();
                currentBgmName = "";
            }
            else if (row.bgm != currentBgmName)
            {
                AudioClip bgmClip = Resources.Load<AudioClip>($"Audio/BGM/{row.bgm}");
                if (bgmClip != null)
                {
                    currentBgmName = row.bgm;
                    bgmSource.clip = bgmClip;
                    bgmSource.loop = true;
                    bgmSource.Play();
                }
            }
        }

        if (!string.IsNullOrEmpty(row.sfx) && sfxSource != null)
        {
            AudioClip sfxClip = Resources.Load<AudioClip>($"Audio/SFX/{row.sfx}");
            if (sfxClip != null) sfxSource.PlayOneShot(sfxClip);
        }

        if (!string.IsNullOrEmpty(effectKey))
        {
            if (effectKey.Equals("Fadein_BG", System.StringComparison.OrdinalIgnoreCase))
            {
                if (bgEffectOverlayImage != null)
                {
                    bgEffectOverlayImage.color = Color.black;
                    fadeCoroutine = StartCoroutine(OverlayFadeInCoroutine(bgEffectOverlayImage, Color.black, fadeOutDuration));
                }
            }
            else if (effectKey.Equals("FadeIn_Target", System.StringComparison.OrdinalIgnoreCase))
            {
                if (backgroundImage != null) fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(backgroundImage));
            }
            else if (effectKey.Equals("Whiteout_BG", System.StringComparison.OrdinalIgnoreCase))
            {
                if (bgEffectOverlayImage != null) fadeCoroutine = StartCoroutine(OverlayFadeCoroutine(bgEffectOverlayImage, Color.white, whiteoutDuration));
            }
            else if (effectKey.Equals("Whitein_BG", System.StringComparison.OrdinalIgnoreCase))
            {
                if (bgEffectOverlayImage != null)
                {
                    bgEffectOverlayImage.color = Color.white;
                    fadeCoroutine = StartCoroutine(OverlayFadeInCoroutine(bgEffectOverlayImage, Color.white, whiteinDuration));
                }
            }
            else if (effectKey.Equals("Fadeout_BG", System.StringComparison.OrdinalIgnoreCase))
            {
                if (bgEffectOverlayImage != null) fadeCoroutine = StartCoroutine(OverlayFadeCoroutine(bgEffectOverlayImage, Color.black, fadeOutDuration));
            }
            else if (isFadeLeft && standingLeftImage != null) fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(standingLeftImage));
            else if (isFadeMid && standingMidImage != null) fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(standingMidImage));
            else if (isFadeRight && standingRightImage != null) fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(standingRightImage));
        }
    }

    private void CheckAndShowChoices(DialogueRow row)
    {
        if (row == null || choiceContainer == null || choiceButtons == null || choiceTexts == null || choiceButtons.Length == 0) return;

        if (!string.IsNullOrEmpty(row.choice1))
        {
            isChoiceActive = true;
            choiceContainer.SetActive(true);
            SetButtonActive(0, row.choice1);
            SetButtonActive(1, row.choice2);
            SetButtonActive(2, row.choice3);
        }
    }

    private void SetButtonActive(int index, string choiceText)
    {
        if (index >= choiceButtons.Length || index >= choiceTexts.Length) return;
        if (!string.IsNullOrEmpty(choiceText))
        {
            choiceButtons[index].gameObject.SetActive(true);
            choiceTexts[index].text = choiceText;
        }
        else choiceButtons[index].gameObject.SetActive(false);
    }

    private void BindChoiceButtonEvents()
    {
        if (choiceButtons == null) return;
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int buttonIndex = i;
            if (choiceButtons[i] != null)
            {
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(buttonIndex));
            }
        }
    }

    private void OnChoiceSelected(int buttonIndex)
    {
        if (currentLineIndex >= dialogueRows.Count) return;
        DialogueRow currentRow = dialogueRows[currentLineIndex];
        string targetID = string.Empty;

        if (buttonIndex == 0) targetID = currentRow.nextId1;
        else if (buttonIndex == 1) targetID = currentRow.nextId2;
        else if (buttonIndex == 2) targetID = currentRow.nextId3;

        if (!string.IsNullOrEmpty(targetID))
        {
            int targetIndex = dialogueRows.FindIndex(row => row != null && row.id.Trim() == targetID.Trim());
            if (targetIndex >= 0)
            {
                choiceContainer.SetActive(false);
                isChoiceActive = false;
                currentLineIndex = targetIndex;
                PlayLine(currentLineIndex);
            }
            else
            {
                choiceContainer.SetActive(false);
                isChoiceActive = false;
                OnNextTarget();
            }
        }
        else
        {
            choiceContainer.SetActive(false);
            isChoiceActive = false;
            OnNextTarget();
        }
    }

    private void UpdateStandingCharacter(string spriteName, Image mainImage, Image tempImage, bool isFade, bool isDissolve, float duration)
    {
        if (mainImage == null) return;
        if (string.IsNullOrEmpty(spriteName))
        {
            mainImage.gameObject.SetActive(false);
            if (tempImage != null) tempImage.gameObject.SetActive(false);
        }
        else
        {
            Sprite nextSprite = Resources.Load<Sprite>($"Sprites/{spriteName}");
            if (nextSprite != null)
            {
                if (isDissolve && mainImage.gameObject.activeSelf && mainImage.sprite != null && mainImage.sprite != nextSprite)
                    fadeCoroutine = StartCoroutine(DissolveImageCoroutine(mainImage, tempImage, nextSprite, duration));
                else
                {
                    mainImage.gameObject.SetActive(true);
                    mainImage.sprite = nextSprite;
                    mainImage.color = new Color(1f, 1f, 1f, isFade ? 0f : 1f);
                }
            }
            else mainImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator OverlayFadeCoroutine(Image overlay, Color targetColor, float duration)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            overlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
            yield return null;
        }
        overlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, 1f);
    }

    private IEnumerator OverlayFadeInCoroutine(Image overlay, Color targetColor, float duration)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1.0f - (elapsed / duration));
            overlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
            yield return null;
        }
        overlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
    }

    private IEnumerator DissolveImageCoroutine(Image mainImage, Image tempImage, Sprite nextSprite, float duration)
    {
        if (mainImage == null || tempImage == null || nextSprite == null) yield break;
        tempImage.gameObject.SetActive(true);
        tempImage.sprite = mainImage.sprite;
        tempImage.color = new Color(1, 1, 1, 1f);
        mainImage.gameObject.SetActive(true);
        mainImage.sprite = nextSprite;
        mainImage.color = new Color(1, 1, 1, 0f);

        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            mainImage.color = new Color(1, 1, 1, progress);
            tempImage.color = new Color(1, 1, 1, 1f - progress);
            yield return null;
        }
        mainImage.color = new Color(1, 1, 1, 1f);
        tempImage.color = new Color(1, 1, 1, 0f);
        tempImage.gameObject.SetActive(false);
    }

    private IEnumerator FadeInTargetCoroutine(Image targetImage)
    {
        float elapsed = 0.0f;
        Color baseColor = targetImage.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            targetImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }
        targetImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
    }

    private IEnumerator TypeTextCoroutine(string line, DialogueRow row)
    {
        if (dialogueText == null) yield break;
        dialogueText.text = "";
        isTyping = true;
        foreach (char letter in line.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
        CheckAndShowChoices(row);
    }

    private void StopTypingAndShowFullText()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (dialogueText != null) dialogueText.text = completeDialogue;
        isTyping = false;
        if (currentLineIndex < dialogueRows.Count) CheckAndShowChoices(dialogueRows[currentLineIndex]);
    }
}