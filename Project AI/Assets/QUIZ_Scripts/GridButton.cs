using UnityEngine;
using UnityEngine.UI;

public class GridButton : MonoBehaviour
{
    [Header("UI References")]
    public GameObject outline; // 자식 오브젝트인 Outline

    private int buttonIndex;
    private ImageGameManager manager;
    private bool isSelected = false;

    public void Setup(int index, ImageGameManager gameManager)
    {
        buttonIndex = index;
        manager = gameManager;

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnButtonClick);
        }

        ResetButton();
    }

    private void OnButtonClick()
    {
        if (isSelected)
        {
            SetSelectState(false);
            manager.OnButtonDeselected(buttonIndex);
        }
        else
        {
            // 매니저에게 현재 정답 개수만큼 더 선택할 수 있는지 체크
            if (manager.CanSelectMore())
            {
                SetSelectState(true);
                manager.OnButtonSelected(buttonIndex);
            }
        }
    }

    public void SetSelectState(bool select)
    {
        isSelected = select;
        if (outline != null)
        {
            outline.SetActive(select);
        }
    }

    public void ResetButton()
    {
        SetSelectState(false);
    }
}