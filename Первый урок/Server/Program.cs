using System;
using System.Collections.Generic;
using Fleck;
using MySql.Data.MySqlClient;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;

namespace Server
{
    class data_message
    {
        public int type;
        public int id;
        public string message;
        public string nick;
        public string password;
    }

    class Program
    {
        static string ip = "127.0.0.1", port = "8080";
        static IWebSocketServer server = new WebSocketServer("ws://"+ ip +":"+ port);
        static List<IWebSocketConnection> clients = new List<IWebSocketConnection>();

        static void Main(string[] args)
        {
            try
            {
                server.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        clients.Add(socket);
                        socket.ConnectionInfo.Headers["name"] = "Test";
                        Console.WriteLine("Подключился "+ socket.ConnectionInfo.ClientIpAddress);
                    };
                    socket.OnClose = () =>
                    {
                        clients.Remove(socket);
                        Console.WriteLine("Отключился " + socket.ConnectionInfo.ClientIpAddress);
                    };
                    socket.OnMessage = message =>
                    {
                        data_message data = new data_message();
                        try
                        {
                            data = JsonConvert.DeserializeObject<data_message>(message);
                            switch (data.type)
                            {
                                case 0:
                                    foreach (IWebSocketConnection user in clients)
                                        user.Send("{\"type\":0,\"nameUser\":\""+ socket.ConnectionInfo.Headers["name"] +"\",\"message\":\""+ data.message +"\"}");
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    };
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.ReadLine();
        }
    }
}
