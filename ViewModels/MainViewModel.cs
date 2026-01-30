using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GUI_Perfect.Services;
using GUI_Perfect.Views;

namespace GUI_Perfect.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set { _currentViewModel = value; RaisePropertyChanged(); }
    }

    private bool _isFullScreen = true;
    public bool IsFullScreen
    {
        get => _isFullScreen;
        set { _isFullScreen = value; RaisePropertyChanged(); }
    }

    private readonly UdpVideoReceiver _videoReceiver;
    // TcpCommandServer は削除しました

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

    public MainViewModel()
    {
        // 映像受信開始 (Port 50000)
        _videoReceiver = new UdpVideoReceiver(50000);
        _videoReceiver.OnFrameReceived += (bmp) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                CameraImage = bmp;
            });
        };
        _videoReceiver.Start();

        // 初期画面は時刻設定から
        _currentViewModel = new TimeSettingViewModel(() =>
        {
            Navigate(new HomeViewModel(this));
        });
    }

    public void Navigate(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }

    public void ShutdownApplication()
    {
        _videoReceiver.Stop();
        
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
