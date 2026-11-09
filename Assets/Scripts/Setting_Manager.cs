using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Setting_Manager : MonoBehaviour
{
    public static Setting_Manager Instance { get; private set; }

    public Slider BGMslider; //배경음조절 슬라이더
    public Slider SFXslider; //효과음조절 슬라이더

    [Header("배경 사운드 설정")]
    public List<AudioSource> bgmSoundSource = new List<AudioSource>(); //배경음 사운드 오브젝트의 컴포넌트

    [Header("SFX 사운드 설정")]
    public List<AudioSource> sfxSoundSource = new List<AudioSource>(); //SFX 사운드 오브젝트의 컴포넌트

    void Awake()
    {
        // 싱글톤 패턴 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // 슬라이더 범위 설정 (0~1) 및 초기값 1로 설정
        if (BGMslider != null)
        {
            BGMslider.maxValue = 1;
            BGMslider.minValue = 0f;
            BGMslider.value = 1; // 초기값 1로 설정
            // 슬라이더 값 변경 이벤트 리스너 추가
            BGMslider.onValueChanged.AddListener(OnBGMVolumeChanged);
            // 초기 볼륨 적용
            OnBGMVolumeChanged(BGMslider.value);
        }
        if (SFXslider != null)
        {
            SFXslider.maxValue = 1;
            SFXslider.minValue = 0f;
            SFXslider.value = 1; // 초기값 1로 설정
            // 슬라이더 값 변경 이벤트 리스너 추가
            SFXslider.onValueChanged.AddListener(OnSFXVolumeChanged);
            // 초기 볼륨 적용
            OnSFXVolumeChanged(SFXslider.value);
        }
    }

    /// <summary>
    /// BGM 슬라이더 값 변경 시 호출되는 메서드
    /// </summary>
    private void OnBGMVolumeChanged(float value)
    {
        // slider 값(0~1)을 그대로 AudioSource volume에 적용
        foreach (AudioSource source in bgmSoundSource)
        {
            if (source != null)
            {
                source.volume = value;
            }
        }
    }

    /// <summary>
    /// SFX 슬라이더 값 변경 시 호출되는 메서드
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        // slider 값(0~1)을 그대로 AudioSource volume에 적용
        foreach (AudioSource source in sfxSoundSource)
        {
            if (source != null)
            {
                source.volume = value;
            }
        }
    }

    // SFX 오브젝트 등록 - GameObject의 AudioSource를 찾아서 리스트에 추가
    public void RegisterSFX(GameObject obj)
    {
        if (obj != null)
        {
            sfxSoundSource.Add(obj.GetComponent<AudioSource>());
        }
    }

    // SFX 오브젝트 등록 해제 - GameObject의 AudioSource를 리스트에서 제거
    public void UnregisterSFX(GameObject obj)
    {
        if (obj != null)
        {
            sfxSoundSource.Remove(obj.GetComponent<AudioSource>());
        }
    }
}
