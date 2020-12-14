using System;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace ZhPPPSecretSantaTGBot
{
    class Program
    {
        static ITelegramBotClient BotClient;
        static Logger Logger;
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
                Logger.Log($"Received a text message in chat {e.Message.Chat.Id}|@{e.Message.From.Username}|{e.Message.From.FirstName} {e.Message.From.LastName}");
                Logger.Log(e.Message.Text);

                await BotClient.SendTextMessageAsync(
                    chatId: e.Message.Chat,
                    text: "You said:\n" + e.Message.Text
                );
            }
        }
    }
}