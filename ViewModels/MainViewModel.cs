using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using GUI_Perfect.Services;

namespace GUI_Perfect.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set { _currentViewModel = value; RaisePropertyChanged(); UpdateViewStatus(value); }
    }

    // 古い判定フラグ（互換性のため残すが、レイアウト判定には下を使う）
    private bool _isGalleryActive = false;
    public bool IsGalleryActive
    {
        get => _isGalleryActive;
        set { _isGalleryActive = value; RaisePropertyChanged(); }
    }

    // 【新規】レイアウト制御用フラグ（これがTrueならカメラを消して全画面にする）
    private bool _isFullScreen = false;
    public bool IsFullScreen
    {
        get => _isFullScreen;
        set { _isFullScreen = value; RaisePropertyChanged(); }
    }

    private Bitmap? _cameraImage;
    public Bitmap? CameraImage
    {
        get => _cameraImage;
        set { _cameraImage = value; RaisePropertyChanged(); }
    }

    private bool _isCameraPaused = false;
    public bool IsCameraPaused
    {
        get => _isCameraPaused;
        set { _isCameraPaused = value; RaisePropertyChanged(); }
    }

    private readonly UdpVideoReceiver _udpReceiver;

    public MainViewModel()
    {
        _udpReceiver = new UdpVideoReceiver(50000);

        _udpReceiver.OnFrameReady = (bitmap) =>
        {
            if (!IsCameraPaused)
            {
                CameraImage = bitmap;
            }
        };

        // 起動時は時刻設定（全画面）→ 完了後にホーム（分割画面）＆カメラ開始
        _currentViewModel = new TimeSettingViewModel(() =>
        {
            _udpReceiver.Start();
            Navigate(new HomeViewModel(this));
        });
        
        // 初期状態のレイアウト更新
        UpdateViewStatus(_currentViewModel);
    }

    public void Navigate(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }

    private void UpdateViewStatus(ViewModelBase viewModel)
    {
        IsGalleryActive = viewModel is GalleryViewModel;
        
        // 【変更点】ギャラリー または 時刻設定なら「全画面モード」にする
        IsFullScreen = (viewModel is GalleryViewModel) || (viewModel is TimeSettingViewModel);

        IsCameraPaused = false;
    }

    public void ShutdownApplication()
    {
        _udpReceiver.Stop();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
