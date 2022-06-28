using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Net.WebSockets;


namespace WebSocketServer.Middleware
{
    public class WebSocketServerConnectionManager
    {
        private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public ConcurrentDictionary<string, WebSocket> GetAllSockets()
        {
            return _sockets;
        }

        public string AddSocket(WebSocket socket)
        {
            string ConnID = Guid.NewGuid().ToString(); // Creates a new id
            _sockets.TryAdd(ConnID, socket); // adds it to a dictionary
            Console.WriteLine("Connection Added: " + ConnID);

            return ConnID;
        }


        public int GetAmountOfUsers()
        {
            return _sockets.Count();
        }
    }
}
