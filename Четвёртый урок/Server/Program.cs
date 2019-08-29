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
        public int chatId;
        public string message;
        public string nick;
        public string password;
    }

    class Program
    {
        static string ip = "127.0.0.1", port = "8080", sql = "", connStr = "server=" + ip + ";user=root;database=chat;password='';", id = "", name = "", password = "";
        static MySqlConnection conn = new MySqlConnection(connStr);
        static IWebSocketServer server = new WebSocketServer("ws://" + ip + ":" + port);
        static List<IWebSocketConnection> clients = new List<IWebSocketConnection>();
        static List<IWebSocketConnection> users = new List<IWebSocketConnection>();
        static List<string> nicks = new List<string>();

        static void Main(string[] args)
        {
            try
            {
                server.Start(socket =>
                {
                    void GetMessage()
                    {
                        conn.Open();
                        sql = "SELECT id, userName, message, chatId FROM messages";
                        MySqlCommand commandMessage = new MySqlCommand(sql, conn);
                        MySqlDataReader readerMessage = commandMessage.ExecuteReader();
                        while (readerMessage.Read())
                        {
                            socket.Send("{\"type\":0,\"nameUser\":\"" + readerMessage["userName"] + "\",\"message\":\"" + readerMessage["message"] + "\",\"chatId\":\"" + readerMessage["chatId"] + "\"}");
                        }
                        conn.Close();
                    }
                    socket.OnOpen = () =>
                    {
                        socket.ConnectionInfo.Headers["name"] = null;
                        clients.Add(socket);
                        Console.WriteLine("Подключился " + socket.ConnectionInfo.ClientIpAddress);
                    };
                    socket.OnClose = () =>
                    {
                        if (socket.ConnectionInfo.Headers["name"] == null)
                        {
                            clients.Remove(socket);
                        }
                        else
                        {
                            users.Remove(socket);
                            nicks.Remove(socket.ConnectionInfo.Headers["name"]);
                            Console.WriteLine(socket.ConnectionInfo.Headers["name"] + " отключился!");
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
                                    {
                                        bool chatId = false;
                                        conn.Open();
                                        sql = "SELECT id, listOfNicks, chatName FROM chats WHERE listOfNicks LIKE '%" + socket.ConnectionInfo.Headers["name"] + "%' AND id = \"" + data.chatId + "\"";
                                        MySqlCommand checkCommand = new MySqlCommand(sql, conn);
                                        MySqlDataReader checkReader = checkCommand.ExecuteReader();
                                        if (checkReader.Read())
                                            chatId = true;
                                        conn.Close();
                                        if (chatId)
                                        {
                                            conn.Open();
                                            sql = "INSERT INTO messages(userName, message, chatId) VALUES ('" + socket.ConnectionInfo.Headers["name"] + "','" + data.message + "','" + data.chatId + "')";
                                            MySqlCommand command = new MySqlCommand(sql, conn);
                                            command.ExecuteNonQuery();
                                            conn.Close();
                                            foreach (IWebSocketConnection user in users)
                                            {
                                                user.Send("{\"type\":0,\"nameUser\":\"" + socket.ConnectionInfo.Headers["name"] + "\",\"message\":\"" + data.message + "\",\"chatId\":\"" + data.chatId + "\"}");
                                            }
                                        }
                                    }
                                    break;
                                case 1:
                                    if (data.nick != null & data.password != null & nicks.IndexOf(data.nick, 0) == -1)
                                    {
                                        conn.Open();
                                        sql = "SELECT id, name, password FROM users WHERE name=\"" + data.nick + "\" AND password=\"" + data.password + "\"";
                                        MySqlCommand command = new MySqlCommand(sql, conn);
                                        MySqlDataReader readerUser = command.ExecuteReader();
                                        if (readerUser.Read())
                                        {
                                            id = readerUser["id"].ToString();
                                            name = readerUser["name"].ToString();
                                            password = readerUser["password"].ToString();
                                            socket.ConnectionInfo.Headers["name"] = name;
                                            clients.Remove(socket);
                                            users.Add(socket);
                                            nicks.Add(name);
                                            Console.WriteLine(socket.ConnectionInfo.Headers["name"] + " подключился!");
                                            socket.Send("{\"type\":1,\"error\":\"0\"}");
                                        }
                                        else
                                        {
                                            socket.Send("{\"type\":1,\"error\":\"1\"}");
                                        }
                                        conn.Close();
                                        if (socket.ConnectionInfo.Headers["name"] != null)
                                            GetMessage();
                                    }
                                    else
                                    {
                                        socket.Send("{\"type\":1,\"error\":\"2\"}");
                                    }
                                    break;
                                case 2:
                                    bool chatIdBool = false;
                                    conn.Open();
                                    sql = "SELECT id, listOfNicks, chatName FROM chats WHERE listOfNicks LIKE '%" + socket.ConnectionInfo.Headers["name"] + "%' AND id = \"" + data.chatId + "\"";
                                    MySqlCommand checkChatCommand = new MySqlCommand(sql, conn);
                                    MySqlDataReader checkChatReader = checkChatCommand.ExecuteReader();
                                    if (checkChatReader.Read())
                                        chatIdBool = true;
                                    conn.Close();
                                    if (chatIdBool) {
                                        socket.Send("{\"type\":0,\"clear\":\"1\",\"chatId\":\"" + data.chatId + "\"}");
                                        conn.Open();
                                        sql = "SELECT msg.id, msg.userName, msg.message, msg.chatId, chat.listOfNicks, chat.chatName FROM messages msg INNER JOIN chats chat ON msg.chatId = chat.id WHERE chat.listOfNicks LIKE '%" + socket.ConnectionInfo.Headers["name"] + "%'";
                                        MySqlCommand commandMessage = new MySqlCommand(sql, conn);
                                        MySqlDataReader readerMessage = commandMessage.ExecuteReader();
                                        while (readerMessage.Read())
                                        {
                                            socket.Send("{\"type\":0,\"nameUser\":\"" + readerMessage["userName"] + "\",\"message\":\"" + readerMessage["message"] + "\",\"chatId\":\"" + readerMessage["chatId"] + "\"}");
                                        }
                                        conn.Close();
                                    }
                                    else
                                    {
                                        socket.Send("{\"type\":0,\"errorCode\":0}");
                                    }
                                    break;
                                case 3:
                                    conn.Open();
                                    sql = "SELECT id, listOfNicks, chatName, lastMessage FROM chats WHERE listOfNicks LIKE '%" + socket.ConnectionInfo.Headers["name"] + "%'";
                                    MySqlCommand commandTest = new MySqlCommand(sql, conn);
                                    MySqlDataReader reader = commandTest.ExecuteReader();
                                    while (reader.Read())
                                    {
                                        string chatName;
                                        if (reader["chatName"].ToString() == "") {
                                            chatName = "";
                                            if (reader["listOfNicks"].ToString().Split(',')[0] == socket.ConnectionInfo.Headers["name"])
                                                chatName = reader["listOfNicks"].ToString().Split(',')[1];
                                            else
                                                chatName = reader["listOfNicks"].ToString().Split(',')[0];
                                        }
                                        else
                                            chatName = reader["chatName"].ToString();
                                        socket.Send("{\"type\":2,\"chatName\":\"" + chatName + "\",\"lastMessage\":\"" + reader["lastMessage"] + "\", \"chatId\":\""+ reader["id"] + "\"}");
                                    }
                                    conn.Close();
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
