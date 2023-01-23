using monsterTradingCardGame.RestServer;

namespace monsterTradingCardGame.Test
{
    internal class RequestTests
    {
        public void ParseRequestTest()
        {
            //Arrange
            string IncomingRequest = "POST /packages HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nContent-Type: application/json\r\n" +
                                     "Authorization: Basic admin-mtcgToken\r\nContent-Length: 410\r\n\r\n" +
                                     "[{\"Id\":\"2272ba48-6662-404d-a9a1-41a9bed316d9\", \"Name\":\"WaterGoblin\", \"Damage\": 11.0}]";

            Request request = new();

            //Act
            request.ParseRequest(IncomingRequest);


            //Assert
            Assert.Pass(request.method, Is.EqualTo("POST"));
            Assert.Pass(request.path, Is.EqualTo("/packages"));
            Assert.Pass(request.GetToken(), Is.EqualTo("admin-mtcgToken"));
            Assert.Pass(request.body, Is.EqualTo("[{\"Id\":\"2272ba48-6662-404d-a9a1-41a9bed316d9\", \"Name\":\"WaterGoblin\", \"Damage\": 11.0}]"));
        }

       
        [Test]
        public void ParseRequest_additionalRoute_Tests()
        {
            //Arrange
            string IncomingRequest = "PUT /users/kienboec HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nContent-Type: application/json\r\nAuthorization: Basic kienboec-mtcgToken\r\nContent-Length: 56\r\n\r\n{\"Name\": \"Hoax\",  \"Bio\": \"me playin...\", \"Image\": \":-)\"}";
            Request result = new();
            
            //Act
            result.ParseRequest(IncomingRequest);



            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.path, Is.EqualTo("/users/kienboec"));
                Assert.That(result.method, Is.EqualTo("PUT"));
                Assert.That(result.add_path[0], Is.EqualTo(String.Empty));
                Assert.That(result.add_path[1], Is.EqualTo("users"));
                Assert.That(result.add_path[2], Is.EqualTo("kienboec"));
                Assert.That(result.GetToken(), Is.EqualTo("kienboec-mtcgToken"));
                Assert.That(result.body, Is.EqualTo("{\"Name\": \"Hoax\",  \"Bio\": \"me playin...\", \"Image\": \":-)\"}"));
            });
        }

        [Test]
        public void ParseJsonTests()
        {
            //Arrange
            string IncomingRequest = "PUT /users/kienboec HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nContent-Type: application/json\r\nAuthorization: Basic kienboec-mtcgToken\r\nContent-Length: 56\r\n\r\n{\"Name\": \"Hoax\",  \"Bio\": \"me playin...\", \"Image\": \":-)\"}";

            //Act
            Request result = new();
            result.ParseRequest(IncomingRequest);



            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.path, Is.EqualTo("/users/kienboec"));
                Assert.That(result.method, Is.EqualTo("PUT"));
                Assert.That(result.add_path[0], Is.EqualTo(String.Empty));
                Assert.That(result.add_path[1], Is.EqualTo("users"));
                Assert.That(result.add_path[2], Is.EqualTo("kienboec"));
                Assert.That(result.GetToken(), Is.EqualTo("kienboec-mtcgToken"));
                Assert.That(result.body, Is.EqualTo("{\"Name\": \"Hoax\",  \"Bio\": \"me playin...\", \"Image\": \":-)\"}"));
            });
        }

        [Test]
        public void ParseJsonTest()
        {
            //Arrange
            Guid id = Guid.NewGuid();

            string IncomingRequest = "POST /packages HTTP/1.1\r\nHost: localhost:10001\r\nUser-Agent: curl/7.83.1\r\nAccept: */*\r\nContent-Type: application/json\r\n" +
                                     "Authorization: Basic admin-mtcgToken\r\nContent-Length: 410\r\n\r\n" +
                                     "[{\"Id\":\"" + id + "\", \"Name\":\"WaterGoblin\", \"Damage\": 11.0}]";

            Request request = new();
            request.ParseRequest(IncomingRequest);


            //Act
            var result = request.ParseJson();
            List<Card> cards = (List<Card>)result;
            Card c = cards[0];


            //Assert
            Assert.That(typeof(List<Card>), Is.EqualTo(result.GetType()));
            Assert.That(c.Id, Is.EqualTo(id));
            Assert.That(c.CType, Is.EqualTo(Type.goblin));
            Assert.That(c.Damage == 11.0);
        }

    }
}
