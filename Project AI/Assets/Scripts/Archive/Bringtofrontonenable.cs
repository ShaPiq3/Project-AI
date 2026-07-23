using UnityEngine;

/// <summary>
/// 이 컴포넌트가 붙은 오브젝트는 활성화(SetActive(true))될 때마다
/// 자동으로 형제 오브젝트들 중 맨 앞(Hierarchy 마지막 = 화면상 가장 위)으로 옵니다.
///
/// 버튼의 OnClick()이 단순히 GameObject.SetActive(true)만 호출하고 있어서
/// 다른 패널들 뒤에 깔리는 문제를 해결할 때 씁니다.
/// 기존 버튼 연결(OnClick)은 전혀 건드릴 필요 없이, 이 패널 오브젝트에
/// 컴포넌트만 추가하면 끝입니다.
/// </summary>
public class BringToFrontOnEnable : MonoBehaviour
{
    private void OnEnable()
    {
        transform.SetAsLastSibling();
    }
}