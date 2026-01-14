using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GUI_Perfect.Views
{
    public partial class TimeSettingView : UserControl
    {
        public TimeSettingView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
