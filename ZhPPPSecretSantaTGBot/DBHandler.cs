using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace ZhPPPSecretSantaTGBot
{
    public class DBHandler
    {
        private Logger Logger;
        private XmlSerializer Serializer = new XmlSerializer(typeof(User));
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

            var filesList = Directory.GetFiles("data");

            if (filesList.Length == 0)
            {
                Logger.Log("No data files found");
                CurrentFileVersion = 1;
                fs = new FileStream($"data\\{CurrentFileVersion}.txt", FileMode.Create);
                Logger.Log($"Created new data file data\\{CurrentFileVersion}.txt");
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
                fs = new FileStream($"data\\{CurrentFileVersion}.txt", FileMode.Create);
                Write();
            }
        }

        public void Write()
        {
            Serializer.Serialize(fs, Users);
        }
    }
}