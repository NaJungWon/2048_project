using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Move_Slot : MonoBehaviour
{
    //각 숫자일 때 이미지 리스트
    public List<Sprite> numberSprites;

    public int slotNumber = 2;
    public int gridX = 0;
    public int gridY = 0;
    public TextMeshProUGUI text;
    public Image slotImage;
    public GameObject particleSystem;
    public AudioSource animalSound;
    public bool acceptScale = false; // 파티클 시스템의 scale x, y에 적용 여부
    public bool hasMergedThisTurn = false; // 이번 턴에 합쳐졌는지 여부
    public bool isNewlyCreated = true; // 새로 생성된 슬롯인지 여부

    void Awake()
    {
        particleSystem = this.transform.GetChild(1).gameObject;
        animalSound = GetComponent<AudioSource>();
    }
    // Start is called before the first frame update
    void Start()
    {
        UpdateImage();
    }
    
    /// <summary>
    /// 숫자에 따라 numberSprites 리스트에서 적절한 이미지로 업데이트
    /// </summary>
    public void UpdateImage()
    {
        if (slotImage == null)
        {
            slotImage = GetComponent<Image>();
        }
        
        if (slotImage == null || numberSprites == null || numberSprites.Count == 0)
        {
            return;
        }
        
        // 2048 게임은 2의 거듭제곱으로 숫자가 증가 (2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048...)
        // log2(slotNumber)를 사용하여 인덱스 계산
        // 2 = 2^1 -> 인덱스 0, 4 = 2^2 -> 인덱스 1, 8 = 2^3 -> 인덱스 2...
        int spriteIndex = 0;
        
        if (slotNumber >= 2)
        {
            // log2(slotNumber) - 1을 계산하여 인덱스 얻기
            float logValue = Mathf.Log(slotNumber, 2f);
            spriteIndex = Mathf.FloorToInt(logValue) - 1;
            
            // 리스트 범위를 벗어나지 않도록 클램프
            spriteIndex = Mathf.Clamp(spriteIndex, 0, numberSprites.Count - 1);
        }
        
        // 이미지 업데이트
        if (spriteIndex >= 0 && spriteIndex < numberSprites.Count && numberSprites[spriteIndex] != null)
        {
            slotImage.sprite = numberSprites[spriteIndex];
        }
    }

    /// <summary>
    /// 파티클 재생 - 슬롯 생성 또는 숫자 업그레이드 시 호출
    /// </summary>
    public void PlayParticle()
    {
        // 파티클 시스템이 비어 있으면 자식에서 자동으로 검색
        if (particleSystem == null)
        {
            particleSystem = this.transform.GetChild(1).gameObject;
        }
        // 슬롯 크기를 한 번만 파티클 스케일에 반영
        if (!acceptScale)
        {
            RectTransform slotRect = GetComponent<RectTransform>();
            if (slotRect != null)
            {
                // 현재 슬롯의 width, height 가져오기
                float width = slotRect.sizeDelta.x;
                float height = slotRect.sizeDelta.y;
                
                // 파티클 시스템의 scale x, y에 적용
                particleSystem.transform.localScale = new Vector3(width, height, 1f);
            }
            acceptScale = true;
        }

        ParticleSystem ps = particleSystem.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogWarning($"[{name}] ParticleSystem 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
        animalSound.Play();
    }
    
    // 폰트 크기 설정
    public void SetFontSize(float fontSize)
    {
        if (text == null)
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.text = slotNumber.ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
