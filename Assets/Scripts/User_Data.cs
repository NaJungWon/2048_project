using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using System.Threading.Tasks;
using System;

public class User_Data : MonoBehaviour
{
    public Game_System gameSystem;
    private DatabaseReference dbref = null; // Firebase 데이터베이스 참조
    private bool isInitialized = false; // Firebase 초기화 상태
    private string playerUID = ""; // 플레이어 고유 ID
    // 플레이어 데이터 구조를 정의하는 클래스
    [System.Serializable]
    public class PlayerData
    {
        public string user_name; // 사용자 이름
        public int high_score; // 최고 점수
        public float playTime; // 플레이 시간
        public int connectCount; // 접속 횟수
        public int lastLoginTime; // 마지막 로그인 시간
    }

    // Start is called before the first frame update
    async void Start()
    {
        gameSystem = GetComponent<Game_System>();
        
        // 게임 시작 시 Firebase 초기화 후 자동으로 게스트 로그인 수행
        await InitializeFirebase();
        
        // Firebase 초기화가 성공한 경우 게스트 로그인 자동 실행
        if (isInitialized && dbref != null)
        {
            await GuestLogin();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    //Firebase 초기화
    private async Task InitializeFirebase()
    {
        try
        {
            // Firebase 초기화 체크
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase 앱이 이미 초기화되어 있는지 확인
                if (FirebaseApp.DefaultInstance == null)
                {
                    Debug.LogError("Firebase 앱이 초기화되지 않았습니다.");
                    return;
                }

                // Database 참조 가져오기
                dbref = FirebaseDatabase.DefaultInstance.RootReference;
                isInitialized = true; // 초기화 완료 상태로 설정
                Debug.Log("Firebase 초기화 성공!");
            }
            else
            {
                Debug.LogError($"Firebase 종속성 오류: {dependencyStatus}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Firebase 초기화 실패: {e.Message}");
        }
    }

    // 플레이어 UID 초기화 (기존 플레이어 확인 후 새 UID 생성)
    private async Task InitializePlayerUID()
    {
        try
        {
            // 1. 기존 플레이어 데이터 확인
            string existingUID = await CheckExistingPlayer();
            
            if (!string.IsNullOrEmpty(existingUID))
            {
                // 기존 플레이어 발견 - 기존 UID 사용
                playerUID = existingUID;
                Debug.Log($"기존 플레이어 발견 - UID: {playerUID}");
                // 기존 플레이어 데이터 업데이트
                await UpdateExistingPlayerData();
            }
            else
            {
                // 새 플레이어 - 새 UID 생성
                playerUID = await GenerateNewUID();
                Debug.Log($"새 플레이어 - 생성된 UID: {playerUID}");
                // 새 플레이어 데이터 생성
                await CreateNewPlayerData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UID 초기화 실패: {e.Message}");
        }
    }

    // 기존 플레이어 확인 (디바이스 ID로 검색)
    private async Task<string> CheckExistingPlayer()
    {
        try
        {
            string deviceID = SystemInfo.deviceUniqueIdentifier;
            
            // 디바이스 ID로 기존 플레이어 검색
            var snapshot = await dbref.Child("device_mapping").Child(deviceID).GetValueAsync();
            
            if (snapshot.Exists)
            {
                return snapshot.Value.ToString();
            }
            
            return null; // 기존 플레이어 없음
        }
        catch (System.Exception e)
        {
            Debug.LogError($"기존 플레이어 확인 실패: {e.Message}");
            return null;
        }
    }

    // 새 UID 생성
    private async Task<string> GenerateNewUID()
    {
        try
        {
            // Firebase에서 현재 최대 UID 가져오기
            var snapshot = await dbref.Child("uid_counter").GetValueAsync();
            
            int nextUID = 1000001; // 기본 시작 번호
            
            if (snapshot.Exists)
            {
                nextUID = int.Parse(snapshot.Value.ToString()) + 1;
            }
            
            // 새로운 UID를 카운터에 저장
            await dbref.Child("uid_counter").SetValueAsync(nextUID);
            
            return nextUID.ToString();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"새 UID 생성 실패: {e.Message}");
            // 실패 시 디바이스 ID 기반으로 폴백
            return SystemInfo.deviceUniqueIdentifier.GetHashCode().ToString().Replace("-", "");
        }
    }

    // 기존 플레이어 데이터 업데이트
    private async Task UpdateExistingPlayerData()
    {
        try
        {
            // Firebase가 초기화되지 않았으면 저장하지 않음
            if (!isInitialized || dbref == null)
            {
                Debug.LogWarning("Firebase가 초기화되지 않아 데이터를 저장할 수 없습니다.");
                return;
            }

            // 기존 데이터 가져오기
            var existingData = await GetExistingPlayerData();
            if (existingData != null)
            {
                // 연결 횟수 증가 및 로그인 시간 업데이트
                existingData.connectCount++;
                existingData.lastLoginTime = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
                
                // Firebase에 업데이트된 데이터 저장
                string json = JsonUtility.ToJson(existingData);
                await dbref.Child("players").Child(playerUID).SetRawJsonValueAsync(json);
                
                Debug.Log("기존 플레이어 데이터 업데이트 완료!");
            }
            else
            {
                Debug.LogError("기존 플레이어 데이터를 찾을 수 없습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"기존 플레이어 데이터 업데이트 실패: {e.Message}");
        }
    }

    // 새 플레이어 데이터 생성
    private async Task CreateNewPlayerData()
    {
        try
        {
            // Firebase가 초기화되지 않았으면 저장하지 않음
            if (!isInitialized || dbref == null)
            {
                Debug.LogWarning("Firebase가 초기화되지 않아 데이터를 저장할 수 없습니다.");
                return;
            }

            // 새 플레이어 데이터 생성
            PlayerData newPlayerData = new PlayerData
            {
                user_name = playerUID,
                high_score = 0,
                playTime = 0,
                connectCount = 1,
                lastLoginTime = int.Parse(DateTime.Now.ToString("yyyyMMdd"))
            };

            // Firebase에 새 플레이어 데이터 저장
            string json = JsonUtility.ToJson(newPlayerData);
            await dbref.Child("players").Child(playerUID).SetRawJsonValueAsync(json);
            
            // 디바이스 ID와 UID 매핑 저장
            await dbref.Child("device_mapping").Child(SystemInfo.deviceUniqueIdentifier).SetValueAsync(playerUID);
            
            Debug.Log("새 플레이어 데이터 생성 완료!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"새 플레이어 데이터 생성 실패: {e.Message}");
        }
    }

    // 기존 플레이어 데이터 가져오기
    private async Task<PlayerData> GetExistingPlayerData()
    {
        try
        {
            var snapshot = await dbref.Child("players").Child(playerUID).GetValueAsync();
            if (snapshot.Exists)
            {
                string json = snapshot.GetRawJsonValue();
                return JsonUtility.FromJson<PlayerData>(json);
            }
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"기존 데이터 가져오기 실패: {e.Message}");
            return null;
        }
    }
    
    // 완전 초기화 - Firebase의 모든 데이터 삭제
    public async Task Delete()
    {
        // Firebase가 초기화되지 않았거나 dbref가 null인 경우 종료
        if (!isInitialized || dbref == null) return;

        try
        {
            Debug.Log("=== 완전 초기화 시작 ===");
            
            // 1. 모든 플레이어 데이터 삭제
            await dbref.Child("players").RemoveValueAsync();
            Debug.Log("모든 플레이어 데이터 삭제 완료");
            
            // 2. 모든 디바이스 매핑 삭제
            await dbref.Child("device_mapping").RemoveValueAsync();
            Debug.Log("모든 디바이스 매핑 삭제 완료");
            
            // 3. UID 카운터 리셋 (기본 시작 번호로)
            await dbref.Child("uid_counter").SetValueAsync(1000000);
            Debug.Log("UID 카운터 리셋 완료 (시작 번호: 1000001)");
            
            // 4. 로컬 UID 초기화
            playerUID = "";
            
            Debug.Log("=== 완전 초기화 완료 ===");
            Debug.Log("다음 게임 시작 시 새 플레이어로 시작됩니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"완전 초기화 실패: {e.Message}");
        }
    }

    // 게스트 로그인 메서드 (Start에서 자동 호출 또는 수동 호출 가능)
    public async Task GuestLogin()
    {
        try
        {
            // Firebase가 초기화되지 않았으면 로그인 불가
            if (!isInitialized || dbref == null)
            {
                Debug.LogError("Firebase가 초기화되지 않았습니다. 잠시 후 다시 시도해주세요.");
                return;
            }

            Debug.Log("게스트 로그인 시작...");
            
            // 플레이어 UID 확인 및 생성
            await InitializePlayerUID();
            
            Debug.Log("게스트 로그인 완료!");
            
            // 게스트 로그인 완료 후 최고 점수 가져와서 Game_System에 적용
            if (gameSystem != null)
            {
                var playerData = await GetCurrentPlayerData();
                if (playerData != null)
                {
                    gameSystem.ApplyHighScore(playerData.high_score);
                }
                else
                {
                    gameSystem.ApplyHighScore(0);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"게스트 로그인 실패: {e.Message}");
            // 에러 발생 시에도 기본값 적용
            if (gameSystem != null)
            {
                gameSystem.ApplyHighScore(0);
            }
        }
    }

    // 현재 UID 반환
    public string GetCurrentUID()
    {
        return playerUID;
    }

    // Firebase에서 플레이어 데이터를 직접 가져오기
    public async Task<PlayerData> GetCurrentPlayerData()
    {
        try
        {
            if (string.IsNullOrEmpty(playerUID))
            {
                Debug.LogError("UID가 설정되지 않았습니다.");
                return null;
            }

            var snapshot = await dbref.Child("players").Child(playerUID).GetValueAsync();
            if (snapshot.Exists)
            {
                string json = snapshot.GetRawJsonValue();
                return JsonUtility.FromJson<PlayerData>(json);
            }
            else
            {
                Debug.LogWarning("저장된 플레이어 데이터가 없습니다.");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"플레이어 데이터 가져오기 실패: {e.Message}");
            return null;
        }
    }

    // 플레이어 데이터 업데이트 (Firebase에 직접 저장)
    public async Task UpdatePlayerData(PlayerData newData)
    {
        try
        {
            if (string.IsNullOrEmpty(playerUID))
            {
                Debug.LogError("UID가 설정되지 않았습니다.");
                return;
            }

            // 기존 데이터 가져오기
            PlayerData currentData = await GetCurrentPlayerData();
            if (currentData == null)
            {
                Debug.LogError("기존 플레이어 데이터를 찾을 수 없습니다.");
                return;
            }

            // 기존 데이터와 새 데이터 병합
            // user_name: 명시적으로 설정된 경우에만 업데이트 (빈 문자열이 아닌 경우)
            if (!string.IsNullOrEmpty(newData.user_name) && newData.user_name != "")
                currentData.user_name = newData.user_name;
            
            // high_score: 더 큰 값으로 업데이트 (게임 오버 시 업데이트되므로)
            if (newData.high_score > currentData.high_score)
                currentData.high_score = newData.high_score;
            
            // playTime: 누적 시간이므로 기존 값에 더함
            if (newData.playTime > 0)
                currentData.playTime += newData.playTime;
            
            // connectCount: 명시적으로 설정된 경우에만 업데이트
            if (newData.connectCount > 0)
                currentData.connectCount = newData.connectCount;
            
            // lastLoginTime: 명시적으로 설정된 경우에만 업데이트
            if (newData.lastLoginTime > 0)
                currentData.lastLoginTime = newData.lastLoginTime;

            string json = JsonUtility.ToJson(currentData);
            await dbref.Child("players").Child(playerUID).SetRawJsonValueAsync(json);
            Debug.Log("플레이어 데이터 업데이트 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"플레이어 데이터 업데이트 실패: {e.Message}");
        }
    }

    // 플레이어 데이터를 Firebase에 저장 (Game_Manager에서 사용)
    public async Task SavePlayerDataToFirebase()
    {
        try
        {
            if (string.IsNullOrEmpty(playerUID))
            {
                Debug.LogError("UID가 설정되지 않았습니다.");
                return;
            }

            // 현재 플레이어 데이터 가져오기
            var currentData = await GetCurrentPlayerData();
            if (currentData != null)
            {
                // Firebase에 저장
                string json = JsonUtility.ToJson(currentData);
                await dbref.Child("players").Child(playerUID).SetRawJsonValueAsync(json);
                Debug.Log("플레이어 데이터 저장 완료");
            }
            else
            {
                Debug.LogWarning("저장할 플레이어 데이터가 없습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"플레이어 데이터 저장 실패: {e.Message}");
        }
    }

    // 게스트 로그인 완료 후 최고 점수 가져오기
    public async Task<int> GetHighScoreAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(playerUID))
            {
                Debug.LogWarning("UID가 설정되지 않았습니다.");
                return 0;
            }

            var playerData = await GetCurrentPlayerData();
            if (playerData != null)
            {
                return playerData.high_score;
            }
            
            return 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"최고 점수 가져오기 실패: {e.Message}");
            return 0;
        }
    }
}
