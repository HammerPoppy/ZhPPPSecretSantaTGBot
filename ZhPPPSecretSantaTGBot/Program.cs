using System;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace ZhPPPSecretSantaTGBot
{
    class Program
    {
        static ITelegramBotClient botClient;

        static void Main()
        {
            if (File.Exists("config\\access token.txt"))
            {
                StreamReader sr = new StreamReader("config\\access token.txt");
                string accessToken = sr.ReadLine();
                botClient = new TelegramBotClient(accessToken);
            }
            else
            {
                Console.WriteLine(
                    "Please create directory \"config\" and put there a file \"access token.txt\" with your bot access token");
                Console.ReadKey();
                return;
            }

            var me = botClient.GetMeAsync().Result;
            Console.WriteLine(
                $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
            );

            botClient.OnMessage += Bot_OnMessage;
            botClient.StartReceiving();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            botClient.StopReceiving();
        }

        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                Console.WriteLine($"Received a text message in chat {e.Message.Chat.Id}.");

                await botClient.SendTextMessageAsync(
                    chatId: e.Message.Chat,
                    text: "You said:\n" + e.Message.Text
                );
            }
        }
    }
}