using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ReactiveUI;
using WireMockInspector.ViewModels;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
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
            // this.WhenActivated((disposables) =>
            // {
            //     ViewModel!.LoadRequestsCommand
            //         .ThrownExceptions
            //         .ObserveOn(RxApp.MainThreadScheduler)
            //         .Subscribe(async _ =>
            //         {
            //             var msg = MessageBoxManager.GetMessageBoxStandard("Error",
            //                 "Cannot connect to WireMock Admin API.\r\n\r\nMake sure that:\r\n- The URL is correct\r\n- WireMock is started with Admin API enabled", ButtonEnum.Ok,
            //                 MsBox.Avalonia.Enums.Icon.Error, WindowStartupLocation.CenterOwner);
            //            await msg.ShowAsync();
            //         }).DisposeWith(disposables);
            //
            //     ViewModel.SaveServerSettings
            //         .ThrownExceptions
            //         .ObserveOn(RxApp.MainThreadScheduler)
            //         .Subscribe(async ex =>
            //         {
            //             var msg =   MessageBoxManager.GetMessageBoxStandard("Error",
            //                 $"Cannot save server settings.\r\n\r\n {ex.Message}", ButtonEnum.Ok,
            //                 MsBox.Avalonia.Enums.Icon.Error, WindowStartupLocation.CenterOwner);
            //             await msg.ShowAsync();
            //         }).DisposeWith(disposables);
            //
            //     if (Settings != null)
            //     {
            //         ViewModel.AdminUrl = Settings.AdminUrl;
            //         if (Settings.AutoLoad)
            //         {
            //           ViewModel.LoadRequestsCommand.Execute().Subscribe().DisposeWith(disposables);
            //         }
            //
            //         if (string.IsNullOrWhiteSpace(Settings.InstanceName) == false)
            //         {
            //             SetInstanceName(Settings.InstanceName);
            //         }
            //     }
            // });
            _mainTitle = $"WireMockInspector v{Assembly.GetEntryAssembly()!.GetName().Version}";
            Title = _mainTitle;
        }

        public void SetInstanceName(string instanceName) => Title = $"{_mainTitle} - {instanceName}";
    }
}