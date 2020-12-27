using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

// ReSharper disable InconsistentNaming

namespace ZhPPPSecretSantaTGBot
{
    public class DBHandler
    {
        private readonly Logger Logger;
        private readonly XmlSerializer Serializer = new XmlSerializer(typeof(User[]));
        private FileStream fs;

        private int CurrentFileVersion;
        private int WriteCounter;
        private const int WriteThreshold = 5;
        private const int WriteCountDeltaSec = 120;
        private bool HasChanges;

        private bool AppClosing;

        public User[] Users;

        public DBHandler(Logger logger)
        {
            Logger = logger;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            if (!Directory.Exists("data"))
            {
                Directory.CreateDirectory("data");
                Logger.Log("Created data folder");
            }

            FileInfo[] filesInfoList = new DirectoryInfo("data").GetFiles().OrderBy(p => p.CreationTime).ToArray();
            List<string> filesList = new List<string>();
            foreach (var fileInfo in filesInfoList)
            {
                filesList.Add("data\\" + fileInfo.Name);
            }

            if (filesList.Count == 0)
            {
                Logger.Log("No data files found");
                CurrentFileVersion = 1;
                Logger.Log($"Creating new data file data\\{CurrentFileVersion}.txt");
                fs = new FileStream($"data\\{CurrentFileVersion}.txt", FileMode.Create);
                fs.Close();
                Write();
            }
            else
            {
                string toWrite = "";
                toWrite += "Found data files:";
                foreach (var i in filesList)
                {
                    toWrite += " " + i;
                }

                Logger.Log(toWrite);

                string filePath = filesList.Last();
                int leftBound = filePath.IndexOf("\\") + 1;
                int length = filePath.IndexOf(".") - leftBound;
                CurrentFileVersion = Convert.ToInt32(filePath.Substring(leftBound, length));

                Logger.Log($"Opening data file {CurrentFileVersion}.txt");
                fs = new FileStream(filesList.Last(), FileMode.Open);
                Users = (User[]) Serializer.Deserialize(fs);
                fs.Close();
                Logger.Log(Users == null ? "Loaded 0 user profiles" : $"Loaded {Users.Length} user profiles");

                do
                {
                    CurrentFileVersion++;
                } while (filesList.Contains($"data\\{CurrentFileVersion}.txt"));

                Logger.Log($"Creating new data file {CurrentFileVersion}.txt");
                fs = new FileStream($"data\\{CurrentFileVersion}.txt", FileMode.OpenOrCreate);
                fs.Close();
                Write();
            }

            Task.Run(async () =>
            {
                do
                {
                    WriteCount(false);
                    await Task.Delay(TimeSpan.FromSeconds(WriteCountDeltaSec));
                } while (!AppClosing);
            });
        }

        public void WriteCount(bool hasChanges = true)
        {
            if (hasChanges)
            {
                HasChanges = true;
            }

            WriteCounter++;
            if (WriteCounter >= WriteThreshold)
            {
                Logger.Log(
                    $"Current WriteCounter {WriteCounter} is over threshold {WriteThreshold}");
                if (HasChanges)
                {
                    CurrentFileVersion++;
                    Write();
                    WriteCounter = 0;
                }
                else
                {
                    Logger.Log("But no changes - skip");
                    WriteCounter = 0;
                }
            }
        }

        private void Write()
        {
            try
            {
                Logger.Log($"--- Writing to new file data\\{CurrentFileVersion}.txt");
                fs = new FileStream($"data\\{CurrentFileVersion}.txt", FileMode.Create);
            }
            catch (IOException ioException)
            {
                Logger.Log($"Error: {ioException.Message} at {ioException.StackTrace}");
            }

            Serializer.Serialize(fs, Users);
            HasChanges = false;
            Logger.Log("--- Done.");
            fs.Close();
        }

        public bool ContainsUser(int id)
        {
            var result = false;

            if (Users == null) return false;
            if (Users.Length == 0) return false;

            foreach (var user in Users)
            {
                if (user.Id == id)
                {
                    result = true;
                }
            }

            return result;
        }

        public ref User GetUserById(int id)
        {
            for (var i = 0; i < Users.Length; i++)
            {
                if (Users[i].Id == id)
                {
                    return ref Users[i];
                }
            }

            throw new Exception();
        }

        public ref User AddNewUser(User user)
        {
            if (Users == null)
            {
                Users = new[] {user};

                return ref Users[0];
            }

            var oldSize = Users.Length;

            User[] temp = Users;
            Users = new User[oldSize + 1];
            for (int i = 0; i < temp.Length; i++)
            {
                Users[i] = temp[i];
            }

            Users[oldSize] = user;

            WriteCount();

            return ref Users[oldSize];
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            AppClosing = true;
            Write();
        }

        public int findSantaId(Telegram.Bot.Types.User user)
        {
            int targetId = user.Id;
            foreach (var localUser in Users)
            {
                if (localUser.TargetId == targetId)
                {
                    return localUser.Id;
                }
            }

            return 0;
        }
    }
}