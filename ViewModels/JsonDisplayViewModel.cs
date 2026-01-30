using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Threading;
using GUI_Perfect.Services;

namespace GUI_Perfect.ViewModels;

public class JsonDisplayViewModel : ViewModelBase
{
    private readonly MainViewModel _main;
    private readonly TcpJsonClient _server;

    // 受信ログを表示するためのコレクション
    public ObservableCollection<string> JsonLogs { get; } = new();

    private string _statusText = "初期化中...";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; RaisePropertyChanged(); }
    }

    private string _latestJson = "";
    public string LatestJson
    {
        get => _latestJson;
        set { _latestJson = value; RaisePropertyChanged(); }
    }

    public ICommand BackCommand { get; }
    public ICommand ClearCommand { get; }

    public JsonDisplayViewModel(MainViewModel main)
    {
        _main = main;

        // 戻るボタン
        BackCommand = new RelayCommand(() =>
        {
            Cleanup();
            _main.IsCameraPaused = false; // カメラ再開
            _main.Navigate(new HomeViewModel(_main));
        });

        // クリアボタン
        ClearCommand = new RelayCommand(() =>
        {
            JsonLogs.Clear();
            LatestJson = "";
        });

        // ポート 55555 で待ち受け開始（競合は解消済み）
        _server = new TcpJsonClient(55555);

        _server.OnStatusChanged += (msg) =>
        {
            Dispatcher.UIThread.Post(() => StatusText = msg);
        };

        _server.OnJsonReceived += (json) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                LatestJson = json;

                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                JsonLogs.Insert(0, $"[{timestamp}] {json}");
                
                if (JsonLogs.Count > 100) JsonLogs.RemoveAt(JsonLogs.Count - 1);
            });
        };

        _server.Start();
    }

    private void Cleanup()
    {
        _server.Stop();
    }
}
