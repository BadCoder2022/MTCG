using System.Text.Json.Serialization;

namespace monsterTradingCardGame.Trades
{
    /// <summary>
    /// This class is used to work with the Trade information provided by the users or stored by the database.
    /// </summary>
    internal class Trade
    {
        [JsonPropertyName("Id")]
        public Guid TradeID { set; get; }

        [JsonPropertyName("CardToTrade")]
        public Guid Cardid { set; get; }
        [JsonPropertyName("Type")]
        public string Type { set; get; }
        [JsonPropertyName("MinimumDamage")]
        public double MinDamage { get; set; }
        public string Trader { set; get; }

        public Trade(Guid tradeID, Guid cardid, string type, double minDamage, string trader)
        {
            TradeID = tradeID;
            Cardid = cardid;
            Type = type;
            MinDamage = minDamage;
            Trader = trader;
        }

        public Trade(Guid tradeID, Guid cardid, string type, double minDamage)
        {
            TradeID = tradeID;
            Cardid = cardid;
            Type = type;
            MinDamage = minDamage;
            Trader = "";
        }

        public Trade()
        {
            TradeID = new Guid();
            Cardid = new Guid();
            Type = "";
            MinDamage = 0;
            Trader = "";
        }

        //Returns True when the Trader wants a monster.
        public bool isMonster()
        {
            if (Type.ToLower().Contains("monster"))
                return true;
            else
                return false;
        }

        //Returns whether the Trader wants a spell.
        public bool isSpell()
        {
            if (Type.ToLower().Contains("spell"))
                return true;
            else
                return false;
        }
    }
}
