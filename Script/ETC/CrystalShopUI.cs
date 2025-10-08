using UnityEngine;
using UnityEngine.UI;

public class CrystalShopUI : MonoBehaviour
{
    [Header("UI 패널들 (순서대로 첫 번째 UI부터)")]
    [SerializeField] private GameObject[] uiPanels;

    [Header("탭 버튼들 (UI 순서와 맞춰서 연결)")]
    [SerializeField] private Button[] buttons;

    [SerializeField] private Color selectedColor = new Color(0.00f, 0.78f, 0.66f); // 민트
    [SerializeField] private Color unselectedColor = Color.white;

    private int currentIndex = 0;

    private void Start()
    {
        // 탭 버튼 연결
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            if (buttons[i] != null)
                buttons[i].onClick.AddListener(() => ShowUI(index));
        }
    }

    private void OnEnable()
    {
        if (uiPanels != null && uiPanels.Length > 0)
            ShowUI(0); // 항상 첫 번째 UI 켜기
    }

    private void OnDisable()
    {
        if (uiPanels != null)
        {
            foreach (var panel in uiPanels)
                if (panel != null) panel.SetActive(false);
        }

        if (buttons != null)
        {
            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    var img = btn.GetComponent<Image>();
                    if (img != null) img.color = unselectedColor;
                }
            }
        }
    }

    private void ShowUI(int index)
    {
        for (int i = 0; i < uiPanels.Length; i++)
        {
            if (uiPanels[i] != null) uiPanels[i].SetActive(i == index);

            if (i < buttons.Length && buttons[i] != null)
            {
                var img = buttons[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == index) ? selectedColor : unselectedColor;
            }
        }
        currentIndex = index;
    }
}