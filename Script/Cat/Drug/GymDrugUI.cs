using System;
using UnityEngine;
using UnityEngine.UI;

public class GymDrugUI : MonoBehaviour
{
    [Header("UI")]
    public Text[] Text_DrugLevel;     // 각 약물 Lv. x/y
    public Text[] Text_DrugAbility;   // 능력 프리뷰 문자열
    public Text[] DrugPrice;          // 다음 레벨 가격 표시 (황금코인)
    public Button[] DrugBuyBtn;       // 구매 버튼

    // 버튼 색상 (CatGym과 동일 톤)
    [Header("Colors")]
    [SerializeField] private Color buyableColor = new Color(0f, 1f, 0.647f); // #00FFA5 민트
    [SerializeField] private Color notBuyableColor = Color.white;            // #FFFFFF
    [SerializeField] private Color maxColor = Color.gray;

    private CatGymDrugManager mgr;
    private int enumCount;

    private void Awake()
    {
        mgr = CatGymDrugManager.Instance ?? FindObjectOfType<CatGymDrugManager>();
        if (mgr == null)
        {
            Debug.LogError("[GymDrugUI] CatGymDrugManager 를 씬에 배치하세요.");
            enabled = false; return;
        }

        enumCount = Enum.GetValues(typeof(CatGymDrugManager.DrugType)).Length;
        WireButtons();
    }

    private void OnEnable()
    {
        if (mgr != null) mgr.OnChanged += RefreshUI;

        // 황금코인 변동 시 UI 갱신
        var coin = CatCoinManager.Instance;
        if (coin != null) coin.OnCatGoldCoinChanged += HandleGoldChanged;

        RefreshUI();
    }

    private void OnDisable()
    {
        if (mgr != null) mgr.OnChanged -= RefreshUI;

        var coin = CatCoinManager.Instance;
        if (coin != null) coin.OnCatGoldCoinChanged -= HandleGoldChanged;
    }

    private void WireButtons()
    {
        if (DrugBuyBtn == null) return;

        for (int i = 0; i < DrugBuyBtn.Length && i < enumCount; i++)
        {
            int idx = i;
            if (DrugBuyBtn[idx] != null)
            {
                DrugBuyBtn[idx].onClick.RemoveAllListeners();
                DrugBuyBtn[idx].onClick.AddListener(() =>
                {
                    var type = (CatGymDrugManager.DrugType)idx;
                    TryBuyWithGold(type); // ← 황금코인 결제
                });
            }
        }
    }

    private void TryBuyWithGold(CatGymDrugManager.DrugType type)
    {
        var coin = CatCoinManager.Instance;
        if (coin == null || mgr == null) return;

        int curLv = mgr.GetLevel(type);
        int maxLv = mgr.GetMaxLevel(type);
        if (curLv >= maxLv) { RefreshUI(); return; }

        int price = mgr.GetNextPrice(type);
        if (price < 0) { RefreshUI(); return; } // MAX/구매불가

        long haveGold = coin.HaveCatGoldCoinCount;
        if (haveGold < (long)price) { RefreshUI(); return; }

        // 차감 → 업그레이드
        coin.RemoveCatGoldCoin(price);
        mgr.TryUpgrade(type); // 내부에서 OnChanged 호출됨

        // 안전차원에서 즉시 갱신
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (mgr == null) return;

        var coin = CatCoinManager.Instance;
        long haveGold = coin != null ? coin.HaveCatGoldCoinCount : 0;

        for (int i = 0; i < enumCount; i++)
        {
            var type = (CatGymDrugManager.DrugType)i;
            int curLv = mgr.GetLevel(type);
            int maxLv = mgr.GetMaxLevel(type);
            bool isMax = mgr.IsMax(type);

            // 레벨: "Lv. 현재/최대"
            if (Text_DrugLevel != null && i < Text_DrugLevel.Length && Text_DrugLevel[i] != null)
                Text_DrugLevel[i].text = $"Lv. {curLv}/{maxLv}";

            // 능력 프리뷰
            if (Text_DrugAbility != null && i < Text_DrugAbility.Length && Text_DrugAbility[i] != null)
                Text_DrugAbility[i].text = mgr.GetAbilityText(type);

            // 가격(황금코인)
            int nextPrice = mgr.GetNextPrice(type);
            if (DrugPrice != null && i < DrugPrice.Length && DrugPrice[i] != null)
                DrugPrice[i].text = (nextPrice < 0) ? "MAX" : nextPrice.ToString();

            // 버튼 상태/색상
            if (DrugBuyBtn != null && i < DrugBuyBtn.Length && DrugBuyBtn[i] != null)
            {
                var btn = DrugBuyBtn[i];
                var colors = btn.colors;

                if (isMax || nextPrice < 0)
                {
                    btn.interactable = false;
                    colors.normalColor = colors.highlightedColor = colors.pressedColor =
                    colors.selectedColor = colors.disabledColor = maxColor;
                }
                else
                {
                    bool canBuy = haveGold >= (long)nextPrice;
                    btn.interactable = canBuy;

                    Color target = canBuy ? buyableColor : notBuyableColor;
                    colors.normalColor = colors.highlightedColor = colors.pressedColor =
                    colors.selectedColor = colors.disabledColor = target;
                }

                btn.colors = colors;
            }
        }
    }

    private void HandleGoldChanged(long _)
    {
        // 황금코인 수 변동 시 즉시 UI 갱신
        RefreshUI();
    }
}