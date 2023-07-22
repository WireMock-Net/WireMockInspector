using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WireMockInspector.Views;

public partial class RequestLogs : UserControl
{
    public RequestLogs()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}