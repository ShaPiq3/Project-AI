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

    [Tooltip("선택지가 뜰 때 비활성화(숨김) 처리할 대사창(말상자) 부모 오브젝트를 드래그 앤 드롭해 주세요.")]
    [SerializeField] private GameObject dialogueWindow;

    [Header("Audio References")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambienceSource;

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
    private string currentAmbienceName = "";

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
        if (ambienceSource == null) Debug.LogWarning("[인스펙터 권장] 'Ambience Source'가 연결되지 않았습니다. 인스펙터에 오디오소스를 넣어주세요.");
        if (dialogueWindow == null) Debug.LogWarning("[인스펙터 권장] 'Dialogue Window' 슬롯이 비어있습니다. 선택지 출현 시 숨길 대사창(말상자) 부모 오브젝트를 꽂아주세요.");
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

    public void OnNextTarget()
    {
        if (currentLineIndex >= 0 && currentLineIndex < dialogueRows.Count)
        {
            DialogueRow currentRow = dialogueRows[currentLineIndex];
            string autoNextID = !string.IsNullOrEmpty(currentRow.nextId) ? currentRow.nextId.Trim() : string.Empty;

            if (!string.IsNullOrEmpty(autoNextID))
            {
                if (autoNextID.Equals("QUIT", System.StringComparison.OrdinalIgnoreCase))
                {
                    TriggerExitVisualNovel();
                    return;
                }

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

    private void TriggerExitVisualNovel()
    {
        Debug.Log("[시스템] 비주얼 노벨 시나리오 종료 -> 작동 정지 및 종료 처리");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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

        DialogueRow row = dialogueRows[index];

        // 🌟 [핵심 변경] 재생 직전, 이번 행에 선택지가 있는지 "선행 검사"합니다.
        bool hasChoice = !string.IsNullOrEmpty(row.choice1);

        if (dialogueWindow != null)
        {
            // 선택지가 있으면 처음부터 대사창을 켜지 않고 완전히 꺼둡니다. 텀이 생기지 않습니다.
            dialogueWindow.SetActive(!hasChoice);
        }

        string effectRaw = !string.IsNullOrEmpty(row.effect) ? row.effect.Trim() : "";
        List<string> activeEffects = new List<string>();
        if (!string.IsNullOrEmpty(effectRaw))
        {
            string[] splitEffects = effectRaw.Split(new char[] { ',', '/' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var fx in splitEffects)
            {
                activeEffects.Add(fx.Trim().ToLower());
            }
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (!activeEffects.Contains("whitein_bg") && !activeEffects.Contains("fadein_bg"))
        {
            if (bgEffectOverlayImage != null) bgEffectOverlayImage.color = new Color(0, 0, 0, 0f);
        }

        if (backgroundImage != null) backgroundImage.color = Color.white;
        if (bgDissolveTemp != null) bgDissolveTemp.gameObject.SetActive(false);

        if (speakerText != null && !string.IsNullOrEmpty(row.speaker)) speakerText.text = row.speaker;
        else if (speakerText != null) speakerText.text = "";

        completeDialogue = !string.IsNullOrEmpty(row.dialogue) ? row.dialogue : "";
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        // 🌟 선택지가 없을 때만 타이핑 연출을 시작합니다.
        if (!hasChoice)
        {
            typingCoroutine = StartCoroutine(TypeTextCoroutine(completeDialogue, row));
        }
        else
        {
            // 선택지가 있다면 텍스트 처리 단계를 즉시 통과시키고 선택지 버튼을 바로 출력합니다.
            if (dialogueText != null) dialogueText.text = "";
            CheckAndShowChoices(row);
        }

        bool isFadeBG = activeEffects.Contains("fadein_bg");
        bool isDissolveBG = activeEffects.Contains("dissolve_bg");

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

        bool isFadeLeft = activeEffects.Contains("fadein_left");
        bool isFadeMid = activeEffects.Contains("fadein_mid");
        bool isFadeRight = activeEffects.Contains("fadein_right");

        bool isDissolveLeft = activeEffects.Contains("dissolve_left");
        bool isDissolveMid = activeEffects.Contains("dissolve_mid");
        bool isDissolveRight = activeEffects.Contains("dissolve_right");

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

        if (!string.IsNullOrEmpty(row.ambience) && ambienceSource != null)
        {
            if (row.ambience.Equals("StopAmbience", System.StringComparison.OrdinalIgnoreCase))
            {
                ambienceSource.Stop();
                currentAmbienceName = "";
            }
            else if (row.ambience != currentAmbienceName)
            {
                AudioClip ambClip = Resources.Load<AudioClip>($"Audio/Ambience/{row.ambience}");
                if (ambClip != null)
                {
                    currentAmbienceName = row.ambience;
                    ambienceSource.clip = ambClip;
                    ambienceSource.loop = true;
                    ambienceSource.Play();
                }
            }
        }

        if (!string.IsNullOrEmpty(row.sfx) && sfxSource != null)
        {
            AudioClip sfxClip = Resources.Load<AudioClip>($"Audio/SFX/{row.sfx}");
            if (sfxClip != null) sfxSource.PlayOneShot(sfxClip);
        }

        if (activeEffects.Count > 0)
        {
            if (activeEffects.Contains("fadein_bg"))
            {
                if (bgEffectOverlayImage != null)
                {
                    bgEffectOverlayImage.color = Color.black;
                    fadeCoroutine = StartCoroutine(OverlayFadeInCoroutine(bgEffectOverlayImage, Color.black, fadeOutDuration));
                }
            }

            if (activeEffects.Contains("fadein_target"))
            {
                if (backgroundImage != null) fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(backgroundImage));
            }

            if (activeEffects.Contains("whiteout_bg"))
            {
                if (bgEffectOverlayImage != null) fadeCoroutine = StartCoroutine(OverlayFadeCoroutine(bgEffectOverlayImage, Color.white, whiteoutDuration));
            }

            if (activeEffects.Contains("whitein_bg"))
            {
                if (bgEffectOverlayImage != null)
                {
                    bgEffectOverlayImage.color = Color.white;
                    fadeCoroutine = StartCoroutine(OverlayFadeInCoroutine(bgEffectOverlayImage, Color.white, whiteinDuration));
                }
            }

            if (activeEffects.Contains("fadeout_bg"))
            {
                if (bgEffectOverlayImage != null) fadeCoroutine = StartCoroutine(OverlayFadeCoroutine(bgEffectOverlayImage, Color.black, fadeOutDuration));
            }

            if (isFadeLeft && standingLeftImage != null) fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(standingLeftImage));
            if (isFadeMid && standingMidImage != null) fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(standingMidImage));
            if (isFadeRight && standingRightImage != null) fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(standingRightImage));
        }
    }

    private void CheckAndShowChoices(DialogueRow row)
    {
        if (row == null || choiceContainer == null || choiceButtons == null || choiceTexts == null || choiceButtons.Length == 0) return;

        if (!string.IsNullOrEmpty(row.choice1))
        {
            isChoiceActive = true;
            choiceContainer.SetActive(true);

            if (dialogueWindow != null) dialogueWindow.SetActive(false);

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

                if (dialogueWindow != null) dialogueWindow.SetActive(true);

                currentLineIndex = targetIndex;
                PlayLine(currentLineIndex);
            }
            else
            {
                choiceContainer.SetActive(false);
                isChoiceActive = false;
                if (dialogueWindow != null) dialogueWindow.SetActive(true);
                OnNextTarget();
            }
        }
        else
        {
            choiceContainer.SetActive(false);
            isChoiceActive = false;
            if (dialogueWindow != null) dialogueWindow.SetActive(true);
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
                    mainImage.material = null;
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
            targetImage.color = new Color(baseColor.r, baseColor.b, baseColor.g, alpha);
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