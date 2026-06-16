using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AuctionData
{
    public string AuctionName;
    public string Items;

    public AuctionData(string auctionName)
    {
        Dictionary<string, int> items = new Dictionary<string, int>();
        items["Diamond"] = 0;   //다이아몬드 - Diamond
        items["Iron"] = 0;     //철 - Iron
        items["Gold"] = 0;   //금 - Gold

        AuctionName = auctionName;
        Items = JsonConvert.SerializeObject(items);
    }
}
