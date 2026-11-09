using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class Game_System : MonoBehaviour
{
    public enum Game_State { Main, Game, Result }
    public Game_State gameState = Game_State.Main;
    public int slotWidth = 4;
    public int slotHeight = 4;
    public GameObject slotFrame;
    //public TextMeshProUGUI scoreText;
    public int score = 0;
    public GameObject moveSlotGroup;
    public GameObject defaultSlot;
    public GameObject numberSlot;

    private Slot_Status[,] board;
    private List<Move_Slot> activeNumberSlots = new List<Move_Slot>();

    public AudioSource titleBGM;
    public AudioSource inGameBGM;
    public AudioSource gameOverBGM;

    // 마우스 드래그 입력
    public float dragThreshold = 50f; // 최소 드래그 거리(픽셀)
    public float inputCooldown = 0.15f; // 스와이프 간 최소 텀(초)
    private Vector2 dragStartPos;
    private bool isDragging = false;
    private float nextInputAllowedTime = 0f;
    private enum SwipeDirection { Up, Down, Left, Right }
    private User_Data userData;
    private Game_UI gameUI;
    private float playTime = 0f;
    private bool isMoving = false; // 이동 애니메이션 중인지 여부
    public float moveDuration = 0.5f; // 이동 애니메이션 시간 (모든 타일 동일)

    // ========== Unity 생명주기 메서드 ==========
    
    /// <summary>
    /// 게임 시작 시 초기화 - 컴포넌트 참조 및 슬롯 초기화
    /// </summary>
    void Start()
    {
        userData = GetComponent<User_Data>();
        gameUI = GetComponent<Game_UI>();
        //scoreText.text = score.ToString();
        gameUI.titleUI.SetActive(true);
        gameState = Game_State.Main;
    }

    /// <summary>
    /// 매 프레임 업데이트 - 게임 중일 때 마우스 입력 처리 및 플레이 시간 업데이트
    /// </summary>
    void Update()
    {
        if (gameState == Game_State.Main)
        {
            if(!titleBGM.isPlaying)
            {
                titleBGM.Play();
            }
        }
        else if (gameState == Game_State.Game)
        {
            if(!inGameBGM.isPlaying)
            {
                titleBGM.Stop();
                inGameBGM.Play();
            }
            HandleMouseDragInput();
            playTime += Time.deltaTime;
            if (playTime >= 60f)
            {
                userData.UpdatePlayerData(new User_Data.PlayerData
                {
                    playTime = 60f
                });
                playTime = 0f;
            }
        }
        else if (gameState == Game_State.Result)
        {
            if(inGameBGM.isPlaying)
            {
                inGameBGM.Stop();
                gameOverBGM.Play();
            }
        }
    }

    public void Title()
    {
        gameUI.titleUI.SetActive(false);
        InitializeSlots();
    }

    // ========== 게임 제어 메서드 ==========

    /// <summary>
    /// 게임 시작 - 게임 상태 변경, 시작 버튼 숨김, 첫 숫자 생성, 버튼 비활성화
    /// </summary>
    public void StartGame()
    {
        gameState = Game_State.Game;
        gameUI.startButton.SetActive(false);
        createNumber();
        
        // 버튼 비활성화 (원클릭 방지)
        UnityEngine.UI.Button button4x4 = gameUI.gameScale4x4Button.GetComponent<UnityEngine.UI.Button>();
        if (button4x4 != null)
        {
            button4x4.interactable = false;
        }
        
        UnityEngine.UI.Button button8x8 = gameUI.gameScale8x8Button.GetComponent<UnityEngine.UI.Button>();
        if (button8x8 != null)
        {
            button8x8.interactable = false;
        }
    }

    /// <summary>
    /// 게임 리셋 - 게임 상태 초기화, 점수 리셋, 슬롯 정리, 버튼 활성화, 슬롯 재초기화
    /// </summary>
    public void ResetGame()
    {
        gameState = Game_State.Main;
        gameUI.startButton.SetActive(true);
        if (gameUI.gameOverUI.activeSelf)
        {
            gameUI.gameOverUI.SetActive(false);
        }
        score = 0;
        if (gameUI != null && gameUI.currentScoreText != null)
        {
            gameUI.currentScoreText.text = score.ToString();
        }
        activeNumberSlots.Clear();
        foreach (Transform child in moveSlotGroup.transform)
        {
            Destroy(child.gameObject);
        }
        // 버튼 활성화
        UnityEngine.UI.Button button4x4 = gameUI.gameScale4x4Button.GetComponent<UnityEngine.UI.Button>();
        if (button4x4 != null)
        {
            button4x4.interactable = true;
        }
        
        UnityEngine.UI.Button button8x8 = gameUI.gameScale8x8Button.GetComponent<UnityEngine.UI.Button>();
        if (button8x8 != null)
        {
            button8x8.interactable = true;
        }
        InitializeSlots();
    }

    /// <summary>
    /// 게임 종료 - Unity 에디터에서는 플레이 모드 종료, 빌드에서는 게임 종료
    /// </summary>
    public void ExitGame()
    {
        #if UNITY_EDITOR
        // 에디터에서 실행 중일 때
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 빌드된 게임에서 실행 중일 때
        Application.Quit();
        #endif
    }

    /// <summary>
    /// 게임 크기 설정 (4x4) - 슬롯 크기를 4x4로 설정하고 초기화
    /// </summary>
    public void GameScale4x4()
    {
        slotWidth = 4;
        slotHeight = 4;
        InitializeSlots();
    }

    /// <summary>
    /// 게임 크기 설정 (8x8) - 슬롯 크기를 8x8로 설정하고 초기화
    /// </summary>
    public void GameScale8x8()
    {
        slotWidth = 8;
        slotHeight = 8;
        InitializeSlots();
    }

    // ========== 초기화 및 설정 메서드 ==========

    /// <summary>
    /// 슬롯 초기화 - 게임 보드 생성, GridLayout 설정, 빈 슬롯 생성
    /// </summary>
    public void InitializeSlots()
    {
        gameState = Game_State.Game;
        //gameUI.startButton.SetActive(false);
        if (slotFrame == null || defaultSlot == null)
        {
            Debug.LogWarning("slotFrame 또는 defaultSlot이 설정되지 않았습니다.");
            return;
        }

        // GridLayoutGroup 가져오기
        UnityEngine.UI.GridLayoutGroup gridLayout = slotFrame.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogError("slotFrame에 GridLayoutGroup 컴포넌트가 없습니다.");
            return;
        }

        // 기존 자식 슬롯 정리 (필요 시)
        for (int i = slotFrame.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(slotFrame.transform.GetChild(i).gameObject);
        }

        board = new Slot_Status[slotWidth, slotHeight];

        // slotWidth에 따라 GridLayout 설정
        float cellSize;
        float spacing;
        float slotSize;
        
        if (slotWidth == 4)
        {
            cellSize = 200f;
            spacing = 20f;
            slotSize = 200f;
        }
        else // slotWidth == 8
        {
            cellSize = 100f;
            spacing = 10f;
            slotSize = 100f;
        }
        
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.spacing = new Vector2(spacing, spacing);

        for (int y = 0; y < slotHeight; y++)
        {
            for (int x = 0; x < slotWidth; x++)
            {
                GameObject slot = Instantiate(defaultSlot);
                RectTransform slotRect = slot.GetComponent<RectTransform>();
                if (slotRect != null)
                {
                    slotRect.sizeDelta = new Vector2(slotSize, slotSize);
                }
                slot.name = $"Slot ({x},{y})";
                // Grid Layout Group에 의해 정렬되도록 부모 설정만 수행
                slot.transform.SetParent(slotFrame.transform, false);

                Slot_Status status = slot.GetComponent<Slot_Status>();
                if (status != null)
                {
                    status.slotInfo = Slot_Status.slot_info.Empty;
                    board[x, y] = status;
                }
            }
        }
    }

    /// <summary>
    /// 최고 점수 적용 - User_Data에서 게스트 로그인 완료 시 호출되어 UI에 최고 점수 표시
    /// </summary>
    public void ApplyHighScore(int highScore)
    {
        if (gameUI != null && gameUI.highScoreText != null)
        {
            gameUI.highScoreText.text = highScore.ToString();
            Debug.Log($"최고 점수 적용: {highScore}");
        }
    }

    // ========== 숫자 슬롯 생성 메서드 ==========

    /// <summary>
    /// 숫자 슬롯 생성 - 빈 슬롯 중 랜덤 위치에 숫자 2 생성
    /// </summary>
    public void createNumber()
    {
        if (numberSlot == null || moveSlotGroup == null || slotFrame == null)
        {
            Debug.LogWarning("numberSlot, moveSlotGroup 또는 slotFrame이 설정되지 않았습니다.");
            return;
        }

        //gameState = Game_State.Game;
        //gameUI.startButton.SetActive(false);
        // 보드(그리드) 기준으로 Empty 슬롯 수집
        List<(int x, int y, Slot_Status status)> emptySlots = new List<(int, int, Slot_Status)>();
        for (int y = 0; y < slotHeight; y++)
        {
            for (int x = 0; x < slotWidth; x++)
            {
                Slot_Status status = board != null ? board[x, y] : null;
                if (status != null && status.slotInfo == Slot_Status.slot_info.Empty)
                {
                    emptySlots.Add((x, y, status));
                }
            }
        }

        if (emptySlots.Count == 0)
        {
            Debug.Log("생성 가능한 빈 슬롯이 없습니다.");
            return;
        }

        // 랜덤 슬롯 선택
        int randomIndex = Random.Range(0, emptySlots.Count);
        var pick = emptySlots[randomIndex];
        int gridX = pick.x;
        int gridY = pick.y;
        Slot_Status targetStatus = pick.status;
        Transform targetTransform = targetStatus.transform;
        float slotSize;
        
        if (slotWidth == 4)
        {
            slotSize = 200f;
        }
        else // slotWidth == 8
        {
            slotSize = 100f;
        }
        // numberSlot 생성 및 그룹 하위로 이동
        GameObject number = Instantiate(numberSlot);
        RectTransform numberRect = number.GetComponent<RectTransform>();
        if (numberRect != null)
        {
            numberRect.sizeDelta = new Vector2(slotSize, slotSize);
        }
        number.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(slotSize, slotSize);
        
        number.transform.SetParent(moveSlotGroup.transform, false);

        // Move_Slot 컴포넌트 설정
        Move_Slot moveSlot = number.GetComponent<Move_Slot>();
        if (moveSlot != null)
        {
            moveSlot.gridX = gridX;
            moveSlot.gridY = gridY;
            // moveSlot.slotNumber = Random.Range(0, 2) == 0 ? 2 : 4; // 2 또는 4 랜덤 생성
            moveSlot.slotNumber = 2; // 항상 2로 시작
            moveSlot.isNewlyCreated = true; // 새로 생성된 슬롯으로 설정
            
            // 폰트 크기 조정 (8x8일 때 절반 크기) - Start() 이후에 실행되도록 코루틴 사용
            float fontSize = slotWidth == 4 ? 50f : 25f;
            moveSlot.SetFontSize(fontSize);
            
            // 이미지 업데이트
            moveSlot.UpdateImage();
            
            activeNumberSlots.Add(moveSlot);
        }

        // 월드 좌표 정렬로 정확히 배치
        number.transform.position = targetTransform.position;

        // 슬롯 상태 점유로 변경
        targetStatus.slotInfo = Slot_Status.slot_info.Occupied;
        
        // 새 숫자 생성 애니메이션 효과 적용 (파티클 포함)
        StartCoroutine(AnimateSlotScale(number, true));
    }

    // ========== 입력 처리 메서드 ==========

    /// <summary>
    /// 마우스 드래그 입력 처리 - 마우스 드래그를 감지하여 스와이프 방향 결정
    /// </summary>
    private void HandleMouseDragInput()
    {
        // 이동 중이면 입력 무시
        if (isMoving)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            dragStartPos = Input.mousePosition;
            isDragging = true;
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            if (Time.time < nextInputAllowedTime)
            {
                isDragging = false;
                return; // 쿨다운 중 입력 무시
            }
            Vector2 dragEndPos = Input.mousePosition;
            Vector2 delta = dragEndPos - dragStartPos;
            isDragging = false; // 한 번 처리 후 재입력 대기

            if (delta.magnitude < dragThreshold)
            {
                return; // 드래그로 취급하지 않음
            }

            SwipeDirection dir;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                dir = delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            }
            else
            {
                dir = delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
            }

            StartCoroutine(OnSwipeCoroutine(dir));
            nextInputAllowedTime = Time.time + inputCooldown; // 쿨다운 설정
        }
    }

    // 이동할 슬롯 정보를 저장하는 구조체
    private struct SlotMoveInfo
    {
        public Move_Slot slot;
        public Vector3 targetPosition;
        public int targetX;
        public int targetY;
    }

    // 합치기 정보를 저장하는 구조체
    private struct SlotMergeInfo
    {
        public Move_Slot targetSlot; // 합쳐질 대상 슬롯
        public Move_Slot sourceSlot; // 합쳐질 소스 슬롯
        public int mergedValue; // 합쳐진 값
    }

    /// <summary>
    /// 스와이프 처리 코루틴 - 이동 처리 후 숫자 생성 또는 게임 오버 체크
    /// </summary>
    private IEnumerator OnSwipeCoroutine(SwipeDirection direction)
    {
        string label = "";
        switch (direction)
        {
            case SwipeDirection.Up: label = "상"; break;
            case SwipeDirection.Down: label = "하"; break;
            case SwipeDirection.Left: label = "좌"; break;
            case SwipeDirection.Right: label = "우"; break;
        }
        Debug.Log($"{label} 방향 이동");
        
        // 이동할 슬롯 정보 및 합치기 정보 수집
        List<SlotMoveInfo> movesToAnimate = new List<SlotMoveInfo>();
        List<SlotMergeInfo> mergesToProcess = new List<SlotMergeInfo>();
        bool moved = ProcessMoveWithAnimation(direction, movesToAnimate, mergesToProcess);
        
        if (moved && (movesToAnimate.Count > 0 || mergesToProcess.Count > 0))
        {
            // 이동 중 플래그 설정
            isMoving = true;
            
            // 모든 슬롯을 애니메이션으로 이동
            if (movesToAnimate.Count > 0)
            {
                yield return StartCoroutine(AnimateSlots(movesToAnimate));
            }
            
            // 모든 이동이 완료된 후 합치기 처리
            if (mergesToProcess.Count > 0)
            {
                // 합치기 전에 소스 슬롯의 이동 정보를 리스트에서 제거 (파괴될 슬롯이므로)
                foreach (var mergeInfo in mergesToProcess)
                {
                    if (mergeInfo.sourceSlot != null)
                    {
                        // movesToAnimate 리스트에서 소스 슬롯의 이동 정보 제거
                        movesToAnimate.RemoveAll(m => m.slot == mergeInfo.sourceSlot);
                    }
                }
                
                ProcessMerges(mergesToProcess);
            }
            
            // 이동 완료 후 플래그 해제
            isMoving = false;
            
            // 모든 이동이 완료된 후 한 번만 새 숫자 생성
            createNumber();

            // 새 타일로 마지막 빈칸이 채워졌을 수 있으므로 바로 게임 오버 확인
            CheckGameOver();
            /*
            // 모든 슬롯을 노란색으로 변경 (새로 생성된 슬롯 제외)
            foreach (Move_Slot slot in activeNumberSlots)
            {
                if (!slot.isNewlyCreated)
                {
                    slot.UpdateColor();
                }
            }
            */
        }
        else
        {
            // 이동이 없을 때 게임 오버 체크
            CheckGameOver();
        }
    }

    /// <summary>
    /// 슬롯 이동 애니메이션 - 모든 슬롯을 목표 위치로 부드럽게 이동 (병렬 실행)
    /// </summary>
    private IEnumerator AnimateSlots(List<SlotMoveInfo> moves)
    {
        if (moves.Count == 0)
        {
            yield break;
        }

        // 모든 슬롯의 이동 애니메이션을 병렬로 시작
        int completedCount = 0;
        int totalCount = 0; // 실제로 시작한 코루틴 수만 센다 (null 타일을 세면 대기가 끝나지 않음)

        foreach (var moveInfo in moves)
        {
            if (moveInfo.slot != null)
            {
                totalCount++;
                StartCoroutine(AnimateSingleSlot(moveInfo, () => { completedCount++; }));
            }
        }
        
        // 모든 애니메이션이 완료될 때까지 대기
        while (completedCount < totalCount)
        {
            yield return null;
        }
    }

    /// <summary>
    /// 단일 슬롯 이동 애니메이션 - moveDuration 동안 목표 위치로 이동
    /// </summary>
    private IEnumerator AnimateSingleSlot(SlotMoveInfo moveInfo, System.Action onComplete)
    {
        // 초기 null 체크
        if (moveInfo.slot == null || moveInfo.slot.transform == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 startPos = moveInfo.slot.transform.position;
        Vector3 endPos = moveInfo.targetPosition;

        // 모든 타일이 같은 시간에 도착하도록 고정 시간으로 이동
        // (이전의 거리/속도 계산은 좌표가 픽셀 단위라 항상 상한값이 선택되어 실제로 쓰이지 않았음)
        float duration = moveDuration;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 매 프레임마다 오브젝트가 파괴되었는지 확인
            if (moveInfo.slot == null || moveInfo.slot.transform == null)
            {
                onComplete?.Invoke();
                yield break; // 오브젝트가 파괴되었으면 애니메이션 중단
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            moveInfo.slot.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        // 정확한 위치로 설정 (오브젝트가 여전히 존재하는 경우에만)
        if (moveInfo.slot != null && moveInfo.slot.transform != null)
        {
            moveInfo.slot.transform.position = endPos;
        }
        onComplete?.Invoke();
    }

    /// <summary>
    /// EaseOutElastic 이징 함수 - 탄성 효과를 주는 애니메이션 이징
    /// </summary>
    private float EaseOutElastic(float t)
    {
        const float c4 = (2f * Mathf.PI) / 3f;
        
        if (t == 0f)
        {
            return 0f;
        }
        else if (t == 1f)
        {
            return 1f;
        }
        else
        {
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
    }

    /// <summary>
    /// 슬롯 스케일 애니메이션 - easeOutElastic 효과를 사용하여 스케일 애니메이션 적용
    /// </summary>
    private IEnumerator AnimateSlotScale(GameObject slotObject, bool isNewSlot = false)
    {
        if (slotObject == null)
        {
            yield break;
        }

        Transform slotTransform = slotObject.transform;
        Vector3 originalScale = slotTransform.localScale;
        float duration = 0.6f; // 애니메이션 지속 시간
        float elapsed = 0f;

        Vector3 startScale;
        if (isNewSlot)
        {
            // 새 슬롯 생성 시: 0에서 시작
            startScale = Vector3.zero;
            slotTransform.localScale = Vector3.zero;
            yield return null; // 한 프레임 대기
        }
        else
        {
            // 합치기 시: 약간 확대된 상태에서 시작
            startScale = originalScale * 1.2f;
            slotTransform.localScale = startScale;
        }

        // 애니메이션 시작 시 한 번 파티클 재생 (Move_Slot이 있는 경우)
        Move_Slot moveSlot = slotObject.GetComponent<Move_Slot>();
        if (moveSlot != null)
        {
            moveSlot.PlayParticle();
        }

        while (elapsed < duration)
        {
            // 매 프레임마다 오브젝트가 파괴되었는지 확인
            if (slotObject == null || slotTransform == null)
            {
                yield break; // 오브젝트가 파괴되었으면 애니메이션 중단
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // easeOutElastic 적용
            float easedT = EaseOutElastic(t);
            
            // 스케일 애니메이션
            if (slotObject != null && slotTransform != null)
            {
                slotTransform.localScale = Vector3.Lerp(startScale, originalScale, easedT);
            }
            
            yield return null;
        }

        // 정확한 스케일로 설정 (오브젝트가 여전히 존재하는 경우에만)
        if (slotObject != null && slotTransform != null)
        {
            slotTransform.localScale = originalScale;
        }
    }

    /// <summary>
    /// 합치기 처리 - 모든 이동이 완료된 후 최종 위치에서 합치기 실행
    /// </summary>
    private void ProcessMerges(List<SlotMergeInfo> merges)
    {
        foreach (var mergeInfo in merges)
        {
            if (mergeInfo.targetSlot == null || mergeInfo.sourceSlot == null)
            {
                continue;
            }

            // 소스 슬롯이 이미 목표 위치로 이동했으므로, 현재 위치(목표 위치)의 슬롯 상태를 확인
            // 합치기 실행
            mergeInfo.targetSlot.slotNumber = mergeInfo.mergedValue;
            if (mergeInfo.targetSlot.text != null)
            {
                mergeInfo.targetSlot.text.text = mergeInfo.mergedValue.ToString();
            }
            mergeInfo.targetSlot.hasMergedThisTurn = true;
            
            // 이미지 업데이트 (합치기로 숫자가 변경되었으므로)
            mergeInfo.targetSlot.UpdateImage();
            
            // 점수 추가
            score += mergeInfo.mergedValue;
            if (gameUI != null)
            {
                gameUI.currentScoreText.text = score.ToString();
            }
            
            // 합치기 애니메이션 효과 적용
            StartCoroutine(AnimateSlotScale(mergeInfo.targetSlot.gameObject));
            
            // 소스 슬롯의 현재 위치(목표 위치)의 슬롯 상태는 이미 Occupied이므로 변경 불필요
            // 소스 슬롯 제거
            activeNumberSlots.Remove(mergeInfo.sourceSlot);
            Destroy(mergeInfo.sourceSlot.gameObject);
        }
    }

    // ========== 이동 처리 메서드 ==========

    /// <summary>
    /// 이동 처리 (애니메이션용) - 모든 슬롯을 지정된 방향으로 이동 및 합치기 처리, 이동 정보 수집
    /// </summary>
    private bool ProcessMoveWithAnimation(SwipeDirection direction, List<SlotMoveInfo> movesToAnimate, List<SlotMergeInfo> mergesToProcess)
    {
        bool anyMoved = false;
        bool movedThisRound = true;
        
        // 모든 슬롯의 합치기 플래그 초기화
        foreach (Move_Slot slot in activeNumberSlots)
        {
            slot.hasMergedThisTurn = false;
            slot.isNewlyCreated = false; // 이제 기존 슬롯으로 변경
        }
        
        // 더 이상 이동할 수 없을 때까지 반복
        while (movedThisRound)
        {
            movedThisRound = false;
            
            // 이동 방향에 따라 처리 순서 결정
            switch (direction)
            {
                case SwipeDirection.Up:
                    for (int x = 0; x < slotWidth; x++)
                    {
                        if (ProcessColumnStepWithAnimation(x, 0, 1, direction, movesToAnimate, mergesToProcess)) movedThisRound = true;
                    }
                    break;
                case SwipeDirection.Down:
                    for (int x = 0; x < slotWidth; x++)
                    {
                        if (ProcessColumnStepWithAnimation(x, slotHeight - 1, -1, direction, movesToAnimate, mergesToProcess)) movedThisRound = true;
                    }
                    break;
                case SwipeDirection.Left:
                    for (int y = 0; y < slotHeight; y++)
                    {
                        if (ProcessRowStepWithAnimation(y, 0, 1, direction, movesToAnimate, mergesToProcess)) movedThisRound = true;
                    }
                    break;
                case SwipeDirection.Right:
                    for (int y = 0; y < slotHeight; y++)
                    {
                        if (ProcessRowStepWithAnimation(y, slotWidth - 1, -1, direction, movesToAnimate, mergesToProcess)) movedThisRound = true;
                    }
                    break;
            }
            
            if (movedThisRound) anyMoved = true;
        }
        
        return anyMoved;
    }

    /// <summary>
    /// 열 단위 이동 처리 (애니메이션용) - 특정 열의 슬롯들을 이동 방향에 따라 정렬 후 처리
    /// </summary>
    private bool ProcessColumnStepWithAnimation(int x, int startY, int step, SwipeDirection direction, List<SlotMoveInfo> movesToAnimate, List<SlotMergeInfo> mergesToProcess)
    {
        List<Move_Slot> slotsInColumn = new List<Move_Slot>();
        
        // 해당 열의 모든 numberSlot 수집
        foreach (Move_Slot slot in activeNumberSlots)
        {
            if (slot.gridX == x)
            {
                slotsInColumn.Add(slot);
            }
        }
        
        // 이동 방향에 따라 정렬
        if (direction == SwipeDirection.Up)
        {
            slotsInColumn.Sort((a, b) => a.gridY.CompareTo(b.gridY));
        }
        else
        {
            slotsInColumn.Sort((a, b) => b.gridY.CompareTo(a.gridY));
        }
        
        return ProcessSlotsLineStepWithAnimation(slotsInColumn, step, direction, movesToAnimate, mergesToProcess);
    }

    /// <summary>
    /// 행 단위 이동 처리 (애니메이션용) - 특정 행의 슬롯들을 이동 방향에 따라 정렬 후 처리
    /// </summary>
    private bool ProcessRowStepWithAnimation(int y, int startX, int step, SwipeDirection direction, List<SlotMoveInfo> movesToAnimate, List<SlotMergeInfo> mergesToProcess)
    {
        List<Move_Slot> slotsInRow = new List<Move_Slot>();
        
        // 해당 행의 모든 numberSlot 수집
        foreach (Move_Slot slot in activeNumberSlots)
        {
            if (slot.gridY == y)
            {
                slotsInRow.Add(slot);
            }
        }
        
        // 이동 방향에 따라 정렬
        if (direction == SwipeDirection.Left)
        {
            slotsInRow.Sort((a, b) => a.gridX.CompareTo(b.gridX));
        }
        else
        {
            slotsInRow.Sort((a, b) => b.gridX.CompareTo(a.gridX));
        }
        
        return ProcessSlotsLineStepWithAnimation(slotsInRow, step, direction, movesToAnimate, mergesToProcess);
    }

    /// <summary>
    /// 슬롯 라인 이동 처리 (애니메이션용) - 한 줄의 슬롯들을 이동 방향으로 이동 및 합치기 처리, 이동 정보 수집
    /// </summary>
    private bool ProcessSlotsLineStepWithAnimation(List<Move_Slot> slots, int step, SwipeDirection direction, List<SlotMoveInfo> movesToAnimate, List<SlotMergeInfo> mergesToProcess)
    {
        bool moved = false;
        
        for (int i = 0; i < slots.Count; i++)
        {
            Move_Slot currentSlot = slots[i];
            int targetX = currentSlot.gridX;
            int targetY = currentSlot.gridY;
            
            // 이동 방향에 따라 한 칸 앞의 목표 좌표 계산
            if (direction == SwipeDirection.Up)
            {
                targetY = currentSlot.gridY - 1;
            }
            else if (direction == SwipeDirection.Down)
            {
                targetY = currentSlot.gridY + 1;
            }
            else if (direction == SwipeDirection.Left)
            {
                targetX = currentSlot.gridX - 1;
            }
            else if (direction == SwipeDirection.Right)
            {
                targetX = currentSlot.gridX + 1;
            }
            
            // 목표 위치가 보드 범위 내에 있는지 확인
            if (targetX >= 0 && targetX < slotWidth && targetY >= 0 && targetY < slotHeight)
            {
                // 목표 위치에 다른 슬롯이 있는지 확인
                Move_Slot targetSlot = GetSlotAt(targetX, targetY);
                
                if (targetSlot == null)
                {
                    // 빈 슬롯으로 이동 - 이동 정보 수집
                    Vector3 targetPosition = GetTargetPosition(targetX, targetY);
                    
                    // 같은 슬롯이 이미 리스트에 있으면 업데이트, 없으면 추가
                    int existingIndex = movesToAnimate.FindIndex(m => m.slot == currentSlot);
                    SlotMoveInfo moveInfo = new SlotMoveInfo
                    {
                        slot = currentSlot,
                        targetPosition = targetPosition,
                        targetX = targetX,
                        targetY = targetY
                    };
                    
                    if (existingIndex >= 0)
                    {
                        movesToAnimate[existingIndex] = moveInfo; // 최종 위치로 업데이트
                    }
                    else
                    {
                        movesToAnimate.Add(moveInfo);
                    }
                    
                    // 보드 상태만 즉시 업데이트 (시각적 이동은 애니메이션에서 처리)
                    MoveSlotToImmediate(currentSlot, targetX, targetY);
                    moved = true;
                }
                else if (targetSlot.slotNumber == currentSlot.slotNumber && !targetSlot.hasMergedThisTurn && !currentSlot.hasMergedThisTurn && currentSlot.slotNumber < 2048)
                {
                    // 같은 숫자 합치기 - 합치기 정보만 수집 (실제 합치기는 애니메이션 완료 후)
                    // 2048 이상의 숫자는 합쳐지지 않음
                    int mergedValue = targetSlot.slotNumber + currentSlot.slotNumber;
                    
                    // 합치기 정보 수집
                    SlotMergeInfo mergeInfo = new SlotMergeInfo
                    {
                        targetSlot = targetSlot,
                        sourceSlot = currentSlot,
                        mergedValue = mergedValue
                    };
                    mergesToProcess.Add(mergeInfo);
                    
                    // 합치기 플래그 설정 (중복 방지)
                    targetSlot.hasMergedThisTurn = true;
                    currentSlot.hasMergedThisTurn = true;
                    
                    // 보드 상태만 즉시 업데이트 (시각적 이동은 애니메이션에서 처리)
                    // 소스 슬롯을 목표 위치로 이동 정보에 추가 (합치기 전에 이동)
                    Vector3 targetPosition = GetTargetPosition(targetX, targetY);
                    SlotMoveInfo moveInfo = new SlotMoveInfo
                    {
                        slot = currentSlot,
                        targetPosition = targetPosition,
                        targetX = targetX,
                        targetY = targetY
                    };
                    movesToAnimate.Add(moveInfo);
                    MoveSlotToImmediate(currentSlot, targetX, targetY);
                    
                    moved = true;
                }
                // else: 이동 불가능 (다른 숫자이거나 범위 밖)
            }
        }
        
        return moved;
    }

    /// <summary>
    /// 특정 위치의 슬롯 가져오기 - 그리드 좌표로 활성 슬롯 검색
    /// </summary>
    private Move_Slot GetSlotAt(int x, int y)
    {
        foreach (Move_Slot slot in activeNumberSlots)
        {
            if (slot.gridX == x && slot.gridY == y)
            {
                return slot;
            }
        }
        return null;
    }

    /// <summary>
    /// 목표 위치 가져오기 - 그리드 좌표로부터 실제 월드 위치 반환
    /// </summary>
    private Vector3 GetTargetPosition(int x, int y)
    {
        if (board != null && x >= 0 && x < slotWidth && y >= 0 && y < slotHeight)
        {
            Slot_Status targetStatus = board[x, y];
            if (targetStatus != null)
            {
                return targetStatus.transform.position;
            }
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 슬롯 이동 (즉시) - 보드 상태만 업데이트하고 시각적 이동은 하지 않음 (애니메이션용)
    /// </summary>
    private void MoveSlotToImmediate(Move_Slot slot, int newX, int newY)
    {
        // 원래 위치의 슬롯 상태를 Empty로 초기화
        if (board != null && slot.gridX >= 0 && slot.gridX < slotWidth && slot.gridY >= 0 && slot.gridY < slotHeight)
        {
            Slot_Status originalStatus = board[slot.gridX, slot.gridY];
            if (originalStatus != null)
            {
                originalStatus.slotInfo = Slot_Status.slot_info.Empty;
            }
        }
        
        // 그리드 좌표 업데이트
        slot.gridX = newX;
        slot.gridY = newY;
        
        // 새 위치의 슬롯 상태를 Occupied로 설정
        if (board != null && newX >= 0 && newX < slotWidth && newY >= 0 && newY < slotHeight)
        {
            Slot_Status targetStatus = board[newX, newY];
            if (targetStatus != null)
            {
                targetStatus.slotInfo = Slot_Status.slot_info.Occupied;
            }
        }
    }

    // ========== 게임 오버 체크 메서드 ==========

    /// <summary>
    /// 빈 슬롯 확인 - 보드에 빈 슬롯이 있는지 확인
    /// </summary>
    private bool HasEmptySlot()
    {
        for (int y = 0; y < slotHeight; y++)
        {
            for (int x = 0; x < slotWidth; x++)
            {
                Slot_Status status = board != null ? board[x, y] : null;
                if (status != null && status.slotInfo == Slot_Status.slot_info.Empty)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 특정 방향 이동 가능 여부 확인 - 실제 이동 없이 해당 방향으로 이동 가능한지 체크
    /// </summary>
    private bool CanMoveInDirection(SwipeDirection direction)
    {
        // 각 슬롯이 해당 방향으로 이동 가능한지 확인
        foreach (Move_Slot slot in activeNumberSlots)
        {
            int x = slot.gridX;
            int y = slot.gridY;
            
            // 방향에 따라 경로를 따라 확인
            switch (direction)
            {
                case SwipeDirection.Up:
                    // 위쪽으로 빈 공간이나 같은 숫자가 있는지 확인
                    for (int checkY = y - 1; checkY >= 0; checkY--)
                    {
                        Move_Slot targetSlot = GetSlotAt(x, checkY);
                        if (targetSlot == null)
                        {
                            return true; // 빈 공간 발견
                        }
                        if (targetSlot.slotNumber == slot.slotNumber && slot.slotNumber < 2048)
                        {
                            return true; // 같은 숫자 발견 (합치기 가능, 2048 미만만 합치기 가능)
                        }
                        if (targetSlot.slotNumber != slot.slotNumber)
                        {
                            break; // 다른 숫자로 막혀있음
                        }
                    }
                    break;
                    
                case SwipeDirection.Down:
                    // 아래쪽으로 빈 공간이나 같은 숫자가 있는지 확인
                    for (int checkY = y + 1; checkY < slotHeight; checkY++)
                    {
                        Move_Slot targetSlot = GetSlotAt(x, checkY);
                        if (targetSlot == null)
                        {
                            return true; // 빈 공간 발견
                        }
                        if (targetSlot.slotNumber == slot.slotNumber && slot.slotNumber < 2048)
                        {
                            return true; // 같은 숫자 발견 (합치기 가능, 2048 미만만 합치기 가능)
                        }
                        if (targetSlot.slotNumber != slot.slotNumber)
                        {
                            break; // 다른 숫자로 막혀있음
                        }
                    }
                    break;
                    
                case SwipeDirection.Left:
                    // 왼쪽으로 빈 공간이나 같은 숫자가 있는지 확인
                    for (int checkX = x - 1; checkX >= 0; checkX--)
                    {
                        Move_Slot targetSlot = GetSlotAt(checkX, y);
                        if (targetSlot == null)
                        {
                            return true; // 빈 공간 발견
                        }
                        if (targetSlot.slotNumber == slot.slotNumber && slot.slotNumber < 2048)
                        {
                            return true; // 같은 숫자 발견 (합치기 가능, 2048 미만만 합치기 가능)
                        }
                        if (targetSlot.slotNumber != slot.slotNumber)
                        {
                            break; // 다른 숫자로 막혀있음
                        }
                    }
                    break;
                    
                case SwipeDirection.Right:
                    // 오른쪽으로 빈 공간이나 같은 숫자가 있는지 확인
                    for (int checkX = x + 1; checkX < slotWidth; checkX++)
                    {
                        Move_Slot targetSlot = GetSlotAt(checkX, y);
                        if (targetSlot == null)
                        {
                            return true; // 빈 공간 발견
                        }
                        if (targetSlot.slotNumber == slot.slotNumber && slot.slotNumber < 2048)
                        {
                            return true; // 같은 숫자 발견 (합치기 가능, 2048 미만만 합치기 가능)
                        }
                        if (targetSlot.slotNumber != slot.slotNumber)
                        {
                            break; // 다른 숫자로 막혀있음
                        }
                    }
                    break;
            }
        }
        return false;
    }

    /// <summary>
    /// 게임 오버 체크 및 처리 - 빈 슬롯이 없고 모든 방향으로 이동 불가능하면 게임 오버 처리 및 최고 점수 업데이트
    /// </summary>
    private async void CheckGameOver()
    {
        // 빈 슬롯이 있으면 아직 게임 계속 가능
        if (HasEmptySlot())
        {
            return;
        }

        // 모든 방향으로 이동 가능한지 확인
        bool canMoveUp = CanMoveInDirection(SwipeDirection.Up);
        bool canMoveDown = CanMoveInDirection(SwipeDirection.Down);
        bool canMoveLeft = CanMoveInDirection(SwipeDirection.Left);
        bool canMoveRight = CanMoveInDirection(SwipeDirection.Right);

        // 모든 방향으로 이동 불가능하면 게임 오버
        if (!canMoveUp && !canMoveDown && !canMoveLeft && !canMoveRight)
        {
            Debug.Log("게임 오버!");
            gameState = Game_State.Result;
            gameUI.gameOverUI.SetActive(true);
            // 점수를 high_score에 저장
            if (userData != null)
            {
                var currentData = await userData.GetCurrentPlayerData();
                if (currentData != null)
                {
                    // 현재 점수가 최고 점수보다 높으면 업데이트
                    if (score > currentData.high_score)
                    {
                        // 바꿀 필드(high_score)만 넘긴다.
                        // 서버 데이터 전체를 넘기면 병합 규칙이 playTime을 한 번 더 누적해 두 배가 됨
                        await userData.UpdatePlayerData(new User_Data.PlayerData { high_score = score });
                        Debug.Log($"최고 점수 업데이트: {score}");
                        
                        // 최고 점수 업데이트 후 UI도 갱신
                        if (gameUI != null && gameUI.highScoreText != null)
                        {
                            gameUI.highScoreText.text = score.ToString();
                        }
                    }
                }
            }
        }
    }
}
