using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CollectibleImageIcon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("ImageGenSlotItems.csv 의 ImageID 와 동일하게 입력")]
    public string imageID;

    [Header("자동 하이라이트 설정")]
    [SerializeField] private Color highlightOverlayColor = new Color(1f, 1f, 0f, 0.35f); // 반투명 노란색 덮개

    private GameObject generatedHighlightObj;

    private void Awake()
    {
        CreateHighlightObjectAutomatically();
    }

    /// <summary>
    /// 원본 Image 컴포넌트의 크기, RectTransform, Sprite를 그대로 복사하여 
    /// 자식 하이라이트 오브젝트를 코드로 자동 생성합니다.
    /// </summary>
    private void CreateHighlightObjectAutomatically()
    {
        Image myImage = GetComponent<Image>();
        if (myImage == null) return;

        // 1. 자식 GameObject 생성
        generatedHighlightObj = new GameObject("Auto_Highlight_Overlay");
        generatedHighlightObj.transform.SetParent(transform, false);

        // 2. RectTransform 설정 (부모 크기에 100% 딱 맞게 앵커 설정)
        RectTransform rect = generatedHighlightObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        // 3. Image 컴포넌트 설정 및 원본 Sprite 복사
        Image highlightImg = generatedHighlightObj.AddComponent<Image>();
        highlightImg.sprite = myImage.sprite;
        highlightImg.type = myImage.type; // Sliced 등 원본 타일링 형태 유지
        highlightImg.color = highlightOverlayColor;

        // 💡 매우 중요: 하이라이트 이미지가 마우스 커서를 가려서 이벤트가 튀지 않도록 끔
        highlightImg.raycastTarget = false;

        // 시작할 땐 꺼둠
        generatedHighlightObj.SetActive(false);
    }

    public void Init(string id)
    {
        imageID = id;
    }

    private bool IsInteractable(out string failReason)
    {
        if (DataLogManager.Instance == null)
        {
            failReason = "DataLogManager.Instance가 null입니다.";
            return false;
        }
        if (!DataLogManager.Instance.IsClueSearchModeActive)
        {
            failReason = "단서 수집 모드가 꺼져있습니다.";
            return false;
        }
        if (string.IsNullOrEmpty(imageID))
        {
            failReason = "imageID가 비어있습니다.";
            return false;
        }
        if (ImageGenerationManager.Instance == null)
        {
            failReason = "ImageGenerationManager.Instance가 null입니다.";
            return false;
        }
        if (!ImageGenerationManager.Instance.IsImageValidForCurrentQuest(imageID))
        {
            failReason = $"현재 퀘스트에 유효하지 않은 imageID('{imageID}')입니다.";
            return false;
        }
        if (ImageGenerationManager.Instance.IsImageAlreadyRegistered(imageID))
        {
            failReason = $"이미 슬롯에 등록된 imageID('{imageID}')입니다.";
            return false;
        }

        failReason = "통과";
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable(out _)) return;

        if (generatedHighlightObj != null)
        {
            generatedHighlightObj.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (generatedHighlightObj != null)
        {
            generatedHighlightObj.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable(out _)) return;

        if (generatedHighlightObj != null)
        {
            generatedHighlightObj.SetActive(false);
        }

        if (ImageGenerationManager.Instance != null)
        {
            ImageGenerationManager.Instance.RegisterImageToSlot(imageID);
        }
    }
}