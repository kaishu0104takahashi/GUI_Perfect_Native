using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using GUI_Perfect.Models;
using GUI_Perfect.Services;

namespace GUI_Perfect.ViewModels;

public class SimpleInspectViewModel : ViewModelBase
{
    public MainViewModel Main { get; }
    private readonly DatabaseService _dbService;

    private Bitmap? _capturedImage;
    public Bitmap? CapturedImage
    {
        get => _capturedImage;
        set { _capturedImage = value; RaisePropertyChanged(); }
    }

    private bool _isCaptured;
    public bool IsCaptured
    {
        get => _isCaptured;
        set { _isCaptured = value; RaisePropertyChanged(); }
    }

    private string _statusMessage = "検査対象をセットして\n撮影ボタンを押してください";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; RaisePropertyChanged(); }
    }

    public ICommand BackCommand { get; }
    public ICommand CaptureCommand { get; }
    public ICommand RetakeCommand { get; }
    public ICommand SaveCommand { get; }

    public SimpleInspectViewModel(MainViewModel main)
    {
        Main = main;
        _dbService = new DatabaseService();

        BackCommand = new RelayCommand(() => 
        {
            Main.IsCameraPaused = false;
            Main.Navigate(new HomeViewModel(Main));
        });

        // 【修正】撮影ロジックの改善
        CaptureCommand = new RelayCommand(() =>
        {
            if (Main.CameraImage != null)
            {
                // 1. 先にカメラを止める（これで映像の更新が止まる）
                Main.IsCameraPaused = true;

                // 2. 現在の映像を静止画として保持する
                CapturedImage = Main.CameraImage;
                
                // 3. 表示切り替え
                IsCaptured = true;
                
                StatusMessage = "この画像で保存しますか？";
            }
        });

        RetakeCommand = new RelayCommand(() =>
        {
            CapturedImage = null;
            IsCaptured = false;
            
            // カメラ再開
            Main.IsCameraPaused = false;
            StatusMessage = "検査対象をセットして\n撮影ボタンを押してください";
        });

        SaveCommand = new RelayCommand(async () =>
        {
            if (CapturedImage == null) return;

            StatusMessage = "保存中...";
            await Task.Delay(50); 

            try
            {
                string baseDir = "/home/shikoku-pc/pic";
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                string saveDir = Path.Combine(baseDir, timestamp);
                Directory.CreateDirectory(saveDir);

                string filePath = Path.Combine(saveDir, "simple_omote.bmp");
                CapturedImage.Save(filePath);
                
                string thumbPath = Path.Combine(saveDir, "thumb.bmp");
                CapturedImage.Save(thumbPath);

                _dbService.Initialize();
                var record = new InspectionRecord
                {
                    Date = timestamp,
                    SaveName = timestamp,
                    SaveAbsolutePath = saveDir,
                    ThumbnailPath = thumbPath,
                    Type = 0,
                    SimpleOmotePath = filePath
                };
                _dbService.InsertInspection(record);

                Main.IsCameraPaused = false;
                Main.Navigate(new HomeViewModel(Main));
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                Console.WriteLine(ex);
            }
        });
    }
}
