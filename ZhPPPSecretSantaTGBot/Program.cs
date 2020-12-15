using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace ZhPPPSecretSantaTGBot
{
    class Program
    {
        static ITelegramBotClient BotClient;
        private static Logger Logger;
        private static DBHandler DBHandler;

        static void Main()
        {
            Logger = new Logger();
            DBHandler = new DBHandler(Logger);

            if (File.Exists("config\\access token.txt"))
            {
                StreamReader sr = new StreamReader("config\\access token.txt");
                string accessToken = sr.ReadLine();
                BotClient = new TelegramBotClient(accessToken);
                Logger.Log("Access token successfully acquired");
            }
            else
            {
                Logger.Log("Please create directory \"config\" and put there a file " +
                           "\"access token.txt\" with your bot access token");
                Console.ReadKey();
                return;
            }

            var me = BotClient.GetMeAsync().Result;
            Logger.Log($"Connected successfully. Bot ID {me.Id}; named {me.FirstName}.");

            BotClient.OnMessage += Bot_OnMessage;
            BotClient.StartReceiving();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            Logger.Log("Ended execution by user command");

            BotClient.StopReceiving();
        }

        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                var from = e.Message.From;
                Logger.Log(
                    $"Received a text message in chat {e.Message.Chat.Id}|@{from.Username}|{from.FirstName} {from.LastName}");
                Logger.Log(e.Message.Text);

                if (DBHandler.ContainsUser(from.Id))
                {
                    var user = DBHandler.GetUserById(from.Id);

                    try
                    {
                        await BotClient.SendTextMessageAsync(
                            chatId: e.Message.Chat,
                            text: "Hi! I remembered you, last time you said:\n" +
                                  user.FanOf
                        );
                    }
                    catch (System.Net.Http.HttpRequestException httpRequestException)
                    {
                        Logger.Log($"Error: {httpRequestException.Message} at {httpRequestException.StackTrace}");
                    }

                    user.FanOf = e.Message.Text;
                    DBHandler.Write();
                }
                else
                {
                    var user = new User(from.Id, from.Username, from.FirstName, from.LastName);
                    user = DBHandler.AddNewUser(user);
                    user.FanOf = e.Message.Text;
                    DBHandler.Write();
                    try
                    {
                        await BotClient.SendTextMessageAsync(
                            chatId: e.Message.Chat,
                            text: "Oh hello there! You are new here I see... I'll remember you 🙂"
                        );
                    }
                    catch (System.Net.Http.HttpRequestException httpRequestException)
                    {
                        Logger.Log($"Error: {httpRequestException.Message} at {httpRequestException.StackTrace}");
                    }
                }
            }
        }
    }
}