using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using GUI_Perfect.ViewModels;
using System.Linq;

namespace GUI_Perfect.Views
{
    public partial class GalleryView : UserControl
    {
        public GalleryView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // テキストボックスにフォーカスが当たったとき、自動でキーボードを表示
        public void OnSearchBoxFocused(object? sender, GotFocusEventArgs e)
        {
            if (DataContext is GalleryViewModel vm)
            {
                // すでに開いていなければ開くコマンドを実行
                if (!vm.IsSearchKeyboardVisible)
                {
                    vm.OpenSearchKeyboardCommand.Execute(null);
                }
            }
        }

        // 画面のどこかがクリック（タップ）されたとき
        public void OnBackgroundClicked(object? sender, PointerPressedEventArgs e)
        {
            var source = e.Source as Visual;
            if (source == null) return;

            // 名前でコントロールを探す
            var searchBox = this.FindControl<TextBox>("SearchTextBox");
            var keyboardContainer = this.FindControl<Grid>("KeyboardContainer");

            // クリックされたのが「テキストボックス」そのもの、または「キーボードエリア」の中なら何もしない
            // (キーボード操作中や入力開始操作だから)
            bool isTextBox = IsChildOf(source, searchBox);
            bool isKeyboard = IsChildOf(source, keyboardContainer);

            if (!isTextBox && !isKeyboard)
            {
                // それ以外（背景や他の場所）を触ったら、フォーカスを外してキーボードを閉じる
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.FocusManager != null)
                {
                    topLevel.FocusManager.ClearFocus();
                }

                if (DataContext is GalleryViewModel vm)
                {
                    if (vm.IsSearchKeyboardVisible)
                    {
                        vm.CloseSearchKeyboardCommand.Execute(null);
                    }
                }
            }
        }

        // sourceがtargetの子孫（あるいは本人）かどうかを判定するヘルパー
        private bool IsChildOf(Visual? source, Visual? target)
        {
            if (source == null || target == null) return false;
            
            return target.IsVisualAncestorOf(source) || source == target;
        }
    }
}
