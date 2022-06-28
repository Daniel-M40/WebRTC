using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;

namespace WebSocketServer.Middleware
{
    public class WebSocketServerMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly WebSocketServerConnectionManager _manager;

        public WebSocketServerMiddleware(RequestDelegate next, WebSocketServerConnectionManager manager)
        {
            _next = next;
            _manager = manager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync(); // Creates websocket connection
                Console.WriteLine("WebSocket Connected");

                string ConnID = _manager.AddSocket(webSocket); // generates a unique identifier (GUID)
                await SendConnIDAsync(webSocket, ConnID);

                await ReceiveMessage(webSocket, async (result, buffer) =>
                {
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        Console.WriteLine("Message Received");
                        Console.WriteLine($"Message: {Encoding.UTF8.GetString(buffer, 0, result.Count)}");
                        await RouteJSONMessageAsync(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        return;
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        string id = _manager.GetAllSockets().FirstOrDefault(s => s.Value == webSocket).Key;

                        Console.WriteLine("Recived Close Message");

                        _manager.GetAllSockets().TryRemove(id, out WebSocket sock); // When the user closes the connection remove their id from the dictionary

                        await sock.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                        return;
                    }
                });
            }
            else
            {
                Console.WriteLine("Hello from the 2nd request delegate.");
                await _next(context);
            }
        }

        private async Task SendConnIDAsync(WebSocket socket, string connID)
        {
            JsonID data = new JsonID(connID.ToString(), _manager.GetAmountOfUsers()); ;
            var json = JsonConvert.SerializeObject(data);
            var buffer = Encoding.UTF8.GetBytes(json);

            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ReceiveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 1024 * 1024];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);

                handleMessage(result, buffer);
            }
        }

        public async Task RouteJSONMessageAsync(string message)
        {
            var routeOb = JsonConvert.DeserializeObject<dynamic>(message);

            if (routeOb.operation == 0)
            {
                await RouteMessagesAsync(routeOb);
            }
            else if (routeOb.operation == 1) 
            {
                await GetUserCountAsync(routeOb);
            }
            



            
        }


        public async Task RouteMessagesAsync(dynamic routeOb) 
        {
            string base64String = routeOb.base64String;

            if (Guid.TryParse(routeOb.To.ToString(), out Guid guidOutput)) // Checks to see if there is an id to send the message to
            {
                Console.WriteLine("Targeted");
                var sock = _manager.GetAllSockets().FirstOrDefault(s => s.Key == routeOb.From.ToString()); // Finds the connection id
                
                if (sock.Value != null)
                {
                    if (sock.Value.State == WebSocketState.Open)
                    {
                        
                        JsonMessage data = new JsonMessage(base64String, routeOb.To.ToString(), "Direct");
                        var json = JsonConvert.SerializeObject(data);
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None); // Sends the message to the recipient
                    }
                }
                else
                {
                    Console.WriteLine("Invalid recipient");
                }
            }
            else // if the reciepient id is null then broadcast the message to everyone
            {
                Console.WriteLine("Broadcast");
                foreach (var sock in _manager.GetAllSockets())
                {
                    if (sock.Value.State == WebSocketState.Open)
                    {
                        JsonMessage data = new JsonMessage(base64String, routeOb.To.ToString(), "Broadcast");
                        var json = JsonConvert.SerializeObject(data);
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
        }


        public async Task GetUserCountAsync(dynamic routeOb)
        {
            JsonUserCount data = new JsonUserCount(_manager.GetAmountOfUsers());
            var json = JsonConvert.SerializeObject(data);

            foreach (var sock in _manager.GetAllSockets())
            {
                if (sock.Value.State == WebSocketState.Open)
                {
                    await sock.Value.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        public Base64 Video(dynamic data)
        {
            string base64 = "";
            try
            {
                string path = @"..\" + data.path;
                byte[] myVideoByteArray = File.ReadAllBytes(path);
                base64 = Convert.ToBase64String(myVideoByteArray);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            Base64 obj = new Base64(base64);

            return obj;

        }


    }

    public class Base64
    {
        public Base64(string data)
        {
            base64 = data;
        }

        public string base64 { get; set; }

    }

    public class JsonUserCount
    {
        public JsonUserCount(int users)
        {
            usersAmount = users;
        }

        public int usersAmount { get; set; }

    }


    public class JsonMessage
    {
        public JsonMessage(string base64, string id, string type) 
        {
            base64String = base64;
            ID = id;
            Type = type;
        }

        public string base64String { get; set; }
        public string ID { get; set; }
        public string Type { get; set; }

    }


    public class JsonID
    {
        public JsonID(string id, int users)
        {
            ID = id;
            usersAmount = users;
        }

        public string ID { get; set; }
        public int usersAmount { get; set; }

    }
}
