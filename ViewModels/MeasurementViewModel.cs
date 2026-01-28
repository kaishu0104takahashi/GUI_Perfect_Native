using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;

namespace GUI_Perfect.ViewModels
{
    public class MeasurementViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        // --- プロパティ ---

        // ライブ映像 (MainViewModelから中継)
        public Bitmap? LiveImage => _mainViewModel.CameraImage;

        // 撮影された静止画
        private Bitmap? _capturedImage;
        public Bitmap? CapturedImage
        {
            get => _capturedImage;
            set { _capturedImage = value; RaisePropertyChanged(); }
        }

        // 結果テキスト
        private string _resultText = "";
        public string ResultText
        {
            get => _resultText;
            set { _resultText = value; RaisePropertyChanged(); }
        }

        // 測定中フラグ
        private bool _isMeasuring;
        public bool IsMeasuring
        {
            get => _isMeasuring;
            set { _isMeasuring = value; RaisePropertyChanged(); }
        }

        // 結果表示中フラグ
        private bool _hasResult;
        public bool HasResult
        {
            get => _hasResult;
            set { _hasResult = value; RaisePropertyChanged(); }
        }

        // --- コマンド ---
        public ICommand ExecuteMeasurementCommand { get; }
        public ICommand RetryCommand { get; }
        public ICommand BackCommand { get; }

        public MeasurementViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            // MainViewModelの映像更新を検知して通知する
            _mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

            ExecuteMeasurementCommand = new RelayCommand<object>(async _ => await ExecuteMeasurement());

            RetryCommand = new RelayCommand(() =>
            {
                // リセット
                CapturedImage = null;
                HasResult = false;
                ResultText = "";
            });

            // --- 戻るボタン（変更箇所） ---
            BackCommand = new RelayCommand(async () =>
            {
                // MJPEGへ戻すコマンドを送信
                await SendTcpCommandAsync("change_format", new { format = "MJPEG" });

                // 購読解除してホームへ
                _mainViewModel.PropertyChanged -= MainViewModel_PropertyChanged;
                _mainViewModel.Navigate(new HomeViewModel(_mainViewModel));
            });
        }

        private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CameraImage))
            {
                RaisePropertyChanged(nameof(LiveImage));
            }
        }

        private async Task ExecuteMeasurement()
        {
            if (IsMeasuring) return;

            // 1. 測定開始
            IsMeasuring = true;
            HasResult = false;

            // 2. 処理シミュレーション (C++処理待ちの想定)
            await Task.Delay(300); // 0.3秒

            // 3. 画像取得
            if (_mainViewModel.CameraImage != null)
            {
                CapturedImage = _mainViewModel.CameraImage;
            }

            // 4. 結果セット (モック)
            ResultText = "1kΩ";

            // 5. 完了
            IsMeasuring = false;
            HasResult = true;
        }

        // TCPコマンド送信用のメソッド
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
                    // 接続タイムアウト処理（1秒待機）
                    var connectTask = tcpClient.ConnectAsync(remoteIp, remotePort);
                    if (await Task.WhenAny(connectTask, Task.Delay(1000)) == connectTask)
                    {
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
}
