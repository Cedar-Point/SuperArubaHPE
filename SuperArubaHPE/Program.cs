using System;
using System.IO;
using Renci.SshNet;
using System.Text.RegularExpressions;
using System.Threading;

namespace SuperArubaHPE
{
    class Program
    {
        private static ShellStream stream;
        private static bool waiting = true;
        private static Regex ansi = new Regex("[\\u001B\\u009B][[\\]()#;?]*(?:(?:(?:[a-zA-Z\\d]*(?:;[-a-zA-Z\\d\\/#&.:=?%@~_]*)*)?\\u0007)|(?:(?:\\d{1,4}(?:;\\d{0,4})*)?[\\dA-PR-TZcf-ntqry=><~]))");
        private static string lastMessage = "";
        private static int returnCode = 0;
        static int Main(string[] args)
        {
            if (!Directory.Exists(@".\scripts\") || Directory.GetFiles(@".\scripts\").Length == 0)
            {
                Console.WriteLine("Missing scripts folder or empty...");
                Console.ReadLine();
                Thread.Sleep(3000);
                return 2;
            }
            string username = "";
            string password = "";
            if (args.Length != 2)
            {
                Console.Write("Enter Username > ");
                username = Console.ReadLine();
                Console.Write("Enter Password > ");
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter) break;
                    password += key.KeyChar;
                }
                Console.WriteLine("");
            }
            else
            {
                username = args[0];
                password = args[1];
            }
            foreach (string file in Directory.EnumerateFiles(@".\scripts\"))
            {
                string host = new FileInfo(file).Name;
                Console.WriteLine("Connecting to: " + host + "...");
                PasswordAuthenticationMethod pa = new PasswordAuthenticationMethod(username, password);
                ConnectionInfo ci = new ConnectionInfo(host, username, pa);
                SshClient ssh = new SshClient(ci);
                try
                {
                    ssh.Connect();
                }
                catch(Exception e)
                {
                    Console.WriteLine("Failed to connect: " + e.Message);
                    returnCode = 59;
                    continue;
                }
                stream = ssh.CreateShellStream("HPE", 80, 60, 500, 500, 1024);
                stream.DataReceived += Stream_DataReceived;
                Console.WriteLine("Connected!");
                Console.WriteLine("-----------------------------------------------");
                stream.WriteLine("");
                Thread.Sleep(1000);
                foreach (string line in File.ReadAllLines(file))
                {
                    waiting = true;
                    lastMessage = line;
                    stream.WriteLine(line);
                    while (waiting) { }
                }
                Console.WriteLine("-----------------------------------------------");
                Console.WriteLine("Flushing...");
                stream.Flush();
                stream.Dispose();
                Console.WriteLine("Disconnecting...");
                ssh.Disconnect();
                Console.WriteLine("Disconnected!");
            }
            Thread.Sleep(3000);
            return returnCode;
        }
        private static void Stream_DataReceived(object sender, Renci.SshNet.Common.ShellDataEventArgs e)
        {
            string text = stream.Read();
            text = ansi.Replace(text, "");
            if (lastMessage != "")
            {
                text = text.Replace(lastMessage, lastMessage + "\n\r");
            }
            Console.Write(text);
            waiting = false;
        }
    }
}