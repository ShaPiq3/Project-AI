using UnityEngine;

public class NewsTabManager : MonoBehaviour
{
    [Header("카테고리별 기사 리스트 패널들")]
    public GameObject politicsPanel; // 정치 패널
    public GameObject socialPanel;   // 사회 패널
    public GameObject economyPanel;  // 경제 패널

    [Header("각 탭 버튼 밑에 있는 강조선(밑줄) 오브젝트들")]
    // 💡 인스펙터창에서 각 버튼 아래에 만든 밑줄(Line)을 여기에 쏙 넣어줄 겁니다!
    public GameObject politicsLine;
    public GameObject socialLine;
    public GameObject economyLine;

    void Start()
    {
        // 게임 시작 시 기본적으로 '정치' 탭이 선택된 상태로 채워줍니다.
        SelectPoliticsTab();
    }

    // 🏛️ 정치 탭 클릭
    public void SelectPoliticsTab()
    {
        SetAllPanelsInactive();
        if (politicsPanel != null) politicsPanel.SetActive(true);

        // 정치 밑줄만 켜고, 나머지는 끕니다.
        SetLineStatus(true, false, false);
    }

    // 👥 사회 탭 클릭
    public void SelectSocialTab()
    {
        SetAllPanelsInactive();
        if (socialPanel != null) socialPanel.SetActive(true);

        // 사회 밑줄만 켜고, 나머지는 끕니다.
        SetLineStatus(false, true, false);
    }

    // 📈 경제 탭 클릭
    public void SelectEconomyTab()
    {
        SetAllPanelsInactive();
        if (economyPanel != null) economyPanel.SetActive(true);

        // 경제 밑줄만 켜고, 나머지는 끕니다.
        SetLineStatus(false, false, true);
    }

    // 기사 패널 리셋
    private void SetAllPanelsInactive()
    {
        if (politicsPanel != null) politicsPanel.SetActive(false);
        if (socialPanel != null) socialPanel.SetActive(false);
        if (economyPanel != null) economyPanel.SetActive(false);
    }

    // 밑줄 제어용 서브 함수
    private void SetLineStatus(bool poly, bool soc, bool eco)
    {
        if (politicsLine != null) politicsLine.SetActive(poly);
        if (socialLine != null) socialLine.SetActive(soc);
        if (economyLine != null) economyLine.SetActive(eco);
    }
}