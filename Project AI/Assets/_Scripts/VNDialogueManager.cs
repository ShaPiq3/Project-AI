using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

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

    [Tooltip("선택지 출현 시 혹은 특정 이펙트(hide_window) 재생 시 숨길 대사창(말상자) 부모 오브젝트를 드래그 앤 드롭해 주세요.")]
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

    [Header("Scene Transition")]
    [Tooltip("대화가 끝났을 때 자동으로 이동할 씬 이름. Build Settings에 등록된 정확한 씬 이름과 일치해야 합니다.")]
    [SerializeField] private string nextSceneName = "MainScene";


    private List<DialogueRow> dialogueRows;
    private int currentLineIndex = 0;
    private string currentBgmName = "";
    private string currentAmbienceName = "";

    private Coroutine typingCoroutine;
    private Coroutine fadeCoroutine;
    private Coroutine effectSequenceCoroutine;
    private bool isTyping = false;
    private string completeDialogue = "";

    private bool isChoiceActive = false;
    private bool isEffectPlaying = false;

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
            bgDissolveTemp = CreateDissolveTempObject(backgroundImage, "BG_Dissolve_Temp");

        if (standingLeftImage != null)
            leftDissolveTemp = CreateDissolveTempObject(standingLeftImage, "Left_Dissolve_Temp");

        if (standingMidImage != null)
            midDissolveTemp = CreateDissolveTempObject(standingMidImage, "Mid_Dissolve_Temp");

        if (standingRightImage != null)
            rightDissolveTemp = CreateDissolveTempObject(standingRightImage, "Right_Dissolve_Temp");
    }

    void Start()
    {
        CheckInspectorAssignments();
        dialogueRows = VNCSVParser.ParseCSV(csvFileName);

        if (dialogueRows != null)
        {
            dialogueRows.RemoveAll(row => row == null || (string.IsNullOrEmpty(row.speaker) && string.IsNullOrEmpty(row.dialogue) && string.IsNullOrEmpty(row.effect) && string.IsNullOrEmpty(row.choice1)));
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
        // 선택지가 활성화되어 있거나 연출이 재생 중일 때는 모든 입력 무시
        if (isChoiceActive || isEffectPlaying) return;

#if ENABLE_INPUT_SYSTEM
        // 🌟 Unity New Input System 사용 시 (스페이스바 OR 마우스 좌클릭 OR 터치)
        bool spacePressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
        bool mousePressed = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;

        if (spacePressed || mousePressed)
        {
            HandleInput();
        }
#else
    // 🌟 Unity Legacy Input 사용 시 (스페이스바 OR 마우스 좌클릭)
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

    private Image CreateDissolveTempObject(Image sourceImage, string name)
    {
        if (sourceImage == null) return null;

        Image tempImg = Instantiate(sourceImage, sourceImage.transform.parent);
        tempImg.name = name;
        tempImg.transform.SetSiblingIndex(sourceImage.transform.GetSiblingIndex());

        var extraManager = tempImg.GetComponent<VNDialogueManager>();
        if (extraManager != null) Destroy(extraManager);

        tempImg.color = new Color(1f, 1f, 1f, 0f);
        tempImg.gameObject.SetActive(false);
        return tempImg;
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
        if (isChoiceActive || isEffectPlaying) return;
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
        Debug.Log($"[시스템] 비주얼 노벨 시나리오 종료 -> {nextSceneName}씬으로 전환");

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[VNDialogueManager] nextSceneName이 비어있습니다! Inspector에서 이동할 씬 이름을 지정해주세요.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
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

        bool hasChoice = !string.IsNullOrEmpty(row.choice1);

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

        bool hasDialogueText = !string.IsNullOrEmpty(row.dialogue);
        bool hasActiveEffects = (activeEffects.Count > 0);

        // 🌟 [명령어 구별] 임시 숨김(hide_window)과 무조건 유지 숨김(hide_window_keep) 분석
        bool shouldHideWindow = activeEffects.Contains("hide_window");
        bool shouldKeepHideWindow = activeEffects.Contains("hide_window_keep");

        if (dialogueWindow != null)
        {
            // 둘 중 하나라도 걸려있으면 연출을 위해 대사창을 강제로 꺼둡니다.
            if ((hasActiveEffects && hasDialogueText) || shouldHideWindow || shouldKeepHideWindow)
            {
                dialogueWindow.SetActive(false);
            }
            else
            {
                dialogueWindow.SetActive(!hasChoice);
            }
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (effectSequenceCoroutine != null) StopCoroutine(effectSequenceCoroutine);

        if (!activeEffects.Contains("whitein_bg") && !activeEffects.Contains("fadein_bg"))
        {
            if (bgEffectOverlayImage != null) bgEffectOverlayImage.color = new Color(0, 0, 0, 0f);
        }

        if (backgroundImage != null) backgroundImage.color = Color.white;
        if (bgDissolveTemp != null) bgDissolveTemp.gameObject.SetActive(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        effectSequenceCoroutine = StartCoroutine(PlayEffectsAndDialogueSequence(row, activeEffects, hasChoice, shouldHideWindow, shouldKeepHideWindow));
    }

    private IEnumerator PlayEffectsAndDialogueSequence(DialogueRow row, List<string> activeEffects, bool hasChoice, bool shouldHideWindow, bool shouldKeepHideWindow)
    {
        isEffectPlaying = true;

        float maxEffectDuration = 0f;

        // 1. 배경 이미지 변경 연출
        bool isFadeBG = activeEffects.Contains("fadein_bg");
        bool isDissolveBG = activeEffects.Contains("dissolve_bg");

        if (!string.IsNullOrEmpty(row.background) && backgroundImage != null)
        {
            Sprite nextBgSprite = Resources.Load<Sprite>($"Backgrounds/{row.background}");
            if (nextBgSprite != null)
            {
                if (isDissolveBG && backgroundImage.sprite != nextBgSprite && backgroundImage.gameObject.activeSelf)
                {
                    fadeCoroutine = StartCoroutine(DissolveImageCoroutine(backgroundImage, bgDissolveTemp, nextBgSprite, dissolveDuration));
                    maxEffectDuration = Mathf.Max(maxEffectDuration, dissolveDuration);
                }
                else
                {
                    backgroundImage.sprite = nextBgSprite;
                    backgroundImage.color = new Color(1f, 1f, 1f, 1f);
                }
            }
        }

        // 2. 캐릭터 스탠딩 이미지 변경 연출
        bool isFadeLeft = activeEffects.Contains("fadein_left");
        bool isFadeMid = activeEffects.Contains("fadein_mid");
        bool isFadeRight = activeEffects.Contains("fadein_right");

        bool isDissolveLeft = activeEffects.Contains("dissolve_left");
        bool isDissolveMid = activeEffects.Contains("dissolve_mid");
        bool isDissolveRight = activeEffects.Contains("dissolve_right");

        if (isDissolveLeft || isDissolveMid || isDissolveRight)
        {
            maxEffectDuration = Mathf.Max(maxEffectDuration, dissolveStandingDuration);
        }

        UpdateStandingCharacter(row.standingLeft, standingLeftImage, leftDissolveTemp, isFadeLeft, isDissolveLeft, dissolveStandingDuration);
        UpdateStandingCharacter(row.standingMid, standingMidImage, midDissolveTemp, isFadeMid, isDissolveMid, dissolveStandingDuration);
        UpdateStandingCharacter(row.standingRight, standingRightImage, rightDissolveTemp, isFadeRight, isDissolveRight, dissolveStandingDuration);

        // 3. 오디오 연출 재생
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

        // 4. 화면 이펙트 연출 재생
        if (activeEffects.Count > 0)
        {
            if (activeEffects.Contains("fadein_bg"))
            {
                if (bgEffectOverlayImage != null)
                {
                    bgEffectOverlayImage.color = Color.black;
                    fadeCoroutine = StartCoroutine(OverlayFadeInCoroutine(bgEffectOverlayImage, Color.black, fadeOutDuration));
                    maxEffectDuration = Mathf.Max(maxEffectDuration, fadeOutDuration);
                }
            }

            if (activeEffects.Contains("fadein_target"))
            {
                if (backgroundImage != null)
                {
                    fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(backgroundImage));
                    maxEffectDuration = Mathf.Max(maxEffectDuration, fadeDuration);
                }
            }

            if (activeEffects.Contains("whiteout_bg"))
            {
                if (bgEffectOverlayImage != null)
                {
                    fadeCoroutine = StartCoroutine(OverlayFadeCoroutine(bgEffectOverlayImage, Color.white, whiteoutDuration));
                    maxEffectDuration = Mathf.Max(maxEffectDuration, whiteoutDuration);
                }
            }

            if (activeEffects.Contains("whitein_bg"))
            {
                if (bgEffectOverlayImage != null)
                {
                    bgEffectOverlayImage.color = Color.white;
                    fadeCoroutine = StartCoroutine(OverlayFadeInCoroutine(bgEffectOverlayImage, Color.white, whiteinDuration));
                    maxEffectDuration = Mathf.Max(maxEffectDuration, whiteinDuration);
                }
            }

            if (activeEffects.Contains("fadeout_bg"))
            {
                if (bgEffectOverlayImage != null)
                {
                    fadeCoroutine = StartCoroutine(OverlayFadeCoroutine(bgEffectOverlayImage, Color.black, fadeOutDuration));
                    maxEffectDuration = Mathf.Max(maxEffectDuration, fadeOutDuration);
                }
            }

            if (isFadeLeft && standingLeftImage != null)
            {
                fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(standingLeftImage));
                maxEffectDuration = Mathf.Max(maxEffectDuration, fadeDuration);
            }
            if (isFadeMid && standingMidImage != null)
            {
                fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(standingMidImage));
                maxEffectDuration = Mathf.Max(maxEffectDuration, fadeDuration);
            }
            if (isFadeRight && standingRightImage != null)
            {
                fadeCoroutine = StartCoroutine(FadeInTargetCoroutine(standingRightImage));
                maxEffectDuration = Mathf.Max(maxEffectDuration, fadeDuration);
            }
        }

        // 5. 모든 연출 재생이 끝날 때까지 대기
        if (maxEffectDuration > 0f)
        {
            yield return new WaitForSeconds(maxEffectDuration);
        }

        isEffectPlaying = false;

        bool hasDialogueText = !string.IsNullOrEmpty(row.dialogue);

        if (!hasChoice)
        {
            if (speakerText != null && !string.IsNullOrEmpty(row.speaker))
            {
                speakerText.text = row.speaker;
            }

            // 🌟 [조건 처리] 'hide_window_keep'을 사용했다면 연출이 끝나도 대사창을 활성화하지 않습니다.
            // 다음 마우스 클릭이나 키보드 입력이 일어날 때까지 화면은 대사창이 없는 채로 유지됩니다.
            if (dialogueWindow != null)
            {
                dialogueWindow.SetActive(!shouldKeepHideWindow);
            }

            // 'hide_window_keep'일 때는 타이핑을 생략하거나 바로 다음 줄 클릭 대기 상태로 이행합니다.
            if (hasDialogueText && !shouldKeepHideWindow)
            {
                completeDialogue = row.dialogue;
                typingCoroutine = StartCoroutine(TypeTextCoroutine(completeDialogue, row));
            }
            else
            {
                isTyping = false;
            }
        }
        else
        {
            if (dialogueText != null) dialogueText.text = "";
            CheckAndShowChoices(row);
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
            return;
        }

        Sprite nextSprite = Resources.Load<Sprite>($"Sprites/{spriteName}");
        if (nextSprite == null)
        {
            mainImage.gameObject.SetActive(false);
            if (tempImage != null) tempImage.gameObject.SetActive(false);
            return;
        }

        if (mainImage.gameObject.activeSelf && mainImage.sprite == nextSprite)
        {
            return;
        }

        if (isDissolve)
        {
            fadeCoroutine = StartCoroutine(DissolveImageCoroutine(mainImage, tempImage, nextSprite, duration));
        }
        else
        {
            mainImage.gameObject.SetActive(true);
            mainImage.sprite = nextSprite;
            mainImage.color = new Color(1f, 1f, 1f, isFade ? 0f : 1f);
            mainImage.material = null;
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

        if (mainImage.gameObject.activeSelf && mainImage.sprite != null)
        {
            tempImage.gameObject.SetActive(true);
            tempImage.sprite = mainImage.sprite;
            tempImage.color = new Color(1f, 1f, 1f, 1f);
        }
        else
        {
            tempImage.gameObject.SetActive(false);
        }

        mainImage.color = new Color(1f, 1f, 1f, 0f);
        mainImage.sprite = nextSprite;
        mainImage.gameObject.SetActive(true);

        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            mainImage.color = new Color(1f, 1f, 1f, progress);
            yield return null;
        }

        mainImage.color = new Color(1f, 1f, 1f, 1f);
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