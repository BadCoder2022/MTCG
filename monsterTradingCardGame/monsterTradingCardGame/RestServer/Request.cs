using monsterTradingCardGame.Trades;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace monsterTradingCardGame.RestServer
{
    /// <summary>
    /// This class is used to parse an incoming HTTP-Request
    /// </summary>
    internal class Request
    {
        public string method = string.Empty;
        public string path = string.Empty;
        public string httpversion = string.Empty;
        public List<string> header = new();
        public string body = string.Empty;
        private string? authorization;
        public string[]? add_path;

        /// <summary>
        /// This method returns the parsed token which was set by the ParseRequest method.
        /// </summary>
        /// <returns>The string of the parsed token.</returns>
        public string GetToken()
        {
            return authorization;
        }

        /// <summary>
        /// Parses the provided request to retrieve relevant information.
        /// </summary>
        /// <param name="request"> Parses the provided string, which should be the HTTP-method stored as a string, to retrieve important information from it.</param>
        public void ParseRequest(string request)
        {
            string[] rqLines = request.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            string[] rqParts = rqLines[0].Split(' ');

            method = rqParts[0];
            path = rqParts[1];
            httpversion = rqParts[2];
            body = "";
            add_path = path.Split('/');

            int i = 1;       //i is used to iterate until the first empty line is read, to separate the HTTP header from the body.
            for (; !string.IsNullOrWhiteSpace(rqLines[i]); i++)
            {
                header.Add(rqLines[i]);
            }

            //Stores the HTTP-body
            for (; i < rqLines.Length; i++)
            {
                body += rqLines[i];
            }

            //Extracts the token in the HTTP-Request
            foreach (string s in header)
            {
                if (s.Contains("Authorization: Basic"))
                {
                    int start = s.IndexOf("Authorization: Basic ") + "Authorization: Basic ".Length;
                    authorization = s.Substring(start, s.Length - start);
                }
            }
        }

        /// <summary>
        /// This method tries to parse the HTTP-body as a JSON-object depending on the requested routes.
        /// </summary>
        /// <returns>Either a user, a card, a list of cards, a list of cardGUIDs or a trade object.</returns>
        public object? ParseJson()
        {
            if (add_path is null)
                return null;
            if (add_path.Length > 2)
                path = "/" + add_path[1];
            switch (path)   ///Parse the /users/USERNAME path
            {
                case "/sessions":
                case "/users":
                    User user = JsonSerializer.Deserialize<User>(body);
                    return user;

                case "/packages":
                    List<Card> cards = JsonSerializer.Deserialize<List<Card>>(body);
                    foreach (Card card in cards)
                    {
                        card.SetType();
                    }
                    return cards;

                case "/deck":
                    List<Guid> cardguids = JsonSerializer.Deserialize<List<Guid>>(body);
                    return cardguids;

                case "/tradings":
                    if (add_path.Count() < 3)
                    {
                        Trade trade = JsonSerializer.Deserialize<Trade>(body);
                        return trade;
                    }
                    Guid g = JsonSerializer.Deserialize<Guid>(body);
                    return g;

                default:
                    break;
            }
            return null;
        }
    }
}
