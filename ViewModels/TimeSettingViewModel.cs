using System;
using System.Diagnostics;
using System.Windows.Input;

namespace GUI_Perfect.ViewModels;

public class TimeSettingViewModel : ViewModelBase
{
    private readonly Action _onBack;
    
    // 編集中の日時データ
    private DateTime _targetDateTime;

    public string Year => _targetDateTime.Year.ToString();
    public string Month => _targetDateTime.Month.ToString("00");
    public string Day => _targetDateTime.Day.ToString("00");
    public string Hour => _targetDateTime.Hour.ToString("00");
    public string Minute => _targetDateTime.Minute.ToString("00");

    public ICommand BackCommand { get; }
    public ICommand ApplyCommand { get; }
    
    // 増減コマンド
    public ICommand IncreaseCommand { get; }
    public ICommand DecreaseCommand { get; }

    public TimeSettingViewModel(Action onBack)
    {
        _onBack = onBack;
        // 現在時刻で初期化
        _targetDateTime = DateTime.Now;

        BackCommand = new RelayCommand(_onBack);
        
        ApplyCommand = new RelayCommand(ApplyTimeSetting);

        IncreaseCommand = new LocalRelayCommand<string>(unit => UpdateTime(unit, 1));
        DecreaseCommand = new LocalRelayCommand<string>(unit => UpdateTime(unit, -1));
    }

    private void UpdateTime(string unit, int value)
    {
        try
        {
            switch (unit)
            {
                case "Year":   _targetDateTime = _targetDateTime.AddYears(value); break;
                case "Month":  _targetDateTime = _targetDateTime.AddMonths(value); break;
                case "Day":    _targetDateTime = _targetDateTime.AddDays(value); break;
                case "Hour":   _targetDateTime = _targetDateTime.AddHours(value); break;
                case "Minute": _targetDateTime = _targetDateTime.AddMinutes(value); break;
            }
            // 全プロパティ更新通知
            RaisePropertyChanged(nameof(Year));
            RaisePropertyChanged(nameof(Month));
            RaisePropertyChanged(nameof(Day));
            RaisePropertyChanged(nameof(Hour));
            RaisePropertyChanged(nameof(Minute));
        }
        catch
        {
            // 計算不能な日付等は無視
        }
    }

    private void ApplyTimeSetting()
    {
        try 
        {
            // 日時文字列を作成 (例: "2026-01-23 10:00:00")
            string timeStr = _targetDateTime.ToString("yyyy-MM-dd HH:mm:00");
            
            var psi = new ProcessStartInfo
            {
                FileName = "sudo",
                Arguments = $"date -s \"{timeStr}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
            
            _onBack();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Time Set Error: {ex.Message}");
        }
    }
}
