using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace ZhPPPSecretSantaTGBot
{
    class Program
    {
        private static ITelegramBotClient BotClient;
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

            Logger.Log("Ending execution by user command");

            BotClient.StopReceiving();
        }

        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                var from = e.Message.From;
                var to = e.Message.Chat;
                User user;
                Logger.Log(
                    $"Received a text message in chat {e.Message.Chat.Id}|@{from.Username}|{from.FirstName} {from.LastName}");
                Logger.Log(e.Message.Text);
                
                // TODO detect non text
                if (DBHandler.ContainsUser(from.Id))
                {
                    user = DBHandler.GetUserById(from.Id);
                }
                else
                {
                    user = new User(from.Id, from.Username, from.FirstName, from.LastName);
                    user = DBHandler.AddNewUser(user);
                    Logger.Log($"Created new user {from.Username}");
                }

                switch (e.Message.Text)
                {
                    case "/start":
                        SendIntroMessages(to);

                        await Task.Delay(TimeSpan.FromSeconds(4));

                        SendMemo(to);

                        var textToSend = "Чтобы начать регистрацию отправьте команду /start_registration";
                        Logger.Log($"Sending to {to.Id}");
                        Logger.Log(textToSend);
                        SendMessage(to, textToSend);
                        break;
                    case "/send_memo":
                        SendMemo(to);
                        break;
                    case "/send_my_profile":
                        SendUserProfile(to, user);
                        break;
                    default:
                        break;
                }
            }
        }

        static async void SendMemo(ChatId to)
        {
            string textToSend = "21.12 12:21 - закрытие регистрации\n" +
                                "22-е - получение анкет\n" +
                                "26-о вечером - отправка подарков\n" +
                                "27-о вечером (ориентировочно) - получение подарков";

            Logger.Log($"Sending memo to {to}");
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
        }

        static async void SendUserProfile(ChatId to, User user)
        {
            string textToSend = "";
            textToSend += "Статус анкеты: ";
            switch (user.State)
            {
                case States.Registered:
                    textToSend += "Регистрация завершена\n";
                    break;
                case States.NewUser:
                    textToSend += "Регистрация не начата\n";
                    break;
                case States.TargetChosen:
                    textToSend += "Регистрация завершена\n";
                    break;
                case States.TargetSended:
                    textToSend += "Регистрация завершена, цель получена\n";
                    break;
                default:
                    textToSend += "Регистрация в процессе\n";
                    break;
            }

            textToSend += "ФИО: ";
            textToSend += user.OfficialName + "\n";
            textToSend += "Номер телефона: ";
            textToSend += user.Phone + "\n";
            textToSend += "Город и номер отделения НП: ";
            textToSend += user.Post + "\n";
            textToSend += "Я фанат: ";
            textToSend += user.FanOf + "\n";
            textToSend += "Мне не стоит дарить: ";
            textToSend += user.Ban + "\n";

            Logger.Log($"Sending profile to {to}");
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
        }

        static async void SendIntroMessages(ChatId to)
        {
            double sendOffsetInSecs = 0.3;
            Logger.Log($"Sending intro messages to {to}");

            string textToSend = "Приветствуем Вас в боте Секретного Санты ЖППП!";
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend =
                "С помощью этого бота вы можете зарегистрироваться для участия, а также узнать всю подробную инфу" +
                " касательно Секретного Санты 2020 by Zhopki Popki©! <i>главный админ убьет меня за такое название ну не" +
                " важно давайте начинать уже там целая простынь текста дальше...</i>";
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend =
                "Поскольку этот год оч прикольный такой и интересный в некоторых моментах, мы в редакции Желтой" +
                " Прессы решили, что разумнее всего будет провести данный ивент с некоторыми поправками, касающимися" +
                " доставки подарков. И ДА - подарки будут отправляться по почте :(";
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "И тут есть два момента: во-первых - Вы не сможете ехидно пронаблюдать за тем как кто-то" +
                         " распаковывает, тщательно Вами запакованную в матрешку из коробок и туалетной бумаги флешку," +
                         " но на этот счет мы попробуем кое-что предпринять; а во-вторых это то, что человек," +
                         " принимающий подарок, сможет узнать от кого получил подарок (из-за накладной).";
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "<i>(Кто-то по секрету сказал что можно попробовать на почте при отправке указать не Ваши" +
                         " а абсолютно рандомные данные и таким образом остаться анонимным, однако этот вариант" +
                         " стоит рассматривать только для энтузиастов и вообще обязательным не является)</i>";
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Ладно, давайте перейдем к делу. Для начала Вам нужно будет заполнить небольшую анкету," +
                         " в которой указать такую информацию:\n" +
                         "ФИО\n" +
                         "Номер телефона\n" +
                         "Инфа о почтовом отделении\n" +
                         "А также некоторая информация для человека который будет дарить, дабы ни у кого " +
                         "не было проблем с выбором подарка";
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Форма для регистрации будет открыта до 21.12 12:21 (поторопитесь, попингуйте-потэгайте" +
                         " всех ППшничков которые должны в этом участвовать чтобы никто не пропустил), и 22-о" +
                         " днем Вам придет анкета того кому Вы будете дарить подарок. Далее Вы готовите подарок " +
                         "и отправляете его 26-о числа (по-возможности так чтоб посылка пришла получателю 27-о числа).";
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Ориентировочно 27-о числа вечером Вам приходит посылочка и Вы идете ее забираете. " +
                         "Радостные открываете свои носки с оленями и такие же радостные делаете фоточки-видосики и " +
                         "кидаете их сюда в бот, в чатик Прикладного питания или делитесь ими со всеми каким либо " +
                         "другим образом. Так каждый сможет понаблюдать за получателем подарка и все разделят " +
                         "радость праздника с другими.";
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Касательно подарка - это может быть что угодно, главное чтоб было весело и " +
                         "приятно <i>и <b>от души</b></i>. Наши рекомендации (чтобы никому не было обидно) по " +
                         "поводу стоимости подарка это 100-200грн, а также не забывайте что около 50грн пойдет на " +
                         "отправку посылки. К тому же это стоит учитывать при упаковке подарка, например всякие " +
                         "банты на коробках по любому помнут на почте если не предпринять какие-то меры 😭. <i>Банты топ.</i>";
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "И на этом наконец все! Совсем скоро Вы уже начнете заполнять анкету, но перед этим " +
                         "хотелось бы сказать про еще один очень важный момент. Этот бот написан каким-то криворуким " +
                         "ППшником на коленке и будет очень неудивительно если бот ляжет или не сможет исполнять " +
                         "свои функции в каком-то другом плане. В таком случае пишите на аккаунт нашей поддержки" +
                         " - @bIudger, и мы постараемся помочь Вам в ближайшее время. " +
                         "Иии да, можете уже заполнять анкету, удачи Вам с подготовкой подарка и счастливых праздников 🥳!";
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "С любовью, редакция @ppidory <i>(ахвахвхахв у нас внатуре тэг канала - ПИПИДОРЫ)</i>";
            Logger.Log(textToSend);
            SendMessage(to, textToSend);
        }

        static async void SendMessage(ChatId to, string message)
        {
            try
            {
                await BotClient.SendTextMessageAsync(
                    chatId: to,
                    text: message,
                    parseMode: ParseMode.Html
                );
            }
            catch (Exception exception)
            {
                Logger.Log($"Error: {exception}");
            }
        }
    }
}