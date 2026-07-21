using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아카이브 창은 아이템이 씬/프리팹에 수동으로 배치되어 있으므로,
/// 오브젝트 자체를 없앨 수는 없지만 "어떤 ImageID 를 가지는가" 는
/// 이 스크립트가 ArchiveImageIDMap.csv 를 읽어서 자동으로 붙여줍니다.
///
/// 사용법:
/// 1) 아카이브 창의 최상위(모든 이미지 오브젝트를 자식으로 포함하는) 오브젝트에 이 스크립트를 붙임
/// 2) archiveIDMapCsv 에 ArchiveImageIDMap.csv 연결
/// 3) 아카이브에 새 단서 이미지를 배치할 때는:
///      - 오브젝트 이름을 CSV의 ObjectName 과 동일하게 짓고
///      - CSV에 그 이름 + ImageID 한 줄만 추가하면 끝 (컴포넌트를 손으로 붙이거나
///        Inspector에서 imageID 를 타이핑할 필요 없음)
/// </summary>
public class ArchiveCollectibleAutoBinder : MonoBehaviour
{
    [SerializeField] private TextAsset archiveIDMapCsv; // ArchiveImageIDMap.csv

    void Start()
    {
        BindAll();
    }

    private void BindAll()
    {
        if (archiveIDMapCsv == null) return;

        var map = ParseCsv(archiveIDMapCsv);

        // 자기 자신 하위의 모든 자식(비활성 포함)을 이름으로 찾아서 매칭
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        Dictionary<string, Transform> byName = new Dictionary<string, Transform>();
        foreach (var t in allChildren)
        {
            // 같은 이름이 여러 개면 첫 번째만 사용 (이름은 유일하게 지어주세요)
            if (!byName.ContainsKey(t.name)) byName.Add(t.name, t);
        }

        foreach (var kv in map)
        {
            string objectName = kv.Key;
            string imageID = kv.Value;

            if (!byName.TryGetValue(objectName, out Transform target))
            {
                Debug.LogWarning($"[ArchiveCollectibleAutoBinder] 오브젝트 '{objectName}' 를 아카이브 하위에서 찾을 수 없습니다.");
                continue;
            }

            CollectibleImageIcon icon = target.GetComponent<CollectibleImageIcon>();
            if (icon == null) icon = target.gameObject.AddComponent<CollectibleImageIcon>();
            icon.Init(imageID);
        }
    }

    private Dictionary<string, string> ParseCsv(TextAsset csv)
    {
        var result = new Dictionary<string, string>();
        string[] rows = csv.text.Replace("\r", "").Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < rows.Length; i++) // 0번째는 헤더
        {
            string[] c = rows[i].Split(',');
            if (c.Length < 2) continue;

            string objectName = c[0].Trim();
            string imageID = c[1].Trim();
            if (string.IsNullOrEmpty(objectName)) continue;

            result[objectName] = imageID;
        }

        return result;
    }
}