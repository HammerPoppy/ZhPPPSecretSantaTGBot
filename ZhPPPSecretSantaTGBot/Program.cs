using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming

namespace ZhPPPSecretSantaTGBot
{
    class Program
    {
        private static ITelegramBotClient BotClient;
        private static Logger Logger;
        private static DBHandler DBHandler;

        private static readonly DateTime SecondStageDateTime = new DateTime(2020, 12, 21, 12, 21, 00);
        private static bool IsInSecondStage;

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

            if (DateTime.Now > SecondStageDateTime)
            {
                IsInSecondStage = true;
                Logger.Log("Bot are in second stage");
            }
            else
            {
                Logger.Log("Bot are in first stage");
            }

            if (IsInSecondStage)
            {
                Logger.Log("Setting second stage command set");
                BotClient.SetMyCommandsAsync(new[]
                {
                    new BotCommand {Command = "start", Description = "получить начальные сообщения заново"},
                    new BotCommand {Command = "send_memo", Description = "посмотреть памятку с датами"},
                    new BotCommand {Command = "send_my_profile", Description = "посмотреть свою анкету"},
                    new BotCommand {Command = "send_target_profile ", Description = "посмотреть анкету цели"}
                });
            }
            else
            {
                Logger.Log("Setting first stage command set");
                BotClient.SetMyCommandsAsync(new[]
                {
                    new BotCommand {Command = "start", Description = "получить начальные сообщения заново"},
                    new BotCommand {Command = "send_memo", Description = "посмотреть памятку с датами"},
                    new BotCommand {Command = "send_my_profile", Description = "посмотреть свою анкету"},
                    new BotCommand {Command = "start_registration", Description = "начать регистрацию"},
                    new BotCommand {Command = "abort_registration", Description = "отменить регистрацию"}
                });
            }

            BotClient.OnMessage += Bot_OnMessage;
            BotClient.StartReceiving();

            do
            {
                Console.WriteLine("Send help to list available commands");
                var command = Console.ReadLine();
                switch (command)
                {
                    case "help":
                        Console.WriteLine("help - list available commands\n" +
                                          "send registratiom end reminder - self explanatory\n" +
                                          "exit - end program");
                        break;
                    case "send registratiom end reminder":
                        var users = DBHandler.Users;
                        foreach (var user in users)
                        {
                            if (user.State == States.NewUser || user.State == States.RegistrationStarted)
                            {
                                var difference = SecondStageDateTime - DateTime.Now;
                                SendMessage(user.Id, $"Напоминаем, что конец регистрации уже через {(int)difference.TotalMinutes} минут");
                            }
                        }
                        break;
                    case "exit":
                        Logger.Log("Ending execution by user command");
                        BotClient.StopReceiving();
                        return;
                    default:
                        Console.WriteLine("Send help to list available commands");
                        break;
                }
            } while (true);
        }

        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                var from = e.Message.From;
                var chat = e.Message.Chat;
                User user;
                Logger.Log(
                    $"Received a text message in chat {e.Message.Chat.Id}|@{from.Username}|{from.FirstName} {from.LastName}");
                Logger.Log(e.Message.Text);

                if (chat.Id == 484323184)
                {
                    SendMessage(chat, "пошел нахуй дима");
                    return;
                }

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

                string textToSend;
                switch (e.Message.Text)
                {
                    case "/start":
                        SendIntroMessages(chat, from);

                        await Task.Delay(TimeSpan.FromSeconds(4));

                        SendMemo(chat, from);

                        if (user.State == States.NewUser)
                        {
                            textToSend = "Чтобы начать регистрацию отправьте команду /start_registration";
                            // Logger.Log($"Sending to {from}");
                            // Logger.Log(textToSend);
                            SendMessage(chat, textToSend);
                        }
                        else if (user.State == States.RegistrationStarted)
                        {
                            AskProfileQuestion(chat, from, user);
                        }

                        break;

                    case "/send_memo":
                        SendMemo(chat, from);
                        break;

                    case "/send_my_profile":
                        SendUserProfile(chat, user, from);
                        break;

                    case "/start_registration":
                        Logger.Log($"{from} asked for starting registration");

                        if (user.State == States.RegistrationCompleted || user.State == States.TargetChosen ||
                            user.State == States.TargetSent)
                        {
                            Logger.Log("But he completed his registration already");
                            textToSend = "Вы уже завершили регистрацию, если вы хотите начать регистрацию заново, " +
                                         "то сначала отмените прежнюю отправив команду /abort_registration ";
                            // Logger.Log($"Sending to {from}");
                            // Logger.Log(textToSend);
                            SendMessage(chat, textToSend);
                        }
                        else if (user.State == States.RegistrationStarted)
                        {
                            Logger.Log("But he started his registration already");
                            textToSend = "Вы уже начали регистрацию. Чтобы отменить нынешнюю регистрацию " +
                                         "отправьте команду /abort_registration, либо же /confirm_registration " +
                                         "чтобы завершить нынешнюю";
                            // Logger.Log($"Sending to {from}");
                            // Logger.Log(textToSend);
                            SendMessage(chat, textToSend);
                        }
                        else if (user.State == States.NewUser)
                        {
                            Logger.Log("He has state NewUser. Starting registration");
                            user.State = States.RegistrationStarted;
                            user.Stage = Stages.None;
                            DBHandler.WriteCount();

                            Logger.Log("Asking user a question");
                            AskProfileQuestion(chat, from, user);
                        }

                        break;

                    case "/confirm_registration":
                        Logger.Log($"{from} asked for confirming registration");

                        if (user.State == States.RegistrationCompleted || user.State == States.TargetChosen ||
                            user.State == States.TargetSent)
                        {
                            Logger.Log("But he completed his registration already");
                            textToSend = "Вы уже завершили регистрацию. :)";
                            // Logger.Log($"Sending to {from}");
                            // Logger.Log(textToSend);
                            SendMessage(chat, textToSend);
                        }
                        else if (user.State == States.NewUser)
                        {
                            Logger.Log("But he didnt start registration");
                            textToSend = "Вы еще не начинали регистрацию, чтобы начать регистрацию " +
                                         "отправьте /start_registration";
                            // Logger.Log($"Sending to {from}");
                            // Logger.Log(textToSend);
                            SendMessage(chat, textToSend);
                        }
                        else if (user.State == States.RegistrationStarted)
                        {
                            if (user.Stage == Stages.StageBan)
                            {
                                Logger.Log($"{from} has Stage Ban so compliting his registration");
                                textToSend = "Поздравляем, Вы успешно всё заполнили и теперь остается только ждать, " +
                                             "когда бот пришлет анкету Вашей жертвы. Если Вам нужна будет помощь " +
                                             "или есть какие-то серьезные вопросы, то пишите сюда @bIudger. Для того " +
                                             "чтобы еще раз посмотреть памятку по датам отправьте команду /send_memo, " +
                                             "для того чтобы посмотреть свою анкету отправьте команду " +
                                             "/send_my_profile, чтобы изменить что-то в анкете отправьте " +
                                             "/abort_registration и заполните ее заново 👹";
                                // Logger.Log($"Sending to {from}");
                                // Logger.Log(textToSend);
                                SendMessage(chat, textToSend);

                                user.State = States.RegistrationCompleted;
                                DBHandler.WriteCount();
                                Logger.Log($"Set {from} State to RegistrationCompleted");
                            }
                            else
                            {
                                Logger.Log($"{from} has another than Ban Stage so cant complite his registration");
                                textToSend = "Вы еще не закончили регистрацию, пожалуйста заполните анкету до конца.";
                                // Logger.Log($"Sending to {from}");
                                // Logger.Log(textToSend);
                                SendMessage(chat, textToSend);

                                await Task.Delay(TimeSpan.FromSeconds(0.2));
                                AskProfileQuestion(chat, from, user);
                            }
                        }

                        break;

                    case "/abort_registration":
                        Logger.Log($"{from} asked for aborting registration");

                        if (user.State == States.TargetChosen ||
                            user.State == States.TargetSent)
                        {
                            Logger.Log("But he had recieve target so his profile cant be deleted");
                            textToSend = "Вы уже получили анкету цели и не можете удалить свою анкету. " +
                                         "Если Вам необходима помощь - обращайтесь в наш аккаунт поддержки @bIudger";
                            // Logger.Log($"Sending to {from}");
                            // Logger.Log(textToSend);
                            SendMessage(chat, textToSend);
                        }
                        else if (user.State == States.NewUser)
                        {
                            Logger.Log("But he didnt start registration");
                            textToSend = "Вы еще не начинали регистрацию, чтобы начать регистрацию " +
                                         "отправьте /start_registration";
                            // Logger.Log($"Sending to {from}");
                            // Logger.Log(textToSend);
                            SendMessage(chat, textToSend);
                        }
                        else
                        {
                            textToSend =
                                "Вы хотите отменить регистрацию. Это очистит все поля в вашей анкете, вы уверены?\n" +
                                "Для подтверждения отправьте команду /confirm_abort_registration";
                            // Logger.Log($"Sending to {from}");
                            // Logger.Log(textToSend);
                            SendMessage(chat, textToSend);
                        }

                        break;

                    case "/confirm_abort_registration":
                        Logger.Log($"{from} tried to confirm aborting registration");

                        if (user.State == States.TargetChosen ||
                            user.State == States.TargetSent)
                        {
                            Logger.Log("But he had recieve target so his profile cant be deleted");
                            textToSend = "Вы уже получили анкету цели и не можете удалить свою анкету. " +
                                         "Если Вам необходима помощь - обращайтесь в наш аккаунт поддержки @bIudger";
                            // Logger.Log($"Sending to {from}");
                            // Logger.Log(textToSend);
                            SendMessage(chat, textToSend);
                        }
                        else if (user.State == States.NewUser)
                        {
                            Logger.Log("But he didnt start registration");
                            textToSend = "Вы еще не начинали регистрацию, чтобы начать регистрацию " +
                                         "отправьте /start_registration";
                            // Logger.Log($"Sending to {from}");
                            // Logger.Log(textToSend);
                            SendMessage(chat, textToSend);
                        }
                        else
                        {
                            Logger.Log("Wiping user answers...");
                            user.OfficialName = null;
                            Logger.Log("--OfficialName");
                            user.Phone = null;
                            Logger.Log("--Phone");
                            user.Post = null;
                            Logger.Log("--Post");
                            user.FanOf = null;
                            Logger.Log("--FanOf");
                            user.Ban = null;
                            Logger.Log("--Ban");
                            Logger.Log("Done");
                            DBHandler.WriteCount();

                            user.State = States.NewUser;
                            user.Stage = Stages.None;
                            DBHandler.WriteCount();

                            Logger.Log($"Successfully wiped {from} profile");
                            textToSend = "Ваша анкета очищена и статус регистрации сброшен. " +
                                         "Чтобы начать регистрацию отправьте команду /start_registration";
                            // Logger.Log($"Sending to {from}");
                            // Logger.Log(textToSend);
                            SendMessage(chat, textToSend);

                            await Task.Delay(TimeSpan.FromSeconds(0.2));
                            SendUserProfile(chat, user, from);
                        }

                        break;

                    // TODO non-in-registration response

                    default:
                        Logger.Log($"{from} sent {e.Message.Text}");
                        if (user.State == States.RegistrationStarted)
                        {
                            Logger.Log("He is in State RegistrationStarted");
                            switch (user.Stage)
                            {
                                case Stages.None:
                                    Logger.Log($"{from} is on None stage, saving his answer to Name");
                                    user.OfficialName = e.Message.Text;
                                    Logger.Log($"{from} setting Stage to Name");
                                    user.Stage = Stages.StageOffName;
                                    DBHandler.WriteCount();
                                    AskProfileQuestion(chat, from, user);
                                    break;

                                case Stages.StageOffName:
                                    Logger.Log($"{from} is on Name stage, saving his answer to Phone");
                                    user.Phone = e.Message.Text;
                                    Logger.Log($"{from} setting Stage to Phone");
                                    user.Stage = Stages.StagePhone;
                                    DBHandler.WriteCount();
                                    AskProfileQuestion(chat, from, user);
                                    break;

                                case Stages.StagePhone:
                                    Logger.Log($"{from} is on Phone stage, saving his answer to Post");
                                    user.Post = e.Message.Text;
                                    Logger.Log($"{from} setting Stage to Post");
                                    user.Stage = Stages.StagePost;
                                    DBHandler.WriteCount();
                                    AskProfileQuestion(chat, from, user);
                                    break;

                                case Stages.StagePost:
                                    Logger.Log($"{from} is on Post stage, saving his answer to Fan");
                                    user.FanOf = e.Message.Text;
                                    Logger.Log($"{from} setting Stage to Fan");
                                    user.Stage = Stages.StageFan;
                                    DBHandler.WriteCount();
                                    AskProfileQuestion(chat, from, user);
                                    break;

                                case Stages.StageFan:
                                    Logger.Log($"{from} is on Fan stage, saving his answer to Ban");
                                    user.Ban = e.Message.Text;
                                    Logger.Log($"{from} setting Stage to Ban");
                                    user.Stage = Stages.StageBan;
                                    DBHandler.WriteCount();
                                    AskProfileQuestion(chat, from, user);
                                    break;

                                case Stages.StageBan:
                                    Logger.Log(
                                        $"{from} is on Ban stage, sending him info about registration confirmation");
                                    textToSend = "Проверьте Вашу анкету еще раз потому, что после подтверждения " +
                                                 "изменить ответы через бот невозможно:";
                                    // Logger.Log($"Sending to {from}");
                                    // Logger.Log(textToSend);
                                    SendMessage(chat, textToSend);

                                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                                    SendUserProfile(chat, user, from);

                                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                                    textToSend = "Если все хорошо, то нажмите команду /confirm_registration " +
                                                 "если же хотите что-то изменить, то нажмите команду /abort_registration " +
                                                 "и заполните заново 👹";
                                    // Logger.Log($"Sending to {from}");
                                    // Logger.Log(textToSend);
                                    SendMessage(chat, textToSend);
                                    break;
                            }
                        }

                        break;
                }
            }
        }

        // TODO send memo on deleting confirmation

        private static async void AskProfileQuestion(Chat chat, Telegram.Bot.Types.User from, User user)
        {
            string textToSend;
            Logger.Log($"Asking {from} next question");
            switch (user.Stage)
            {
                case Stages.None:
                    Logger.Log($"{from} is on None stage, asking Name question");
                    textToSend = "ФИО (полное, на рідній или русском)";
                    // Logger.Log($"Sending to {from}");
                    // Logger.Log(textToSend);
                    SendMessage(chat, textToSend);
                    break;

                case Stages.StageOffName:
                    Logger.Log($"{from} is on Name stage, asking Phone question");
                    textToSend = "Номер телефона (необходим для отправки посылки)";
                    // Logger.Log($"Sending to {from}");
                    // Logger.Log(textToSend);
                    SendMessage(chat, textToSend);
                    break;

                case Stages.StagePhone:
                    Logger.Log($"{from} is on Phone stage, asking Post question");
                    textToSend = "Город и номер отделения НП (или подробно описать другой метод доставки)";
                    // Logger.Log($"Sending to {from}");
                    // Logger.Log(textToSend);
                    SendMessage(chat, textToSend);
                    break;

                case Stages.StagePost:
                    Logger.Log($"{from} is on Post stage, asking Fan question");
                    textToSend = "Теперь пара вопросов для того чтобы можно было получше выбрать Вам подарок";
                    // Logger.Log($"Sending to {from}");
                    // Logger.Log(textToSend);
                    SendMessage(chat, textToSend);

                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                    textToSend = "Опишите фанатом чего вы являетесь (например Гарри Поттер, Initial D, " +
                                 "Гречка (ну та которая музыку поет), Райан Гослинг)";
                    // Logger.Log($"Sending to {from}");
                    // Logger.Log(textToSend);
                    SendMessage(chat, textToSend);
                    break;

                case Stages.StageFan:
                    Logger.Log($"{from} is on Fan stage, asking Ban question");
                    textToSend = "А теперь укажите что Вам лучше не дарить " +
                                 "<i>(конечно можете пропустить этот пункт (тогда напишите что-то типа " +
                                 "\"Все равно...\"), но любой Ваш отзыв обязательно обработают и учтут наши " +
                                 "сотрудники отдела качества)</i>";
                    // Logger.Log($"Sending to {from}");
                    // Logger.Log(textToSend);
                    SendMessage(chat, textToSend);
                    break;

                case Stages.StageBan:
                    Logger.Log($"{from} is on Ban stage, asking registration confirmation");
                    textToSend = "Отлично, это были все вопросы на которые необходимо было ответить! " +
                                 "Теперь проверьте Вашу анкету еще раз потому, что после подтверждения " +
                                 "изменить ответы через бот невозможно:";
                    // Logger.Log($"Sending to {from}");
                    // Logger.Log(textToSend);
                    SendMessage(chat, textToSend);

                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                    SendUserProfile(chat, user, from);

                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                    textToSend = "Если все хорошо, то нажмите команду /confirm_registration " +
                                 "если же хотите что-то изменить, то нажмите команду /abort_registration " +
                                 "и заполните заново 👹";
                    // Logger.Log($"Sending to {from}");
                    // Logger.Log(textToSend);
                    SendMessage(chat, textToSend);
                    break;
            }
        }

        static void SendMemo(ChatId chat, Telegram.Bot.Types.User @from)
        {
            string textToSend = "21.12 12:21 - закрытие регистрации\n" +
                                "22-е - получение анкет\n" +
                                "26-о вечером - отправка подарков\n" +
                                "27-о вечером (ориентировочно) - получение подарков";

            Logger.Log($"Sending memo to {from}");
            // Logger.Log("\n" + textToSend);
            SendMessage(chat, textToSend);
        }

        static void SendUserProfile(ChatId chat, User user, Telegram.Bot.Types.User @from)
        {
            string textToSend = "";
            textToSend += "Статус анкеты: ";
            switch (user.State)
            {
                case States.RegistrationCompleted:
                    textToSend += "Регистрация завершена\n";
                    break;
                case States.NewUser:
                    textToSend += "Регистрация не начата\n";
                    break;
                case States.TargetChosen:
                    textToSend += "Регистрация завершена\n";
                    break;
                case States.TargetSent:
                    textToSend += "Регистрация завершена, цель получена\n";
                    break;
                case States.RegistrationStarted:
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

            Logger.Log($"Sending profile to {from}");
            // Logger.Log("\n" + textToSend);
            SendMessage(chat, textToSend);
        }

        static async void SendIntroMessages(ChatId chat, Telegram.Bot.Types.User @from)
        {
            double sendOffsetInSecs = 0.3;
            Logger.Log($"Sending intro messages to {from}");

            string textToSend = "Приветствуем Вас в боте Секретного Санты ЖППП!";
            // Logger.Log(textToSend);
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend =
                "С помощью этого бота вы можете зарегистрироваться для участия, а также узнать всю подробную инфу" +
                " касательно Секретного Санты 2020 by Zhopki Popki©! <i>главный админ убьет меня за такое название ну не" +
                " важно давайте начинать уже там целая простынь текста дальше...</i>";
            // Logger.Log(textToSend);
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend =
                "Поскольку этот год оч прикольный такой и интересный в некоторых моментах, мы в редакции Желтой" +
                " Прессы решили, что разумнее всего будет провести данный ивент с некоторыми поправками, касающимися" +
                " доставки подарков. И ДА - подарки будут отправляться по почте :(";
            // Logger.Log(textToSend);
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "И тут есть два момента: во-первых - Вы не сможете ехидно пронаблюдать за тем как кто-то" +
                         " распаковывает, тщательно Вами запакованную в матрешку из коробок и туалетной бумаги флешку," +
                         " но на этот счет мы попробуем кое-что предпринять; а во-вторых это то, что человек," +
                         " принимающий подарок, сможет узнать от кого получил подарок (из-за накладной).";
            // Logger.Log(textToSend);
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "<i>(Кто-то по секрету сказал что можно попробовать на почте при отправке указать не Ваши" +
                         " а абсолютно рандомные данные и таким образом остаться анонимным, однако этот вариант" +
                         " стоит рассматривать только для энтузиастов и вообще обязательным не является)</i>";
            // Logger.Log(textToSend);
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Ладно, давайте перейдем к делу. Для начала Вам нужно будет заполнить небольшую анкету," +
                         " в которой указать такую информацию:\n" +
                         "ФИО\n" +
                         "Номер телефона\n" +
                         "Инфа о почтовом отделении\n" +
                         "А также некоторая информация для человека который будет дарить, дабы ни у кого " +
                         "не было проблем с выбором подарка";
            // Logger.Log(textToSend);
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Форма для регистрации будет открыта до 21.12 12:21 (поторопитесь, попингуйте-потэгайте" +
                         " всех ППшничков которые должны в этом участвовать чтобы никто не пропустил), и 22-о" +
                         " днем Вам придет анкета того кому Вы будете дарить подарок. Далее Вы готовите подарок " +
                         "и отправляете его 26-о числа (по-возможности так чтоб посылка пришла получателю 27-о числа).";
            // Logger.Log(textToSend);
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Ориентировочно 27-о числа вечером Вам приходит посылочка и Вы идете ее забираете. " +
                         "Радостные открываете свои носки с оленями и такие же радостные делаете фоточки-видосики и " +
                         "кидаете их сюда в бот, в чатик Прикладного питания или делитесь ими со всеми каким либо " +
                         "другим образом. Так каждый сможет понаблюдать за получателем подарка и все разделят " +
                         "радость праздника с другими.";
            // Logger.Log(textToSend);
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Касательно подарка - это может быть что угодно, главное чтоб было весело и " +
                         "приятно <i>и <b>от души</b></i>. Наши рекомендации (чтобы никому не было обидно) по " +
                         "поводу стоимости подарка это 150-250грн, а также не забывайте что около 50грн пойдет на " +
                         "отправку посылки. К тому же это стоит учитывать при упаковке подарка, например всякие " +
                         "банты на коробках по любому помнут на почте если не предпринять какие-то меры 😭. <i>Банты топ.</i>";
            // Logger.Log(textToSend);
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "И на этом наконец все! Совсем скоро Вы уже начнете заполнять анкету, но перед этим " +
                         "хотелось бы сказать про еще один очень важный момент. Этот бот написан каким-то криворуким " +
                         "ППшником на коленке и будет очень неудивительно если бот ляжет или не сможет исполнять " +
                         "свои функции в каком-то другом плане. В таком случае пишите на аккаунт нашей поддержки" +
                         " - @bIudger, и мы постараемся помочь Вам в ближайшее время. " +
                         "Иии да, можете уже заполнять анкету, удачи Вам с подготовкой подарка и счастливых праздников 🥳!";
            // Logger.Log(textToSend);
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "С любовью, редакция @ppidory <i>(ахвахвхахв у нас внатуре тэг канала - ПИПИДОРЫ)</i>";
            // Logger.Log(textToSend);
            SendMessage(chat, textToSend);
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