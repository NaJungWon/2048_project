using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFX_Control : MonoBehaviour
{
    void OnEnable()
    {
        // Setting_Manager에 자신을 등록 (AudioSource가 자동으로 추가됨)
        if (Setting_Manager.Instance != null)
        {
            // AudioSource를 찾아서 현재 SFX 슬라이더 값으로 볼륨 설정
            AudioSource audioSource = GetComponent<AudioSource>();
            audioSource.volume = Setting_Manager.Instance.SFXslider.value;
            
            // 리스트에 등록 (RegisterSFX에서도 볼륨이 설정되지만, 먼저 설정)
            Setting_Manager.Instance.RegisterSFX(gameObject);
        }
        else
        {
            Debug.LogError("Setting_Manager not found");
        }
    }

    void OnDisable()
    {
        // Setting_Manager에서 등록 해제
        if (Setting_Manager.Instance != null)
        {
            Setting_Manager.Instance.UnregisterSFX(gameObject);
        }
    }
}
