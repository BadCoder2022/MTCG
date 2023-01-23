using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Xml.Linq;

namespace monsterTradingCardGame.Test
{
    public class UserTests
    {

        [Test]
        public void getTokenTest()
        {
            string result = User.CreateTokenString("TestUser");

            Assert.Pass(result,Is.EqualTo("TestUser-mtcgToken"));
        }

        [Test]
        public void UserTest()
        {
            User user = new("TestUser", "pass");

            Assert.IsFalse(user.isLoggedIn == true);
            Assert.IsTrue(user.coins == 20);
        }

        [Test]
        public void UserConstructorTests()
        {
            //Arrange
            string name = "Testname";
            string pwd = "Advanc3dSecurity";
            string token = name + "-mtcgTokenTest";
            int coins = 17;
            string alias = "tester";
            string bio = "dead inside";
            string image = "¯\\_(ツ)_/¯";

            //Act
            User user = new(name, pwd, token, coins, alias, bio, image);

            //Assert
            Assert.That(user.name, Is.EqualTo(name));
            Assert.That(user.pwd, Is.EqualTo(pwd));
            Assert.That(user.token, Is.EqualTo(token));
            Assert.That(user.coins, Is.EqualTo(coins));
            Assert.That(user.aliasname, Is.EqualTo(alias));
            Assert.That(user.bio, Is.EqualTo(bio));
            Assert.That(user.image, Is.EqualTo(image));
        }
    }
}