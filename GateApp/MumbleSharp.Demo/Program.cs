using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MumbleSharp.Services;
using MumbleSharp.Services.TextMessaging;
using MumbleSharp.Services.UsersManagement;
using MumbleSharp.Voice;

namespace MumbleSharp.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(
                builder => builder
                    .AddConsole(options => options.DisableColors = false)
                    .SetMinimumLevel(LogLevel.Debug));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();


            string addr, name, pass;
            int port;
            FileInfo serverConfigFile = new FileInfo(Path.Combine(Environment.CurrentDirectory , "server.txt"));
            if (serverConfigFile.Exists)
            {
                using (StreamReader reader = new StreamReader(serverConfigFile.OpenRead()))
                {
                    addr = reader.ReadLine();
                    port = int.Parse(reader.ReadLine());
                    name = reader.ReadLine();
                    pass = reader.ReadLine();
                }
            }
            else
            {
                Console.WriteLine("Enter server address:");
                addr = Console.ReadLine();
                Console.WriteLine("Enter server port (leave blank for default (64738)):");
                string line = Console.ReadLine();
                if (line == "")
                {
                    port = 64738;
                }
                else
                {
                    port = int.Parse(line);
                }
                Console.WriteLine("Enter name:");
                name = Console.ReadLine();
                Console.WriteLine("Enter password:");
                pass = Console.ReadLine();

                using (StreamWriter writer = new StreamWriter(serverConfigFile.OpenWrite()))
                {
                    writer.WriteLine(addr);
                    writer.WriteLine(port);
                    writer.WriteLine(name);
                    writer.WriteLine(pass);
                }
            }

            var connection = new MumbleConnection(new IPEndPoint(Dns.GetHostAddresses(addr).First(a => a.AddressFamily == AddressFamily.InterNetwork), port), loggerFactory);
            var serverStateService = new ServerSyncStateService(connection, loggerFactory);
            var usersManagementService = new UsersManagementService(connection, serverStateService);
            var tms = new TextMessagingService(connection, usersManagementService);
            var voice = new VoiceService(connection, usersManagementService, loggerFactory);
            var recorder = new Recorder(voice, loggerFactory);

            tms.ChannelMessageReceived += (sender, eventArgs) => Console.WriteLine($"ChannelMsg: {eventArgs.Message.Text}");
            tms.PersonalMessageReceived += (sender, eventArgs) => Console.WriteLine($"PersonalMsg: {eventArgs.Message.Text}");

            connection.Connect(name, pass, new string[0], addr, ValidateCertificate, SelectCertificate);

            while (!serverStateService.ReceivedServerSync)
            {
                Thread.Sleep(5000);
            }
            recorder.StartCapture();

            Console.ReadLine();
        }

        private static X509Certificate SelectCertificate(object sender, string targethost, X509CertificateCollection localcertificates, X509Certificate remotecertificate, string[] acceptableissuers)
        {
            return null;
        }

        private static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }
    }
}
