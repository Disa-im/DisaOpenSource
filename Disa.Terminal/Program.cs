using System;
using Disa.Framework;
using System.Collections.Generic;
using System.Linq;
using Disa.Framework.Bubbles;
using Disa.Framework.Telegram;

namespace Disa.Terminal
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Disa.Terminal!");

            Initialize(new Service[] { new Telegram() });

            Console.WriteLine("Initialized.");

            Console.WriteLine("What would you like to do?");

            while (true)
            {
                var command = Console.ReadLine();
                try
                {
                    DoCommand(command);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to perform that command: " + ex);
                    Console.WriteLine("Type 'help' to get help");
                }
            }
        }

        //From http://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp
        private static IEnumerable<string> SplitCommandLine(string commandLine)
        {
            bool inQuotes = false;
            bool isEscaping = false;

            return Split(commandLine, c => {
                if (c == '\\' && !isEscaping) { isEscaping = true; return false; }

                if (c == '\"' && !isEscaping)
                    inQuotes = !inQuotes;

                isEscaping = false;

                return !inQuotes && Char.IsWhiteSpace(c)/*c == ' '*/;
            })
                .Select(arg => TrimMatchingQuotes(arg.Trim(), '\"').Replace("\\\"", "\""))
                .Where(arg => !string.IsNullOrEmpty(arg));
        }
        private static string TrimMatchingQuotes(string input, char quote)
        {
            if ((input.Length >= 2) && 
                (input[0] == quote) && (input[input.Length - 1] == quote))
                return input.Substring(1, input.Length - 2);

            return input;
        }
        private static IEnumerable<string> Split(string str, 
            Func<char, bool> controller)
        {
            int nextPiece = 0;

            for (int c = 0; c < str.Length; c++)
            {
                if (controller(str[c]))
                {
                    yield return str.Substring(nextPiece, c - nextPiece);
                    nextPiece = c + 1;
                }
            }

            yield return str.Substring(nextPiece);
        }

        private static async void DoCommand(string command)
        {
            var args = SplitCommandLine(command).ToArray();

            switch (args[0].ToLower())
            {
                case "help":
                    PrintHelp();
                    break;
                case "register":
                    {
                        
                        var service = ServiceManager.GetByName(args[1]);
                        ServiceManager.Register(service);
                        ServiceManager.RegisteredServicesDatabase.SaveAllRegistered();
                        Console.WriteLine(service + " registered");
                    }
                    break;
                case "startall":
                    {
                        foreach (var service in ServiceManager.Registered)
                        {
                            var unifiedService = service as UnifiedService;
                            if (unifiedService != null)
                            {
                                ServiceManager.StartUnified(unifiedService, null);
                            }
                            else
                            {
                                await ServiceManager.Start(service, true);
                            }
                            Console.WriteLine(service + " started");
                        }
                    }
                    break;
                case "stop":
                    {
                        var service = ServiceManager.GetByName(args[1]);
                        await ServiceManager.Abort(service);
                        Console.WriteLine(service + " stopped");
                    }
                    break;
                case "start":
                    {
                        var service = ServiceManager.GetByName(args[1]);
                        await ServiceManager.Start(service, true);
                        Console.WriteLine(service + " started");
                    }
                    break;
                case "send":
                    {
                        var service = ServiceManager.GetByName(args[1]);
                        var address = args[2];
                        var message = args[3];
                        var textBubble = new TextBubble(Time.GetNowUnixTimestamp(), Bubble.BubbleDirection.Outgoing, 
                            address, null, false, service, message);
                        await BubbleManager.Send(textBubble);
                        Console.WriteLine(textBubble + " sent");
                    }
                    break;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Help:");
            Console.WriteLine("\tRegister a service: register [serviceName]");
            Console.WriteLine("\tStart all registered services: startall");
            Console.WriteLine("\tStart a service: start [serviceName]");
            Console.WriteLine("\tStop a service: stop [serviceName]");
            Console.WriteLine("\tSend a solo text bubble: send [serviceName] [address] [message]");
        }

        private static void Initialize(IEnumerable<Service> services)
        {
            var allServices = new [] { new UnifiedService() }.Concat(services);

            PlatformManager.InitializePlatform(new WindowsUnix());

            // Initialize the PhoneBook
            var locale = Platform.GetCurrentLocale();
            PhoneBook.Mcc = "000"; //TODO
            PhoneBook.Mnc = "000"; //TODO
            PhoneBook.Language = locale.Substring(0, 2);
            PhoneBook.Country = locale.Substring(3, locale.Length - 3);

            // Uncomment if you want to get information from the Framework
            Utils.Logging = true;

            PlatformManager.InitializeMain(allServices.ToArray());


            BubbleGroupEvents.OnBubbleInserted += (bubble, bubbleGroup) =>
            {
                var textBubble = bubble as TextBubble;
                if (textBubble != null)
                {
                    Console.WriteLine("Message " + (textBubble.Direction == Bubble.BubbleDirection.Incoming ? "from" : "to") + ": " + bubbleGroup.Title + " (" + bubbleGroup.Address + "): " + textBubble.Message);
                }
            };
        }
    }
}
