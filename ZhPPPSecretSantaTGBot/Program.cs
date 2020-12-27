using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication.ExtendedProtection;
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
    internal static class Program
    {
        private static ITelegramBotClient BotClient;
        private static Logger Logger;
        private static DBHandler DBHandler;

        private static readonly DateTime SecondStageDateTime = new DateTime(2020, 12, 21, 12, 21, 00);
        private static bool IsInSecondStage;
        private static readonly ChatId ownerChatId = 265814543;
        private static List<Telegram.Bot.Types.User> MesWaitList;

        private static void Main()
        {
            Logger = new Logger();
            DBHandler = new DBHandler(Logger);

            if (File.Exists("config\\access token.txt"))
            {
                var sr = new StreamReader("config\\access token.txt");
                var accessToken = sr.ReadLine();
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

            CheckBotStage();

            BotClient.OnMessage += Bot_OnMessage;
            BotClient.StartReceiving();

            do
            {
                Console.WriteLine("Send help to list available commands");
                var command = Console.ReadLine();
                User[] users;
                switch (command)
                {
                    case "?":
                    case "help":
                        Console.WriteLine("help (?) - list available commands\n" +
                                          "send registratiom end reminder (srr) - self explanatory\n" +
                                          "check stage (cs) - check is bot in second stage already + setting flag and appropriate command set\n" +
                                          "send target profiles (stp) - send to all users their target profiles\n" +
                                          "exit - end program");
                        break;
                    case "srr":
                    case "send registratiom end reminder":
                        Console.WriteLine("You want to send everyone REMINDERS. Send YES if you are sure:");
                        command = Console.ReadLine();

                        if (command == "YES")
                        {
                            Logger.Log("Sendings reminders to not registered users...");
                            users = DBHandler.Users;
                            foreach (var user in users)
                            {
                                if (user.State == States.NewUser || user.State == States.RegistrationStarted)
                                {
                                    var difference = SecondStageDateTime - DateTime.Now;
                                    SendMessage(user.Id,
                                        $"Напоминаем, что конец регистрации уже через {(int) difference.TotalMinutes} минут");
                                    Logger.Log(
                                        $"{user.Username ?? $"{user.FirstName} {user.LastName}"}  Sent reminder");
                                }
                            }

                            Logger.Log("Done");
                        }

                        break;
                    case "cs":
                    case "check stage":
                        CheckBotStage();
                        break;
                    case "stp":
                    case "send target profiles":
                        Console.WriteLine("You want to send everyone their TARGET PROFILES. Send YES if you are sure:");
                        command = Console.ReadLine();

                        if (command == "YES")
                        {
                            Logger.Log("Sendings target profiles to users...");
                            users = DBHandler.Users;
                            foreach (var user in users)
                            {
                                switch (user.State)
                                {
                                    case States.TargetChosen:
                                        SendMessage(user.Id,
                                            "Эй йоу, мы наконец-то определили цель для тебя! " +
                                            "Вот, держи его(ее) анкету и быстрее шуруй думать подарок. " +
                                            "Всем удачи в этом нелегком деле!");
                                        SendTargetProfile(user.Id, user, DBHandler.GetUserById(user.TargetId));
                                        DBHandler.GetUserById(user.Id).State = States.TargetSent;
                                        DBHandler.WriteCount();
                                        break;
                                    case States.TargetSent:
                                        SendTargetProfile(user.Id, user, DBHandler.GetUserById(user.TargetId));
                                        break;
                                }
                            }

                            Logger.Log("Done");
                        }

                        break;
                    case "exit":
                        Logger.Log("Ending execution by user command");
                        BotClient.StopReceiving();
                        return;
                }
            } while (true);
        }

        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var user = e.Message.From;
            var chat = e.Message.Chat;
            User localUser;

            if (chat.Id == 484323184)
            {
                SendMessage(chat, "пошел нахуй дима");
                return;
            }

            if (DBHandler.ContainsUser(user.Id))
            {
                localUser = DBHandler.GetUserById(user.Id);
            }
            else
            {
                localUser = new User(user.Id, user.Username, user.FirstName, user.LastName);
                localUser = DBHandler.AddNewUser(localUser);
                Logger.Log($"Created new user {user.Username}");
            }

            switch (e.Message.Type)
            {
                case MessageType.Photo:
                    Logger.Log($"{user} sent a photo message");
                    Logger.Log($"Forwarding to bot owner, id - {ownerChatId}");
                    await BotClient.ForwardMessageAsync(ownerChatId, chat, e.Message.MessageId);
                    SendMessage(chat, "Принято ;)");
                    break;
                case MessageType.Video:
                    Logger.Log($"{user} sent a video message");
                    Logger.Log($"Forwarding to bot owner, id - {ownerChatId}");
                    await BotClient.ForwardMessageAsync(ownerChatId, chat, e.Message.MessageId);
                    SendMessage(chat, "Принято ;)");
                    break;
                case MessageType.Voice:
                    Logger.Log($"{user} sent a voice message");
                    Logger.Log($"Forwarding to bot owner, id - {ownerChatId}");
                    await BotClient.ForwardMessageAsync(ownerChatId, chat, e.Message.MessageId);
                    SendMessage(chat, "Принято ;)");
                    break;
                case MessageType.VideoNote:
                    Logger.Log($"{user} sent a video note message");
                    Logger.Log($"Forwarding to bot owner, id - {ownerChatId}");
                    await BotClient.ForwardMessageAsync(ownerChatId, chat, e.Message.MessageId);
                    SendMessage(chat, "Принято ;)");
                    break;

                case MessageType.Text:

                    Logger.Log(
                        $"{user} sent a text message");
                    Logger.Log(e.Message.Text);

                    string textToSend;

                    if (MesWaitList != null)
                    {
                        if (MesWaitList.Contains(user))
                        {
                            Logger.Log($"{user} listed in message wait list");
                            if (e.Message.Text == "/cancel")
                            {
                                Logger.Log($"{user} sent /cancel so aborting operation");
                                MesWaitList.Remove(user);
                                textToSend = "Отправка сообщения Вашему Тайному Санте отменена";
                                SendMessage(chat, textToSend);
                            }
                            else
                            {
                                var santaId = DBHandler.findSantaId(user);
                                if (santaId == 0)
                                {
                                    Logger.Log("Error: santa ID didnt found");
                                    return;
                                }

                                Logger.Log($"{user} Santa ID found - {santaId}, forwarding message to him");
                                SendMessage(santaId, "Вам сообщение от Вашей цели:");
                                await BotClient.ForwardMessageAsync(santaId, chat, e.Message.MessageId);

                                textToSend = "Ваше сообщение успешно переслано!";
                                SendMessage(chat, textToSend);
                                MesWaitList.Remove(user);
                            }
                        }
                    }

                    switch (e.Message.Text)
                    {
                        case "/start":
                            SendIntroMessages(chat, user);

                            await Task.Delay(TimeSpan.FromSeconds(4));

                            SendMemo(chat, user);

                            if (IsInSecondStage)
                            {
                                textToSend = "К сожалению регистрация уже закрыта";
                                SendMessage(chat, textToSend);
                            }
                            else
                                switch (localUser.State)
                                {
                                    case States.NewUser:
                                        textToSend = "Чтобы начать регистрацию отправьте команду /start_registration";
                                        SendMessage(chat, textToSend);
                                        break;
                                    case States.RegistrationStarted:
                                        AskProfileQuestion(chat, user, localUser);
                                        break;
                                }

                            break;

                        case "/send_memo":
                            SendMemo(chat, user);
                            break;

                        case "/send_my_profile":
                            SendUserProfile(chat, localUser, user);
                            break;

                        case "/send_target_profile":
                            Logger.Log($"{user} asked for target profile");
                            if (IsInSecondStage)
                            {
                                if (localUser.State == States.TargetChosen || localUser.State == States.TargetSent)
                                {
                                    SendTargetProfile(chat, localUser, DBHandler.GetUserById(localUser.TargetId));
                                }
                                else
                                {
                                    Logger.Log(
                                        $"{user} asked for target profile but he has no target, sending info message");
                                    SendMessage(chat, "Извините, Вам не назначена цель, если Вам нужна помощь - " +
                                                      "пишите в наш аккаунт поддержки @bIudger");
                                }
                            }
                            else
                            {
                                Logger.Log("But bot isnt in second stage, ignoring");
                            }

                            break;

                        case "/start_registration":
                            Logger.Log($"{user} asked for starting registration");

                            if (IsInSecondStage)
                            {
                                Logger.Log($"{user} But bot is in second stage, sending refuse message");
                                textToSend = "Извините, регистрация уже закончилась";
                                SendMessage(chat, textToSend);
                            }
                            else
                            {
                                if (localUser.State == States.RegistrationCompleted ||
                                    localUser.State == States.TargetChosen ||
                                    localUser.State == States.TargetSent)
                                {
                                    Logger.Log($"{user} But he completed his registration already");
                                    textToSend =
                                        "Вы уже завершили регистрацию, если вы хотите начать регистрацию заново, " +
                                        "то сначала отмените прежнюю отправив команду /abort_registration ";
                                    SendMessage(chat, textToSend);
                                }
                                else if (localUser.State == States.RegistrationStarted)
                                {
                                    Logger.Log($"{user} But he started his registration already");
                                    textToSend = "Вы уже начали регистрацию. Чтобы отменить нынешнюю регистрацию " +
                                                 "отправьте команду /abort_registration, либо же /confirm_registration " +
                                                 "чтобы завершить нынешнюю";
                                    SendMessage(chat, textToSend);
                                }
                                else if (localUser.State == States.NewUser)
                                {
                                    Logger.Log($"{user} has state NewUser. Starting registration");
                                    localUser.State = States.RegistrationStarted;
                                    localUser.Stage = Stages.None;
                                    DBHandler.WriteCount();

                                    Logger.Log($"{user}Asking a question");
                                    AskProfileQuestion(chat, user, localUser);
                                }
                            }

                            break;

                        case "/confirm_registration":
                            Logger.Log($"{user} asked for confirming registration");

                            if (IsInSecondStage)
                            {
                                Logger.Log($"But bot is in second stage, sending refuse message");
                                textToSend = "Извините, регистрация уже закончилась";
                                SendMessage(chat, textToSend);
                            }
                            else
                            {
                                if (localUser.State == States.RegistrationCompleted ||
                                    localUser.State == States.TargetChosen ||
                                    localUser.State == States.TargetSent)
                                {
                                    Logger.Log("But he completed his registration already");
                                    textToSend = "Вы уже завершили регистрацию. :)";
                                    SendMessage(chat, textToSend);
                                }
                                else if (localUser.State == States.NewUser)
                                {
                                    Logger.Log("But he didnt start registration");
                                    textToSend = "Вы еще не начинали регистрацию, чтобы начать регистрацию " +
                                                 "отправьте /start_registration";
                                    SendMessage(chat, textToSend);
                                }
                                else if (localUser.State == States.RegistrationStarted)
                                {
                                    if (localUser.Stage == Stages.StageBan)
                                    {
                                        Logger.Log($"{user} has Stage Ban so compliting his registration");
                                        textToSend =
                                            "Поздравляем, Вы успешно всё заполнили и теперь остается только ждать, " +
                                            "когда бот пришлет анкету Вашей жертвы. Если Вам нужна будет помощь " +
                                            "или есть какие-то серьезные вопросы, то пишите сюда @bIudger. Для того " +
                                            "чтобы еще раз посмотреть памятку по датам отправьте команду /send_memo, " +
                                            "для того чтобы посмотреть свою анкету отправьте команду " +
                                            "/send_my_profile, чтобы изменить что-то в анкете отправьте " +
                                            "/abort_registration и заполните ее заново 👹";
                                        SendMessage(chat, textToSend);

                                        localUser.State = States.RegistrationCompleted;
                                        DBHandler.WriteCount();
                                        Logger.Log($"{user} Set State to RegistrationCompleted");
                                    }
                                    else
                                    {
                                        Logger.Log(
                                            $"{user} has another than Ban Stage so cant complite his registration");
                                        textToSend =
                                            "Вы еще не закончили регистрацию, пожалуйста заполните анкету до конца.";
                                        SendMessage(chat, textToSend);

                                        await Task.Delay(TimeSpan.FromSeconds(0.2));
                                        AskProfileQuestion(chat, user, localUser);
                                    }
                                }
                            }

                            break;

                        case "/abort_registration":
                            Logger.Log($"{user} asked for aborting registration");

                            if (IsInSecondStage)
                            {
                                Logger.Log($"But bot is in second stage, sending refuse message");
                                textToSend = "Извините, регистрация уже закончилась";
                                SendMessage(chat, textToSend);
                            }
                            else
                            {
                                if (localUser.State == States.TargetChosen ||
                                    localUser.State == States.TargetSent)
                                {
                                    Logger.Log("But he had recieve target so his profile cant be deleted");
                                    textToSend = "Вы уже получили анкету цели и не можете удалить свою анкету. " +
                                                 "Если Вам необходима помощь - обращайтесь в наш аккаунт поддержки @bIudger";
                                    SendMessage(chat, textToSend);
                                }
                                else if (localUser.State == States.NewUser)
                                {
                                    Logger.Log("But he didnt start registration");
                                    textToSend = "Вы еще не начинали регистрацию, чтобы начать регистрацию " +
                                                 "отправьте /start_registration";
                                    SendMessage(chat, textToSend);
                                }
                                else
                                {
                                    textToSend =
                                        "Вы хотите отменить регистрацию. Это очистит все поля в вашей анкете, вы уверены?\n" +
                                        "Для подтверждения отправьте команду /confirm_abort_registration";
                                    SendMessage(chat, textToSend);
                                }
                            }

                            break;

                        case "/confirm_abort_registration":
                            Logger.Log($"{user} tried to confirm aborting registration");

                            if (IsInSecondStage)
                            {
                                Logger.Log($"But bot is in second stage, sending refuse message");
                                textToSend = "Извините, регистрация уже закончилась";
                                SendMessage(chat, textToSend);
                            }
                            else
                            {
                                if (localUser.State == States.TargetChosen ||
                                    localUser.State == States.TargetSent)
                                {
                                    Logger.Log("But he had recieve target so his profile cant be deleted");
                                    textToSend = "Вы уже получили анкету цели и не можете удалить свою анкету. " +
                                                 "Если Вам необходима помощь - обращайтесь в наш аккаунт поддержки @bIudger";
                                    SendMessage(chat, textToSend);
                                }
                                else if (localUser.State == States.NewUser)
                                {
                                    Logger.Log("But he didnt start registration");
                                    textToSend = "Вы еще не начинали регистрацию, чтобы начать регистрацию " +
                                                 "отправьте /start_registration";
                                    SendMessage(chat, textToSend);
                                }
                                else
                                {
                                    Logger.Log("Wiping user answers...");
                                    localUser.OfficialName = null;
                                    Logger.Log("--OfficialName");
                                    localUser.Phone = null;
                                    Logger.Log("--Phone");
                                    localUser.Post = null;
                                    Logger.Log("--Post");
                                    localUser.FanOf = null;
                                    Logger.Log("--FanOf");
                                    localUser.Ban = null;
                                    Logger.Log("--Ban");
                                    Logger.Log("Done");
                                    DBHandler.WriteCount();

                                    localUser.State = States.NewUser;
                                    localUser.Stage = Stages.None;
                                    DBHandler.WriteCount();

                                    Logger.Log($"{user} Successfully wiped profile");
                                    textToSend = "Ваша анкета очищена и статус регистрации сброшен. " +
                                                 "Чтобы начать регистрацию отправьте команду /start_registration";
                                    SendMessage(chat, textToSend);

                                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                                    SendUserProfile(chat, localUser, user);
                                }
                            }

                            break;

                        case "/send_message_to_santa":
                            Logger.Log($"{user} askes to send message to santa");

                            if (!IsInSecondStage)
                            {
                                Logger.Log($"{user} But bot isnt in second stage, sending refuse message");
                                textToSend = "Извините, регистрация еще не закончилась";
                                SendMessage(chat, textToSend);
                            }
                            else
                            {
                                if (localUser.State != States.TargetSent)
                                {
                                    Logger.Log($"{user} But he had not recieve target, sending refuse message");
                                    textToSend = "Извините, вы не учавствуете в тайном санте. " +
                                                 "Если Вам необходима помощь - обращайтесь в наш аккаунт поддержки @bIudger";
                                    SendMessage(chat, textToSend);
                                }
                                else
                                {
                                    Logger.Log($"{user} Asking for message for santa");
                                    textToSend = "Следующее Ваше сообщение (только первое) будет отправлено " +
                                                 "Вашему Тайному Санте. Чтобы отменить это отправьте /cancel.";
                                    SendMessage(chat, textToSend);

                                    Logger.Log($"{user} Adding user to MesWaitList");
                                    if (MesWaitList == null)
                                    {
                                        MesWaitList = new List<Telegram.Bot.Types.User> {user};
                                    }
                                    else
                                    {
                                        MesWaitList.Add(user);
                                    }
                                }
                            }

                            break;

                        // TODO non-in-registration response

                        default:
                            if (localUser.State == States.RegistrationStarted)
                            {
                                Logger.Log($"{user} is in State RegistrationStarted");
                                if (IsInSecondStage)
                                {
                                    Logger.Log($"{user} But bot is in second stage, ignoring");
                                }
                                else
                                {
                                    switch (localUser.Stage)
                                    {
                                        case Stages.None:
                                            Logger.Log($"{user} is on None stage, saving his answer to Name");
                                            localUser.OfficialName = e.Message.Text;
                                            Logger.Log($"{user} setting Stage to Name");
                                            localUser.Stage = Stages.StageOffName;
                                            DBHandler.WriteCount();
                                            AskProfileQuestion(chat, user, localUser);
                                            break;

                                        case Stages.StageOffName:
                                            Logger.Log($"{user} is on Name stage, saving his answer to Phone");
                                            localUser.Phone = e.Message.Text;
                                            Logger.Log($"{user} setting Stage to Phone");
                                            localUser.Stage = Stages.StagePhone;
                                            DBHandler.WriteCount();
                                            AskProfileQuestion(chat, user, localUser);
                                            break;

                                        case Stages.StagePhone:
                                            Logger.Log($"{user} is on Phone stage, saving his answer to Post");
                                            localUser.Post = e.Message.Text;
                                            Logger.Log($"{user} setting Stage to Post");
                                            localUser.Stage = Stages.StagePost;
                                            DBHandler.WriteCount();
                                            AskProfileQuestion(chat, user, localUser);
                                            break;

                                        case Stages.StagePost:
                                            Logger.Log($"{user} is on Post stage, saving his answer to Fan");
                                            localUser.FanOf = e.Message.Text;
                                            Logger.Log($"{user} setting Stage to Fan");
                                            localUser.Stage = Stages.StageFan;
                                            DBHandler.WriteCount();
                                            AskProfileQuestion(chat, user, localUser);
                                            break;

                                        case Stages.StageFan:
                                            Logger.Log($"{user} is on Fan stage, saving his answer to Ban");
                                            localUser.Ban = e.Message.Text;
                                            Logger.Log($"{user} setting Stage to Ban");
                                            localUser.Stage = Stages.StageBan;
                                            DBHandler.WriteCount();
                                            AskProfileQuestion(chat, user, localUser);
                                            break;

                                        case Stages.StageBan:
                                            Logger.Log(
                                                $"{user} is on Ban stage, sending him info about registration confirmation");
                                            textToSend =
                                                "Проверьте Вашу анкету еще раз потому, что после подтверждения " +
                                                "изменить ответы через бот невозможно:";
                                            SendMessage(chat, textToSend);

                                            await Task.Delay(TimeSpan.FromSeconds(0.2));
                                            SendUserProfile(chat, localUser, user);

                                            await Task.Delay(TimeSpan.FromSeconds(0.2));
                                            textToSend = "Если все хорошо, то нажмите команду /confirm_registration " +
                                                         "если же хотите что-то изменить, то нажмите команду /abort_registration " +
                                                         "и заполните заново 👹";
                                            SendMessage(chat, textToSend);
                                            break;
                                    }
                                }
                            }

                            break;
                    }

                    break;
                default:
                    Logger.Log($"{user} sent a {e.Message.Type} message");
                    Logger.Log($"Unresolved type, sending warning message");
                    SendMessage(chat, "Извините, бот не принимает таких сообщений, " +
                                      "если Вам необходима помощь - обращайтесь в наш аккаунт поддержки @bIudger ");
                    break;
            }
        }

        private static void CheckBotStage()
        {
            Logger.Log("Checking bot stage...");
            if (DateTime.Now > SecondStageDateTime)
            {
                IsInSecondStage = true;
                Logger.Log("Bot are in second stage");
            }
            else
            {
                IsInSecondStage = false;
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
                    new BotCommand {Command = "send_target_profile", Description = "посмотреть анкету цели"},
                    new BotCommand {Command = "send_message_to_santa", Description = "отправить сообщение своему Санте"}
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
        }

        // TODO send memo on deleting confirmation

        private static async void AskProfileQuestion(Chat chat, Telegram.Bot.Types.User user, User localUser)
        {
            string textToSend;
            Logger.Log($"{user} Asking next question");
            switch (localUser.Stage)
            {
                case Stages.None:
                    Logger.Log($"{user} is on None stage, asking Name question");
                    textToSend = "ФИО (полное, на рідній или русском)";
                    SendMessage(chat, textToSend);
                    break;

                case Stages.StageOffName:
                    Logger.Log($"{user} is on Name stage, asking Phone question");
                    textToSend = "Номер телефона (необходим для отправки посылки)";
                    SendMessage(chat, textToSend);
                    break;

                case Stages.StagePhone:
                    Logger.Log($"{user} is on Phone stage, asking Post question");
                    textToSend = "Город и номер отделения НП (или подробно описать другой метод доставки)";
                    SendMessage(chat, textToSend);
                    break;

                case Stages.StagePost:
                    Logger.Log($"{user} is on Post stage, asking Fan question");
                    textToSend = "Теперь пара вопросов для того чтобы можно было получше выбрать Вам подарок";
                    SendMessage(chat, textToSend);

                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                    textToSend = "Опишите фанатом чего вы являетесь (например Гарри Поттер, Initial D, " +
                                 "Гречка (ну та которая музыку поет), Райан Гослинг)";
                    SendMessage(chat, textToSend);
                    break;

                case Stages.StageFan:
                    Logger.Log($"{user} is on Fan stage, asking Ban question");
                    textToSend = "А теперь укажите что Вам лучше не дарить " +
                                 "<i>(конечно можете пропустить этот пункт (тогда напишите что-то типа " +
                                 "\"Все равно...\"), но любой Ваш отзыв обязательно обработают и учтут наши " +
                                 "сотрудники отдела качества)</i>";
                    SendMessage(chat, textToSend);
                    break;

                case Stages.StageBan:
                    Logger.Log($"{user} is on Ban stage, asking registration confirmation");
                    textToSend = "Отлично, это были все вопросы на которые необходимо было ответить! " +
                                 "Теперь проверьте Вашу анкету еще раз потому, что после подтверждения " +
                                 "изменить ответы через бот невозможно:";
                    SendMessage(chat, textToSend);

                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                    SendUserProfile(chat, localUser, user);

                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                    textToSend = "Если все хорошо, то нажмите команду /confirm_registration " +
                                 "если же хотите что-то изменить, то нажмите команду /abort_registration " +
                                 "и заполните заново 👹";
                    SendMessage(chat, textToSend);
                    break;
            }
        }

        private static void SendMemo(ChatId chat, Telegram.Bot.Types.User user)
        {
            const string textToSend = "21.12 12:21 - закрытие регистрации\n" +
                                      "22-е - получение анкет\n" +
                                      "26-о вечером - отправка подарков\n" +
                                      "27-о вечером (ориентировочно) - получение подарков";

            Logger.Log($"{user} Sending memo");
            SendMessage(chat, textToSend);
        }

        private static void SendUserProfile(ChatId chat, User localUser, Telegram.Bot.Types.User user)
        {
            string textToSend = "";
            textToSend += "Статус анкеты: ";
            switch (localUser.State)
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
            textToSend += localUser.OfficialName + "\n";
            textToSend += "Номер телефона: ";
            textToSend += localUser.Phone + "\n";
            textToSend += "Город и номер отделения НП: ";
            textToSend += localUser.Post + "\n";
            textToSend += "Я фанат: ";
            textToSend += localUser.FanOf + "\n";
            textToSend += "Мне не стоит дарить: ";
            textToSend += localUser.Ban + "\n";

            Logger.Log($"{user} Sending profile");
            SendMessage(chat, textToSend);
        }

        private static void SendTargetProfile(ChatId chat, User localUser, User targetUser)
        {
            string textToSend = "";

            textToSend += $"@{targetUser.Username ?? $"{targetUser.FirstName} {targetUser.LastName}"}\n";

            textToSend += "ФИО: ";
            textToSend += targetUser.OfficialName + "\n";
            textToSend += "Номер телефона: ";
            textToSend += targetUser.Phone + "\n";
            textToSend += "Город и номер отделения НП: ";
            textToSend += targetUser.Post + "\n";
            textToSend += "Я фанат: ";
            textToSend += targetUser.FanOf + "\n";
            textToSend += "Мне не стоит дарить: ";
            textToSend += targetUser.Ban + "\n";

            Logger.Log(
                $"@{localUser.Username ?? $"{localUser.FirstName} {localUser.LastName}"} Sending target (@{targetUser.Username ?? $"{targetUser.FirstName} {targetUser.LastName}"}) profile");
            SendMessage(chat, textToSend);
            DBHandler.WriteCount();
        }

        private static async void SendIntroMessages(ChatId chat, Telegram.Bot.Types.User user)
        {
            const double sendOffsetInSecs = 0.3;
            Logger.Log($"{user} Sending intro messages");

            var textToSend = "Приветствуем Вас в боте Секретного Санты ЖППП!";
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend =
                "С помощью этого бота вы можете зарегистрироваться для участия, а также узнать всю подробную инфу" +
                " касательно Секретного Санты 2020 by Zhopki Popki©! <i>главный админ убьет меня за такое название ну не" +
                " важно давайте начинать уже там целая простынь текста дальше...</i>";
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend =
                "Поскольку этот год оч прикольный такой и интересный в некоторых моментах, мы в редакции Желтой" +
                " Прессы решили, что разумнее всего будет провести данный ивент с некоторыми поправками, касающимися" +
                " доставки подарков. И ДА - подарки будут отправляться по почте :(";
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "И тут есть два момента: во-первых - Вы не сможете ехидно пронаблюдать за тем как кто-то" +
                         " распаковывает, тщательно Вами запакованную в матрешку из коробок и туалетной бумаги флешку," +
                         " но на этот счет мы попробуем кое-что предпринять; а во-вторых это то, что человек," +
                         " принимающий подарок, сможет узнать от кого получил подарок (из-за накладной).";
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "<i>(Кто-то по секрету сказал что можно попробовать на почте при отправке указать не Ваши" +
                         " а абсолютно рандомные данные и таким образом остаться анонимным, однако этот вариант" +
                         " стоит рассматривать только для энтузиастов и вообще обязательным не является)</i>";
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Ладно, давайте перейдем к делу. Для начала Вам нужно будет заполнить небольшую анкету," +
                         " в которой указать такую информацию:\n" +
                         "ФИО\n" +
                         "Номер телефона\n" +
                         "Инфа о почтовом отделении\n" +
                         "А также некоторая информация для человека который будет дарить, дабы ни у кого " +
                         "не было проблем с выбором подарка";
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Форма для регистрации будет открыта до 21.12 12:21 (поторопитесь, попингуйте-потэгайте" +
                         " всех ППшничков которые должны в этом участвовать чтобы никто не пропустил), и 22-о" +
                         " днем Вам придет анкета того кому Вы будете дарить подарок. Далее Вы готовите подарок " +
                         "и отправляете его 26-о числа (по-возможности так чтоб посылка пришла получателю 27-о числа).";
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Ориентировочно 27-о числа вечером Вам приходит посылочка и Вы идете ее забираете. " +
                         "Радостные открываете свои носки с оленями и такие же радостные делаете фоточки-видосики и " +
                         "кидаете их сюда в бот, в чатик Прикладного питания или делитесь ими со всеми каким либо " +
                         "другим образом. Так каждый сможет понаблюдать за получателем подарка и все разделят " +
                         "радость праздника с другими.";
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "Касательно подарка - это может быть что угодно, главное чтоб было весело и " +
                         "приятно <i>и <b>от души</b></i>. Наши рекомендации (чтобы никому не было обидно) по " +
                         "поводу стоимости подарка это 150-250грн, а также не забывайте что около 50грн пойдет на " +
                         "отправку посылки. К тому же это стоит учитывать при упаковке подарка, например всякие " +
                         "банты на коробках по любому помнут на почте если не предпринять какие-то меры 😭. <i>Банты топ.</i>";
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "И на этом наконец все! Совсем скоро Вы уже начнете заполнять анкету, но перед этим " +
                         "хотелось бы сказать про еще один очень важный момент. Этот бот написан каким-то криворуким " +
                         "ППшником на коленке и будет очень неудивительно если бот ляжет или не сможет исполнять " +
                         "свои функции в каком-то другом плане. В таком случае пишите на аккаунт нашей поддержки" +
                         " - @bIudger, и мы постараемся помочь Вам в ближайшее время. " +
                         "Иии да, можете уже заполнять анкету, удачи Вам с подготовкой подарка и счастливых праздников 🥳!";
            SendMessage(chat, textToSend);
            await Task.Delay(TimeSpan.FromSeconds(sendOffsetInSecs));

            textToSend = "С любовью, редакция @ppidory <i>(ахвахвхахв у нас внатуре тэг канала - ПИПИДОРЫ)</i>";
            SendMessage(chat, textToSend);
        }

        private static async void SendMessage(ChatId chat, string message)
        {
            try
            {
                await BotClient.SendTextMessageAsync(
                    chatId: chat,
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