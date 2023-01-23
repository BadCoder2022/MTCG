using monsterTradingCardGame.BattleFunctions;
using monsterTradingCardGame.Trades;
using Npgsql;
using System.Data;
using System.Reflection.PortableExecutable;
using System.Text;

namespace monsterTradingCardGame
{
    /// <summary>
    /// This class is used for every database connection, except for the optional feature, which uses a seperate connection.
    /// Every method that needs a database query or needs to execute an sql-command uses one of these methods.
    /// A lock is needed, as only one connection is used for nearly all database queries.
    /// Without the lock, an NPGSQL-Exception was sometimes thrown, because a progress was already in use.
    /// </summary>
    internal class DB
    {
        private readonly NpgsqlConnection con;
        private static readonly object _lock = new();
        public DB()
        {
            string cs = "Host=localhost;Username=postgres;Password=s$cret;Database=mtcg";
            con = new NpgsqlConnection(cs);
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
                System.Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Clears the tokens for each user, to make it easier to test the session / token login functionality, even if the user already exist.
        /// </summary>
        public void InitDB()
        {
            string cmd = @"UPDATE gamer 
                         SET token = NULL
                         WHERE token IS NOT NULL;";
            using NpgsqlCommand sqlcmd = new(cmd, con);
            sqlcmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Closes the database connection
        /// </summary>
        public void Close()
        {
            con.Close();
        }

        /// <summary>
        /// Creates a user in the db.
        /// </summary>
        /// <param name="username">This is the used username, which is unique.</param>
        /// <param name="pwd">This is the password, which will be hashed, by the use of the pgcrypto extension for PostgresQL.</param>
        /// <returns>True if the user could be created, False if it fails.</returns>
        public bool CreateUser(string username, string pwd)
        {
            if (username == null || pwd == null)
            {
                Console.WriteLine("Missing username or password");
                return false;
            }

            string cmd = @"
                INSERT INTO gamer 
                    (id, name, coins, password)
                VALUES
                    (default, @name, 20, crypt(@password, gen_salt('bf')));
                INSERT INTO score 
                        (id, gamer, wins, losses, draws, elo)
                    VALUES
                        (default, @name, 0, 0, 0, 100);";
            using NpgsqlCommand sqlcmd = new(cmd, con);
            sqlcmd.Parameters.AddWithValue("@name", username);
            sqlcmd.Parameters.AddWithValue("@password", pwd);

            return sqlcmd.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Checks whether a user with the provided username already exists in the database.
        /// This is important, because usernames need to be unique.
        /// </summary>
        /// <param name="username">The username which will be looked for in the database.</param>
        /// <returns>True if a user was found, false if not.</returns>
        public virtual bool UserExists(string username)
        {
            string cmd = @"SELECT id
                            FROM gamer
                            WHERE name = @username;
                          ";
            lock (_lock)
            {
                NpgsqlCommand sqlcmd = new(cmd, con);
                sqlcmd.Parameters.AddWithValue("@username", username);
                using var reader = sqlcmd.ExecuteReader();
                return reader.HasRows;
            }
        }
        /// <summary>
        /// Gets the stored userinformation from the database using the token as an identifier.
        /// The token is also unique, due to the fact, that the username is used and a string added to it.
        /// </summary>
        /// <param name="token">The token which will be used to get the stored user information.</param>
        /// <returns>A User object containing the information stored in the db.</returns>
        public virtual User? GetUserInformation(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            string cmd = @"SELECT *
                            FROM gamer
                            WHERE token = @token;
                          ";
            lock (_lock)
            {
                using NpgsqlCommand sqlcmd = new(cmd, con);
                sqlcmd.Parameters.AddWithValue("@token", token);
                using var reader = sqlcmd.ExecuteReader();

                User dbUser = new("", "");
                while (reader.Read())
                {

                    dbUser.name = reader.GetString(1);
                    dbUser.coins = reader.GetInt32(2);
                    dbUser.token = reader.GetString(3);
                    dbUser.pwd = reader.GetString(4);

                    dbUser.aliasname = reader.IsDBNull(5) ? "" : reader.GetString(5);
                    dbUser.bio = reader.IsDBNull(6) ? "" : reader.GetString(6);
                    dbUser.image = reader.IsDBNull(7) ? "" : reader.GetString(7);

                }
                reader.Close();
                return dbUser;
            }
        }

        /// <summary>
        /// Verifies whether the provided username and password match with the stored user and sets the token in the database.
        /// The token is created by a method from the User class.
        /// </summary>
        /// <param name="username">The username used for checking the credentials.</param>
        /// <param name="password">The password used for checking whether the credentials are correct.</param>
        /// <returns>True if the credentials are correct and the token could be updated, false if not.</returns>
        public bool VerifyLogin(string username, string password)
        {
            if (password == null)
            {
                return false;
            }

            string cmd = @"SELECT id
                            FROM gamer
                            WHERE name = @username
                            AND password = crypt(@password, password);
                           ";

            lock (_lock)
            {
                using NpgsqlCommand sqlcmd = new(cmd, con);
                sqlcmd.Parameters.AddWithValue("@username", username);
                sqlcmd.Parameters.AddWithValue("@password", password);

                using var reader = sqlcmd.ExecuteReader();

                if (reader.HasRows)
                {
                    reader.Close();
                    reader.Dispose();
                    string tokencmd = @"UPDATE gamer
                                    set token = @token
                                    WHERE name = @username
                                    ;";
                    using NpgsqlCommand sqltokenCommand = new(tokencmd, con);
                    sqltokenCommand.Parameters.AddWithValue("@token", User.CreateTokenString(username));
                    sqltokenCommand.Parameters.AddWithValue("@username", username);
                    return (sqltokenCommand.ExecuteNonQuery() > 0);
                }
            }
            return false;
        }

        /// <summary>
        /// Edits the optional additional user information: bio, alias, image.
        /// </summary>
        /// <param name="jsonuser">This User object should contain the aliasname, the bio and the image which will be inserted into the db.</param>
        /// <param name="username">The username is used to update the user specified.</param>
        /// <returns>True if a user entry could be updated.</returns>
        public bool EditUser(User jsonuser, string username)
        {
            string cmd = @"Update gamer
                           SET alias = @alias,
                               bio = @bio,
                               image = @image
                           WHERE name = @name;";
            using NpgsqlCommand sqlcmd = new(cmd, con);
            sqlcmd.Parameters.AddWithValue("@alias", jsonuser.aliasname);
            sqlcmd.Parameters.AddWithValue("@bio", jsonuser.bio);
            sqlcmd.Parameters.AddWithValue("@image", jsonuser.image);
            sqlcmd.Parameters.AddWithValue("@name", username);

            return sqlcmd.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Adds a card object into the database.
        /// This method is only used by the CreatePackage method.
        /// </summary>
        /// <param name="card">The specified card will be entered into the card table.</param>
        /// <returns>True if the card could be updated.</returns>
        private bool AddCard(Card card)
        {
            if (card.CType == Type.NotInitialized)
                card.SetType();
            string cmd = @"INSERT INTO card 
                                (id, type, name, damage,ismonster)
                            VALUES
                                (@id, @type, @name, @damage, @ismonster);";

            using NpgsqlCommand sqlcmd = new(cmd, con);
            sqlcmd.Parameters.AddWithValue("@id", card.Id);
            sqlcmd.Parameters.AddWithValue("@type", (int)card.CType);
            sqlcmd.Parameters.AddWithValue("@name", card.Name);
            sqlcmd.Parameters.AddWithValue("@damage", card.Damage);
            if (card.CType == Type.spell)
                sqlcmd.Parameters.AddWithValue("@ismonster", false);
            else
                sqlcmd.Parameters.AddWithValue("@ismonster", true);

            return sqlcmd.ExecuteNonQuery() > 0;
        }
        /// <summary>
        /// Creates a package and calls the AddCard method for adding the cards before creating the package.
        /// </summary>
        /// <param name="cards">The List of cards needs to consist of 5 cards, it will be aborted otherwise</param>
        /// <returns>If something goes wrong while creating a single card or the package or the list of provided Card objects is not 5 false will be returned. True otherwise</returns>
        public bool CreatePackage(List<Card> cards)
        {
            bool failure = false;
            if (cards.Count != 5)
                return false;
            foreach (Card card in cards)
            {
                if (!AddCard(card))
                {
                    Console.WriteLine("Error");
                    failure = true;
                }
            }

            List<Guid> ids = cards.Select(m => m.Id).ToList();
            string cmd = @"INSERT INTO cpackage
                                (id, c0id, c1id, c2id, c3id, c4id)
                            VALUES
                                (default, @c0id, @c1id, @c2id, @c3id, @c4id);";
            using NpgsqlCommand sqlcmd = new(cmd, con);

            for (int i = 0; i < 5; i++)
            {
                sqlcmd.Parameters.AddWithValue("@c" + i + "id",
                    ids[i]); //Replaces cXid with the id of the respective card in the list.
            }
            return (!failure && sqlcmd.ExecuteNonQuery() > 0);
        }
        /// <summary>
        /// Returns the cards which already exist in the database and have the same GUID as the provided list of Cards.
        /// </summary>
        /// <param name="package">A list of cards which is used to check whether a card with this guid already exists in the DB.</param>
        /// <returns>A list of mathing cards.</returns>
        public List<Guid>? CardsExistAlready(List<Card> package)
        {
            List<Guid> ids = package.Select(m => m.Id).ToList();
            StringBuilder sb = new();

            for (int i = 0; i < ids.Count; i++)
            {
                sb.Append("id = @guid" + i + " OR ");       
            }

            sb.Remove(sb.Length - 4, 4); //Removes the last 4 characters (Space,O,R,Space): " OR "
            string cmd = sb.ToString();

            cmd = string.Format("SELECT id FROM card WHERE {0}", cmd);

            lock (_lock)
            {
                using NpgsqlCommand sqlcmd = new(cmd, con);
                for (int i = 0; i < ids.Count; i++)
                {
                    sqlcmd.Parameters.Add("@guid" + i, NpgsqlTypes.NpgsqlDbType.Uuid);
                    sqlcmd.Parameters["@guid" + i].Value = ids[i];
                }

                using var reader = sqlcmd.ExecuteReader();
                if (reader.HasRows)
                {
                    List<Guid> dbGuids = new();
                    while (reader.Read())
                    {
                        Guid dbGuid = reader.GetGuid(0);
                        dbGuids.Add(dbGuid);
                        Console.WriteLine("Following Card already exist: " + dbGuid.ToString());
                    }
                    reader.Close();
                    return dbGuids;
                }
                reader.Close();
                return null;
            }
        }

        /// <summary>
        /// Gets the first package and adds the cards to the users stack.
        /// Deletes the package afterwards.
        /// </summary>
        /// <param name="user">The user who is buying the package.</param>
        /// <returns>true if the cards could be added to the users stack, false if something fails during these steps. </returns>
        public virtual bool GetPackage(User user)
        {
            bool packageavailable = false;
            Guid[] cardGuids = new Guid[5];
            int pid = -1;                   //package ID which is 

            string cmd = @"SELECT * FROM cpackage
                            LIMIT 1;";

            lock (_lock)
            {
                using (NpgsqlCommand sqlcmd = new(cmd, con))
                {
                    using var reader = sqlcmd.ExecuteReader();
                    //Retrieves all of the cardGuids from the package
                    while (reader.Read())
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            cardGuids[i] = reader.GetGuid(i + 1);
                            if (cardGuids[i].Equals(Guid.Empty))        //Check if a GUID is empty, to make sure nothing goes wrong when inserting CardIds in the stack later on.
                                return false;
                        }
                        pid = reader.GetInt32(0);                   //Retrieves the package ID, which is later used to remove this specific package from the cpackage table.
                    }
                    packageavailable = reader.HasRows;
                    reader.Close();
                }
            }
            if (pid == -1)
                return false;

            //Delete the package 
            if (packageavailable)
            {
                string deletecmd = @"DELETE FROM cpackage
                            WHERE id = @id;";

                using NpgsqlCommand sqldeletecmd = new(deletecmd, con);
                sqldeletecmd.Parameters.AddWithValue("@id", pid);
                if (sqldeletecmd.ExecuteNonQuery() < 1)     //No Rows affected
                    return false;
            }

            //Add the cards to the users stack.
            if (packageavailable)
            {
                for (int i = 0; i < cardGuids.Length; i++)
                {
                    string updatecmd = @"INSERT INTO stack
                                    (id, gamer, card, partofdeck)
                                VALUES
                                    (default, @name, @cid, FALSE);";

                    using (NpgsqlCommand sqlupdatecmd = new(updatecmd, con))
                    {
                        sqlupdatecmd.Parameters.AddWithValue("@name", user.name);
                        sqlupdatecmd.Parameters.AddWithValue("@cid", cardGuids[i]);
                        if (sqlupdatecmd.ExecuteNonQuery() < 0)
                            return false;
                    }
                }

                //Subtract five coins from the user:
                string updateCoincmd = @"UPDATE gamer
                                        SET coins = @coins
                                        WHERE name = @name;";
                using (NpgsqlCommand sqlupdateCoincmd = new(updateCoincmd, con))
                {
                    sqlupdateCoincmd.Parameters.AddWithValue("@name", user.name);
                    sqlupdateCoincmd.Parameters.AddWithValue("@coins", user.coins - 5);
                    sqlupdateCoincmd.ExecuteNonQuery();
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a package is available
        /// </summary>
        /// <returns>Tre if a package exists, false if not.</returns>
        public bool PackageAvailable()
        {
            string cmd = @"SELECT * FROM cpackage
                            LIMIT 1;";

            lock (_lock)
            {
                using NpgsqlCommand sqlcmd = new(cmd, con);
                using var reader = sqlcmd.ExecuteReader();
                return reader.HasRows;
            }

        }
        /*
        public List<Guid> ListGuidsFromStackOrDeck(string username, bool returnStack = true)
        {
            string cmd = @"SELECT card FROM stack
                            WHERE gamer = @name";

            if (!returnStack)
            {
                cmd += " and partofDeck = true";
            }
            cmd += ";";
            lock (_lock)
            {
                using NpgsqlCommand sqlcmd = new(cmd, con);
                sqlcmd.Parameters.AddWithValue("@name", username);
                using var reader = sqlcmd.ExecuteReader();

                //using a list, because the number of rows is unknown.
                List<Guid> cardGuids = new();
                while (reader.Read())
                {
                    int ordinal = reader.GetOrdinal("card");
                    Guid card = reader.GetGuid(ordinal);
                    if (!card.Equals(Guid.Empty))           //Check if a GUID is not empty, to make sure no empty card is read from the stack.
                        cardGuids.Add(card);
                }
                return cardGuids;
            }
        }*/
        
        /// <summary>
        /// This method is used to return a list of Cards either from the stack or deck, depending on the source variable.
        /// </summary>
        /// <param name="username">The username whose stack or deck should be returned.</param>
        /// <param name="source">This string should either be "stack" or "deck", returns an empty list otherwise.</param>
        /// <returns>Returns either the stack or the deck depending on the source variable. If source does not match "stack" or "deck" an empty list will be returned.</returns>
        public List<Card> ListStackOrDeck(string username, string source = "stack")
        {
            List<Card> cards = new();
            string cmd = "SELECT stack.card, card.type, card.name, card.damage, card.ismonster " +
                         "FROM stack " +
                         "INNER JOIN card ON card.id=stack.card " +
                         "WHERE stack.gamer = @gamer";

            if (source.ToLower() == "stack")
                cmd += ";";
            else if (source.ToLower() == "deck")
                cmd += " and partofdeck = TRUE;";
            else
                return cards;

            lock (_lock)
            {
                using (NpgsqlCommand sqlcmd = new(cmd, con))
                {
                    sqlcmd.Parameters.AddWithValue("@gamer", username);

                    using NpgsqlDataReader reader = sqlcmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Card card = new(reader.GetGuid(0), reader.GetString(2), reader.GetDouble(3));
                        cards.Add(card);
                    }
                }
            }
            return cards;
        }

        /// <summary>
        /// Adds the List of Guids provided to the users deck
        /// </summary>
        /// <param name="cardGuids">The list of cards which will be moved from the stack to the deck.</param>
        /// <param name="username">The username of the user whose cards will be moved to the deck. </param>
        /// <returns>True if at least one card was added to the deck.</returns>
        public bool AddCardsToDeck(List<Guid> cardGuids, string username)
        {
            StringBuilder sb = new(@"Update stack
                            SET partOfDeck = TRUE
                            WHERE gamer = @name and (");

            for (int i = 0; i < cardGuids.Count; i++)
            {
                sb.Append($"card = @id{i} or ");
            }
            string cmd = sb.Remove(sb.Length - 4, 4).Append(");").ToString();

            using NpgsqlCommand sqlcmd = new(cmd, con);
            for (int i = 0; i < cardGuids.Count; i++)
            {
                sqlcmd.Parameters.Add("@id" + i, NpgsqlTypes.NpgsqlDbType.Uuid);
                sqlcmd.Parameters["@id" + i].Value = cardGuids[i];
            }
            sqlcmd.Parameters.AddWithValue("@name", username);

            return sqlcmd.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Verifies if the provided token matches the token stored in the db.
        /// </summary>
        /// <param name="token">The token which will be used to check whether this token already exists.</param>
        /// <returns>True if the token was found, false otherwise.</returns>
        public bool CheckToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            string cmd = @"SELECT token FROM gamer
                            WHERE token = @token;";

            lock (_lock)
            {
                using NpgsqlCommand sqlcmd = new(cmd, con);
                sqlcmd.Parameters.AddWithValue("@token", token);

                using var reader = sqlcmd.ExecuteReader();
                return reader.HasRows;
            }
        }

        /// <summary>
        /// This method is primarily called after a battle to move the cards from one users deck to the other stack.
        /// </summary>
        /// <param name="movingCards"> A Dictionary which contains the cards, to move and the User to which the cards will be moved to.</param>
        public void MoveCards(Dictionary<Card, User> movingCards)
        {
            int i = 0;                              //Iterator used in the foreach loop of the dictionary
            StringBuilder sb = new(@"UPDATE stack
                                   SET gamer = @user1,
                                   partofdeck = false
                                   WHERE ");
            StringBuilder sb2 = new(sb.ToString());
            sb2.Replace('1', '2');

            //First name of user object is used as username1, whereas the other username is used as username2
            if (movingCards.Count == 0)
                return;
            string username1 = movingCards.First().Value.name;
            string username2 = "";

            foreach (var item in movingCards)
            {
                if (item.Value.name != username1)
                {
                    sb2.Append(@"card = @card" + i + " or ");
                    username2 = item.Value.name;
                    Console.WriteLine("Doesthis even work?");
                }
                else
                {
                    sb.Append(@"card = @card" + i + " or ");
                }
                i++;

            }
            //Status: sb has all Cards from user1, whereas sb2 has all cards from user2
            string cmd1 = sb.Remove(sb.Length - 4, 4).Append(';').ToString();
            string cmd2 = sb2.Remove(sb2.Length - 4, 4).Append(';').ToString();

            using NpgsqlCommand sqlUser1cmd = new(cmd1, con);
            using NpgsqlCommand sqlUser2cmd = new(cmd2, con);
            sqlUser1cmd.Parameters.AddWithValue("@user1", username1);

            i = 0;
            foreach (KeyValuePair<Card, User> item in movingCards)
            {
                if (item.Value.name != username1)
                {
                    sqlUser2cmd.Parameters.Add("@card" + i, NpgsqlTypes.NpgsqlDbType.Uuid);
                    sqlUser2cmd.Parameters["@card" + i].Value = item.Key.Id;
                }
                else
                {
                    sqlUser1cmd.Parameters.Add("@card" + i, NpgsqlTypes.NpgsqlDbType.Uuid);
                    sqlUser1cmd.Parameters["@card" + i].Value = item.Key.Id;
                    // sqlUser1cmd.Parameters.AddWithValue("@card" + i + " ", item.Key.Id);              // Does not work with GUIDs...
                }
                i++;
            }

            sqlUser1cmd.ExecuteNonQuery();
            if (username2 != String.Empty)
            {
                sqlUser2cmd.Parameters.AddWithValue("@user2", username2);
                sqlUser2cmd.ExecuteNonQuery();
            }
            return;
        }

        /// <summary>
        /// Updates the score after a battle.
        /// This is also used to update the elo after a battle
        /// </summary>
        /// <param name="battelog"> The battlelog which is used to determine if the battle is a draw, or who won.</param>
        public void AddScore(BLog battelog)
        {
            string updatescore;
            Console.WriteLine(battelog.draw + "  Winner:  " + battelog.Winner.name);
            if (battelog.draw == false)
            {
                updatescore = @" 
                        Update score
                        SET draws = draws + 1
                        WHERE gamer = @name1 or gamer = @name2
                        ; ";
            }
            else
            {
                updatescore = @"
                                UPDATE score 
                                SET wins = wins + 1,
                                elo = elo + 3
                                WHERE gamer = @name1;
                                UPDATE score
                                SET losses = losses + 1,
                                elo = elo - 5
                                WHERE gamer = @name2;";
            }

            using NpgsqlCommand sqlUpdateScore = new(updatescore, con);
            sqlUpdateScore.Parameters.AddWithValue("@name1", battelog.Winner.name);
            sqlUpdateScore.Parameters.AddWithValue("@name2", battelog.Loser.name);
            sqlUpdateScore.ExecuteNonQuery();
            return;
        }

        /// <summary>
        /// Returns the current scoreboard from the db.
        /// </summary>
        /// <returns> A List of scores, if at least one battle was played, which is to make sure that the admin is not returned, as he does not play games.</returns>
        public List<Score> GetScoreBoard()
        {
            string scoreboardquery = @"SELECT gamer, wins, losses, draws, elo 
                                FROM score 
                                WHERE wins != 0 OR losses != 0 OR draws != 0 
                                ORDER BY elo DESC; ";
            lock (_lock)
            {
                using (NpgsqlCommand getScoreBoard = new(scoreboardquery, con))
                {
                    using var reader = getScoreBoard.ExecuteReader();
                    List<Score> scoreboard = new();
                    while (reader.Read())
                    {
                        Score score = new(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4));
                        scoreboard.Add(score);
                    }
                    return scoreboard;
                }
            }
        }

        /// <summary>
        /// Returns the score of a single user.
        /// </summary>
        /// <param name="username"> The user whose score will be returned</param>
        /// <returns> A score object, which contains wins, losses, draws and the current elo.</returns>
        public Score GetUserScore(string username)
        {
            string scorequery = @"SELECT wins, losses, draws, elo 
                                    FROM score 
                                    WHERE gamer = @name"
            ;

            lock (_lock)
            {
                using NpgsqlCommand getScore = new(scorequery, con);
                getScore.Parameters.AddWithValue("@name", username);
                using var reader = getScore.ExecuteReader();
                Score score = new(username, 0, 0, 0, 0);
                while (reader.Read())
                {
                    score = new(username, reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3));
                }
                return score;
            }
        }

        /// <summary>
        /// Verifies if a card is tradeable. ( not part of a deck and a card in a stack.)
        /// </summary>
        /// <param name="cardid"> The GUID which is used to look for the card.</param>
        /// <returns>The string "Tradeable" if the card was found and is not in the stack, "Not Found" otherwise.</returns>
        public string IsCardPartofDeck(Guid cardid)
        {
            string cmd = @"SELECT card, partofdeck 
                                FROM stack
                                WHERE card = @cardid and partofdeck = false";

            lock (_lock)
            {
                using NpgsqlCommand sqlcmd = new(cmd, con);
                sqlcmd.Parameters.AddWithValue("@cardid", cardid);

                using var reader = sqlcmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    return "NotFound";
                }
                else
                    return "Tradeable";
            }
        }

        /// <summary>
        /// Creates a trade with the GUID as TradeID and the Card with the provided Cardid.
        /// The trade object also contains the requirements needed to be met for a trade to be successful.
        /// </summary>
        /// <param name="trade"> This object all the needed trade information.</param>
        /// <returns>True if a card was able to be added to the trade-table.</returns>
        public bool AddCard2Trade(Trade trade)
        {
            string tradequery = @"INSERT INTO trades
                                (id, card, wantsMonster, minDamage)
                                VALUES
                                    (@tradeid, @cardid, @monsterrequested, @mindamage);";

            using NpgsqlCommand getScore = new(tradequery, con);
            getScore.Parameters.AddWithValue("@tradeid", trade.TradeID);
            getScore.Parameters.AddWithValue("@cardid", trade.Cardid);
            getScore.Parameters.AddWithValue("@monsterrequested", trade.isMonster());
            getScore.Parameters.AddWithValue("@mindamage", trade.MinDamage);

            return getScore.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Lists all active trades.
        /// </summary>
        /// <returns> A List of all active trades.</returns>
        public List<Trade> ListTrades()
        {
            string tradequery = @"SELECT trades.id, trades.card, trades.wantsmonster, trades.mindamage, stack.gamer 
                                FROM trades INNER JOIN 
                                stack ON stack.card = trades.card;";

            lock (_lock)
            {
                using NpgsqlCommand sqltradecmd = new(tradequery, con);
                List<Trade> trades = new();
                using var reader = sqltradecmd.ExecuteReader();
                while (reader.Read())
                {
                    Trade t;
                    if (reader.GetBoolean(2))
                        t = new(reader.GetGuid(0), reader.GetGuid(1), "monster", reader.GetDouble(3), reader.GetString(4));
                    else
                        t = new(reader.GetGuid(0), reader.GetGuid(1), "spell", reader.GetDouble(3), reader.GetString(4));
                    trades.Add(t);
                }
                return trades;
            }
        }

        /// <summary>
        /// Deletes a trade from any user.
        /// The check if a user is allowed to delete the trade needs to be done beforehand.
        /// </summary>
        /// <param name="TradeId"> The Trade with the provided TradeId will be deleted</param>
        /// <returns>True if a trade was deleted.</returns>
        public bool DeleteTrade(Guid TradeId)
        {
            string tradequery = @"DELETE FROM trades
                                WHERE id = @tradeid";

            using NpgsqlCommand sqltradecmd = new(tradequery, con);

            sqltradecmd.Parameters.Add("@tradeid", NpgsqlTypes.NpgsqlDbType.Uuid);
            sqltradecmd.Parameters["@tradeid"].Value = TradeId;

            return sqltradecmd.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// This method checks the requirements of a trade and completes it if the requirments are fulfilled.
        /// </summary>
        /// <param name="t"> This is the current active trade, which will be used to check if the counteroffer matches this trades requirements.</param>
        /// <param name="counteroffer"> The Card Id of the offer to an existing trade.</param>
        /// <param name="consumer"> The name of the user who offered his counteroffer-card.</param>
        /// <returns>True if the trade was completed successfully.</returns>
        public bool CheckReqAndAcceptTrade(Trade t, Guid counteroffer, string consumer)
        {
            //Get information about the card to check if the requirements are met
            string Cardinfocmd = @"SELECT card.id, card.name, card.damage, stack.gamer, stack.partofdeck
                                   FROM card INNER JOIN stack on card.id = stack.card
                                   WHERE card.id = @guid and stack.partofdeck = false and stack.gamer != @trader;";
            lock (_lock)
            {
                using NpgsqlCommand sqltradecmd = new(Cardinfocmd, con);
                sqltradecmd.Parameters.Add("@guid", NpgsqlTypes.NpgsqlDbType.Uuid);
                sqltradecmd.Parameters["@guid"].Value = counteroffer;
                sqltradecmd.Parameters.AddWithValue("@trader", t.Trader);
                using var reader = sqltradecmd.ExecuteReader();
                Card? c = null;
                while (reader.Read())
                {
                    c = new Card(reader.GetGuid(0), reader.GetString(1), reader.GetDouble(2));
                }
                if (c is null)
                    return false;
                reader.Close();

                if ((t.isMonster().Equals(c.IsMonster())) && (t.MinDamage < c.Damage))
                {
                    // Trade Cards
                    string completeTrade = @"UPDATE stack 
                                         SET gamer = @consumer
                                         WHERE card = @offeredCard;
                                         UPDATE stack 
                                         SET gamer = @offeror
                                         WHERE card = @consumerCard;
                                         DELETE FROM trades where id = @tradeId;";
                    using NpgsqlCommand SQLcompleteTrade = new(completeTrade, con);

                    SQLcompleteTrade.Parameters.AddWithValue("@consumer", consumer);
                    SQLcompleteTrade.Parameters.Add("@offeredCard", NpgsqlTypes.NpgsqlDbType.Uuid);
                    SQLcompleteTrade.Parameters["@offeredCard"].Value = t.Cardid;
                    SQLcompleteTrade.Parameters.AddWithValue("@offeror", t.Trader);
                    SQLcompleteTrade.Parameters.Add("@consumerCard", NpgsqlTypes.NpgsqlDbType.Uuid);
                    SQLcompleteTrade.Parameters["@consumerCard"].Value = counteroffer;
                    SQLcompleteTrade.Parameters.Add("@tradeID", NpgsqlTypes.NpgsqlDbType.Uuid);
                    SQLcompleteTrade.Parameters["@tradeID"].Value = t.TradeID;

                    return SQLcompleteTrade.ExecuteNonQuery() > 0;
                }
                return false;
            }
        }
    }
}
