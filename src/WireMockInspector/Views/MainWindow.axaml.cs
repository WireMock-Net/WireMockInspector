using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using WireMockInspector.ViewModels;
using System;
using Avalonia.Controls;


namespace WireMockInspector.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
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
            });

        }
    }
}