using monsterTradingCardGame.BattleFunctions;

namespace monsterTradingCardGame.Test
{
    internal class BattleLobbyTests
    {
        [Test]
        public void JoinLobbyTest()
        {
            //Arrange
            BattleLobby bl = new();

            User u1 = new("User1", "2131231")
            {
                isLoggedIn = true
            };
            u1.token = User.CreateTokenString(u1.name);

            User u2 = new("User2", "2hkakda")
            {
                isLoggedIn = true
            };
            u2.token = User.CreateTokenString(u2.name);

            List<User>? players1 = new ();
            
            //Act
            var thread1 = new Thread(() => { players1 = bl.JoinLobby(u1); });
            thread1.Start();

            Thread.Sleep(1);                                        //Sleep is needed to make sure, that the spawned thread enters the JointLobby method first.
            

            List<User>? players2 = bl.JoinLobby(u1);                //Should return null, because the same user is already in queue
            List<User>? players3 = bl.JoinLobby(u2);

            //Assess
            
            Assert.Multiple(() =>
            {
                Assert.That(players1, Is.Empty);
                Assert.That(players1 is not null);
                Assert.That(players2, Is.EqualTo(null));                //
                Assert.That(players3 is not null);
                Assert.That(players3, Has.Count.EqualTo(2));
            });
        }
    }
}
