using System;
using Disa.Framework;
using System.Collections.Generic;
using System.Linq;
using Disa.Framework.Bubbles;
using Disa.Framework.Telegram;
using System.IO;
using Managed.Adb;
using Mono.Cecil;
using System.Threading.Tasks;
using System.Text;

namespace Disa.Terminal
{
    public class MainClass
    {
        private static TerminalSettings Settings { get; set; }

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

        [Serializable]
        public class TerminalSettings : DisaMutableSettings
        {
            public List<PluginDeployment> PluginDeployments { get; set; }

            public class PluginDeployment
            {
                public string Name { get; set; }
                public string Path { get; set; }
                public List<Assembly> Assemblies { get; set; }

                public class Assembly
                {
                    public string Name { get; set; }
                    public DateTime Modified { get; set; }
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
			// Clean the input
			command = command.Trim();
			// Don't do anything if we have empty input
			if(command == "")
				return;

			// Split and handle command
            var args = SplitCommandLine(command).ToList();

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
                case "deploy-unregister":
                    {
                        var pluginName = args[1];
                        var deployment = Settings.PluginDeployments.FirstOrDefault(x => x.Name.ToLower() == pluginName.ToLower());
                        if (deployment != null)
                        {
                            Settings.PluginDeployments.Remove(deployment);
                        }
                        MutableSettingsManager.Save(Settings);
                        Console.WriteLine("Removed.");
                    }
                    break;
                case "deploy-register":
                    {
                        var pluginName = args[1];
                        var path = args[2].ToLower();
                        if (Settings.PluginDeployments != null)
                        {
                            var hasDeployment = Settings.PluginDeployments.FirstOrDefault(x => x.Name.ToLower() == pluginName.ToLower()) != null;
                            if (hasDeployment)
                            {
                                Console.WriteLine("Plugin has already been registered in deployment system.");
                                break;
                            }
                        }
                        if (Settings.PluginDeployments == null)
                        {
                            Settings.PluginDeployments = new List<TerminalSettings.PluginDeployment>();
                        }
                        Settings.PluginDeployments.Add(new TerminalSettings.PluginDeployment
                        {
                            Name = pluginName,
                            Path = path,
                        });
                        MutableSettingsManager.Save(Settings);
                        Console.WriteLine("Plugin registered!");
                    }
                    break;
                case "deploy-clean":
                    {
                        var pluginName = args[1];
                        var deployment = Settings.PluginDeployments.FirstOrDefault(x => x.Name.ToLower() == pluginName.ToLower());
                        if (deployment != null)
                        {
                            deployment.Assemblies = null;
                            MutableSettingsManager.Save(Settings);
                            Console.WriteLine("Cleaned assemblies.");
                        }
                        else
                        {
                            Console.WriteLine("Could not find plugin deployment: " + pluginName);
                        }
                    }
                    break;
                case "deploy":
                    {
                        var pluginName = args[1];
                        var deployment = Settings.PluginDeployments.FirstOrDefault(x => x.Name.ToLower() == pluginName.ToLower());
                        var oldAssemblies = deployment.Assemblies ?? new List<TerminalSettings.PluginDeployment.Assembly>();
                        var assembliesToDeploy = new List<TerminalSettings.PluginDeployment.Assembly>();
                        var newAssemblies = new List<TerminalSettings.PluginDeployment.Assembly>();
                        var pluginManifest = Path.Combine(deployment.Path, "PluginManifest.xml");
                        if (!File.Exists(pluginManifest))
                        {
                            Console.WriteLine("A plugin manifest file is needed!");
                            break;
                        }
                        foreach (var assemblyFile in Directory.EnumerateFiles(deployment.Path, "*.dll")
                            .Concat(new [] { pluginManifest }))
                        {
                            var assemblyFileName = Path.GetFileName(assemblyFile);
                            if (PlatformManager.AndroidLinkedAssemblies.Contains(assemblyFileName))
                                continue;

                            var lastModified = File.GetLastWriteTime(assemblyFile);
                            var newAssembly = new TerminalSettings.PluginDeployment.Assembly
                            {
                                Name = assemblyFileName,
                                Modified = lastModified
                            };
                            newAssemblies.Add(newAssembly);

                            var oldAssembly = oldAssemblies.FirstOrDefault(x => x.Name == assemblyFileName);
                            if (oldAssembly == null)
                            {
                                assembliesToDeploy.Add(newAssembly);
                            }
                            else if (oldAssembly.Modified != lastModified)
                            {
                                assembliesToDeploy.Add(newAssembly);
                            }
                        }
                        deployment.Assemblies = newAssemblies;
                        MutableSettingsManager.Save(Settings);
                        var devices = AdbHelper.Instance.GetDevices(AndroidDebugBridge.SocketAddress);
                        Device selectedDevice;
                        if (devices.Count > 1)
                        {
                            Console.WriteLine("Please pick a device:");
                            var counter = 0;
                            foreach (var device in devices)
                            {
                                Console.WriteLine(counter++ + ") " + device.SerialNumber);
                            }
                            Console.Write("Selection: ");
                            var selection = int.Parse(Console.ReadLine().Trim());
                            selectedDevice = devices[selection];
                        }
                        else
                        {
                            selectedDevice = devices.First();
                        }
                        var remotePath = "/sdcard/Disa/plugins/" + deployment.Name;
                        if (!selectedDevice.FileSystem.Exists(remotePath))
                        {
                            selectedDevice.FileSystem.MakeDirectory(remotePath);
                        }
                        foreach (var assemblyToDeploy in assembliesToDeploy)
                        {
                            Console.WriteLine("Transferring " + assemblyToDeploy.Name + "...");
                            var remoteAssembly = remotePath + "/" + assemblyToDeploy.Name;
                            if (selectedDevice.FileSystem.Exists(remoteAssembly))
                            {
                                selectedDevice.FileSystem.Delete(remoteAssembly);
                            }
                            selectedDevice.SyncService.PushFile(Path.Combine(deployment.Path, assemblyToDeploy.Name),
                                remoteAssembly, new SyncServiceProgressMonitor());
                        }
                        Console.WriteLine("Plugin deployed! Restarting Disa...");
                        selectedDevice.ExecuteShellCommand("am force-stop com.disa", new ShellOutputReceiver());
                        Task.Delay(250).Wait();
                        selectedDevice.ExecuteShellCommand("monkey -p com.disa -c android.intent.category.LAUNCHER 1", new ShellOutputReceiver());
                        Console.WriteLine("Disa restarted!");
                    }
                    break;
                case "deploy-print-dependencies":
                    {
                        var pluginName = args[1];
                        var deployment = Settings.PluginDeployments.FirstOrDefault(x => x.Name.ToLower() == pluginName.ToLower());
                        foreach (var assemblyFile in Directory.EnumerateFiles(deployment.Path, "*.dll"))
                        {
                            var assemblyFileName = Path.GetFileName(assemblyFile);
                            if (PlatformManager.AndroidLinkedAssemblies.Contains(assemblyFileName))
                                continue;
                            var module = ModuleDefinition.ReadModule(assemblyFile);
                            Console.WriteLine(assemblyFileName + ": ");
                            foreach (var referenceAssembly in module.AssemblyReferences)
                            {
                                Console.WriteLine("> " + referenceAssembly.FullName);
                            }  
                        }
                    }
                    break;
                default:
                    {
                        var service = ServiceManager.GetByName(args[0]);
                        if (service != null)
                        {
                            var terminal = service as ITerminal;
                            if (terminal != null)
                            {
                                try
                                {
                                    terminal.DoCommand(args.GetRange(1, args.Count - 1).ToArray());
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error in processing a service terminal command: " + ex);
                                }
                            }

                        }           
                    }
                    break;
            }
        }

        private class ShellOutputReceiver : IShellOutputReceiver
        {
            public void AddOutput(byte[] data, int offset, int length)
            {
            }
            public void Flush()
            {
            }
            public bool IsCancelled
            {
                get
                {
                    return false;
                }
            }
        }

        private class SyncServiceProgressMonitor : ISyncProgressMonitor
        {
            public void Start(long totalWork)
            {
            }
            public void Stop()
            {
            }
            public void StartSubTask(string source, string destination)
            {
            }
            public void Advance(long work)
            {
            }
            public bool IsCanceled
            {
                get
                {
                    return false;
                }
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

            Settings = MutableSettingsManager.Load<TerminalSettings>();

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
