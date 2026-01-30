using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GUI_Perfect.Views;

public partial class JsonDisplayView : UserControl
{
    public JsonDisplayView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
