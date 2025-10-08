using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CatJournalUI : MonoBehaviour
{
    [Header("고양이 도감 UI 정리")]
    [Tooltip("고양이 이름")]
    public Text[] _catJournal_Name;
    [Tooltip("고양이 이미지")]
    public Image[] _catJournal_Image;
    [Tooltip("고양이 설명")]
    public Text[] _catJournal_Desc;

    public Button _catJournalPrev_Btn;
    public Button _catJournalNext_Btn;

    [Header("ETC Variable")]
    [SerializeField] private int _catJournal_InPages = 0; // 현재 페이지(0-base)
    [SerializeField] private int _catJournal_Counts = 6; // 한 페이지 아이템 수
    private int _numberOfPages = 1; // 데이터 길이 기반으로 재산정

    private void Start()
    {
        WireButtons();
    }
    private void OnEnable()
    {
        _catJournal_InPages = 0;
        RefreshUI();
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    private void WireButtons()
    {
        if (_catJournalPrev_Btn) _catJournalPrev_Btn.onClick.AddListener(() => PagesMover(-1));
        if (_catJournalNext_Btn) _catJournalNext_Btn.onClick.AddListener(() => PagesMover(1));
    }

    private void UnwireButtons()
    {
        if (_catJournalPrev_Btn) _catJournalPrev_Btn.onClick.RemoveAllListeners();
        if (_catJournalNext_Btn) _catJournalNext_Btn.onClick.RemoveAllListeners();
    }

    private void PagesMover(int index)
    {
        int next = Mathf.Clamp(_catJournal_InPages + index, 0, Mathf.Max(0, _numberOfPages - 1));
        if (next == _catJournal_InPages) return; // 더 이동 불가
        _catJournal_InPages = next;
        RefreshUI();
    }
    private void RefreshUI()
    {
        var cm = CatManager.Instance;
        var mm = CatMergeManager.Instance;

        if (!cm || !mm) return;

        // 전체 레벨 수 기준으로 총 페이지 수 다시 계산
        int totalCount = cm.catNamesByLevel.Length;
        _numberOfPages = Mathf.CeilToInt((float)totalCount / _catJournal_Counts);

        // 버튼 상태
        if (_catJournalPrev_Btn)
            _catJournalPrev_Btn.gameObject.SetActive(_catJournal_InPages > 0);
        if (_catJournalNext_Btn)
            _catJournalNext_Btn.gameObject.SetActive(_catJournal_InPages < _numberOfPages - 1);

        // 슬롯 렌더
        for (int i = 0; i < _catJournal_Counts; i++)
        {
            int dataIndex = i + _catJournal_InPages * _catJournal_Counts;
            bool discovered = false;
            bool valid = dataIndex >= 0 && dataIndex < totalCount;
            if (valid && cm.IsCatDiscovered != null && dataIndex < cm.IsCatDiscovered.Length)
                discovered = cm.IsCatDiscovered[dataIndex];

            // 이름
            if (_catJournal_Name != null && i < _catJournal_Name.Length)
            {
                _catJournal_Name[i].gameObject.SetActive(valid);
                if (valid)
                    _catJournal_Name[i].text = discovered ? cm.GetCatName(dataIndex) : "???"; // 발견 여부에 따라 이름 변경
            }

            // 이미지
            if (_catJournal_Image != null && i < _catJournal_Image.Length)
            {
                _catJournal_Image[i].gameObject.SetActive(valid);
                if (valid && dataIndex < cm.catPrefabsByLevel.Length)
                {
                    var prefab = cm.catPrefabsByLevel[dataIndex];
                    var img = prefab ? prefab.GetComponent<Image>() : null;
                    _catJournal_Image[i].sprite = img ? img.sprite : null;

                    // 발견 여부에 따라 색상 변경
                    _catJournal_Image[i].color = discovered ? Color.white : Color.black; // #FFFFFF / #000000
                }
                else
                {
                    _catJournal_Image[i].sprite = null;
                }
            }

            // 설명
            if (_catJournal_Desc != null && i < _catJournal_Desc.Length)
            {
                _catJournal_Desc[i].gameObject.SetActive(valid);
                if (valid && dataIndex < mm._catMergeDescList.Length)
                    _catJournal_Desc[i].text = discovered ? $"일반 코인 + <color=cyan>{cm.coinPerSecondByLevel[dataIndex]}</color>" : "미 발견 고양이"; // 발견 여부에 따라 설명 변경
            }
        }
    }
}
