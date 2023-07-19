using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WireMockInspector.Views;

public partial class MappingPage : UserControl
{
    public MappingPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}