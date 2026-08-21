using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SentenceBlock : MonoBehaviour
{
    private TextMeshProUGUI indexText;  // �ڽ� �� 'Label'�� �ڵ����� �˻�
    private TextMeshProUGUI bodyText;   // �ڽ� �� 'Body'�� �ڵ����� �˻�
    private Image backgroundImage;      // �ڱ� �ڽ��� Image ������Ʈ
    private Button blockButton;

    [Header("--- Color Settings ---")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.7f, 0.95f, 0.95f, 1.0f); // ���� �� ��Ʈ�� ���̶���Ʈ

    // �ܺ�(Manager)���� ������ ���� ���θ� �Ǵ��ϱ� ���� ������Ƽ
    public int Index { get; private set; }
    public bool IsSelected { get; private set; }
    public string BodyText => bodyText != null ? bodyText.text : "";

    // ���� �Ŵ����� ���� ��ư���� ��ȸ�ϸ� ��ȣ(Index)�� �Ű��� �� ȣ���ϴ� �ʱ�ȭ �Լ�
    public void Initialize(int index)
    {
        Index = index;
        IsSelected = false;

        // 1) ���� �ڽ� ������Ʈ�� �̸����� ������Ʈ �ڵ� �˻� (�巡�׾ص�� ���ʿ�)
        Transform labelTransform = transform.Find("Label");
        if (labelTransform != null) indexText = labelTransform.GetComponent<TextMeshProUGUI>();

        Transform bodyTransform = transform.Find("Body");
        if (bodyTransform != null) bodyText = bodyTransform.GetComponent<TextMeshProUGUI>();

        backgroundImage = GetComponent<Image>();
        blockButton = GetComponent<Button>();

        // 2) Ŭ�� ������ �ߺ� ���� �� ���
        if (blockButton != null)
        {
            blockButton.onClick.RemoveAllListeners();
            blockButton.onClick.AddListener(ToggleSelect);
        }

        // 3) �ʱ� �÷� ����
        if (backgroundImage != null) backgroundImage.color = defaultColor;
    }

    // Ŭ�� �� ��� ����
    private void ToggleSelect()
    {
        IsSelected = !IsSelected;
        if (backgroundImage != null)
        {
            backgroundImage.color = IsSelected ? selectedColor : defaultColor;
        }
    }
}