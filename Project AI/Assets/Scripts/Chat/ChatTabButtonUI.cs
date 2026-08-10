using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ChatTopBar에 동적으로 생성/파괴되는 대화방 탭 버튼. 기존에 씬에 있던 Chat_Button_1
/// (Toggle + CheckMark + Text(TMP) 구조)을 프리팹으로 만들어 재사용한다.
/// Toggle이 있으면 ToggleGroup으로 "탭 중 하나만 선택됨"을 자동으로 처리하고,
/// Toggle이 없는 구조라면 Button.onClick으로 대체 동작한다.
/// </summary>
public class ChatTabButtonUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [Tooltip("비워두면 같은 오브젝트에서 자동으로 찾음")]
    [SerializeField] private Toggle toggle;
    [Tooltip("Toggle이 없는 구조일 때 대체로 쓰는 Button. 비워두면 같은 오브젝트에서 자동으로 찾음")]
    [SerializeField] private Button button;

    public string ContactID { get; private set; }

    public void Setup(string contactID, string displayName, ToggleGroup group, Action<string> onClicked)
    {
        ContactID = contactID;
        if (nameText != null) nameText.text = displayName;

        if (toggle == null) toggle = GetComponent<Toggle>();
        if (button == null) button = GetComponent<Button>();

        if (toggle != null)
        {
            toggle.group = group;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn) onClicked?.Invoke(ContactID);
            });
        }
        else if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke(ContactID));
        }
        else
        {
            Debug.LogWarning("[ChatTabButtonUI] Toggle도 Button도 없어서 클릭에 반응할 수 없습니다.");
        }
    }

    /// <summary> 코드에서 포커스를 옮길 때 선택 표시만 갱신 (onValueChanged 재발화 없이). </summary>
    public void SetSelectedWithoutNotify(bool selected)
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(selected);
        }
    }
}
