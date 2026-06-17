using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;

public class RewardManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://myproject-9d063-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] Text MyScoreText;
    [SerializeField] Text ScoreText;
    [SerializeField] Transform RewardCanvas;

    int my_score;
    int current_score;
    string userKey;
    ShopManager shopManager;

    void Start()
    {
        database = FirebaseDatabase.GetInstance(databaseUrl);
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        shopManager = gameObject.GetComponent<ShopManager>();

        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            UpdateMessageText("Reward시스템 : 로그인 정보가 없습니다.");
            return;
        }

        LoadUserData();
    }

    void LoadUserData()
    {
        reference
            .Child("UserInfo")
            .Child(userKey)
            .Child("Score")
            .GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        UpdateMessageText("유저 정보를 찾을 수 없습니다.");
                        return;
                    });
                }

                my_score = int.Parse(task.Result.Value.ToString());

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    UpdateMessageText("Reward시스템 : 점수 로드 성공");
                });
            });
    }

    void RefreshUI()
    {
        MyScoreText.text = "My Score:" + my_score;
        ScoreText.text = "Score:" + (current_score <= 0? " - " : current_score);
    }

    void UpdateMessageText(string message) => MessageManager.Instance.UpdateMessageText(message);

    public void ShowRewardUI()
    {
        RewardCanvas.gameObject.SetActive(true);
        RefreshUI();
    }

    //점수를 돌리는 함수 (최고 점수 초과 체크)
    public void GetScore()
    {
        current_score = Mathf.Max(0, NormalDistribution(my_score, 5f));
        ScoreText.text = "Score:" + current_score.ToString();

        if (current_score > my_score)
        {
            my_score = current_score;
            UpdateScoreData();
        }
    }

    //보상을 획득하는 함수
    public void GetReward()
    {
        if (current_score <= 0)
        {
            UpdateMessageText("현재 획득한 점수가 없습니다. 점수 획득을 하고 보상을 획득하세요");
            return;
        }

        int reward = current_score * Mathf.CeilToInt(UnityEngine.Random.value * 10);

        shopManager.AddCoin(reward);
        UpdateCoinData();

        //보상화면 종료
        RewardCanvas.gameObject.SetActive(false);
        current_score = 0;
    }

    //Firebase 점수 갱신 함수
    void UpdateScoreData()
    {
        reference
            .Child("UserInfo")
            .Child(userKey)
            .Child("Score")
            .SetValueAsync(my_score)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        UpdateMessageText("점수 저장 실패");
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    UpdateMessageText($"최고 점수 {my_score} 갱신!!");
                });
            });
    }

    //Firebase 코인 갱신 함수
    void UpdateCoinData()
    {
        reference
            .Child("UserInfo")
            .Child(userKey)
            .Child("Coin")
            .SetValueAsync(shopManager.CurrentCoin)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        UpdateMessageText("코인 저장 실패");
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    UpdateMessageText($"코인 {my_score}개 획득 성공(보상) ");
                });
            });
    }

    //정규분포 함수
    int NormalDistribution(int mean, float stdDev)
    {
        float u1 = Random.value;
        float u2 = Random.value;
        float z = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2);
        return Mathf.FloorToInt(mean + stdDev * z);
    }
}
