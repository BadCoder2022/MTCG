using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace monsterTradingCardGame.BattleFunctions
{
    internal class Battle
    {


        /// <summary>
        /// Starts a battle between player1 and player2.
        /// A battle can take a maximum of 100 rounds and will be called a draw if nobody won during these rounds
        /// </summary>
        /// <param name="player1">The first player with his deck.</param>
        /// <param name="player2">The second player with his deck.</param>
        /// <returns>This method returns a battle log, which can be accessed to return a reply.</returns>
        public static BLog StartBattle(Tuple<User, List<Card>> player1, Tuple<User, List<Card>> player2)
        {
            Console.WriteLine("Enter Battle");
            BLog bl = new();
            StringBuilder sb = new($"Battle between {player1.Item1.name} and {player2.Item1.name} starting shortly.\n\n");
            for (int i = 0; i < 99; i++)         //A Maximum of 100 Rounds
            {
                if (player2.Item2.Count < 1 || player1.Item2.Count < 1)
                {
                    break;
                }

                Card c1 = GetRandomCard(player1.Item2);
                Card c2 = GetRandomCard(player2.Item2);
                sb.Append($"Player1 {player1.Item1.name} drew {c1.Name} ({c1.Damage}) \n");
                sb.Append($"Player2 {player2.Item1.name} drew {c2.Name} ({c2.Damage}) \n");

                Tuple<Card, string> result = CardBattle(c1, c2);
                if (result.Item1 is null)
                {
                    sb.Append(result.Item2 + "\n");
                }
                else if (result.Item1 == c1)
                {
                    // Console.WriteLine("Player 1 lost " + player1.Item2.Count.ToString());
                    bl.LogMovedCard(c1, player2.Item1);
                    player1.Item2.Remove(c1);
                    player2.Item2.Add(c1);
                }
                else
                {
                    //Console.WriteLine("Player 2 lost " + player2.Item2.Count.ToString());
                    bl.LogMovedCard(c2, player1.Item1);
                    player2.Item2.Remove(c2);
                    player1.Item2.Add(c2);
                }
                sb.Append(result.Item2 + "\n\n");
            }

            if (player1.Item2.Count == 0)
            {
                Console.WriteLine("Winner1");
                bl.Winner = player2.Item1;
                bl.Loser = player1.Item1;
                sb.Append($"Player {player2.Item1.name} has won, because he won all cards from player {player1.Item1.name}.");
                bl.Add2Log(sb);
                bl.Convert2String();
                return bl;
            }
            else if (player2.Item2.Count == 0)
            {
                Console.WriteLine("Winner2");
                bl.Winner = player2.Item1;
                bl.Loser = player1.Item1;
                sb.Append($"Player {player1.Item1.name} has won, because he won all cards from player {player2.Item1.name}.");
                bl.Add2Log(sb);
                bl.Convert2String();
                return bl;
            }

            sb.Append($"There was no clear winner after 100 rounds, the result is therefore a draw!");
            bl.draw = true;
            bl.Add2Log(sb);
            bl.Convert2String();
            bl.Winner = player1.Item1;
            bl.Loser = player2.Item1;
            return bl;
        }

        /// <summary>
        /// Battles two cards and returns the card which lost the battle.
        /// There are various special rules and elements which influence the strength of a card.
        /// It is easier this way, because the lost card needs to be moved to the other players deck.
        /// </summary>
        /// <param name="c1">The first card which is part of the battle.</param>
        /// <param name="c2">THe second which is compared to the first one.</param>
        /// <returns>The card which lost the battle.</returns>
        private static Tuple<Card, string> CardBattle(Card c1, Card c2)
        { // Returns the losing card and a Battlelog
            if ((int)c1.CType > 1 && (int)c2.CType > 1)
            {     //MonsterFights

                //First special fight rules
                if (c1.CType == Type.goblin && c2.CType == Type.dragon || c2.CType == Type.goblin && c1.CType == Type.dragon)
                {
                    if (c1.CType == Type.goblin)
                        return Tuple.Create(c1, $"{c1.Name} (goblin) is too afraid to attack {c2.Name} (dragon).");
                    else
                        return Tuple.Create(c2, $"{c2.Name} (goblin) is too afraid to attack {c1.Name} (dragon).");
                }
                else if (c1.CType == Type.wizard && c2.CType == Type.ork || c2.CType == Type.wizard && c1.CType == Type.ork)
                {
                    if (c1.CType == Type.ork)
                        return Tuple.Create(c1, $"{c1.Name} (Ork) is not able to attack {c2.Name} (wizard).");
                    else
                        return Tuple.Create(c2, $"{c2.Name} (Ork) is not able to attack {c1.Name} (wizard).");
                }
                else if (c1.CType == Type.dragon && c2.CType == Type.elf && c2.Name.Contains("fire", StringComparison.OrdinalIgnoreCase) || c2.CType == Type.dragon && c1.CType == Type.elf && c1.Name.Contains("fire", StringComparison.OrdinalIgnoreCase))
                {
                    if (c1.CType == Type.dragon)
                        return Tuple.Create(c1, $"{c1.Name} (FireElves) can evade the {c2.Name} (dragons) attacks.");
                    else
                        return Tuple.Create(c2, $"{c2.Name} (FireElves) can evade the {c1.Name} (dragons) attacks.");
                }
                //Compares the damage between two monster cards, if no special rules apply.
                if (c1.Damage > c2.Damage)
                {
                    return Tuple.Create(c2, $"Monster {c1.Name} ({c1.Damage}) has slain {c2.Name} ({c2.Damage}).");
                }
                else if (c1.Damage < c2.Damage)
                {
                    return Tuple.Create(c1, $"Monster {c2.Name} ({c2.Damage}) has slain {c1.Name} ({c1.Damage}).");
                }
                else //draw between two monster cards
                {
                    return Tuple.Create<Card, string>(null, $"{c1.Name} ({c1.Damage}) drew with {c2.Name} ({c2.Damage})");
                }
            }
            //Mixed or Spell fights
            //Second part of the special fight rules
            if (c1.CType == Type.knight && c2.CType == Type.spell && c2.Name.Contains("water", StringComparison.OrdinalIgnoreCase) || c2.CType == Type.knight && c1.CType == Type.spell && c1.Name.Contains("water", StringComparison.OrdinalIgnoreCase))
            {
                if (c1.CType == Type.spell)
                    return Tuple.Create(c2, $"{c2.Name} has drown in a water spell.");
                else
                    return Tuple.Create(c1, $"{c1.Name} has drown in a water spell.");
            }
            else if (c1.CType == Type.kraken && c2.CType == Type.spell || c2.CType == Type.kraken && c1.CType == Type.spell)
            {
                if (c1.CType == Type.spell)
                    return Tuple.Create(c1, $"{c2.Name} is immune to spells.");
                else
                    return Tuple.Create(c2, $"{c1.Name} is immune to spells.");
            }

            //Elemental check which is only done if at least one card is a spell.
            //effective damage is needed to make the damage comparison easier, as the damage value of a card should not change.
            double c1_effDamage = c1.Damage;
            double c2_effDamage = c2.Damage;

            //Water is strong against Fire.
            //Fire is strong against Normal.
            //Normal is strong against Water.
            if (c1.Element != c2.Element)              //same Element => no changes
            {
                if ((int)c1.Element + 1 == (int)c2.Element || c1.Element == Element.normal && c2.Element == Element.fire)
                {   //Card 2's element is effective
                    c1_effDamage /= 2;
                    c2_effDamage *= 2;
                }
                else
                {   //Card 1's Element is effective 
                    c1_effDamage *= 2;
                    c2_effDamage /= 2;
                }
            }
            //effe
            if (c1_effDamage < c2_effDamage)
            {
                return new Tuple<Card, string>(c1, $"{c2.Name} ({c2.Damage}) has slain {c1.Name} ({c1.Damage}).");
            }
            else if (c1_effDamage > c2_effDamage)
            {
                return new Tuple<Card, string>(c2, $"{c1.Name} ({c1.Damage}) has slain {c2.Name} ({c2.Damage}).");
            }
            return new Tuple<Card, string>(null, $"{c1.Name} ({c1.Damage}) drew with {c2.Name} ({c2.Damage}).");
        }

        /// <summary>
        /// Returns a card chosen randomly from the provided deck.
        /// </summary>
        /// <param name="deck">The current list of card</param>
        /// <returns>A single Card chosen randomly from the provided deck.</returns>
        private static Card GetRandomCard(List<Card> deck)
        {
            Random rand = new();
            return deck[rand.Next(deck.Count)];
        }
    }

    /// <summary>
    /// This class is used to gather and return the results of a battle.
    /// </summary>
    internal class BLog
    {
        private StringBuilder log { get; set; }
        public User Winner { get; set; }
        public User Loser { get; set; }
        public Dictionary<Card, User> movedCards { get; set; }
        public string completeLog { get; set; }

        public bool draw { get; set; }


        public BLog()
        {
            draw = false;
            log = new StringBuilder();
            Winner = new User();
            Loser = new User();
            completeLog = "";
            movedCards = new Dictionary<Card, User>();
        }
        
        public void Add2Log(StringBuilder sb)
        {
            log.Append(sb);
        }

        public void Convert2String()
        {
            completeLog = log.ToString();
        }

        /// <summary>
        /// Is used to log the cards which need to be moved after a battle.
        /// If a card is entered twice, it is deleted from the dictionary.
        /// </summary>
        /// <param name="card"></param>
        /// <param name="winner"></param>
        public void LogMovedCard(Card card, User winner)
        {
            if (movedCards.ContainsKey(card))
            {
                movedCards.Remove(card);
            }
            else
            {
                movedCards.Add(card, winner);
            }
        }
    }
}