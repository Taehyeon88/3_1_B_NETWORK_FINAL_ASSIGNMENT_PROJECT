using Firebase.Database;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://myproject-9d063-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] Text UnitListText;
    [SerializeField] Text MessageText;

    string userKey;
    Dictionary<string, bool> unitList = new Dictionary<string, bool>();

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
            UpdateMessageText("Unit시스템 : 로그인 정보가 없습니다.");
            return;
        }

        LoadUserData();
    }

    void LoadUserData()
    {
        reference
            .Child("UserInfo")
            .Child(userKey)
            .Child("UnitList")
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
                string unitListString = snapshot.Value.ToString();
                unitList = JsonConvert.DeserializeObject<Dictionary<string, bool>>(unitListString);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    UpdateMessageText("유닛 정보 불러오기 성공");
                });
            });
    }

    void RefreshUI()
    {
        UnitListText.text = "My Units : " + "\n";

        foreach (var unit in unitList)
        {
            if (unit.Value)
                UnitListText.text += unit.Key + "\t";
        }
    }

    void UpdateMessageText(string message)
    {
        MessageText.text += message + "\n";
    }

    public void BuyUnit1() => BuyUnit(1, 100);
    public void BuyUnit2() => BuyUnit(2, 150);
    public void BuyUnit3() => BuyUnit(3, 200);
    public void BuyUnit4() => BuyUnit(4, 250);
    public void BuyUnit5() => BuyUnit(5, 300);
    public void BuyUnit6() => BuyUnit(6, 350);

    //유닛 구매 ( 구매한 유닛은 다시 구매 안됨 처리 )
    void BuyUnit(int unitId, int price)
    {
        string unitName = "Unit" + unitId;

        if (unitList.ContainsKey(unitName))
        {
            bool ishave = unitList[unitName];
            if (ishave)
            {
                UpdateMessageText($"유닛:{unitId} 재구매가 불가능합니다.");
                return;
            }
        }
        else return;

        if (shopManager.CurrentCoin < price)
        {
            UpdateMessageText($"유닛:{unitId} 구매 코인이 부족합니다.");
            return;
        }

        //클라이언트 내부 변수 업데이트
        unitList[unitName] = true;
        shopManager.UseCoin(price);

        //Firebase unitList 업데이트
        SaveUnitData();
    }

    //유닛 구매 정보 갱신
    void SaveUnitData()
    {
        string unitListJson = JsonConvert.SerializeObject(unitList);

        Dictionary<string, object> updateData = new Dictionary<string, object>();
        updateData["Coin"] = shopManager.CurrentCoin;
        updateData["UnitList"] = unitListJson;

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
                        UpdateMessageText("유닛 구매 저장 실패");
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    UpdateMessageText("유닛 구매 성공");
                });
            });
    }
}
