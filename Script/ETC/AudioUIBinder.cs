using UnityEngine;
using UnityEngine.UI;

public class AudioUIBinder : MonoBehaviour
{
    [Header("대상 오디오 버스")]
    [SerializeField] private AudioBus bus = AudioBus.SFX;

    [Header("UI References")]
    [SerializeField] private Slider volumeSlider;  // 0~1
    [SerializeField] private Button muteButton;    // 스프라이트 스왑 방식
    [SerializeField] private Image iconImage;      // 버튼 아이콘(대상 Graphic)

    [Header("아이콘 스프라이트")]
    [SerializeField] private Sprite spriteVolumeOn;
    [SerializeField] private Sprite spriteMuted;   // 음소거(슬래시) 아이콘

    [Header("옵션")]
    [SerializeField] private bool syncButtonSpriteStates = true; // Highlighted/Pressed/Selected/Disabled 통일

    private bool _applyingFromEvent;

    void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnVolumeChanged += HandleVolumeChanged;
            // 초기 UI 동기화
            var am = AudioManager.Instance;
            float v = (bus == AudioBus.BGM) ? am.VolumeBGM : am.VolumeSFX;
            bool muted = (bus == AudioBus.BGM) ? am.MutedBGM : am.MutedSFX;
            ApplyVisual(v, muted || v <= 0f, setSlider: true);
        }

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnSliderChanged);

        if (muteButton != null)
            muteButton.onClick.AddListener(OnClickMute);
    }

    void OnDisable()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.OnVolumeChanged -= HandleVolumeChanged;

        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnSliderChanged);

        if (muteButton != null)
            muteButton.onClick.RemoveListener(OnClickMute);
    }

    // ─────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────
    private void HandleVolumeChanged(AudioBus changedBus, float volume, bool isMutedOrZero)
    {
        if (changedBus != bus) return;
        ApplyVisual(volume, isMutedOrZero, setSlider: true);
    }

    private void OnSliderChanged(float value)
    {
        if (_applyingFromEvent) return;
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.SetVolume(bus, value);
        // SetVolume이 다시 OnVolumeChanged를 날려 오므로 UI는 이벤트에서 갱신
    }

    private void OnClickMute()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.ToggleMute(bus);

        // ToggleMute 후 이벤트로 새 상태가 오지만,
        // 사용감 개선을 위해 즉시 슬라이더도 맞춰준다.
        var am = AudioManager.Instance;
        float v = (bus == AudioBus.BGM) ? am.VolumeBGM : am.VolumeSFX;
        bool m = (bus == AudioBus.BGM) ? am.MutedBGM : am.MutedSFX;
        ApplyVisual(v, m || v <= 0f, setSlider: true);
    }

    // ─────────────────────────────────────────────────────────────
    // UI 적용
    // ─────────────────────────────────────────────────────────────
    private void ApplyVisual(float volume, bool isMutedOrZero, bool setSlider)
    {
        _applyingFromEvent = true;

        if (setSlider && volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(volume);

        if (iconImage != null)
        {
            var sprite = isMutedOrZero ? spriteMuted : spriteVolumeOn;
            iconImage.sprite = sprite;

            if (syncButtonSpriteStates && muteButton != null)
                SyncButtonSpriteStates(muteButton, sprite); // 모든 상태 동일 스프라이트
        }

        _applyingFromEvent = false;
    }

    // 버튼 Transition이 SpriteSwap일 때 모든 상태를 같은 스프라이트로 통일
    private void SyncButtonSpriteStates(Button button, Sprite sprite)
    {
        var st = button.spriteState;
        st.highlightedSprite = sprite;
        st.pressedSprite = sprite;
        st.selectedSprite = sprite;
        st.disabledSprite = sprite;
        button.spriteState = st;
    }
}