using System;
using System.Data;
using System.Linq;
using ZhPPPSecretSantaTGBot;

namespace ZhPPPSecretSantaSortingHatTool
{
    class Program
    {
        private static Logger Logger;
        private static DBHandler DBHandler;

        private static bool CanCommit = false;

        static void Main()
        {
            Logger = new Logger();
            DBHandler = new DBHandler(Logger);

            var size = DBHandler.Users.Count(user => user.State == States.RegistrationCompleted);
            var users = GetUsers(size);
            var targets = new User[size];

            Console.Clear();
            PrintRelationship(size, users, targets);

            string command;
            do
            {
                command = Console.ReadLine();

                try
                {
                    if (command == "commit")
                    {
                        foreach (var user in users)
                        {
                            CanCommit = true;
                            if (!targets.Contains(user))
                            {
                                CanCommit = false;
                                break;
                            }
                        }
                        if (CanCommit)
                        {
                            continue;
                        }

                        throw new Exception();
                    }
                    
                    var chosen = int.Parse(command ?? throw new Exception());
                    if (chosen > size || chosen < 1)
                    {
                        throw new Exception();
                    }

                    chosen--;

                    var validTargets = GetValidTargets(users, targets, chosen, users[chosen]);
                    CanCommit = validTargets.Count(target => target != null) == 0;

                    Console.Clear();
                    PrintTargets(validTargets);

                    var canEscape = false;
                    do
                    {
                        command = Console.ReadLine();
                        try
                        {
                            var chosenTarget = int.Parse(command ?? throw new Exception());
                            if (chosenTarget > validTargets.Length || chosenTarget < 0)
                            {
                                throw new Exception();
                            }

                            if (chosenTarget == 0)
                            {
                                targets[chosen] = null;
                                canEscape = true;
                            }
                            else
                            {
                                chosenTarget--;

                                targets[chosen] = validTargets[chosenTarget];
                                canEscape = true;
                            }

                            Console.Clear();
                            PrintRelationship(size, users, targets);
                        }
                        catch (Exception e)
                        {
                            Console.Clear();
                            Console.WriteLine(e);
                            PrintTargets(validTargets);
                        }
                    } while (!canEscape);
                }
                catch (Exception e)
                {
                    Console.Clear();
                    Console.WriteLine(e);
                    PrintRelationship(size, users, targets);
                }
            } while (command != "commit" || !CanCommit);

            for (int i = 0; i < users.Length; i++)
            {
                users[i].TargetId = targets[i].Id;
                users[i].State = States.TargetChosen;
                Logger.Log(
                    $"{users[i].Username ?? $"{users[i].FirstName} {users[i].LastName}"} Set target {targets[i].Username ?? $"{targets[i].FirstName} {targets[i].LastName}"}");
            }
        }

        private static User[] GetValidTargets(User[] users, User[] targets, int chosen, User self)
        {
            var size = users.Length - targets.Count(target => target != null);
            if (!targets.Contains(self))
            {
                size--;
            }

            var validTargets = new User[size];
            var user = users[chosen];
            int j = 0;
            for (int i = 0; i < users.Length; i++)
            {
                if (users[i] != user)
                {
                    if (!targets.Contains(users[i]))
                    {
                        validTargets[j] = users[i];
                        j++;
                    }
                }
            }

            return validTargets;
        }

        private static void PrintTargets(User[] validTargets)
        {
            var lines = new string[validTargets.Length + 1];
            int maxLength1 = 0;
            for (int i = 0; i < lines.Length - 1; i++)
            {
                var target = validTargets[i];
                lines[i] = $"{i + 1}.";
                if (i + 1 < 10)
                {
                    lines[i] += "  ";
                }
                else
                {
                    lines[i] += " ";
                }

                if (target.Username == null)
                {
                    lines[i] += $"{target.FirstName} {target.LastName}";
                }
                else
                {
                    lines[i] += $"@{target.Username}";
                }

                if (lines[i].Length > maxLength1)
                {
                    maxLength1 = lines[i].Length;
                }
            }

            var maxLength2 = 0;
            for (int i = 0; i < lines.Length - 1; i++)
            {
                var target = validTargets[i];

                while (maxLength1 - lines[i].Length > 0)
                {
                    lines[i] += " ";
                }

                lines[i] += $" - {target.OfficialName}";

                if (maxLength2 < lines[i].Length)
                {
                    maxLength2 = lines[i].Length;
                }
            }

            lines[^1] = "0.  None";

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
        }

        private static void PrintRelationship(int size, User[] users, User[] targets)
        {
            var lines = new string[size];
            int maxLength1 = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                var user = users[i];
                lines[i] = $"{i + 1}.";
                if (i + 1 < 10)
                {
                    lines[i] += "  ";
                }
                else
                {
                    lines[i] += " ";
                }

                if (user.Username == null)
                {
                    lines[i] += $"{user.FirstName} {user.LastName}";
                }
                else
                {
                    lines[i] += $"@{user.Username}";
                }

                if (lines[i].Length > maxLength1)
                {
                    maxLength1 = lines[i].Length;
                }
            }

            var maxLength2 = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                var user = users[i];

                while (maxLength1 - lines[i].Length > 0)
                {
                    lines[i] += " ";
                }

                lines[i] += $" - {user.OfficialName}";

                if (maxLength2 < lines[i].Length)
                {
                    maxLength2 = lines[i].Length;
                }
            }

            var maxLength3 = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                while (maxLength2 - lines[i].Length > 0)
                {
                    lines[i] += " ";
                }

                lines[i] += " -> ";

                if (targets[i] != null)
                {
                    var target = targets[i];

                    if (target.Username == null)
                    {
                        lines[i] += $"{target.FirstName} {target.LastName}";
                    }
                    else
                    {
                        lines[i] += $"@{target.Username}";
                    }

                    if (maxLength3 < lines[i].Length)
                    {
                        maxLength3 = lines[i].Length;
                    }
                }
            }

            var maxLength4 = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (targets[i] != null)
                {
                    var target = targets[i];

                    while (maxLength3 - lines[i].Length > 0)
                    {
                        lines[i] += " ";
                    }

                    lines[i] += $" - {target.OfficialName}";

                    if (maxLength4 < lines[i].Length)
                    {
                        maxLength4 = lines[i].Length;
                    }
                }
            }

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
        }

        private static User[] GetUsers(int size)
        {
            var users = new User[size];

            int i = 0;
            foreach (var user in DBHandler.Users)
            {
                if (user.State == States.RegistrationCompleted)
                {
                    users[i] = user;
                    i++;
                }
            }

            return users;
        }
    }
}