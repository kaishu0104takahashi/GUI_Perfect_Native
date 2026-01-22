using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GUI_Perfect.Views
{
    public partial class MeasurementView : UserControl
    {
        public MeasurementView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
