using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특정 말풍선이 나올 때 팝업으로 뜨는 패널에 붙이는 범용 컴포넌트.
/// 가이드 패널, 안내 팝업 등 종류 상관없이 재사용 가능합니다.
/// panelID를 대화 CSV의 특정 컬럼값과 일치시키면, 해당 말풍선이 나올 때 이 패널이 열립니다.
/// </summary>
public class PopupPanelController : MonoBehaviour
{
    [Tooltip("대화 CSV에서 이 패널을 지정할 때 쓰는 고유 ID")]
    [SerializeField] private string panelID;
    public string PanelID => panelID;

    [SerializeField] private Button closeButton;
    [SerializeField] private AudioSource openAudioSource;

    [Header("팝업 애니메이션 설정")]
    [SerializeField] private float animationSpeed = 15f;
    [SerializeField] private float scalePunchMultiplier = 1.1f;

    private static readonly Dictionary<string, PopupPanelController> registry = new Dictionary<string, PopupPanelController>();

    private Coroutine popUpCoroutine;
    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (!string.IsNullOrEmpty(panelID))
        {
            registry[panelID] = this;
        }
    }

    private void Start()
    {

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(panelID) && registry.TryGetValue(panelID, out var self) && self == this)
        {
            registry.Remove(panelID);
        }
    }

    /// <summary>
    /// 대화 CSV의 panelID와 일치하는 패널을 찾아서 반환합니다.
    /// </summary>
    public static PopupPanelController GetByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (registry.TryGetValue(id, out var panel) && panel != null)
        {
            return panel;
        }

        // 비활성화된 오브젝트라 Awake가 아직 안 됐을 수도 있으니 씬 전체에서 한 번 더 탐색
#pragma warning disable CS0618
        PopupPanelController[] all = FindObjectsByType<PopupPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#pragma warning restore CS0618
        foreach (var p in all)
        {
            if (p.panelID == id)
            {
                registry[id] = p;
                return p;
            }
        }

        return null;
    }

    public void OpenPanel()
    {
        Transform current = transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            // 💡 진단: 부모 체인에 CanvasGroup이 있는지, alpha가 얼마인지 확인
            CanvasGroup cg = current.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                Debug.Log($"[진단] {current.name}에 CanvasGroup 있음! alpha:{cg.alpha}, interactable:{cg.interactable}, blocksRaycasts:{cg.blocksRaycasts}");
            }

            current = current.parent;
        }

        transform.SetAsLastSibling();

        RectTransform rt = GetComponent<RectTransform>();
        Debug.Log($"[진단] OpenPanel 최종 상태 - activeInHierarchy:{gameObject.activeInHierarchy}, " +
            $"anchoredPosition:{rt.anchoredPosition}, sizeDelta:{rt.sizeDelta}, " +
            $"lossyScale:{transform.lossyScale}");

        if (openAudioSource != null) openAudioSource.Play();

        if (popUpCoroutine != null) StopCoroutine(popUpCoroutine);
        popUpCoroutine = StartCoroutine(AnimatePopUp());
    }

    public void ClosePanel()
    {
        if (popUpCoroutine != null) StopCoroutine(popUpCoroutine);
        transform.localScale = originalScale;

        gameObject.SetActive(false);
    }

    private IEnumerator AnimatePopUp()
    {
        Vector3 targetMaxScale = originalScale * scalePunchMultiplier;

        while (Vector3.Distance(transform.localScale, targetMaxScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetMaxScale, Time.deltaTime * animationSpeed);
            yield return null;
        }
        transform.localScale = targetMaxScale;

        while (Vector3.Distance(transform.localScale, originalScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * animationSpeed);
            yield return null;
        }
        transform.localScale = originalScale;
    }
}