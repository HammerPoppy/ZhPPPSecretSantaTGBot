using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace ZhPPPSecretSantaTGBot
{
    public class DBHandler
    {
        private Logger Logger;
        private XmlSerializer Serializer = new XmlSerializer(typeof(User[]));
        private FileStream fs;
        private int CurrentFileVersion;

        public User[] Users;

        public DBHandler(Logger logger)
        {
            Logger = logger;

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
                if (Users == null)
                {
                    Logger.Log($"Loaded 0 user profiles");
                }
                else
                {
                    Logger.Log($"Loaded {Users.Length} user profiles");
                }

                do
                {
                    CurrentFileVersion++;
                } while (filesList.Contains($"data\\{CurrentFileVersion}.txt"));

                Logger.Log($"Creating new data file {CurrentFileVersion}.txt");
                fs.Close();
                fs = new FileStream($"data\\{CurrentFileVersion}.txt", FileMode.OpenOrCreate);
                fs.Close();
                Write();
            }
        }

        public void Write()
        {
            try
            {
                Logger.Log("--- Deleting data file...");
                File.Delete($"data\\{CurrentFileVersion}.txt");
                Logger.Log("--- Deleted.");
                Logger.Log($"--- Writing to new file data\\{CurrentFileVersion}.txt");
                fs = new FileStream($"data\\{CurrentFileVersion}.txt", FileMode.Create);
            }
            catch (System.IO.IOException ioException)
            {
                Logger.Log($"Error: {ioException.Message} at {ioException.StackTrace}");
                Write();
                return;
            }

            Serializer.Serialize(fs, Users);
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
            else
            {
                var oldSize = Users.Length;

                User[] temp = Users;
                Users = new User[oldSize + 1];
                for (int i = 0; i < temp.Length; i++)
                {
                    Users[i] = temp[i];
                }

                Users[oldSize] = user;

                return ref Users[oldSize];
            }
        }
    }
}