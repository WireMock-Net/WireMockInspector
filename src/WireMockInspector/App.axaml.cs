using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.CommandLine;
using System.Diagnostics;
using WireMockInspector.ViewModels;
using WireMockInspector.Views;

namespace WireMockInspector
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindowViewModel = new MainWindowViewModel();
                var mainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel,
                };
               
                if (desktop.Args is { } args)
                {
                    var rootCommand = new RootCommand("WireMockInspector CLI");

                    var attachCommand = new Command("attach", "Attach WireMockInspector to running WireMock server");
                    var adminUrlOption = new Option<string>("--adminUrl") { IsRequired = true };
                    attachCommand.AddOption(adminUrlOption);
                    var autoLoadOption = new Option<bool>("--autoLoad") { IsRequired = false };
                    attachCommand.AddOption(autoLoadOption);
                    var instanceNameOption = new Option<string>("--instanceName") { IsRequired = false };
                    attachCommand.AddOption(instanceNameOption);

                    attachCommand.SetHandler((adminUrl, autoLoad, instanceName) =>
                    {
                        mainWindow.Settings = new StartupSettings
                        {
                            AdminUrl = adminUrl,
                            InstanceName = instanceName,
                            AutoLoad = autoLoad
                        };
                    }, adminUrlOption, autoLoadOption, instanceNameOption);
                    rootCommand.AddCommand(attachCommand);
                    rootCommand.SetHandler(() => { });
                    rootCommand.Invoke(args);
                }

                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }

    public class StartupSettings
    {
        public string AdminUrl { get; set; }
        public bool AutoLoad { get; set; }
        public string InstanceName { get; set; }
    }
}