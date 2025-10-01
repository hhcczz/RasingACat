using System;
using System.Collections.Generic;
using UnityEngine;

public enum AudioBus { BGM, SFX }
public enum SfxKey
{
    ButtonClick,
    MeowRandom,
    FishSpawn,
    FishToss,
    RelicsSmash,
    RelicsSuccess,
    RelicsFail,
    RelicsBreak,
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("효과음 클립들")]
    [SerializeField] private AudioClip[] catMeowClips;   // 랜덤 고양이 울음
    [SerializeField] private AudioClip buttonClickClip;  // 버튼 클릭 소리
    [SerializeField] private AudioClip fishSpawnClip;    // 물고기 소환(획득/드랍 생성 등)
    [SerializeField] private AudioClip fishTossClip;     // 물고기 투척(던지기) 사운드

    [SerializeField] private AudioClip RelicsSmashClip;
    [SerializeField] private AudioClip RelicsSuccessClip;
    [SerializeField] private AudioClip RelicsFailClip;
    [SerializeField] private AudioClip RelicsBreakClip;

    [Header("배경음악(BGM)")]
    [SerializeField] private AudioClip[] bgmClips;       // BGM (여러개 가능)

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    // ───────── 볼륨/뮤트 상태 ─────────
    public float VolumeBGM { get; private set; } = 0.2f;
    public float VolumeSFX { get; private set; } = 0.8f;
    public bool MutedBGM { get; private set; }
    public bool MutedSFX { get; private set; }
    public float BGMPitch { get; private set; } = 1f;

    private float lastNonZeroBGM = 0.2f;
    private float lastNonZeroSFX = 0.8f;

    public event Action<AudioBus, float, bool> OnVolumeChanged;

    // SFX 키 → 클립 매핑 (MeowRandom은 예외적으로 배열 랜덤)
    private readonly Dictionary<SfxKey, AudioClip> _sfxMap = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 오디오 소스 준비
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        // 키-클립 매핑
        _sfxMap[SfxKey.ButtonClick] = buttonClickClip;
        _sfxMap[SfxKey.FishSpawn] = fishSpawnClip;
        _sfxMap[SfxKey.FishToss] = fishTossClip;

        _sfxMap[SfxKey.RelicsSmash] = RelicsSmashClip;
        _sfxMap[SfxKey.RelicsSuccess] = RelicsSuccessClip;
        _sfxMap[SfxKey.RelicsFail] = RelicsFailClip;
        _sfxMap[SfxKey.RelicsBreak] = RelicsBreakClip;
        // SfxKey.MeowRandom 은 catMeowClips에서 랜덤 추출하므로 _sfxMap에 직접 넣지 않음
    }

    // ───────────────────────────────
    // 재생 API
    // ───────────────────────────────

    public void PlayRandomMeow()
    {
        if (catMeowClips == null || catMeowClips.Length == 0) return;
        var idx = UnityEngine.Random.Range(0, catMeowClips.Length);
        PlaySFX(catMeowClips[idx]);
    }

    public void PlayButtonClick()
    {
        PlaySFX(SfxKey.ButtonClick);
    }

    public void PlaySmashSound()
    {
        PlaySFX(SfxKey.RelicsSmash);
    }

    public void PlaySuccessSound()
    {
        PlaySFX(SfxKey.RelicsSuccess);
    }

    public void PlayFailSound()
    {
        PlaySFX(SfxKey.RelicsFail);
    }

    public void PlayBreakSound()
    {
        PlaySFX(SfxKey.RelicsBreak);
    }

    /// <summary> 키 기반 재생 (MeowRandom은 내부에서 랜덤) </summary>
    public void PlaySFX(SfxKey key)
    {
        if (key == SfxKey.MeowRandom)
        {
            PlayRandomMeow();
            return;
        }
        if (_sfxMap.TryGetValue(key, out var clip) && clip != null)
            PlaySFX(clip);
    }

    /// <summary> 직접 클립 재생 (VolumeSFX/MutedSFX 적용) </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        if (!MutedSFX && VolumeSFX > 0f)
            sfxSource.PlayOneShot(clip, volume * VolumeSFX);
    }

    public void PlayBGM(int index)
    {
        if (bgmClips == null || index < 0 || index >= bgmClips.Length) return;
        if (bgmSource.clip == bgmClips[index] && bgmSource.isPlaying) return;

        bgmSource.clip = bgmClips[index];
        bgmSource.volume = MutedBGM ? 0f : VolumeBGM;
        bgmSource.pitch = BGMPitch;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    // ───────────────────────────────
    // 볼륨 / 음소거 관리
    // ───────────────────────────────

    public void SetVolume(AudioBus bus, float value)
    {
        value = Mathf.Clamp01(value);
        if (bus == AudioBus.BGM)
        {
            VolumeBGM = value;
            if (value > 0f) lastNonZeroBGM = value;
            ApplyVolumes();
            OnVolumeChanged?.Invoke(AudioBus.BGM, VolumeBGM, MutedBGM || VolumeBGM <= 0f);
        }
        else
        {
            VolumeSFX = value;
            if (value > 0f) lastNonZeroSFX = value;
            ApplyVolumes();
            OnVolumeChanged?.Invoke(AudioBus.SFX, VolumeSFX, MutedSFX || VolumeSFX <= 0f);
        }
    }

    public void SetBGMPitch(float pitch)
    {
        BGMPitch = Mathf.Clamp(pitch, 0.5f, 1.5f);
        if (bgmSource != null) bgmSource.pitch = BGMPitch;
    }

    public void ToggleMute(AudioBus bus)
    {
        if (bus == AudioBus.BGM)
        {
            bool wasMuted = MutedBGM;
            MutedBGM = !MutedBGM;

            if (!MutedBGM && VolumeBGM == 0f)
                VolumeBGM = Mathf.Max(lastNonZeroBGM, 0.5f);

            ApplyVolumes();
            OnVolumeChanged?.Invoke(AudioBus.BGM, VolumeBGM, MutedBGM || VolumeBGM <= 0f);

            if (wasMuted && !MutedBGM) PlayButtonClick();
        }
        else
        {
            bool wasMuted = MutedSFX;
            MutedSFX = !MutedSFX;

            if (!MutedSFX && VolumeSFX == 0f)
                VolumeSFX = Mathf.Max(lastNonZeroSFX, 0.5f);

            ApplyVolumes();
            OnVolumeChanged?.Invoke(AudioBus.SFX, VolumeSFX, MutedSFX || VolumeSFX <= 0f);

            if (wasMuted && !MutedSFX) PlayButtonClick();
        }
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = MutedBGM ? 0f : VolumeBGM;
        if (sfxSource != null)
            sfxSource.volume = MutedSFX ? 0f : VolumeSFX;
    }
}