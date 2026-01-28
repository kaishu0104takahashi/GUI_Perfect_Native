using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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

    public ICommand StopCommand { get; }            
    public ICommand ShutdownJetsonCommand { get; }  
    public ICommand ShutdownAllCommand { get; }     

    public HomeViewModel(MainViewModel main)
    {
        _main = main;
        CaptureCommand = new RelayCommand(() => _main.Navigate(new SimpleInspectViewModel(_main)));
        GalleryCommand = new RelayCommand(() => _main.Navigate(new GalleryViewModel(_main)));

        // --- 測定モードへ移動（変更箇所） ---
        MeasurementCommand = new RelayCommand(async () => 
        {
            // YUV422へ変更コマンドを送信
            await SendTcpCommandAsync("change_format", new { format = "YUV422" });
            
            // 画面遷移
            _main.Navigate(new MeasurementViewModel(_main));
        });

        TimeSettingCommand = new RelayCommand(() =>
        {
             _main.Navigate(new TimeSettingViewModel(() =>
             {
                 _main.Navigate(new HomeViewModel(_main));
             }));
        });

        // アプリ終了
        StopCommand = new RelayCommand(_main.ShutdownApplication);

        // Jetson停止
        ShutdownJetsonCommand = new RelayCommand(async () =>
        {
            await SendTcpCommandAsync("shutdown", new { });
        });

        // 全電源オフ
        ShutdownAllCommand = new RelayCommand(async () =>
        {
            await SendTcpCommandAsync("shutdown", new { });
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

    // TCPコマンド送信用の共通メソッド
    private async Task SendTcpCommandAsync(string commandName, object argsObj)
    {
        try
        {
            string remoteIp = "192.168.76.230";
            int remotePort = 55555; 

            var cmdData = new
            {
                type = "cmd",
                command = commandName,
                args = argsObj
            };

            string jsonString = JsonSerializer.Serialize(cmdData);
            byte[] data = Encoding.UTF8.GetBytes(jsonString);

            using (var tcpClient = new TcpClient())
            {
                // 接続タイムアウトを設ける（簡易実装）
                var connectTask = tcpClient.ConnectAsync(remoteIp, remotePort);
                if (await Task.WhenAny(connectTask, Task.Delay(1000)) == connectTask)
                {
                    // 接続成功
                    await connectTask; 
                    using (var stream = tcpClient.GetStream())
                    {
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                    Console.WriteLine($"[TCP Sent] {jsonString}");
                }
                else
                {
                    Console.WriteLine("[TCP Error] Connection Timed out");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TCP Send Error: {ex.Message}");
        }
    }
}
