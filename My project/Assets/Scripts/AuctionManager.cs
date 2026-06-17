using Firebase.Database;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AuctionManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://myproject-9d063-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] Text DiamondCountText;
    [SerializeField] Text IronCountText;
    [SerializeField] Text GoldCountText;
    [SerializeField] Transform AuctionCanvas;

    const string auctionName = "MainAuction";

    string userKey;
    ShopManager shopManager;
    InventoryManager inventoryManager;

    Dictionary<string, int> items = new Dictionary<string, int>();

    void Start()
    {
        database = FirebaseDatabase.GetInstance(databaseUrl);
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        shopManager = gameObject.GetComponent<ShopManager>();
        inventoryManager = gameObject.GetComponent<InventoryManager>();

        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            UpdateMessageText("[경매장] Auction시스템 : 로그인 정보가 없습니다.");
            return;
        }

        LoadAuction();
    }
    void UpdateMessageText(string message) => MessageManager.Instance.UpdateMessageText(message);

    //경매장 아이템 UI 갱신 함수
    void RefreshUI()
    {
        DiamondCountText.text = "Diamond : " + GetItemCount("Diamond");
        IronCountText.text = "Iron : " + GetItemCount("Iron");
        GoldCountText.text = "Gold : " + GetItemCount("Gold");
    }

    int GetItemCount(string itemName)
    {
        if (items.ContainsKey(itemName))
        {
            return items[itemName];
        }
        return 0;
    }

    public void ShowAuctionUI()
    {
        AuctionCanvas.gameObject.SetActive(true);
    }
    public void CloseAuctionUI()
    {
        AuctionCanvas.gameObject.SetActive(false);
    }

    public void OnClickBuyDiamond() => BuyItem("Diamond", 180);
    public void OnClickBuyIron() => BuyItem("Iron", 40);
    public void OnClickBuyGold() => BuyItem("Gold", 130);

    public void OnClickSellDiamond() => SellItem("Diamond", 140);
    public void OnClickSellIron() => SellItem("Iron", 30);
    public void OnClickSellGold() => SellItem("Gold", 80);

    //경매장 아이템 구매 함수

    void BuyItem(string itemName, int price)
    {
        if (shopManager.CurrentCoin < price)
        {
            UpdateMessageText($"[경매장] 아이템_{itemName} 구매 코인이 부족합니다.");
            return;
        }

        if (items.ContainsKey(itemName))
        {
            int count = items[itemName];
            if (count <= 0)
            {
                UpdateMessageText($"[경매장] 경매장에 아이템_{itemName} 재고가 없습니다.");
                return;
            }
        }
        else return;

        shopManager.UseCoin(price);          //코인 감소
        inventoryManager.AddItem(itemName);  //인벤토리에 아이템 추가

        items[itemName]--;

        SaveUserData(itemName, true);
        SaveAuctionItem();
    }

    //경매장 아이템 판매 함수
    void SellItem(string itemName, int price)
    {
        if (inventoryManager.Inventory.ContainsKey(itemName))
        {
            int count = inventoryManager.Inventory[itemName];
            if (count <= 0)
            {
                UpdateMessageText($"[경매장] 아이템_{itemName} 개수가 부족합니다.");
                return;
            }
        }
        else return;

        shopManager.AddCoin(price);
        inventoryManager.RemoveItem(itemName);

        if (items.ContainsKey(itemName))
            items[itemName]++;

        SaveUserData(itemName, false);
        SaveAuctionItem();
    }


    //Firebase 유저 코인 및 아이템 업데이트 함수
    void SaveUserData(string boughtItemName, bool isBuying)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventoryManager.Inventory);

        Dictionary<string, object> updateData = new Dictionary<string, object>();
        updateData["Coin"] = shopManager.CurrentCoin;
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
                        string text = isBuying ? "구매 저장 실패" : "판매 저장 실패";
                        UpdateMessageText("[경매장] " + text);
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    string text = isBuying ? "구매 완료" : "판매 완료";
                    UpdateMessageText("[경매장] " + boughtItemName + text);
                });
            });
    }

    //Firebase 경매장 아이템 업데이트 함수
    void SaveAuctionItem()
    {
        string itemsJson = JsonConvert.SerializeObject(items);

        reference
            .Child("AuctionInfo")
            .Child("Items")
            .SetValueAsync(itemsJson)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        UpdateMessageText("[경매장] 아이템 저장 실패");
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    UpdateMessageText("[경매장] 아이템 저장 성공");
                });
            });
    }


    //Firebase 경매장 데이터 로드 함수
    void LoadAuction()
    {
        reference
            .Child("AuctionInfo")
            .GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        UpdateMessageText("[경매장] 정보 로드 실패");
                    });
                    return;
                }

                DataSnapshot snapshot = task.Result;
                if (!snapshot.HasChildren)
                {
                    dispatcher.Enqueue(() =>
                    {
                        CreateAuction();
                    });
                    return;
                }

                LoadAuctionItemData();
            });

    }


    //Firebase 경매장 데이터 생성
    void CreateAuction()
    {
        AuctionData auctionData = new AuctionData(auctionName);
        string json = JsonUtility.ToJson(auctionData);

        reference.Child("AuctionInfo").SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    UpdateMessageText("[경매장] 데이터 생성 실패");
                });
                return;
            }

            dispatcher.Enqueue(() =>
            {
                UpdateMessageText("[경매장] 데이터 생성 성공");
                LoadAuctionItemData();
            });
        });
    }

    //Firebase 경매장 아이템 정보 로드
    void LoadAuctionItemData()
    {
        reference
            .Child("AuctionInfo")
            .Child("Items")
            .GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        UpdateMessageText("[경매장] 아이템 로드 실패");
                    });
                    return;
                }

                DataSnapshot snapshot = task.Result;
                string itemsJson = snapshot.Value.ToString();
                items = JsonConvert.DeserializeObject<Dictionary<string, int>>(itemsJson);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    UpdateMessageText("[경매장] 아이템 로드 성공");
                });
            });
    }
}
