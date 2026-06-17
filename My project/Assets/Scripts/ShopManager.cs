using Firebase.Database;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://myproject-9d063-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] Text CoinText;

    string userKey;

    public int CurrentCoin => currentCoin;
    int currentCoin;

    private InventoryManager inventoryManager;

    void Start()
    {
        database = FirebaseDatabase.GetInstance(databaseUrl);
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
        inventoryManager = gameObject.GetComponent<InventoryManager>();

        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            UpdateMessageText("로그인 정보가 없습니다.");
            return;
        }

        LoadUserData();
    }

    void LoadUserData()
    {
        reference
            .Child("UserInfo")
            .Child(userKey)
            .GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        UpdateMessageText("유저 정보 불러오기 실패");
                    });
                    return;
                }

                DataSnapshot snapshot = task.Result;

                currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    UpdateMessageText("유저 정보 불러오기 완료");
                });
            });
    }

    void UpdateMessageText(string message) => MessageManager.Instance.UpdateMessageText(message);

    void RefreshUI()
    {
        CoinText.text = currentCoin.ToString();
    }

    public void OnClickBuyDiamond()
    {
        BuyItem("Diamond", 200);
    }

    public void OnClickBuyIron()
    {
        BuyItem("Iron", 50);
    }

    public void OnClickBuyGold()
    {
        BuyItem("Gold", 150);
    }
    public void UseCoin(int price)
    {
        currentCoin -= price;
        RefreshUI();
    }
    public void AddCoin(int price)
    {
        currentCoin += price;
        RefreshUI();
    }

    void BuyItem(string itemName, int price)
    {
        if (currentCoin < price)
        {
            UpdateMessageText($"아이템_{itemName} 구매 코인이 부족합니다.");
            return;
        }

        currentCoin -= price;

        inventoryManager.AddItem(itemName);  //인벤토리에 아이템 추가

        SaveUserData(itemName);
    }

    void SaveUserData(string boughtItemName)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventoryManager.Inventory);

        Dictionary<string, object> updateData = new Dictionary<string, object>();
        updateData["Coin"] = currentCoin;
        updateData["Inventory"] = inventoryJson;

        reference
            .Child("UserInfo")
            .Child(userKey)
            .UpdateChildrenAsync(updateData)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        UpdateMessageText("구매 저장 실패");
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    UpdateMessageText(boughtItemName + " 구매 완료");
                });
            });
    }
}
