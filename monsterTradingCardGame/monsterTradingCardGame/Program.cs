using monsterTradingCardGame.RestServer;

namespace monsterTradingCardGame
{
    internal class Program
    {
        static void Main(string[] args)
        {   
            DB db = new ();
            db.Connect();
            db.InitDB();        //Deletes tokens from the DB, to make users login after a server restart.
            Console.WriteLine("Connected to the DB");

            HttpSrv srv = new(db);
            srv.Start(10001);

        }
    }
}