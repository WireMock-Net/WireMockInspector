using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using WireMockInspector.ViewModels;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using WireMock.Admin.Mappings;
using WireMock.Admin.Requests;


namespace WireMockInspector.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private string? _mainTitle;

        public StartupSettings? Settings { get; set; }

        public MainWindow()
        {
            
            InitializeComponent();
            this.WhenActivated((disposables) =>
            {
                ViewModel!.LoadRequestsCommand
                    .ThrownExceptions
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ =>
                    {
                        var msg = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error",
                            "Cannot connect to WireMock Admin API.\r\n\r\nMake sure that:\r\n- The URL is correct\r\n- WireMock is started with Admin API enabled", ButtonEnum.Ok,
                            MessageBox.Avalonia.Enums.Icon.Error, WindowStartupLocation.CenterOwner);
                        msg.ShowDialog(this);
                    }).DisposeWith(disposables);

                ViewModel.SaveServerSettings
                    .ThrownExceptions
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(ex =>
                    {
                        var msg = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error",
                            $"Cannot save server settings.\r\n\r\n {ex.Message}", ButtonEnum.Ok,
                            MessageBox.Avalonia.Enums.Icon.Error, WindowStartupLocation.CenterOwner);
                        msg.ShowDialog(this);
                    }).DisposeWith(disposables);

                if (Settings != null)
                {
                    ViewModel.AdminUrl = Settings.AdminUrl;
                    if (Settings.AutoLoad)
                    {
                      ViewModel.LoadRequestsCommand.Execute().Subscribe().DisposeWith(disposables);
                    }

                    if (string.IsNullOrWhiteSpace(Settings.InstanceName) == false)
                    {
                        SetInstanceName(Settings.InstanceName);
                    }
                }
            });
            _mainTitle = $"WireMockInspector v{Assembly.GetEntryAssembly()!.GetName().Version}";
            Title = _mainTitle;
        }

        public void SetInstanceName(string instanceName) => Title = $"{_mainTitle} - {instanceName}";
    }
}