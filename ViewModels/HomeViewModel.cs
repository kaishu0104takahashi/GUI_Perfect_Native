using System.Windows.Input;

namespace GUI_Perfect.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly MainViewModel _main;
    public ICommand CaptureCommand { get; } 
    public ICommand GalleryCommand { get; }
    public ICommand MeasurementCommand { get; } // 追加
    public ICommand StopCommand { get; }
    
    public HomeViewModel(MainViewModel main)
    {
        _main = main;
        CaptureCommand = new RelayCommand(() => _main.Navigate(new SimpleInspectViewModel(_main)));
        GalleryCommand = new RelayCommand(() => _main.Navigate(new GalleryViewModel(_main)));
        
        // 追加: 測定モードへ遷移
        MeasurementCommand = new RelayCommand(() => _main.Navigate(new MeasurementViewModel(_main)));
        
        StopCommand = new RelayCommand(_main.ShutdownApplication);
    }
}
