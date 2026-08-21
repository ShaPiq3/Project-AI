using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HUMANDB CSV(HumanDBData.csv)를 읽어 이름으로 검색하고, 검색된 인물의 상세 패널(HumanDBCard)을
/// 열어주는 매니저. 뉴스/커뮤니티와 동일하게, 같은 인물을 또 검색하면 새 창을 만들지 않고
/// 이미 열려있는 창을 맨 앞으로 가져온다.
/// </summary>
public class HumanDBManager : MonoBehaviour
{
    public static HumanDBManager Instance { get; private set; }

    [Header("Data (Resources 폴더의 CSV 파일명, 확장자 제외)")]
    [SerializeField] private string csvFileName = "HumanDBData";

    [Header("Detail Popup Reference")]
    [SerializeField] private HumanDBCard detailPopup;

    [Header("WindowManager 연동")]
    [SerializeField] private WindowManager windowManager;

    private readonly List<HumanData> allHumanData = new List<HumanData>();
    private readonly Dictionary<string, HumanData> lookupByName = new Dictionary<string, HumanData>();

    // 이름별로 "지금 열려있는 창"을 추적 (같은 이름을 다시 검색하면 새로 만들지 않음)
    private readonly Dictionary<string, HumanDBCard> openWindows = new Dictionary<string, HumanDBCard>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (detailPopup != null) detailPopup.gameObject.SetActive(false);
        LoadCsv();
        Debug.Log($"[진단] HumanDBManager 로드 완료. 인물 수:{allHumanData.Count}, detailPopup 연결됨:{detailPopup != null}, windowManager 연결됨:{windowManager != null}");
    }

    private void LoadCsv()
    {
        List<Dictionary<string, object>> rows = CSVReader.Read(csvFileName);

        foreach (var row in rows)
        {
            HumanData data = new HumanData
            {
                id = GetString(row, "id"),
                name = GetString(row, "name"),
                birth = GetString(row, "birth"),
                gender = GetString(row, "gender"),
                bloodType = GetString(row, "bloodType"),
                occupation = GetString(row, "occupation"),
                contact = GetString(row, "contact"),
                email = GetString(row, "email"),
                address = GetString(row, "address"),
                note = GetString(row, "note"),
                imageName = GetString(row, "imageName"),
                addressClueID = GetString(row, "addressClueID"),
                occupationClueID = GetString(row, "occupationClueID"),
                noteClueIDs = GetString(row, "noteClueIDs"),
            };

            if (string.IsNullOrEmpty(data.name)) continue;

            allHumanData.Add(data);

            string key = NormalizeName(data.name);
            if (!lookupByName.ContainsKey(key))
                lookupByName.Add(key, data);
            else
                Debug.LogWarning($"[HumanDBManager] 이름 중복: '{data.name}'");
        }
    }

    private static string GetString(Dictionary<string, object> row, string key)
    {
        return row.TryGetValue(key, out object value) ? value.ToString() : "";
    }

    private static string NormalizeName(string name)
    {
        return string.IsNullOrEmpty(name) ? "" : name.Trim().Replace(" ", "").ToUpper();
    }

    /// <summary>
    /// 검색창에서 입력한 이름과 정확히 일치하는 인물을 찾아 상세 패널을 엽니다.
    /// </summary>
    /// <returns>일치하는 인물을 찾아서 열었으면 true, 없으면 false</returns>
    public bool SearchByName(string name)
    {
        string key = NormalizeName(name);
        if (string.IsNullOrEmpty(key)) return false;

        if (!lookupByName.TryGetValue(key, out HumanData data)) return false;

        OpenDetailPopup(data);
        return true;
    }

    public void OpenDetailPopup(HumanData data)
    {
        if (detailPopup == null)
        {
            Debug.LogWarning("[진단] HumanDBManager의 Detail Popup 필드가 비어있어서 상세 패널을 열 수 없습니다!");
            return;
        }
        if (data == null) return;

        if (openWindows.TryGetValue(data.name, out HumanDBCard existingWindow) && existingWindow != null)
        {
            // 💡 최소화(ToggleWindowImmediate)로 비활성화된 상태일 수 있으므로, 다시 켜준 뒤에 진행합니다.
            // (비활성 오브젝트에서 바로 애니메이션 코루틴을 돌리면 에러가 납니다.)
            if (!existingWindow.gameObject.activeSelf) existingWindow.gameObject.SetActive(true);

            existingWindow.transform.SetAsLastSibling();

            PopupSpawnAnimation existingAnim = existingWindow.GetComponent<PopupSpawnAnimation>();
            if (existingAnim != null) existingAnim.PlayPopAnimation();

            return;
        }

        HumanDBCard newWindow = Instantiate(detailPopup, detailPopup.transform.parent);
        newWindow.gameObject.SetActive(true);
        newWindow.SetHumanData(data);

        openWindows[data.name] = newWindow;

        if (windowManager != null)
        {
            RectTransform cardRect = newWindow.GetComponent<RectTransform>();
            windowManager.RepositionPopupWindow(cardRect);
        }

        PopupSpawnAnimation newAnim = newWindow.GetComponent<PopupSpawnAnimation>();
        if (newAnim != null) newAnim.PlayPopAnimation();
    }

    public void NotifyWindowClosed(string name)
    {
        openWindows.Remove(name);
    }

    /// <summary>
    /// 💡 [추가] DataLogManager가 "원본 보기"를 요청할 때 호출하는 함수.
    /// clueID로 어느 인물의 어느 필드(주소/직업/특이사항)에서 나온 단서인지 찾아서
    /// 해당 인물의 상세 패널을 열어줍니다.
    /// </summary>
    /// <returns>원본을 찾아서 열었으면 true, 못 찾았으면 false</returns>
    public bool TryOpenClueSource(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return false;

        foreach (var data in allHumanData)
        {
            bool isAddressMatch = !string.IsNullOrEmpty(data.addressClueID) && data.addressClueID == clueID;
            bool isOccupationMatch = !string.IsNullOrEmpty(data.occupationClueID) && data.occupationClueID == clueID;
            bool isNoteMatch = NoteClueIDsContains(data.noteClueIDs, clueID);

            if (isAddressMatch || isOccupationMatch || isNoteMatch)
            {
                OpenDetailPopup(data);
                return true;
            }
        }

        return false;
    }

    private static bool NoteClueIDsContains(string noteClueIDs, string clueID)
    {
        if (string.IsNullOrEmpty(noteClueIDs)) return false;

        string[] ids = noteClueIDs.Split('|');
        foreach (string id in ids)
        {
            if (id.Trim() == clueID) return true;
        }
        return false;
    }
}
