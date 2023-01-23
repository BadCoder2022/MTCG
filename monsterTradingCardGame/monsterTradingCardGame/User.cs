using System.Text.Json.Serialization;

namespace monsterTradingCardGame
{
    /// <summary>
    /// The User class is a relevant class that is used for nearly every user interaction in some way.
    /// It is not designed that the user is able to change the password that was set once.
    /// </summary>
    public class User
    {   
        [JsonPropertyName("Username")]
        public string name { get; set; }
        [JsonPropertyName("Password")]
        public string pwd { get; set; }
        public string token { get; set; }
        public int coins { get; set; }
        [JsonPropertyName("Bio")]
        public string? bio { get; set; }
        [JsonPropertyName("Image")]
        public string? image { get; set; }
        [JsonPropertyName("Name")]
        public string? aliasname { get; set; }
        private int elo { get; set; }


        //The Setter of 'isLoggedIn' checks whether a user with the name and the password exists.
        private bool _isLoggedIn;
        [JsonIgnore]
        public bool isLoggedIn
        {
            get
            {
                return this._isLoggedIn;
            }

            set                 
            {
                DB db = new();
                db.Connect();

                if (this.ValidateUser(db))
                    _isLoggedIn = true;
                else
                {
                    _isLoggedIn = false;
                }

                db.Close();
            }
        }

        public User(string name,string pwd)
        {
            this.name = name;
            this.pwd = pwd;
            this.token = User.CreateTokenString(name);
            coins = 20;
            isLoggedIn = false;
            elo = 100;
        }

        public User(string name, string pwd, string token, int coins, string? alias, string? bio, string? image)
        {
            this.name = name;
            this.pwd = pwd;
            this.token = token;
            this.coins = coins;
            isLoggedIn = false;
            elo = 100;
            this.aliasname = alias;
            this.image = image;
            this.bio = bio;
        }

        public User()
        {
            this.name = "";
            this.pwd = "";
            this.token = "";
            coins = 20;
            isLoggedIn = false;
            this.elo = 100;
        }

        /// <summary>
        /// Checks if the credentials of a User are correct.
        /// </summary>
        /// <param name="db">The database object used for the connection.</param>
        /// <returns>True if the credentials match, false otherwise.</returns>
        internal bool ValidateUser(DB db)
        {
            if (db.VerifyLogin(this.name, this.pwd))
                return true;
            return false;
        }

        /// <summary>
        /// Creates the token for the provided username.
        /// </summary>
        /// <param name="name">The username which is used for the token generation.</param>
        /// <returns>The name of the created token.</returns>
        public static string CreateTokenString(string name)
        {
            return name + "-mtcgToken";
        }

        public int GetCurrentElo()
        {
            return this.elo;
        }
    }
}
