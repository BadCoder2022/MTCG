using monsterTradingCardGame.BattleFunctions;
using monsterTradingCardGame.Trades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace monsterTradingCardGame.Test
{
    internal class TradeTests
    {
        [Test]
        public void TradeConstructorTests()
        {
            //Arrange
            Guid g1 = Guid.NewGuid();
            Guid g2 = Guid.NewGuid();
            Guid g3 = Guid.NewGuid();
            Guid g4 = Guid.NewGuid();
            Trade t1 = new();
            Trade t2 = new(g1, g2, "Monster", 45);
            Trade t3 = new(g3, g4, "Spell", 67, "User1");

            //Assess
            Assert.IsInstanceOf<Guid>(t1.TradeID);
            Assert.IsInstanceOf<Guid>(t1.Cardid);
            Assert.That(t2.TradeID, Is.EqualTo(g1));
            Assert.That(t2.Cardid, Is.EqualTo(g2));
            Assert.That(t3.TradeID, Is.EqualTo(g3));
            Assert.That(t3.Cardid, Is.EqualTo(g4));
            Assert.That(t3.Trader, Is.EqualTo("User1"));

        }

        [Test]
        public void TradeisMonsterOrSpellTest()
        {
            //Arrange

            Trade t2 = new(Guid.NewGuid(), Guid.NewGuid(), "Monster", 67);
            Trade t3 = new(Guid.NewGuid(), Guid.NewGuid(), "Spell", 98);

            //Act
            bool result1 = t2.isMonster();
            bool result2 = t3.isMonster();
            bool result3 = t3.isSpell();

            //Assess
            Assert.True(result1);
            Assert.False(result2);
            Assert.True(result3);

        }




        
    }
}
