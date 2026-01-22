using System.Windows.Input;

namespace GUI_Perfect.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly MainViewModel _main;
    public ICommand CaptureCommand { get; } 
    public ICommand GalleryCommand { get; }
    public ICommand MeasurementCommand { get; }
    public ICommand TimeSettingCommand { get; } // 追加
    public ICommand StopCommand { get; }
    
    public HomeViewModel(MainViewModel main)
    {
        _main = main;
        CaptureCommand = new RelayCommand(() => _main.Navigate(new SimpleInspectViewModel(_main)));
        GalleryCommand = new RelayCommand(() => _main.Navigate(new GalleryViewModel(_main)));
        MeasurementCommand = new RelayCommand(() => _main.Navigate(new MeasurementViewModel(_main)));
        
        // 【追加】時刻設定画面へ遷移
        // 設定完了時(onComplete)は、またこのホーム画面に戻ってくるようにする
        TimeSettingCommand = new RelayCommand(() => 
        {
             _main.Navigate(new TimeSettingViewModel(() => 
             {
                 _main.Navigate(new HomeViewModel(_main));
             }));
        });
        
        StopCommand = new RelayCommand(_main.ShutdownApplication);
    }
}
