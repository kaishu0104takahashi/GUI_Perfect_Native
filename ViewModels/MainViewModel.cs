using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GUI_Perfect.Services;

namespace GUI_Perfect.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly UdpVideoReceiver _videoReceiver;
    
    private ViewModelBase _currentViewModel;
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set { _currentViewModel = value; RaisePropertyChanged(); }
    }

    private Bitmap? _cameraImage;
    public Bitmap? CameraImage
    {
        get => _cameraImage;
        set { _cameraImage = value; RaisePropertyChanged(); }
    }
    
    // カメラ停止フラグ
    private bool _isCameraPaused = false;
    public bool IsCameraPaused
    {
        get => _isCameraPaused;
        set 
        { 
            _isCameraPaused = value; 
            if (_videoReceiver != null)
            {
                _videoReceiver.IsPaused = value;
            }
            RaisePropertyChanged(); 
        }
    }

    // 全画面表示判定 (Home画面以外は全画面扱いにしてタスクバー等を隠す)
    public bool IsFullScreen => !(CurrentViewModel is HomeViewModel);

    public MainViewModel()
    {
        // 【ここを変更】初期画面を「時刻設定画面」にする
        // コンストラクタの引数(Action)で、設定完了後の移動先(Home)を指定
        _currentViewModel = new TimeSettingViewModel(() => 
        {
            // 時刻設定が終わったらホームへ移動
            Navigate(new HomeViewModel(this));
        });

        // 初期状態は時刻設定なので、カメラは停止(Pause)にしておく
        _isCameraPaused = true;

        // カメラ受信準備
        _videoReceiver = new UdpVideoReceiver(50000);
        _videoReceiver.OnFrameReady = (bmp) =>
        {
            CameraImage = bmp;
        };
        
        // Pause状態を同期して開始
        _videoReceiver.IsPaused = _isCameraPaused;
        _videoReceiver.Start();
    }

    public void Navigate(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
        RaisePropertyChanged(nameof(IsFullScreen));

        // ギャラリーや時刻設定のときはカメラ処理を止めて軽量化する
        bool shouldPause = (viewModel is GalleryViewModel || viewModel is TimeSettingViewModel);
        IsCameraPaused = shouldPause;
    }

    public void ShutdownApplication()
    {
        _videoReceiver.Stop();
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
