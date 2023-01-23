using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace monsterTradingCardGame.Test
{
    internal class CardTests
    {
        [Test]
        public void CardConstructorTests()
        {
            //Arrange
            Guid g1 = Guid.NewGuid();
            Guid g2 = Guid.NewGuid();
            Guid g3 = Guid.NewGuid();
            Guid g4 = Guid.NewGuid();
            Guid g5 = Guid.NewGuid();

            //Act
            Card c1 = new Card(g1, "Kraken", 3);
            Card c2 = new Card(g2, "WaterSpell", 78);

            Card c3 = new Card(g3, "Wizzard", 7);

            Card c4 = new Card(g4, "FireElves", 65);
            Card c5 = new Card(g5, "ElderDragon", 89);

            //Assess
            Assert.That(c1.Id == g1 && c1.Name == "Kraken" && c1.Element == Element.normal && c1.CType == Type.kraken && c1.Damage == 3);
            Assert.That(c2.Id == g2 && c2.Name == "WaterSpell" && c2.Element == Element.water && c2.CType == Type.spell && c2.Damage == 78);
            Assert.That(c3.Id == g3 && c3.Name == "Wizzard" && c3.Element == Element.normal && c3.CType == Type.wizard && c3.Damage == 7);
            Assert.That(c4.Id == g4 && c4.Name == "FireElves" && c4.Element == Element.fire && c4.CType == Type.elf && c4.Damage == 65);
            Assert.That(c5.Id == g5 && c5.Name == "ElderDragon" && c5.Element == Element.normal && c5.CType == Type.dragon && c5.Damage == 89 );
        }

        [Test]
        public void IsMonsterTest()
        {
            //Arrange
            Card c1 = new Card(Guid.NewGuid(), "Kraken", 3);
            Card c2 = new Card(Guid.NewGuid(), "FireElves", 65);
            Card c3 = new Card(Guid.NewGuid(), "FireSpell", 5);

            //Act
            bool result = c1.IsMonster();
            bool result2 = c2.IsMonster();
            bool result3 = c3.IsMonster();

            //Assess
            Assert.That(result, Is.True);
            Assert.That(result2, Is.True);
            Assert.That(result3, Is.False);
        }

    }
}
