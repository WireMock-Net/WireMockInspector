using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WireMockInspector.Views;

public partial class ScenarioPage : UserControl
{
    public ScenarioPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}