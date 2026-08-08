// CollectibleImageIconAttacherEditorTool.cs
// -----------------------------------------------------------------------------
// 아카이브처럼 CSV가 아니라 프리팹에 손으로 배치된 콘텐츠에서, "이미지 생성
// 수집 대상이든 아니든" 모든 이미지가 단서 수집 모드에서 반응하게 하려면
// CollectibleImageIcon 컴포넌트가 각 이미지에 붙어있어야 합니다.
// 이 도구는 그 부착 작업을 손으로 하나씩 하는 대신 한 번에 처리해줍니다.
// (ClueHoverAttacherEditorTool과 동일한 패턴이지만, 이미지에만 붙습니다.)
//
// 사용법
// 1) 프로젝트 창에서 아카이브 프리팹 에셋을 선택하거나, 프리팹을 더블클릭해
//    Prefab Mode로 연 뒤 그 안의 특정 오브젝트(예: 이미지들이 들어있는 부모)를
//    하이어라키에서 선택합니다. 여러 개를 한 번에 선택해도 됩니다.
// 2) 상단 메뉴 Tools > Clue System > Attach Collectible Image To Selected (Include Children) 클릭
// 3) 선택한 오브젝트 및 그 하위의 모든 Image에 CollectibleImageIcon이 새로 붙습니다.
//    - 이미 CollectibleImageIcon이 붙어있는 곳(ArchiveCollectibleAutoBinder가
//      런타임에 붙이는 진짜 수집 대상 등)은 건드리지 않습니다.
//    - 닫기 버튼, 배경 이미지처럼 상호작용이 필요 없는 오브젝트에도 똑같이 붙을 수
//      있으니, 결과를 보고 필요 없는 곳은 인스펙터에서 컴포넌트만 지워주세요.
// 4) 진짜 이미지 생성 수집 대상인 오브젝트는 인스펙터에서 Image ID를
//    ImageGenSlotItems.csv의 ImageID와 동일하게 입력해주세요. 비워두면
//    "반응은 하지만 수집은 안 되는" 일반 이미지로 동작합니다.
//    (또는 ArchiveImageIDMap.csv에 이름을 등록해서 ArchiveCollectibleAutoBinder가
//    런타임에 자동으로 채우게 해도 됩니다.)
// 5) 프리팹 에셋을 직접 선택해서 실행했다면 자동으로 저장까지 됩니다.
//    Prefab Mode에서 실행했다면 Ctrl+S로 프리팹을 저장해주세요.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CollectibleImageIconAttacherEditorTool
{
    [MenuItem("Tools/Clue System/Attach Collectible Image To Selected (Include Children)")]
    private static void AttachToSelected()
    {
        GameObject[] selectedRoots = Selection.gameObjects;
        if (selectedRoots == null || selectedRoots.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "이미지 생성 상호작용 붙이기",
                "먼저 하이어라키(또는 프로젝트 창의 프리팹 에셋)에서 대상 오브젝트를 선택하세요.",
                "확인");
            return;
        }

        int imageCount = 0;
        int raycastFixedCount = 0;
        HashSet<Object> dirtyAssets = new HashSet<Object>();

        foreach (GameObject root in selectedRoots)
        {
            foreach (Image img in root.GetComponentsInChildren<Image>(true))
            {
                // 💡 [버그 수정] Raycast Target이 꺼져있으면 컴포넌트가 붙어있어도 클릭/호버 이벤트 자체가
                // 전달되지 않습니다. 스와치처럼 장식용으로 꺼둔 이미지가 많아서, 이미 컴포넌트가
                // 붙어있던 곳까지 포함해서 항상 강제로 켜줍니다.
                if (!img.raycastTarget)
                {
                    Undo.RecordObject(img, "Enable Raycast Target");
                    img.raycastTarget = true;
                    raycastFixedCount++;
                    EditorUtility.SetDirty(img);
                }

                // 이미 붙어있으면 컴포넌트 자체는 건드리지 않음 (기존에 태깅해둔 진짜 수집 대상 보존)
                if (img.GetComponent<CollectibleImageIcon>() != null) continue;

                Undo.AddComponent<CollectibleImageIcon>(img.gameObject);
                imageCount++;
                EditorUtility.SetDirty(img.gameObject);
                dirtyAssets.Add(img.gameObject);
            }

            EditorUtility.SetDirty(root);
        }

        // 선택한 것이 씬이 아니라 프리팹 에셋 자체였다면 여기서 바로 파일에 저장됩니다.
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "이미지 생성 상호작용 붙이기 완료",
            $"이미지 {imageCount}개에 CollectibleImageIcon을 새로 붙였습니다.\n" +
            $"Raycast Target이 꺼져있던 이미지 {raycastFixedCount}개도 함께 켰습니다(이미 컴포넌트가 붙어있던 곳 포함).\n\n" +
            "상호작용이 필요 없는 오브젝트(닫기 버튼, 배경 등)에 잘못 붙었다면 " +
            "해당 오브젝트를 선택해서 인스펙터에서 컴포넌트만 지워주세요.\n\n" +
            "Prefab Mode에서 실행했다면 Ctrl+S로 저장하는 것도 잊지 마세요.",
            "확인");
    }
}
