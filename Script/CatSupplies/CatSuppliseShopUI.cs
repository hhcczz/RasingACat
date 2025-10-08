using System;
using UnityEngine;
using UnityEngine.UI;

public class CatSuppliseShopUI : MonoBehaviour
{
    [Header("고양이 용품 Sprite")]
    public Sprite[] CatSuppliesSprite;

    [Header("고양이 용품 상점 UI 관리")]
    public Text[] CatSuppliesNameLabels;
    public Text[] CatSuppliesAbilityLabels;
    public Text[] CatSuppliesPriceLabels;
    public Image[] CatSuppliesImgLabels;
    public Button[] CatSuppliesBuyBtn;

    [Header("고양이 페이지 버튼")]
    public Button CatSuppliesPrevPagesBtn;
    public Button CatSuppliesNextPagesBtn;


    [Header("ETC Variable")]
    [SerializeField] private int _catSuppliesInPages = 0; // 현재 페이지(0-base)
    [SerializeField] private int _catSuppliesCounts = 6; // 한 페이지 아이템 수
    private int _numberOfPages = 1; // 데이터 길이 기반으로 재산정

    void Start()
    {
        ValidateData();
        RefreshUI();
    }
    void Awake()
    {
        ValidateData();
        WireButtons();
    }

    private void OnEnable()
    {
        ValidateData();
        RefreshUI();
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    private void WireButtons()
    {
        if (CatSuppliesPrevPagesBtn) CatSuppliesPrevPagesBtn.onClick.AddListener(() => PagesMover(-1));
        if (CatSuppliesNextPagesBtn) CatSuppliesNextPagesBtn.onClick.AddListener(() => PagesMover(1));

        for (int i = 0; i < CatSuppliesBuyBtn.Length; i++)
        {
            int index = i;
            if (CatSuppliesBuyBtn[index]) CatSuppliesBuyBtn[index].onClick.AddListener(() => BuySupplies(index));
        }

    }

    private void UnwireButtons()
    {
        if (CatSuppliesPrevPagesBtn) CatSuppliesPrevPagesBtn.onClick.RemoveAllListeners();
        if (CatSuppliesNextPagesBtn) CatSuppliesNextPagesBtn.onClick.RemoveAllListeners();

        for (int i = 0; i < CatSuppliesBuyBtn.Length; i++)
        {
            int index = i;
            if (CatSuppliesBuyBtn[index]) CatSuppliesBuyBtn[index].onClick.RemoveAllListeners();
        }
    }

    private void InitialSettingUI()
    {
        _catSuppliesInPages = 0;
    }

    /// <summary>
    /// 고양이 상점 물품 구매
    /// index = 버튼 번호
    /// </summary>
    /// <param name="index"></param>
    private void BuySupplies(int index = -1)
    {
        if (index == -1) return;
        var number = index + _catSuppliesInPages * _catSuppliesCounts;
        var price = CatSuppliesManager.Instance._catSuppliesPriceData[number];
        if (CatCoinManager.Instance.HaveCatCoinCount >= price)
        {
            CatCoinManager.Instance.RemoveCatCoin(price);
            CatSuppliesManager.Instance._catSuppliesBuyItemList[number] = true;


            RefreshUI();
        }
    }

   

    private void ValidateData()
    {
        // UI 슬롯 6개 확인
        if (CatSuppliesNameLabels == null || CatSuppliesNameLabels.Length < _catSuppliesCounts ||
            CatSuppliesAbilityLabels == null || CatSuppliesAbilityLabels.Length < _catSuppliesCounts ||
            CatSuppliesPriceLabels == null || CatSuppliesPriceLabels.Length < _catSuppliesCounts ||
            CatSuppliesImgLabels == null || CatSuppliesImgLabels.Length < _catSuppliesCounts)
        {
            Debug.LogError("[CatSuppliseShopUI] UI Label 배열 길이가 부족합니다.");
        }

        // 데이터 존재 & 길이 일치
        int dataLen = Mathf.Min(
            CatSuppliesManager.Instance._catSuppliesNameData?.Length ?? 0,
            CatSuppliesManager.Instance._catSuppliesAbilityData?.Length ?? 0,
            CatSuppliesManager.Instance._catSuppliesPriceData?.Length ?? 0
        );
        if (dataLen == 0)
        {
            Debug.LogError("[CatSuppliseShopUI] 데이터가 비어 있습니다.");
            _numberOfPages = 1;
            return;
        }

        // 페이지 수를 데이터 길이에서 자동 산정
        _numberOfPages = Mathf.CeilToInt(dataLen / (float)_catSuppliesCounts);

        // 스프라이트 수 점검(있다면)
        if (CatSuppliesSprite != null && CatSuppliesSprite.Length < dataLen)
        {
            Debug.LogWarning($"[CatSuppliseShopUI] 스프라이트 개수({CatSuppliesSprite.Length})가 데이터 개수({dataLen})보다 적습니다.");
        }

        // 현재 페이지 범위 보정
        _catSuppliesInPages = Mathf.Clamp(_catSuppliesInPages, 0, Mathf.Max(0, _numberOfPages - 1));
    }

    private void PagesMover(int delta)
    {
        int next = Mathf.Clamp(_catSuppliesInPages + delta, 0, Mathf.Max(0, _numberOfPages - 1));
        if (next == _catSuppliesInPages) return; // 더 이동 불가
        _catSuppliesInPages = next;
        RefreshUI();
    }

    private void RefreshUI()
    {
        // 버튼 상태
        if (CatSuppliesPrevPagesBtn) CatSuppliesPrevPagesBtn.gameObject.SetActive(_catSuppliesInPages > 0);
        if (CatSuppliesNextPagesBtn) CatSuppliesNextPagesBtn.gameObject.SetActive(_catSuppliesInPages < _numberOfPages - 1);

        // 슬롯 렌더
        for (int i = 0; i < _catSuppliesCounts; i++)
        {
            int dataIndex = i + _catSuppliesInPages * _catSuppliesCounts;

            bool hasData = CatSuppliesManager.Instance._catSuppliesNameData != null && dataIndex < CatSuppliesManager.Instance._catSuppliesNameData.Length;

            // 이름
            if (CatSuppliesNameLabels != null && i < CatSuppliesNameLabels.Length && CatSuppliesNameLabels[i])
                CatSuppliesNameLabels[i].text = hasData ? CatSuppliesManager.Instance._catSuppliesNameData[dataIndex] : "-";

            // 능력
            if (CatSuppliesAbilityLabels != null && i < CatSuppliesAbilityLabels.Length && CatSuppliesAbilityLabels[i])
                CatSuppliesAbilityLabels[i].text = (hasData && dataIndex < CatSuppliesManager.Instance._catSuppliesAbilityData.Length)
                    ? CatSuppliesManager.Instance._catSuppliesAbilityData[dataIndex]
                    : "";

            // 이미지
            if (CatSuppliesImgLabels != null && i < CatSuppliesImgLabels.Length && CatSuppliesImgLabels[i])
                CatSuppliesImgLabels[i].sprite = (CatSuppliesSprite != null && dataIndex < CatSuppliesSprite.Length)
                    ? CatSuppliesSprite[dataIndex]
                    : null;

            // 구매 여부
            bool purchased = hasData
                && CatSuppliesManager.Instance._catSuppliesBuyItemList != null
                && dataIndex < CatSuppliesManager.Instance._catSuppliesBuyItemList.Length
                && CatSuppliesManager.Instance._catSuppliesBuyItemList[dataIndex];

            // 버튼
            if (CatSuppliesBuyBtn != null && i < CatSuppliesBuyBtn.Length && CatSuppliesBuyBtn[i])
                CatSuppliesBuyBtn[i].interactable = hasData && !purchased;

            // 가격/구매완료 텍스트 논리 수정
            if (CatSuppliesPriceLabels != null && i < CatSuppliesPriceLabels.Length && CatSuppliesPriceLabels[i])
            {
                if (!hasData)
                {
                    CatSuppliesPriceLabels[i].text = "";
                }
                else if (purchased)
                {
                    CatSuppliesPriceLabels[i].text = "구매완료";
                }
                else
                {
                    CatSuppliesPriceLabels[i].text = Formatted.FormatKoreanNumber(CatSuppliesManager.Instance._catSuppliesPriceData[dataIndex]);
                }
            }
                
        }
    }
}