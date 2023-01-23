using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;

namespace monsterTradingCardGame.BattleFunctions
{
    /// <summary>
    /// This class is used to get or update the score after a battle.
    /// </summary>
    internal class Score
    {
        public string username { set; get; }
        public int wins { set; get; }
        public int losses { set; get; }
        public int draws { set; get; }
        private int elo { set; get; }

        public Score(string username, int wins, int losses, int draws, int elo)
        {
            this.username = username;
            this.wins = wins;
            this.losses = losses;
            this.draws = draws;
            this.elo = elo;
        }

        public int GetElo()
        {
            return elo;
        }

        public override string ToString()
        {
            return string.Format("User: {0},  Wins: {1}, Losses:{2}, Draws: {3}, Elo: {4}", username, wins, losses, draws, elo);
        }
    }
}
