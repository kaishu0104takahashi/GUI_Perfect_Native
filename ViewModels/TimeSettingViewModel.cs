using System;
using System.Diagnostics;
using System.Windows.Input;

namespace GUI_Perfect.ViewModels
{
    public class TimeSettingViewModel : ViewModelBase
    {
        private readonly Action _onComplete;

        // --- 手動プロパティ定義 ---
        private int _year;
        public int Year { get => _year; set { _year = value; RaisePropertyChanged(); } }

        private int _month;
        public int Month { get => _month; set { _month = value; RaisePropertyChanged(); } }

        private int _day;
        public int Day { get => _day; set { _day = value; RaisePropertyChanged(); } }

        private int _hour;
        public int Hour { get => _hour; set { _hour = value; RaisePropertyChanged(); } }

        private int _minute;
        public int Minute { get => _minute; set { _minute = value; RaisePropertyChanged(); } }

        // --- コマンド定義 ---
        public ICommand AddYearCommand { get; }
        public ICommand AddMonthCommand { get; }
        public ICommand AddDayCommand { get; }
        public ICommand AddHourCommand { get; }
        public ICommand AddMinuteCommand { get; }
        public ICommand ApplyAndStartCommand { get; }

        public TimeSettingViewModel(Action onComplete)
        {
            _onComplete = onComplete;
            
            var now = DateTime.Now;
            Year = now.Year;
            Month = now.Month;
            Day = now.Day;
            Hour = now.Hour;
            Minute = now.Minute;

            // 文字列引数を int に変換して渡す
            AddYearCommand = new LocalRelayCommand<string>(s => AddYear(int.Parse(s)));
            AddMonthCommand = new LocalRelayCommand<string>(s => AddMonth(int.Parse(s)));
            AddDayCommand = new LocalRelayCommand<string>(s => AddDay(int.Parse(s)));
            AddHourCommand = new LocalRelayCommand<string>(s => AddHour(int.Parse(s)));
            AddMinuteCommand = new LocalRelayCommand<string>(s => AddMinute(int.Parse(s)));
            
            // 引数を使わないコマンド
            ApplyAndStartCommand = new LocalRelayCommand<object>(_ => ApplyAndStart());
        }

        public void AddYear(int amount) { Year += amount; }
        public void AddMonth(int amount) { var d = new DateTime(Year, Month, 1).AddMonths(amount); Year = d.Year; Month = d.Month; }
        public void AddDay(int amount) { try { var d = new DateTime(Year, Month, Day).AddDays(amount); Year = d.Year; Month = d.Month; Day = d.Day; } catch { } }
        public void AddHour(int amount) { var d = new DateTime(Year, Month, Day, Hour, Minute, 0).AddHours(amount); Day = d.Day; Hour = d.Hour; }
        public void AddMinute(int amount) { var d = new DateTime(Year, Month, Day, Hour, Minute, 0).AddMinutes(amount); Hour = d.Hour; Minute = d.Minute; }

        public void ApplyAndStart()
        {
            try
            {
                var targetTime = new DateTime(Year, Month, Day, Hour, Minute, 0);
                var timeStr = targetTime.ToString("yyyy-MM-dd HH:mm:ss");
                
                var psi = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"date -s \"{timeStr}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Time set error: {ex.Message}");
            }

            _onComplete?.Invoke();
        }
    }

    // --- 警告対策済みのコマンドクラス ---
    public class LocalRelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        
        public LocalRelayCommand(Action<T> execute) => _execute = execute;

        // 警告 CS0067 対策: 使わないイベントは空の add/remove にする
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            // 警告 CS8604/CS8600 対策: Null安全性を確保
            try {
                if (parameter == null) 
                {
                    // Tがnull許容ならnullを渡す
                    if (default(T) == null) _execute(default!);
                    return;
                }

                if (typeof(T) == typeof(string))
                {
                    _execute((T)(object)parameter.ToString()!);
                }
                else 
                {
                    var converted = Convert.ChangeType(parameter, typeof(T));
                    _execute((T)converted!);
                }
            } catch { 
                // 変換失敗時は安全に無視するかデフォルト値を渡す
                _execute(default!);
            }
        }
    }
}
