using UnityEngine;
using UnityEngine.UI;

public class CatHammerEffect : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator hammerAnimator;     // (이 스크립트와 같은 GO)
    [SerializeField] private Animator effectAnimator;     // 펑 애니메이터

    [Header("State Names (Animator State 이름과 동일)")]
    [SerializeField] private string hammerStateName = "CatHammer_1"; // 상태명 정확히!
    [SerializeField] private string effectStateName = "CatHammerEffect_1";

    [Header("Graphics")]
    [SerializeField] private Image hammerImage;
    [SerializeField] private Image effectImage;

    public RelicsUI relicsui;

    public Button PhoneCloseBtn;
    private bool isPlaying;
    private int remainCycles; // 남은 사이클 수

    void Awake()
    {
        // 시작 시 비가시 + 자동재생 방지
        if (hammerImage) hammerImage.color = new Color(1, 1, 1, 0);
        if (effectImage) effectImage.color = new Color(1, 1, 1, 0);
        if (hammerAnimator) hammerAnimator.enabled = false;
        if (effectAnimator) effectAnimator.enabled = false;
    }

    /// <summary>딱 2번만 내리치기</summary>
    public void PlayTwice()
    {
        if (isPlaying) return;
        PhoneCloseBtn.interactable = false;
        isPlaying = true;
        remainCycles = 2;
        StartOneCycle();
    }

    // ========== Animation Event 연결 함수들 ==========
    /// <summary>내려찍는 프레임에서 호출(애니메이션 이벤트)</summary>
    public void PlayImpact()
    {
        if (!effectAnimator) return;

        // 보이게
        if (effectImage) effectImage.color = new Color(1, 1, 1, 1);

        // 컬링/업데이트 강제
        effectAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        effectAnimator.updateMode = AnimatorUpdateMode.Normal; // 타임스케일 0 가능성이 있으면 UnscaledTime

        // 같은 상태 재시작 보장
        effectAnimator.Rebind();        // 파라미터/포즈 리셋
        effectAnimator.Update(0f);      // 즉시 반영

        effectAnimator.enabled = true;

        // 0초부터 정확히 재생
        // 상태명이 겹치거나 서브 스테이트에 있으면 해시로 호출이 더 안전
        int stateHash = Animator.StringToHash("Base Layer." + effectStateName); // 레이어/경로 맞춰주세요
        if (stateHash != 0)
            effectAnimator.Play(stateHash, 0, 0f);
        else
            effectAnimator.Play(effectStateName, 0, 0f);

        AudioManager.Instance.PlaySmashSound();
    }

    [System.Obsolete]
    /// <summary>클립 마지막 프레임에서 호출(애니메이션 이벤트)</summary>
    public void OnHammerEnd()
    {
        remainCycles--;

        if (remainCycles > 0)
        {
            // 다음 사이클 즉시 재생
            hammerAnimator.Play(hammerStateName, 0, 0f);
            return;
        }

        // 모든 사이클 종료 → 정리
        FinishAll();
    }
    // ==============================================

    private void StartOneCycle()
    {
        if (hammerImage) hammerImage.color = new Color(1, 1, 1, 1);

        if (hammerAnimator)
        {
            hammerAnimator.enabled = true;
            hammerAnimator.Play(hammerStateName, 0, 0f);
        }
    }

    [System.Obsolete]
    private void FinishAll()
    {
        relicsui.StartRelicsReinforce();
        isPlaying = false;

        // 다시 숨김 + 자동재생 방지
        if (hammerImage) hammerImage.color = new Color(1, 1, 1, 0);
        if (effectImage) effectImage.color = new Color(1, 1, 1, 0);
        if (hammerAnimator) hammerAnimator.enabled = false;
        if (effectAnimator) effectAnimator.enabled = false;
    }
}