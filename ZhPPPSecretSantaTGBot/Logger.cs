using System;
using System.IO;
using Microsoft.VisualBasic;

namespace ZhPPPSecretSantaTGBot
{
    public class Logger
    {
        private readonly StreamWriter sr;
        private readonly string signature;

        public Logger()
        {
            if (!Directory.Exists("logs"))
            {
                Directory.CreateDirectory("logs");
            }

            signature = DateAndTime.Now.ToString().Replace(":", "-");
            sr = new StreamWriter($"logs\\{signature}.txt", true) {AutoFlush = true};
            Log($"Log \"{signature}.txt\" started");
        }

        public void Log(string message)
        {
            // TODO handle exception
            string toWrite = $"{DateAndTime.Now.ToString()}| {message}";
            sr.WriteLine(toWrite);
            Console.WriteLine(toWrite);
        }

        ~Logger()
        {
            sr.Close();
        }
    }
}