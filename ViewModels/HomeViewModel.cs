using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace GUI_Perfect.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly MainViewModel _main;
    
    // View側はこのプロパティ経由で画像を取得する
    public Bitmap? CameraImage => _main.CameraImage;

    private string _currentDateTime = "";
    public string CurrentDateTime
    {
        get => _currentDateTime;
        set { _currentDateTime = value; RaisePropertyChanged(); }
    }

    public ICommand CaptureCommand { get; }
    public ICommand GalleryCommand { get; }
    public ICommand MeasurementCommand { get; }
    public ICommand TimeSettingCommand { get; }
    public ICommand StopCommand { get; }            
    public ICommand ShutdownJetsonCommand { get; }  
    public ICommand ShutdownAllCommand { get; }     

    public HomeViewModel(MainViewModel main)
    {
        _main = main;
        
        // 【重要】MainViewModelの映像更新イベントを購読する
        _main.PropertyChanged += MainViewModel_PropertyChanged;
        
        UpdateDateTime();
        DispatcherTimer.Run(() => 
        {
            UpdateDateTime();
            return true; 
        }, TimeSpan.FromSeconds(1));

        // 各画面へ遷移する際は、イベント購読を解除(Cleanup)してから移動する
        CaptureCommand = new RelayCommand(() => 
        {
            Cleanup();
            _main.Navigate(new SimpleInspectViewModel(_main));
        });

        GalleryCommand = new RelayCommand(() => 
        {
            Cleanup();
            _main.Navigate(new GalleryViewModel(_main));
        });

        MeasurementCommand = new RelayCommand(async () => 
        {
            Cleanup();
            await _main.TcpServer.SendCommandAsync("change_format", new { format = "YUV422" });
            _main.Navigate(new MeasurementViewModel(_main));
        });

        TimeSettingCommand = new RelayCommand(() =>
        {
             Cleanup();
             _main.Navigate(new TimeSettingViewModel(() =>
             {
                 _main.Navigate(new HomeViewModel(_main));
             }));
        });

        StopCommand = new RelayCommand(_main.ShutdownApplication);

        ShutdownJetsonCommand = new RelayCommand(async () =>
        {
            await _main.TcpServer.SendCommandAsync("shutdown", new { });
        });

        ShutdownAllCommand = new RelayCommand(async () =>
        {
            await _main.TcpServer.SendCommandAsync("shutdown", new { });
            await Task.Delay(1000);
            try
            {
                var psiLocal = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "shutdown -h now",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psiLocal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Local Shutdown Error: {ex.Message}");
            }
        });
    }

    private void Cleanup()
    {
        _main.PropertyChanged -= MainViewModel_PropertyChanged;
    }

    // MainViewModelのプロパティが変わったら、HomeViewModelとしても通知を出す
    private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CameraImage))
        {
            RaisePropertyChanged(nameof(CameraImage));
        }
    }

    private void UpdateDateTime()
    {
        CurrentDateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
    }
}
