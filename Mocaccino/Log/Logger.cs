using System;
using System.IO;

namespace Mocaccino.Log
{
    class Logger
    {
        public static void WriteLine(string message)
        {
            string tempFile = Path.GetTempFileName();
            string path = AppDomain.CurrentDomain.BaseDirectory + @"Logs\";
            Directory.CreateDirectory(path);
            string fileName = path + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";

            if (File.Exists(fileName))
            {
                using (StreamWriter sw = new StreamWriter(tempFile))
                using (StreamReader sr = new StreamReader(fileName))
                {
                    ///string.Format(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss | {0}"), message)
                    sw.WriteLine($"{DateTime.Now:dd-MM-yyyy HH:mm:ss} | {message}");
                    while (!sr.EndOfStream)
                        sw.WriteLine(sr.ReadLine());
                }
                File.Copy(tempFile, fileName, true);
                File.Delete(tempFile);
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    sw.WriteLine($"{DateTime.Now:dd-MM-yyyy HH:mm:ss} | {message}");
                }
            }
        }
    }
}
