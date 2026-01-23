using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GUI_Perfect.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly MainViewModel _main;
    public ICommand CaptureCommand { get; } 
    public ICommand GalleryCommand { get; }
    public ICommand MeasurementCommand { get; }
    public ICommand TimeSettingCommand { get; }
    
    // 3つの終了コマンド
    public ICommand StopCommand { get; }            // アプリ終了
    public ICommand ShutdownJetsonCommand { get; }  // Jetsonのみ停止
    public ICommand ShutdownAllCommand { get; }     // 全電源オフ (Jetson + Raspi)
    
    public HomeViewModel(MainViewModel main)
    {
        _main = main;
        CaptureCommand = new RelayCommand(() => _main.Navigate(new SimpleInspectViewModel(_main)));
        GalleryCommand = new RelayCommand(() => _main.Navigate(new GalleryViewModel(_main)));
        MeasurementCommand = new RelayCommand(() => _main.Navigate(new MeasurementViewModel(_main)));
        
        TimeSettingCommand = new RelayCommand(() => 
        {
             _main.Navigate(new TimeSettingViewModel(() => 
             {
                 _main.Navigate(new HomeViewModel(_main));
             }));
        });
        
        // 1. アプリ終了
        StopCommand = new RelayCommand(_main.ShutdownApplication);

        // 2. Jetsonのみ停止
        ShutdownJetsonCommand = new RelayCommand(async () => 
        {
            await ShutdownJetsonAsync();
        });

        // 3. 全電源オフ (Jetson -> Raspi)
        ShutdownAllCommand = new RelayCommand(async () => 
        {
            // まずJetsonを停止
            await ShutdownJetsonAsync();

            // 通信バッファ時間を置いて
            await Task.Delay(1000);

            // 自分(Raspi)を停止
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

    // Jetson停止処理の共通メソッド
    private async Task ShutdownJetsonAsync()
    {
        try
        {
            string remoteUser = "shikoku-pc"; 
            string remoteIp = "192.168.76.230";

            var psiRemote = new ProcessStartInfo
            {
                FileName = "ssh",
                // タイムアウト3秒、パスワードなしでshutdownコマンド送信
                Arguments = $"{remoteUser}@{remoteIp} -o StrictHostKeyChecking=no -o ConnectTimeout=3 \"sudo shutdown -h now\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            var p = Process.Start(psiRemote);
            if (p != null) await p.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Jetson Shutdown Error: {ex.Message}");
        }
    }
}
