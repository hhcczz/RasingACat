using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PhoneUI : MonoBehaviour
{
    [Header("Buttons")]
    [Tooltip("고양이 헬스장 열기 버튼")]
    public Button Btn_OpenCatGym;
    [Tooltip("고양이 상점 열기 버튼")]
    public Button Btn_OpenCatShop;
    [Tooltip("보석 상점 열기 버튼")]
    public Button Btn_OpenCrystalShop;
    [Tooltip("환경 설정 열기 버튼")]
    public Button Btn_OpenSetting;
    [Tooltip("고양이 도감 열기 버튼")]
    public Button Btn_OpenCatJournal;
    [Tooltip("플레이 정보 열기 버튼")]
    public Button Btn_OpenPlayInfo;
    [Tooltip("고양이 바구니 열기 버튼")]
    public Button Btn_OpenCatBasket;
    [Tooltip("고양이 유물 열기 버튼")]
    public Button Btn_OpenCatRelics;

    [Tooltip("홈(뒤로가기) 버튼")]
    public Button Btn_Home;

    [Header("Panels")]
    [Tooltip("고양이 헬스장 패널 오브젝트")]
    public GameObject CatGym;
    [Tooltip("고양이 상점 패널 오브젝트")]
    public GameObject CatShop;
    [Tooltip("보석 상점 패널 오브젝트")]
    public GameObject CrystalShop;
    [Tooltip("환경 설정 패널 오브젝트")]
    public GameObject Setting;
    [Tooltip("고양이 도감 패널 오브젝트")]
    public GameObject CatJournal;
    [Tooltip("플레이 정보 패널 오브젝트")]
    public GameObject PlayInfo;
    [Tooltip("고양이 바구니 패널 오브젝트")]
    public GameObject CatBasket;
    [Tooltip("고양이 유물 패널 오브젝트")]
    public GameObject CatRelics;


    private enum PhoneView { Main, CatGym, CatShop, CrystalShop, Setting, CatJournal, PlayInfo, CatBasket, CatRelics }
    private PhoneView currentView = PhoneView.Main;

    void Reset()
    {
        // 필요시 자동 탐색 로직 넣을 수 있음
    }

    void Awake()
    {
        ValidateSetup(); // 에디터/런타임 모두에서 초기 검증
    }

    void Start()
    {
        // EventSystem 체크
        if (!FindObjectOfType<EventSystem>())
            Debug.LogWarning("[PhoneUI] EventSystem이 씬에 없습니다. 버튼/터치가 동작하지 않을 수 있습니다.", this);

        // 안전 리스너 등록
        SafeAddListener(Btn_OpenCatGym, OpenCatGym, "Btn_OpenCatGym");
        SafeAddListener(Btn_OpenCatShop, OpenCatShop, "Btn_OpenCatShop");
        SafeAddListener(Btn_OpenCrystalShop, OpenCrystalShop, "Btn_OpenCrystalShop");
        SafeAddListener(Btn_OpenSetting, OpenSetting, "Btn_OpenSetting");
        SafeAddListener(Btn_OpenCatJournal, OpenCatJournal, "Btn_OpencatJournal");
        SafeAddListener(Btn_OpenPlayInfo, OpenPlayInfo, "Btn_OpenPlayInfo");
        SafeAddListener(Btn_OpenCatBasket, OpenCatBasket, "Btn_OpenCatBasket");
        SafeAddListener(Btn_OpenCatRelics, OpenCatRelics, "Btn_OpenCatRelics");

        SafeAddListener(Btn_Home, GoHome, "Btn_Home");
    }

    void OnEnable()
    {
        SetView(PhoneView.Main);
        GameManager.Instance.EnableBlock();
    }

    void OnDisable()
    {
        DeactivateAll();
        currentView = PhoneView.Main;
        GameManager.Instance.DisableBlock();
    }

    // ─── UI Actions ───
    void OpenCatGym() => SetViewSafe(PhoneView.CatGym, CatGym, "CatGym");
    void OpenCatShop() => SetViewSafe(PhoneView.CatShop, CatShop, "CatShop");
    void OpenCrystalShop() => SetViewSafe(PhoneView.CrystalShop, CrystalShop, "CrystalShop");
    void OpenSetting() => SetViewSafe(PhoneView.Setting, Setting, "Setting");
    void OpenCatJournal() => SetViewSafe(PhoneView.CatJournal, CatJournal, "CatJournal");
    void OpenPlayInfo() => SetViewSafe(PhoneView.PlayInfo, PlayInfo, "PlayInfo");
    void OpenCatBasket() => SetViewSafe(PhoneView.CatBasket, CatBasket, "CatBasket");
    void OpenCatRelics() => SetViewSafe(PhoneView.CatRelics, CatRelics, "CatRelics");

    void GoHome()
    {
        if (currentView == PhoneView.Main)
        {
            gameObject.SetActive(false);
        }
        else
        {
            SetView(PhoneView.Main);
        }
    }

    // ─── Core ───
    void SetViewSafe(PhoneView view, GameObject panel, string panelName)
    {
        if (!panel)
        {
            Debug.LogWarning($"[PhoneUI] '{panelName}' 패널이 비어 있습니다. 메인으로 돌아갑니다.", this);
            SetView(PhoneView.Main);
            return;
        }
        SetView(view);
    }

    void SetView(PhoneView view)
    {
        currentView = view;

        switch (view)
        {
            case PhoneView.Main:
                DeactivateAll();
                break;
            case PhoneView.CatGym:
                ShowOnly(CatGym, "CatGym");
                break;
            case PhoneView.CatShop:
                ShowOnly(CatShop, "CatShop");
                break;
            case PhoneView.CrystalShop:
                ShowOnly(CrystalShop, "CrystalShop");
                break;
            case PhoneView.CatJournal:
                ShowOnly(CatJournal, "CatJournal");
                break;
            case PhoneView.Setting:
                ShowOnly(Setting, "Setting");
                break;
            case PhoneView.PlayInfo:
                ShowOnly(PlayInfo, "PlayInfo");
                break;
            case PhoneView.CatBasket:
                ShowOnly(CatBasket, "CatBasket");
                break;
            case PhoneView.CatRelics:
                ShowOnly(CatRelics, "CatRelics");
                break;
        }
    }

    void ShowOnly(GameObject panel, string panelName)
    {
        if (!panel)
        {
            Debug.LogWarning($"[PhoneUI] '{panelName}' 패널이 없습니다. 메인으로 전환합니다.", this);
            DeactivateAll();
            currentView = PhoneView.Main;
            return;
        }

        if (CatGym) CatGym.SetActive(panel == CatGym);
        else Debug.LogWarning("[PhoneUI] 'CatGym' 패널이 지정되지 않았습니다.", this);

        if (CatShop) CatShop.SetActive(panel == CatShop);
        else Debug.LogWarning("[PhoneUI] 'CatShop' 패널이 지정되지 않았습니다.", this);

        if (CrystalShop) CrystalShop.SetActive(panel == CrystalShop);
        else Debug.LogWarning("[PhoneUI] 'CrystalShop' 패널이 지정되지 않았습니다.", this);

        if (CatJournal) CatJournal.SetActive(panel == CatJournal);
        else Debug.LogWarning("[PhoneUI] 'CatJournal' 패널이 지정되지 않았습니다.", this);

        if (Setting) Setting.SetActive(panel == Setting);
        else Debug.LogWarning("[PhoneUI] 'Setting' 패널이 지정되지 않았습니다.", this);

        if (PlayInfo) PlayInfo.SetActive(panel == PlayInfo);
        else Debug.LogWarning("[PhoneUI] 'PlayInfo' 패널이 지정되지 않았습니다.", this);

        if (CatBasket) CatBasket.SetActive(panel == CatBasket);
        else Debug.LogWarning("[PhoneUI] 'CatBasket' 패널이 지정되지 않았습니다.", this);

        if (CatRelics) CatRelics.SetActive(panel == CatRelics);
        else Debug.LogWarning("[PhoneUI] 'CatRelics' 패널이 지정되지 않았습니다.", this);
    }

    void DeactivateAll()
    {
        if (CatGym) CatGym.SetActive(false); else Debug.LogWarning("[PhoneUI] 'CatGym' 패널이 없습니다.", this);
        if (CatShop) CatShop.SetActive(false); else Debug.LogWarning("[PhoneUI] 'CatShop' 패널이 없습니다.", this);
        if (CrystalShop) CrystalShop.SetActive(false); else Debug.LogWarning("[PhoneUI] 'CrystalShop' 패널이 없습니다.", this);
        if (CatJournal) CatJournal.SetActive(false); else Debug.LogWarning("[PhoneUI] 'CatJournal' 패널이 없습니다.", this);
        if (Setting) Setting.SetActive(false); else Debug.LogWarning("[PhoneUI] 'Setting' 패널이 없습니다.", this);
        if (PlayInfo) PlayInfo.SetActive(false); else Debug.LogWarning("[PhoneUI] 'PlayInfo' 패널이 없습니다.", this);
        if (CatBasket) CatBasket.SetActive(false); else Debug.LogWarning("[PhoneUI] 'CatBasket' 패널이 없습니다.", this);
        if (CatRelics) CatRelics.SetActive(false); else Debug.LogWarning("[PhoneUI] 'CatRelics' 패널이 없습니다.", this);
    }

    // ─── Helpers ───
    void SafeAddListener(Button btn, UnityEngine.Events.UnityAction action, string nameForLog)
    {
        if (!btn)
        {
            Debug.LogError($"[PhoneUI] 버튼 참조가 비었습니다: {nameForLog}", this);
            return;
        }
        btn.onClick.RemoveListener(action); // 중복 방지
        btn.onClick.AddListener(action);
    }

    void ValidateSetup()
    {
        // 버튼 검증
        if (!Btn_OpenCatGym) Debug.LogError("[PhoneUI] 'Btn_OpenCatGym' 버튼이 비어 있습니다.", this);
        if (!Btn_OpenCatShop) Debug.LogError("[PhoneUI] 'Btn_OpenCatShop' 버튼이 비어 있습니다.", this);
        if (!Btn_OpenCrystalShop) Debug.LogError("[PhoneUI] 'Btn_OpenCrystalShop' 버튼이 비어 있습니다.", this);
        if (!Btn_OpenSetting) Debug.LogError("[PhoneUI] 'Btn_OpenSetting' 버튼이 비어 있습니다.", this);
        if (!Btn_OpenCatJournal) Debug.LogError("[PhoneUI] 'Btn_OpenCatJournal' 버튼이 비어 있습니다.", this);
        if (!Btn_OpenPlayInfo) Debug.LogError("[PhoneUI] 'Btn_OpenPlayInfo' 버튼이 비어 있습니다.", this);
        if (!Btn_OpenCatBasket) Debug.LogError("[PhoneUI] 'Btn_OpenCatBasket' 버튼이 비어 있습니다.", this);
        if (!Btn_OpenCatRelics) Debug.LogError("[PhoneUI] 'Btn_OpenCatRelics' 버튼이 비어 있습니다.", this);

        if (!Btn_Home) Debug.LogError("[PhoneUI] 'Btn_Home' 버튼이 비어 있습니다.", this);

        // 패널 검증
        if (!CatGym) Debug.LogWarning("[PhoneUI] 'CatGym' 패널이 비어 있습니다.", this);
        if (!CatShop) Debug.LogWarning("[PhoneUI] 'CatShop' 패널이 비어 있습니다.", this);
        if (!CrystalShop) Debug.LogWarning("[PhoneUI] 'CrystalShop' 패널이 비어 있습니다.", this);
        if (!Setting) Debug.LogWarning("[PhoneUI] 'Setting' 패널이 비어 있습니다.", this);
        if (!CatJournal) Debug.LogWarning("[PhoneUI] 'CatJournal' 패널이 비어 있습니다.", this);
        if (!PlayInfo) Debug.LogWarning("[PhoneUI] 'PlayInfo' 패널이 비어 있습니다.", this);
        if (!CatBasket) Debug.LogWarning("[PhoneUI] 'CatBasket' 패널이 비어 있습니다.", this);
        if (!CatRelics) Debug.LogWarning("[PhoneUI] 'CatRelics' 패널이 비어 있습니다.", this);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // 에디터에서도 즉시 검증 로그를 보고 싶다면 유지
        ValidateSetup();
    }
#endif
}