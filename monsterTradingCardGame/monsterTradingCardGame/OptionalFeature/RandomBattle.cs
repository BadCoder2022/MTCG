using monsterTradingCardGame.BattleFunctions;
using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace monsterTradingCardGame.OptionalFeature
{
    /// <summary>
    /// This is the optional feature which uses its own database connecion to make sure that the main functionality is not hindered in any way.
    /// This class is used to create predefined cards and supply a user with 10 random cards from this predefined list.
    /// This class also features its own lobby, to make sure that the original battle lobby and this one is not mixed.
    /// The user who got more cards from the opponent wins.
    /// A draw is possible if both users got the same amount of cards from each other.
    /// </summary>
    internal class RandomBattle
    {
        private readonly NpgsqlConnection con;
        private static BLog blog = new();
        private static readonly object _lock = new();
        private static readonly ConcurrentQueue<Tuple<User, List<Card>>> playerQueue = new();    
        public RandomBattle()
        {
            string cs = "Host=localhost;Username=postgres;Password=s$cret;Database=mtcg";
            this.con = new NpgsqlConnection(cs);
        }
        /// <summary>
        /// Connects to the database.
        /// </summary>
        public void Connect()
        {
            try
            {
                con.Open();
            }
            catch (Exception)
            {
                Console.WriteLine("Could not connect to the database!\n The program will exit shortly.");
                Environment.Exit(-1);
            }
        }
        /// <summary>
        /// Closes the database connection.
        /// </summary>
        public void Close()
        {
            con?.Close();
        }

        /// <summary>
        /// Creates the predefined list of cards.
        /// This list can be added or changed for balance reasons.
        /// </summary>
        public void Init()
        {
            Connect();

            string cmd = $@"TRUNCATE TABLE randomcard;
                           Insert into Randomcard (id, type, name, damage, isMonster)
                           values ('{Guid.NewGuid()}', {(int)Type.goblin}, 'FireGoblin', 15, true),
                           ('{Guid.NewGuid()}', {(int)Type.goblin}, 'WaterGoblin', 15, true),
                           ('{Guid.NewGuid()}', {(int)Type.goblin}, 'EarthGoblin', 15, true),
                           ('{Guid.NewGuid()}', {(int)Type.ork}, 'FireOrk', 20, true),
                           ('{Guid.NewGuid()}', {(int)Type.ork}, 'WaterOrk', 20, true),
                           ('{Guid.NewGuid()}', {(int)Type.ork}, 'EarthOrk', 20, true),
                           ('{Guid.NewGuid()}', {(int)Type.elf}, 'FireElf', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.elf}, 'WaterElf', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.elf}, 'EarthElf', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.elf}, 'FireElves', 36, true),
                           ('{Guid.NewGuid()}', {(int)Type.elf}, 'WaterElves', 36, true),
                           ('{Guid.NewGuid()}', {(int)Type.elf}, 'EarthElves', 36, true),
                           ('{Guid.NewGuid()}', {(int)Type.wizard}, 'FireWizard', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.wizard}, 'WaterWizard', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.wizard}, 'EarthWizard', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.knight}, 'WaterKnight', 22, true),
                           ('{Guid.NewGuid()}', {(int)Type.knight}, 'FireKnight', 22, true),
                           ('{Guid.NewGuid()}', {(int)Type.knight}, 'EarthKnight', 22, true),
                           ('{Guid.NewGuid()}', {(int)Type.kraken}, 'WaterKraken', 27, true),
                           ('{Guid.NewGuid()}', {(int)Type.kraken}, 'FireKraken', 27, true),
                           ('{Guid.NewGuid()}', {(int)Type.kraken}, 'EarthKraken', 27, true),
                           ('{Guid.NewGuid()}', {(int)Type.dragon}, 'FireDragon', 30, true),
                           ('{Guid.NewGuid()}', {(int)Type.dragon}, 'WaterDragon', 30, true),
                           ('{Guid.NewGuid()}', {(int)Type.dragon}, 'EarthDragon', 30, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Basic FireBall (Spell)', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Basic WaterWhip (Spell)', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Basic EarthLance (Spell)', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Basic FireBall (Spell)', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Basic WaterWhip (Spell)', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Basic EarthLance (Spell)', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Basic FireBall (Spell)', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Basic WaterWhip (Spell)', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Basic EarthLance (Spell)', 17, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Great FireBall (Spell)', 23, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Summon GreatWaterSword (Spell)', 23, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Summon MoonlightSword (Spell)', 23, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Great FireBall (Spell)', 23, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Summon GreatWaterSword (Spell)', 23, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Summon MoonlightSword (Spell)', 23, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Seething Chaos (FireSpell)', 30, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Summon Tsunami (WaterSpell)', 30, true),
                           ('{Guid.NewGuid()}', {(int)Type.spell}, 'Sunlight Spear (Spell)', 30, true),
                           ('{Guid.NewGuid()}', {(int)Type.other}, 'Fatalis (Fire)', 50, true),
                           ('{Guid.NewGuid()}', {(int)Type.other}, 'Velkhana (Water)', 50, true),
                           ('{Guid.NewGuid()}', {(int)Type.other}, 'Alatreon', 50, true);";
            using NpgsqlCommand sqlcmd = new(cmd, con);
            sqlcmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Returns a random stack of 10 cards from the predefined list of cards.
        /// Each card can be acquired more than once.
        /// </summary>
        /// <returns>A list of 10 random cards.</returns>
        public List<Card> getRandomCards()
        {
            List<Card> cards = new List<Card>();

            string cmd = @"(SELECT * FROM randomcard ORDER BY random() limit 1)
                            UNION ALL
                            SELECT * FROM randomcard  LIMIT 10;";
            using NpgsqlCommand sqlcmd = new NpgsqlCommand(cmd, this.con);

            using var reader = sqlcmd.ExecuteReader();

            while (reader.Read())
            {
                Card card = new(reader.GetGuid(0), reader.GetString(2), reader.GetDouble(3));
                cards.Add(card);
            }
            return cards;
        }

        /// <summary>
        /// This is the BattleLobby for this feature, and also starts the battle if two players who want to battle are present.
        /// This method could be changed in a way to retrieve the random cards during the call of this function, but I decided against this to increase modularity.
        /// It also uses the same battle logic implemented in the Battle class
        /// </summary>
        /// <param name="player">This is a tuple of a user and his corresponding deck of cards.</param>
        /// <returns></returns>
        public BLog QueueRandomBattle(Tuple<User, List<Card>> player)
        {
            BLog localBlog = new();
            lock (_lock)
            {
                if(playerQueue.Count > 0)
                {
                    foreach (Tuple<User, List<Card>> t in playerQueue)           //Check if a user with this name is already in the queue, to avaid a user battling with himself
                    { 
                        //Checks for empty users, to make sure nothing unexpected is going to happen later on, also prohibits a user battling with himself.
                        if (t.Item1.name == player.Item1.name || t.Item1 is null)
                            return null;
                    }
                }
                
                playerQueue.Enqueue(player);
                Tuple<User, List<Card>> t1;
                Tuple<User, List<Card>> t2;

                //Lobby part of the method
                if (playerQueue.Count <= 1 )                //This is the thread waiting for the battle result
                {   
                    Monitor.Wait(_lock);
                }
                else
                {                    
                    bool fail1 = playerQueue.TryDequeue(out t1);
                    bool fail2 = playerQueue.TryDequeue(out t2);

                    blog = Battle.StartBattle(t1, t2);
                    
                    //Checks who won more cards during the battle
                    int User1Cards = 0, User2Cards = 0;
                    foreach (User user in blog.movedCards.Values)
                    {
                        if (user.name.Equals(t1.Item1.name))
                            User1Cards++;
                        
                        else if (user.name.Equals(t2.Item1.name))
                            User2Cards++;   
                    }
                    //Modifies the draw of the vanilla Battle.
                    if (User1Cards > User2Cards && blog.draw)
                    {
                        blog.Winner = t1.Item1;
                        blog.Loser = t2.Item1;
                        blog.draw = false;
                        blog.completeLog = blog.completeLog.Substring(0, blog.completeLog.Length - "There was no clear winner after 100 rounds, the result is therefore a draw!".Length) + $"Player {t1.Item1.name} won due to winning {User1Cards} cards from Player {t2.Item1.name}, whereas {t2.Item1.name} only won {User2Cards}.\r\n";
                    }
                    else if (User1Cards < User2Cards && blog.draw)
                    {
                        blog.Winner = t2.Item1;
                        blog.Loser = t1.Item1;
                        blog.draw = false;

                        blog.completeLog = blog.completeLog.Substring(0, blog.completeLog.Length - "There was no clear winner after 100 rounds, the result is therefore a draw!".Length) + $"Player {t2.Item1.name} won due to winning {User2Cards} cards from Player {t1.Item1.name}, whereas {t1.Item1.name} only won {User1Cards}.\r\n";
                    }
                    else if (blog.draw)
                    {
                        blog.draw = true;
                    }
                    
                    Monitor.Pulse(_lock);           //Release the lock so the other Thread can retrieve the modified BattleLog
                }
                localBlog = blog;
            }
            return localBlog;
        }

    }
}
