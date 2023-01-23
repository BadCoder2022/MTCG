using System.Net.Sockets;
using System.Net;
using System.Text;

namespace monsterTradingCardGame.RestServer
{
    /// <summary>
    /// This class is used to send a HTTP-Response to the clients.
    /// </summary>
    internal static class Response
    {
        /// <summary>
        /// This method sends a Response to an HTTP Request.
        /// </summary>
        /// <param name="client">The TcpClient that will be used to send the Request to.</param>
        /// <param name="statusCode">The statuscode of the HTTPMessage.</param>
        /// <param name="body"> The body of the HTTP Message</param>
        /// <param name="isBodyJson"> If True sends the sets the content-Type to application/json</param>
        public static void SendResponse(TcpClient client, HttpStatusCode statusCode, string body, bool isBodyJson = false)
        {

            string data = "HTTP/1.1 " + (int)statusCode + " " + statusCode;

            switch (((int)statusCode).ToString().ToCharArray()[0])      //the first digit of the Status code is used to identify the type of request to send
            {
                case '2':
                    if (isBodyJson)
                        data += "\nContent-Type: application/json";
                    data += $"\nContent-Length: {body.Length}" + $"\n\n{body}";
                    break;
                case '4':
                case '5':
                    data += $"\nContent-Length: {body.Length}" + $"\n\n{body}";
                    break;
                default:
                    data = "This status-Code was not implemented.";
                    break;
            }
            byte[] dbuf = Encoding.ASCII.GetBytes(data);
            client.GetStream().Write(dbuf, 0, dbuf.Length);                    // send a response

            client.GetStream().Close();                                        // shut down the connection
            client.Dispose();

        }
    }
}
