﻿using System;
using CLSTools;
using Renci.SshNet;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using ev3dev_Controller.Properties;

namespace ev3dev_Controller
{
    static class Program
    {
        public static SshClient connection;
        public static string host = Settings.Default.host;
        public static string  username = "robot";
        public static string password = "maker";
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
                Trace.AutoFlush = true;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                while (true)
                {
                    try
                    {
                        Console.Write("Enter Command: ");
                        string input = Console.ReadLine();
                        if (input.ToLower() == "exit" || input.ToLower() == "quit") break;
                        else
                        {
                            HandleInput(input);
                            Console.WriteLine();
                        }
                    }
                    catch (Exception ex)
                    {
                        BasicTools.LogErr(ex);
                        Console.WriteLine(ex.Message);
                    }
                }
                #region Dispose
                Console.WriteLine("Closing...");
                if (connection != null) connection.Disconnect();
                try { Application.OpenForms[0].Dispose(); }
                catch (ArgumentOutOfRangeException) { }
                Application.Exit();
                #endregion
            }
            catch (Exception ex)
            {
                BasicTools.LogErr(ex);
                MessageBox.Show(ex.Message);
            }
        }
        static void HandleArgs(string[] args)
        {
            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "/?":
                        Help();
                        break;
                    case "/g":
                        HandleInput("gui");
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported Arg: {args[0]}");
                }
            }
        }
        static void HandleInput(string input)
        {
            string[] args = input.Split(' ');
            switch (args[0].ToLower())
            {
                case "help":
                    Help();
                    break;
                case "connect":
                    try
                    {
                        host = args[1];
                        Settings.Default.host = host;
                        username = args[2];
                        password = args[3];
                    }
                    catch (IndexOutOfRangeException) { }
                    connection = new SshClient(
                                        host: host,
                                        username: username,
                                        password: password);
                    connection.Connect();
                    Console.WriteLine($"Connected to {host}'s user {username} with password {new string('*', password.Length)}");
                    username = "robot";
                    password = "maker";
                    break;
                case "disconnect":
                    connection.Disconnect();
                    break;
                case "shutdown":
                    using (ShellStream shellStream = connection.CreateShellStream("xterm", 255, 50, 800, 600, 1024,
                        new Dictionary<Renci.SshNet.Common.TerminalModes, uint>()))
                    {
                        shellStream.Write("sudo poweroff\n");
                        shellStream.Expect($"[sudo] password for {username}:");
                        shellStream.Write($"{password}\n");
                    }
                    break;
                case "gui":
                    FormTools.RunInNewThread(new StartupForm(), false);
                    break;
                case "ssh":
                    Console.WriteLine();
                    using (ShellStream shellStream = connection.CreateShellStream("xterm", 255, 50, 800, 600, 1024,
                        new Dictionary<Renci.SshNet.Common.TerminalModes, uint>()))
                    {
                        while (true)
                        {
                            Console.Write(shellStream.Read());
                            string streamInput = Console.ReadLine();
                            if (streamInput == "exit") break;
                            shellStream.WriteLine(streamInput);
                        }
                    }
                    break;
                case "": break;
                default:
                    throw new NotSupportedException($"Unsupported Command: {input}");
            }
        }
        static void Help()
        {
            Console.WriteLine(@"SWITCHES
-------------------------------
/? this help
/g open gui (deprecated)

COMMANDS
-------------------------------
help: this help
disconnect: disconnect from the device
shutdown: poweroff the device
connect {host} [username='robot'] [password='maker']: connect to the device
ssh: open an ssh session with the connected device");
        }
    }
}