using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using monsterTradingCardGame.Trades;
using monsterTradingCardGame.BattleFunctions;
using monsterTradingCardGame.OptionalFeature;
using System.ComponentModel;

namespace monsterTradingCardGame.RestServer
{
    /// <summary>
    /// This class is used to decide how to handle an incoming Request
    /// It is also the REST-Server of this application and therefore a central part.
    /// The REST-Server uses ThreadPools, as the Threads in this application are usually short-lived.
    /// </summary>
    internal class HttpSrv
    {
        public DB db;
        public RandomBattle RBattle;
        private readonly BattleLobby bl = new();
        private BLog blog = new();
        public HttpSrv(DB db)
        {
            this.db = db;
            this.RBattle = new RandomBattle();
        }

        private readonly object _lock = new();

        /// <summary>
        /// This method starts listening on the provided port.
        /// </summary>
        /// <param name="port">Listens on the provided port.</param>
        public void Start(int port)
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");

            // Create a new TCPListener to listen for incoming connections
            TcpListener listener = new(ipAddress, port);
            listener.Start();


            //Init DB-Connection for the random Battle and fill the Table for the RandomBattle with predetermined cards.
            RBattle.Init();
            Console.WriteLine("Listener started on 127.0.0.1 : " + port.ToString());

            // Continuously listens for incoming connections
            while (true)
            {
                // Accept an incoming connection
                TcpClient client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(new WaitCallback(HandleRequest), client);
            }
        }
        /// <summary>
        /// This is the core method, in which every Thread starts out.
        /// It calls every other method in the HTTP-Server and contains the logic which function needs to be called for which request
        /// </summary>
        /// <param name="client">The TcpClient which will be used to send Responses to later on.</param>
        private void HandleRequest(object client)
        {
            Request request = new();
            TcpClient tcpClient = (TcpClient)client;
            // Get the network stream from the TcpClient
            NetworkStream stream = tcpClient.GetStream();

            // Read the request data from the stream
            byte[] requestData = new byte[1024];
            int bytesRead = stream.Read(requestData, 0, requestData.Length);


            request.ParseRequest(Encoding.ASCII.GetString(requestData, 0, bytesRead));

            // Verifies if a user is logged in.
            if (!(request.method == "POST" && (request.path.StartsWith("/users") || request.path.StartsWith("/sessions"))))
            {

                if (!db.CheckToken(request.GetToken()))
                {
                    Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "No user with this token has logged in.");
                    return;
                }
            }

            switch (request.method)
            {
                case "GET":     //Verify first 
                    if (request.path.StartsWith("/cards"))
                        GetStackOrDeck(request, tcpClient, getStack: true);        //Sends a HTTP Response with the stack

                    else if (request.path.StartsWith("/deck"))
                        GetStackOrDeck(request, tcpClient, getStack: false);       //Sends a HTTP Response with the deck

                    else if (request.path.StartsWith("/users"))
                        SendUserInfo(request, tcpClient);

                    else if (request.path.StartsWith("/stats"))
                        ReplyUserScore(request, tcpClient);

                    else if (request.path.StartsWith("/score"))
                        ReplyScoreBoard(request, tcpClient);
                    else if (request.path.StartsWith("/tradings"))
                        ListTrades(request, tcpClient);
                    break;

                case "POST":
                    if (request.path.StartsWith("/users"))
                        AddUser(request, tcpClient);

                    else if (request.path.StartsWith("/sessions"))
                        VerifyUser(request, tcpClient);
                    
                    else if (request.path.StartsWith("/packages"))
                        AddPackage(request, tcpClient);

                    else if (request.path.StartsWith("/transactions"))
                        GetPackage(request, tcpClient);

                    else if (request.path.StartsWith("/battles"))
                        EnterLobby(request, tcpClient);
                    else if (request.path.StartsWith("/tradings"))
                    {
                        if (request.add_path.Count() < 3)
                        {
                            TradeCard(request, tcpClient);
                        }
                        else
                        {
                            AcceptTrade(request, tcpClient);
                        }
                    }
                    else if (request.path.StartsWith("/randombattles"))
                        StartRandomBattle(request, tcpClient);

                    break;

                case "PUT":
                    if (request.path.StartsWith("/deck"))
                        AddCardsToDeck(request, tcpClient);

                    if (request.path.StartsWith("/users/"))
                    {
                        if (request.path.Length > 7)
                        {
                            string user = request.path.Substring(7);
                            EditUser(request, tcpClient, user);
                        }
                    }
                    break;
                case "DELETE":
                    if (request.path.StartsWith("/tradings"))
                        DeleteTrades(request, tcpClient);
                    break;
                default:
                    Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "This path or method has not been implemented");
                    break;
            }

        }
        /// <summary>
        /// This function creates a user in the DB and sends a suitable response.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        private void AddUser(Request request, TcpClient tcpClient)       //TODO change to bool, return true if successful
        {
            object obj = request.ParseJson();
            if (obj is not User)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong while parsing the Request.");
                return;
            }

            User user = (User)obj;

            if (user.name == null || user.pwd == null)
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "There was no username or password provided.");
            else
            {
                if (db.UserExists(user.name))
                    Response.SendResponse(tcpClient, HttpStatusCode.Conflict, "A user with this name exists already.");
                else if (!db.CreateUser(user.name, user.pwd))
                    Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong while trying to add the user into the DB.");
                else
                    Response.SendResponse(tcpClient, HttpStatusCode.OK, "User was created successfully!");
            }
        }

        /// <summary>
        /// Sends the stored User Information
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        private void SendUserInfo(Request request, TcpClient tcpClient)
        {
            User? dbuser = db.GetUserInformation(request.GetToken());

            if (!string.Equals(request.add_path[2], dbuser.name, StringComparison.OrdinalIgnoreCase))
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "This userpath does not match the provided token.");
                return;
            }
            if (dbuser is not null)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.OK, JsonSerializer.Serialize(dbuser), true);
                return;
            }
            Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong while retrieving user information.");
        }

        /// <summary>
        /// This method is used to edit the user information in the DB and send a success response if it worked.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        /// <param name="username">The username whose user information should be updated</param>
        private void EditUser(Request request, TcpClient tcpClient, string username)
        {
            User? user = db.GetUserInformation(request.GetToken());
            if (user is null)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong while parsing the Request.");
            }

            if (!string.Equals(username, user.name, StringComparison.OrdinalIgnoreCase))
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "This userpath does not match the provided token.");
                return;
            }

            object obj = request.ParseJson();
            if (obj is not User)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong while parsing the Request.");
                return;
            }

            User Jsonuser = (User)obj;

            if (db.EditUser(Jsonuser, user.name))
                Response.SendResponse(tcpClient, HttpStatusCode.OK, "DB-Entry has been updated.");
            else
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "The Entry could not be updated.");

        }

        /// <summary>
        /// Creates a Package and checks if the cards which should be created exist already
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        /// <returns>True if a package could be created successfully, false if not</returns>
        private bool AddPackage(Request request, TcpClient tcpClient)
        {
            //TODO check if the logged in user is admin and it the token is correct.
            if (!request.GetToken().Equals("admin-mtcgToken"))
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "The wrong Token was specified");
                return false;
            }

            object obj = request.ParseJson();
            if (obj is not List<Card>)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong during the parsing process.");
                return false;
            }

            List<Card> cards = (List<Card>)obj;

            if (cards.Count != 5)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "Too little or too much cards specified");
                return false;
            }

            List<Guid>? ids = db.CardsExistAlready(cards);
            if (ids is not null)
            {
                StringBuilder sb = new("The card(s) with the GUID(s) [");
                foreach (Guid g in ids)
                {
                    sb.Append("\"" + g.ToString() + "\", ");
                }
                sb.Remove(sb.Length - 2, 2).Append(']');        //removes the space and the colon at the end of the guid.
                string guids = sb.ToString();
                Response.SendResponse(tcpClient, HttpStatusCode.Conflict, guids + " exist already.");
                return false;
            }

            if (db.CreatePackage(cards))                    //Creates the cards first and afterwards a package referring to the card IDs
            {
                Response.SendResponse(tcpClient, HttpStatusCode.OK, "Package was created successfully!");
                return true;
            }

            Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong during the parsing process.");
            return false;
        }

        /// <summary>
        /// Checks whether the provided credentials match and returns the token.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        private void VerifyUser(Request request, TcpClient tcpClient)
        {
            object obj = request.ParseJson();
            if (obj is not User)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong during the parsing process.");
                return;
            }

            User user = (User)obj;
            user.token = User.CreateTokenString(user.name);

            if (user.ValidateUser(db))
            {
                Response.SendResponse(tcpClient, HttpStatusCode.OK, $"[{{\"token\":\"{user.token}\"}}]", true); //User logged in successfully, returning token 
            }
            else
            {
                Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Username or Password is incorrect!");
            }
            return;
        }

        /// <summary>
        /// Buys a Package if a User has enough coins.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        private void GetPackage(Request request, TcpClient tcpClient)
        {
            User user = db.GetUserInformation(request.GetToken());

            if (user.coins < 5)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "To little coins remaining.");
                return;
            }

            if (!db.PackageAvailable())
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "No Package remaining");
                return;
            }

            if (db.GetPackage(user))
                Response.SendResponse(tcpClient, HttpStatusCode.OK, "Cards acquired");
            return;
        }
        /// <summary>
        /// Add the card GUIDs listed in the request from the stack to the deck.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        private void AddCardsToDeck(Request request, TcpClient tcpClient)
        {
            User user = db.GetUserInformation(request.GetToken());

            object obj = request.ParseJson();
            if (obj is not List<Guid>)
                Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong while parsing the Request.");

            if (obj is null)
                Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "No Card-GUIDs provided!");

            List<Guid> guids = (List<Guid>)obj;

            //Check if any of the cards are currently in a trade:
            List<Trade> trades = db.ListTrades();
            if (trades.Any(trade => guids.Contains(trade.Cardid)))
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "A card you are trying to put in your deck is currently offered in a trade.\r\nYou need to remove it from the trade first!");
                return;
            }

            if (db.AddCardsToDeck(guids, user.name))
                Response.SendResponse(tcpClient, HttpStatusCode.OK, "The Cards have been added to the deck.");
            else
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "No Cards have been added to the deck.");

            return;
        }

        /// <summary>
        /// This method returns the cards of either the stack or the deck, depending on the 'getStack' variable in the response.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        /// <param name="getStack">Returns the stack if true, returns the deck if false</param>
        private void GetStackOrDeck(Request request, TcpClient tcpClient, bool getStack = true)
        {
            User? user = db.GetUserInformation(request.GetToken());             //TODO Check if User is null

            string source;
            if (getStack)
                source = "stack";
            else
                source = "deck";
            List<Card> stack = db.ListStackOrDeck(user.name, source);

            if (request.path.Contains("?format=plain", StringComparison.OrdinalIgnoreCase))
            {
                string plain = string.Empty;
                foreach (Card c in stack)
                {
                    plain += c.ToString() + "\n";
                }
                Response.SendResponse(tcpClient, HttpStatusCode.OK, plain, false);
            }
            else
                Response.SendResponse(tcpClient, HttpStatusCode.OK, JsonSerializer.Serialize(stack), true);
        }

        /// <summary>
        /// This method queues players for a battle and starts the battle after two players queued.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        public void EnterLobby(Request request, TcpClient tcpClient)
        {
            User user = db.GetUserInformation(request.GetToken());
            List<User>? participants = bl.JoinLobby(user);
            BLog localBlog = new();

            if (participants is null)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "You can not start a battle with yourself.");
                return;
            }

            lock (_lock)
            {
                if (participants.Count != 2)      // Prepare for Battle
                {   //Thread 1
                    Monitor.Wait(_lock);
                    //Wait for Battle 
                    localBlog = blog;
                }
                else
                {   // Thread2
                    List<Card> deck1 = db.ListStackOrDeck(participants[0].name, "deck");
                    List<Card> deck2 = db.ListStackOrDeck(participants[1].name, "deck");

                    //Call Battle
                    blog = Battle.StartBattle(Tuple.Create(participants[0], deck1), Tuple.Create(participants[1], deck2));
                    localBlog = blog;
                    Monitor.Pulse(_lock);
                    db.MoveCards(localBlog.movedCards);

                    db.AddScore(blog);
                }
            }
            Response.SendResponse(tcpClient, HttpStatusCode.OK, localBlog.completeLog);
        }

        /// <summary>
        /// Returns the current score for both users
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        private void ReplyUserScore(Request request, TcpClient tcpClient)
        {
            Score score = db.GetUserScore(db.GetUserInformation(request.GetToken()).name);

            if (score is not null)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.OK, score.ToString());
                return;
            }
            Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Score is empty!");
        }

        /// <summary>
        /// Returns the scoreboard sorted by ELO.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        private void ReplyScoreBoard(Request request, TcpClient tcpClient)
        {
            List<Score> scoreboard = db.GetScoreBoard();

            if (scoreboard is null)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Scoreboard is empty!");
                return;
            }
            StringBuilder sb = new("|Name    \t| Wins   \t| Losses   \t| Draws   \t| Elo   \t|\n");
            foreach (Score s in scoreboard)
            {
                sb.Append($"|{s.username}\t| {s.wins}  \t\t| {s.losses}   \t\t| {s.draws}   \t\t| {s.GetElo()}  \t\t|\r\n");
            }
            Response.SendResponse(tcpClient, HttpStatusCode.OK, sb.ToString());
        }

        /// <summary>
        /// Lists a card for a Trade and sends an OK Response if it worked.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        private void TradeCard(Request request, TcpClient tcpClient)
        {
            object obj = request.ParseJson();
            if (obj is not Trade)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong while parsing the Request.");
                return;
            }
            Trade trade = (Trade)obj;
            if (!trade.isMonster() && !trade.isSpell())
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "Specified Type is neither Monster nor Spell!");
                return;
            }

            string result = db.IsCardPartofDeck(trade.Cardid);
            if (result == "Tradeable")
            {
                if (db.AddCard2Trade(trade))
                {
                    Response.SendResponse(tcpClient, HttpStatusCode.OK, "Specified Card was set up for a trade.");
                    return;
                }
                else
                    Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong while setting up the trade");
            }
            else if (result == "InDeck")
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "The specified Card-ID is already in a deck and therefore can not be set up for a trade.");
            }
            else
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "The Card with the provided CardId was not found.");
            }
            return;
        }

        /// <summary>
        /// Lists all cards up for a trade and their requirements.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        public void ListTrades(Request request, TcpClient tcpClient)
        {
            List<Trade> trades = db.ListTrades();
            if (trades.Count == 0)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.OK, "No trades active.");
                return;
            }
            else
            {
                StringBuilder sb = new("|Trade-ID   \t\t\t\t| Card-ID   \t\t\t\t| Monster Requested   \t| Minimum Damage   \t|Trader   \t\t\n");
                foreach (Trade t in trades)
                {
                    sb.Append($"|{t.TradeID}\t| {t.Cardid} \t| {t.isMonster()}   \t\t| {t.MinDamage}  \t\t\t|{t.Trader}   \t\r\n");
                }
                Response.SendResponse(tcpClient, HttpStatusCode.OK, sb.ToString());
            }
        }

        /// <summary>
        /// Deletes an active trade.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        public void DeleteTrades(Request request, TcpClient tcpClient)
        {
            bool success = false;
            User user = db.GetUserInformation(request.GetToken());
            List<Trade> trades = db.ListTrades();
            if (trades.Count == 0)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "No trades active.");
                return;
            }
            else
            {
                foreach (Trade t in trades)
                {
                    Console.WriteLine(t.Trader + "\nusername: " + user.name);
                    if (t.TradeID.ToString() == request.add_path[2] && t.Trader.ToLower().Contains(user.name.ToLower()))
                    {
                        db.DeleteTrade(Guid.Parse(request.add_path[2]));
                        success = true;
                    }
                }
            }
            if (success)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.OK, "The specified Trade was deleted.");
                return;
            }
            Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "Could not find the specified trade.");
        }

        /// <summary>
        /// Tries to accept a trade.
        /// Requirements will be checked in the CheckReqAndAcceptTrade method of the DB.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        public void AcceptTrade(Request request, TcpClient tcpClient)
        {
            object obj = request.ParseJson();
            if (obj is not Guid)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "Something went wrong during the parsing process.");
            }
            Guid counteroffer = (Guid)obj;
            User user = db.GetUserInformation(request.GetToken());
            List<Trade> trades = db.ListTrades();
            if (trades.Count == 0)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "No trades active.");
                return;
            }
            else
            {
                foreach (Trade t in trades)
                {
                    if (t.TradeID.ToString() == request.add_path[2])
                    {
                        if (t.Trader == user.name)
                        {
                            Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "You are not allowed to trade with yourself.");
                        }
                        else
                        {
                            if (db.CheckReqAndAcceptTrade(t, counteroffer, user.name))
                                Response.SendResponse(tcpClient, HttpStatusCode.OK, "The Trade was completed successfully.");
                            else
                                Response.SendResponse(tcpClient, HttpStatusCode.BadRequest, "The Trade could not be completed.");
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// optional Feature: RandomBattle
        /// This method queues two players and lets them battle with ten random cards of a pool of predefined cards.
        /// </summary>
        /// <param name="request">This object contains the request object which parsed the original request once.</param>
        /// <param name="tcpClient">The object which is used to send the response</param>
        public void StartRandomBattle(Request request, TcpClient tcpClient)
        {
            //Get the user and afterwards a list of 10 random cards.
            User user = db.GetUserInformation(request.GetToken());
            RandomBattle rb = new RandomBattle();

            rb.Connect();
            List<Card> deck1 = rb.getRandomCards();
            Tuple<User, List<Card>> player = Tuple.Create(user,rb.getRandomCards());
            
            BLog battleLog = rb.QueueRandomBattle(player);

            if(battleLog is null || battleLog.completeLog is null)
            {
                Response.SendResponse(tcpClient, HttpStatusCode.InternalServerError, "Something went wrong during battle.");
                return;
            }
            Console.WriteLine("BattleLog:" + battleLog.completeLog);
            Response.SendResponse(tcpClient, HttpStatusCode.OK, battleLog.completeLog);
            rb.Close();
        }

    }
}
