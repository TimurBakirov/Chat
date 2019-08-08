﻿using System;
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
        static string ip = "127.0.0.1", port = "8080", sql = "", connStr = "server=" + ip + ";user=root;database=chat;password='';", id = "", name = "", password = "";
        static MySqlConnection conn = new MySqlConnection(connStr);
        static IWebSocketServer server = new WebSocketServer("ws://"+ ip +":"+ port);
        static List<IWebSocketConnection> clients = new List<IWebSocketConnection>();
        static List<IWebSocketConnection> users = new List<IWebSocketConnection>();
        static List<string> nicks = new List<string>();

        static void Main(string[] args)
        {
            try
            {
                server.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        clients.Add(socket);
                        Console.WriteLine("Подключился "+ socket.ConnectionInfo.ClientIpAddress);
                    };
                    socket.OnClose = () =>
                    {
                        if (nicks.IndexOf(socket.ConnectionInfo.Headers["name"], 0) == -1)
                        {
                            clients.Remove(socket);
                        }
                        else
                        {
                            users.Remove(socket);
                            nicks.Remove(name);
                        }
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
                                    if (data.message != "" && data.message != null)
                                        foreach (IWebSocketConnection user in users)
                                            user.Send("{\"type\":0,\"nameUser\":\""+ socket.ConnectionInfo.Headers["name"] +"\",\"message\":\""+ data.message +"\"}");
                                    break;
                                case 1:
                                    if (data.nick != null & data.password != null & nicks.IndexOf(data.nick, 0) == -1) {
                                        conn.Open();
                                        sql = "SELECT id, name, password FROM users WHERE name=\"" + data.nick + "\" AND password=\"" + data.password + "\"";
                                        MySqlCommand command = new MySqlCommand(sql, conn);
                                        MySqlDataReader reader = command.ExecuteReader();
                                        if (reader.Read())
                                        {
                                            id = reader["id"].ToString();
                                            name = reader["name"].ToString();
                                            password = reader["password"].ToString();
                                            socket.ConnectionInfo.Headers["name"] = name;
                                            clients.Remove(socket);
                                            users.Add(socket);
                                            nicks.Add(name);
                                            socket.Send("{\"type\":1,\"error\":\"0\"}");
                                        }
                                        else
                                        {
                                            socket.Send("{\"type\":1,\"error\":\"1\"}");
                                        }
                                        conn.Close();
                                    }
                                    else
                                    {
                                        socket.Send("{\"type\":1,\"error\":\"2\"}");
                                    }
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
