using System.Reflection;
using monsterTradingCardGame.BattleFunctions;

namespace monsterTradingCardGame.Test
{
    internal class BattleTests
    {
        [Test]
        public void CardBattleMonsterTests1()
        {
            //Arrange
            Card c1 = new(Guid.NewGuid(), "FireGoblin", 1);
            Card c2 = new(Guid.NewGuid(), "WaterDragon", 2);

            System.Type battleType = typeof(Battle);
            MethodInfo cardBattleMethod = battleType.GetMethod("CardBattle", BindingFlags.Static | BindingFlags.NonPublic);

            // Act
            var result1 = (Tuple<Card, string>)cardBattleMethod.Invoke(null, new object[] { c1, c2 });

            //Assess
            Assert.That(result1.Item1, Is.EqualTo(c1));
        }

        [Test]
        public void CardBattleMonsterTests2()
        {

            //Arrange
            Card c1 = new(Guid.NewGuid(), "Knight", 3);
            Card c2 = new(Guid.NewGuid(), "WaterTroll", 10);

            System.Type battleType = typeof(Battle);
            MethodInfo cardBattleMethod = battleType.GetMethod("CardBattle", BindingFlags.Static | BindingFlags.NonPublic);

            // Act
            var result1 = (Tuple<Card, string>)cardBattleMethod.Invoke(null, new object[] { c1, c2 });

            //Assess
            Assert.That(result1.Item1, Is.EqualTo(c1));
        }

        [Test]
        public void CardBattleTestSpecialRules1()
        {

            //Arrange
            Card c1 = new(Guid.NewGuid(), "FireGoblin", 5);
            Card c2 = new(Guid.NewGuid(), "young WaterDragon", 3);

            System.Type battleType = typeof(Battle);
            MethodInfo cardBattleMethod = battleType.GetMethod("CardBattle", BindingFlags.Static | BindingFlags.NonPublic);

            // Act
            var result1 = (Tuple<Card, string>)cardBattleMethod.Invoke(null, new object[] { c1, c2 });

            //Assess
            Assert.That(result1.Item1, Is.EqualTo(c1));
        }

        [Test]
        public void CardBattleTestSpecialRules2()
        {

            //Arrange
            Card c1 = new(Guid.NewGuid(), "Wizard", 3);
            Card c2 = new(Guid.NewGuid(), "Orks", 89);

            System.Type battleType = typeof(Battle);
            MethodInfo cardBattleMethod = battleType.GetMethod("CardBattle", BindingFlags.Static | BindingFlags.NonPublic);

            // Act

            var result2 = (Tuple<Card, string>)cardBattleMethod.Invoke(null, new object[] { c1, c2 });

            //Assess
            Assert.That(result2.Item1, Is.EqualTo(c2));
        }

        [Test]
        public void CardBattleTestSpecialRules3()
        {

            //Arrange
            Card c1 = new(Guid.NewGuid(), "Kraken", 3);
            Card c2 = new(Guid.NewGuid(), "WaterSpell", 89);


            System.Type battleType = typeof(Battle);
            MethodInfo cardBattleMethod = battleType.GetMethod("CardBattle", BindingFlags.Static | BindingFlags.NonPublic);

            // 
            var result3 = (Tuple<Card, string>)cardBattleMethod.Invoke(null, new object[] { c1, c2 });

            //
                Assert.That(result3.Item1, Is.EqualTo(c2));
        }

        [Test]
        public void CardBattleTestSpecialRules4()
        {
            //Arrange
            Card c1 = new(Guid.NewGuid(), "WaterSpell", 89);
            Card c2 = new(Guid.NewGuid(), "Knight", 7);

            System.Type battleType = typeof(Battle);
            MethodInfo cardBattleMethod = battleType.GetMethod("CardBattle", BindingFlags.Static | BindingFlags.NonPublic);

            // Act
            var result1 = (Tuple<Card, string>)cardBattleMethod.Invoke(null, new object[] { c1, c2 });


            //Assess
            Assert.That(result1.Item1, Is.EqualTo(c2));   
        }


        [Test]
        public void CardBattleTestSpecialRules5()
        {

            //Arrange
            Card c1 = new(Guid.NewGuid(), "FireElves", 65);
            Card c2 = new(Guid.NewGuid(), "ElderDragons", 89);

            System.Type battleType = typeof(Battle);
            MethodInfo cardBattleMethod = battleType.GetMethod("CardBattle", BindingFlags.Static | BindingFlags.NonPublic);

            // Act
            var result = (Tuple<Card, string>)cardBattleMethod.Invoke(null, new object[] { c1, c2 });

            //Assess

            Assert.That(result.Item1, Is.EqualTo(c2));
        }
        [Test]
        public void CardBattleElementalDamageTest()
        {
            //Arrange
            Card c1 = new (Guid.NewGuid(), "FireGoblin", 5);
            Card c2 = new (Guid.NewGuid(), "weak WaterSpell", 3);

            Card c3 = new (Guid.NewGuid(), "undefined WaterCreature", 60);
            Card c4 = new (Guid.NewGuid(), "GravitySpell", 15);

            Card c5 = new (Guid.NewGuid(), "WaterElves", 38);
            Card c6 = new (Guid.NewGuid(), "FireStorm (Spell)", 151);

            Card c7 = new (Guid.NewGuid(), "FireKnight", 35);
            Card c8 = new (Guid.NewGuid(), "Summon Earthlances (Spell)", 69);

            //Act
            System.Type battleType = typeof(Battle);
            MethodInfo cardBattleMethod = battleType.GetMethod("CardBattle", BindingFlags.Static | BindingFlags.NonPublic);

            // Act
            var result1 = (Tuple<Card, string>)cardBattleMethod.Invoke(null, new object[] { c1, c2 });
            var result2 = (Tuple<Card, string>)cardBattleMethod.Invoke(null, new object[] { c3, c4 });
            var result3 = (Tuple<Card, string>)cardBattleMethod.Invoke(null, new object[] { c5, c6 });
            var result4 = (Tuple<Card, string>)cardBattleMethod.Invoke(null, new object[] { c7, c8 });
           

            //Assess
            Assert.Multiple(() =>
            {
                Assert.That(result1.Item1, Is.EqualTo(c1));
                Assert.That(result2.Item1, Is.EqualTo(null));
                Assert.That(result3.Item1, Is.EqualTo(c6));
                Assert.That(result4.Item1, Is.EqualTo(c8));
            });

        }

        [Test]
        public void BattleTest()
        {
            //Arrange
            User u1 = new User("Test","passwd");
            User u2 = new User("Player2", "passwd");
            Card c1 = new(Guid.NewGuid(), "FireKnight", 35);
            Card c2 = new(Guid.NewGuid(), "Summon Earthlances (Spell)", 69);
            List<Card> deck1 = new List<Card>();
            List<Card> deck2 = new List<Card>();
            deck1.Add(c1);
            deck2.Add(c2);

            Tuple<User, List<Card>> t1 = Tuple.Create(u1, deck1);
            Tuple<User, List<Card>> t2 = Tuple.Create(u2, deck2);

            //Act
            BLog result = Battle.StartBattle(t1, t2);

            //Assess
            Assert.That(result.Winner.name, Is.EqualTo(u2.name));
            Assert.That(result.Loser.name, Is.EqualTo(u1.name));
        }
    }
}
