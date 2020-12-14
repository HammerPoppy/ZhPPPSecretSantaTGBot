using System;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace ZhPPPSecretSantaTGBot
{
    class Program
    {
        static ITelegramBotClient botClient;
        static Logger logger;

        static void Main()
        {
            logger = new Logger();

            if (File.Exists("config\\access token.txt"))
            {
                StreamReader sr = new StreamReader("config\\access token.txt");
                string accessToken = sr.ReadLine();
                botClient = new TelegramBotClient(accessToken);
                logger.Log("Access token successfully acquired");
            }
            else
            {
                logger.Log("Please create directory \"config\" and put there a file " +
                           "\"access token.txt\" with your bot access token");
                Console.ReadKey();
                return;
            }

            var me = botClient.GetMeAsync().Result;
            logger.Log($"Connected successfully. Bot ID {me.Id}; named {me.FirstName}.");

            botClient.OnMessage += Bot_OnMessage;
            botClient.StartReceiving();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            logger.Log("Ended execution by user command");

            botClient.StopReceiving();
        }

        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                logger.Log($"Received a text message in chat {e.Message.Chat.Id}|@{e.Message.From.Username}|{e.Message.From.FirstName} {e.Message.From.LastName}");
                logger.Log(e.Message.Text);

                await botClient.SendTextMessageAsync(
                    chatId: e.Message.Chat,
                    text: "You said:\n" + e.Message.Text
                );
            }
        }
    }
}