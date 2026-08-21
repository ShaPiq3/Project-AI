using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUMANDB 검색 결과로 열리는 인물 상세 패널(1번 목업 이미지)에 붙는 컴포넌트.
/// </summary>
public class HumanDBCard : MonoBehaviour
{
    [Header("상세 정보 UI 컴포넌트")]
    [SerializeField] private Image profileImage;
    [SerializeField] private GameObject profileImagePlaceholder; // 사진이 없을 때 보여줄 "이미지" 플레이스홀더
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI birthText;
    [SerializeField] private TextMeshProUGUI genderText;
    [SerializeField] private TextMeshProUGUI bloodTypeText;
    [SerializeField] private TextMeshProUGUI occupationText;
    [SerializeField] private TextMeshProUGUI contactText;
    [SerializeField] private TextMeshProUGUI emailText;
    [SerializeField] private TextMeshProUGUI addressText;

    [Header("특이사항 (뉴스 본문처럼 항목 수만큼 동적으로 생성됨)")]
    [Tooltip("복제할 원본 템플릿 (비활성 상태로 둘 것)")]
    [SerializeField] private TextMeshProUGUI noteValueTemplate;
    [Tooltip("생성된 항목들이 들어갈 부모 (보통 NoteSection)")]
    [SerializeField] private Transform noteContainer;

    private readonly List<TextMeshProUGUI> spawnedNoteValues = new List<TextMeshProUGUI>();

    private string currentName;

    public void SetHumanData(HumanData data)
    {
        currentName = data.name;

        if (nameText != null) nameText.text = data.name;
        if (birthText != null) birthText.text = data.birth;
        if (genderText != null) genderText.text = data.gender;
        if (bloodTypeText != null) bloodTypeText.text = data.bloodType;
        if (contactText != null) contactText.text = data.contact;
        if (emailText != null) emailText.text = data.email;

        if (occupationText != null)
        {
            occupationText.text = data.occupation;
            ConfigureClueText(occupationText, data.occupationClueID, data.name);
        }

        if (addressText != null)
        {
            addressText.text = data.address;
            ConfigureClueText(addressText, data.addressClueID, data.name);
        }

        SetupNoteValues(data);

        TaskbarWindowTrigger taskbarTrigger = GetComponent<TaskbarWindowTrigger>();
        if (taskbarTrigger != null) taskbarTrigger.SetWindowTitle(data.name);

        LoadProfileImage(data.imageName);
    }

    // 특이사항을 '|' 기준으로 나눠서, 뉴스 본문처럼 항목 수만큼 텍스트를 복제해서 채웁니다.
    // 항목별 clueID도 같은 순서로 매칭됩니다. 개수 제한 없음.
    private void SetupNoteValues(HumanData data)
    {
        ClearSpawnedNoteValues();

        if (noteValueTemplate == null || noteContainer == null) return;
        noteValueTemplate.gameObject.SetActive(false);

        if (string.IsNullOrEmpty(data.note)) return;

        string[] items = data.note.Split('|');
        string[] clueIDs = string.IsNullOrEmpty(data.noteClueIDs)
            ? new string[0]
            : data.noteClueIDs.Split('|');

        int number = 1;
        for (int i = 0; i < items.Length; i++)
        {
            string item = items[i].Trim();
            if (string.IsNullOrEmpty(item)) continue;

            TextMeshProUGUI newText = Instantiate(noteValueTemplate, noteContainer);
            newText.text = $"{number}. {item}";
            newText.gameObject.SetActive(true);

            string clueID = i < clueIDs.Length ? clueIDs[i].Trim() : "";
            ConfigureClueText(newText, clueID, data.name);

            spawnedNoteValues.Add(newText);
            number++;
        }
    }

    private void ClearSpawnedNoteValues()
    {
        foreach (var text in spawnedNoteValues)
        {
            if (text != null) Destroy(text.gameObject);
        }
        spawnedNoteValues.Clear();
    }

    // 뉴스/커뮤니티 등과 동일한 방식: ClueTextHoverEffect를 붙여서 단서 수집 모드에서 반응하게 합니다.
    // clueID가 비어있어도(단서가 아닌 일반 항목이어도) 정상 동작합니다.
    private void ConfigureClueText(TextMeshProUGUI text, string clueID, string sourceTitleOverride)
    {
        ClueTextHoverEffect hoverEffect = text.gameObject.GetComponent<ClueTextHoverEffect>();
        if (hoverEffect == null) hoverEffect = text.gameObject.AddComponent<ClueTextHoverEffect>();

        text.raycastTarget = true;
        hoverEffect.Configure(clueID, "", sourceTitleOverride);
    }

    private void LoadProfileImage(string imageName)
    {
        Sprite loadedSprite = string.IsNullOrEmpty(imageName)
            ? null
            : Resources.Load<Sprite>($"HumanDBImages/{imageName}");

        bool hasSprite = loadedSprite != null;

        if (profileImage != null)
        {
            profileImage.sprite = loadedSprite;
            profileImage.gameObject.SetActive(hasSprite);
        }

        if (profileImagePlaceholder != null) profileImagePlaceholder.SetActive(!hasSprite);
    }

    public void ClosePopup()
    {
        if (HumanDBManager.Instance != null)
        {
            HumanDBManager.Instance.NotifyWindowClosed(currentName);
        }

        Destroy(gameObject);
    }
}
