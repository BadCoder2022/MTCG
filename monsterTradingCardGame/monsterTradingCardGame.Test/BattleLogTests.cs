using monsterTradingCardGame.BattleFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace monsterTradingCardGame.Test
{
    internal class BattleLogTests
    {
        [Test]
        public void moveCardTests()
        {
            //Arrange
            Card c1 = new Card(Guid.NewGuid(), "TestCard", 2);
            Card c2 = new Card(Guid.NewGuid(), "2.Card", 7898);
            User u1 = new User("User1", "SecurePassword");
            User u2 = new User("User2", "notSecure");

            BLog battleLog = new();

            //Act
            User result1;
            User result2;
            battleLog.LogMovedCard(c1,u1);
            battleLog.LogMovedCard(c2,u2);
            battleLog.LogMovedCard(c1, u2);
            bool isCardInDictionary1 = battleLog.movedCards.TryGetValue(c1, out result1);
            bool isCardInDictionary2 = battleLog.movedCards.TryGetValue(c2, out result2);

            //Assess
            Assert.IsFalse(isCardInDictionary1);
            Assert.IsTrue(isCardInDictionary2);
            Assert.That(result2.name, Is.EqualTo(u2.name));

        }
    }
}
