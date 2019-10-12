using Dragablz;
using MahApps.Metro.Controls;
using MyLib.Wpf;
using MyPad.Models;
using MyPad.ViewModels;
using MyPad.Views.Components;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using Vanara.InteropServices;
using Vanara.PInvoke;

namespace MyPad.Views
{
    public partial class MainWindow : MetroWindow
    {
        private enum SystemMenuIndex
        {
            ShowMenuBar = 6,
            ShowToolBar = 7,
            ShowSideBar = 8,
            ShowStatusBar = 9,
            _Separater_ = 10,
        }

        private HwndSource _handleSource;
        private User32_Gdi.MENUITEMINFO _lpmiiShowMenuBar;
        private User32_Gdi.MENUITEMINFO _lpmiiShowToolBar;
        private User32_Gdi.MENUITEMINFO _lpmiiShowSideBar;
        private User32_Gdi.MENUITEMINFO _lpmiiShowStatusBar;

        public static readonly ICommand SwitchFocus
            = Interactor.CreateRoutedCommand<MainWindow>(new InputGestureCollection { new KeyGesture(Key.F6, ModifierKeys.None, "F6") });
        public static readonly ICommand ActivateTerminal
            = Interactor.CreateRoutedCommand<MainWindow>(new InputGestureCollection { new KeyGesture(Key.OemTilde, ModifierKeys.Control, "Ctrl+@") });
        public static readonly ICommand ActivateFileExplorer
            = Interactor.CreateRoutedCommand<MainWindow>(new InputGestureCollection { new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift) });
        public static readonly ICommand ActivateGrep
            = Interactor.CreateRoutedCommand<MainWindow>(new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift) });
        public static readonly ICommand ActivateClipboardHistory
            = Interactor.CreateRoutedCommand<MainWindow>(new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift) });
        public static readonly ICommand ActivateProperty
            = Interactor.CreateRoutedCommand<MainWindow>(new InputGestureCollection { new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift) });

        private static readonly DependencyProperty IsVisibleTerminalContentProperty
            = Interactor.RegisterDependencyProperty();

        public bool IsVisibleTerminalContent
        {
            get => (bool)this.GetValue(IsVisibleTerminalContentProperty);
            set => this.SetValue(IsVisibleTerminalContentProperty, value);
        }

        private TextEditor ActiveTextEditor
            => this.GetTextEditor(this.TextEditorTabControl.SelectedIndex);

        private ComboBox ActiveCommandBox
            => this.GetCommandBox(this.TerminalTabControl.SelectedIndex);

        private ComboBox ActiveStatusComboBox
        {
            get
            {
                if (this.EncodingComboBox.IsDropDownOpen)
                    return this.EncodingComboBox;
                else if (this.LanguageComboBox.IsDropDownOpen)
                    return this.LanguageComboBox;
                else
                    return null;
            }
        }

        public MainWindowViewModel ViewModel => this.DataContext as MainWindowViewModel;
        public IInterTabClient InterTabClient { get; } = new MainWindowInterTabClient();
        public ICSharpCode.AvalonEdit.Search.Localization Localization { get; } = new LocalizationWrapper();
        public bool RestorePlacement { get; set; } = true;

        public MainWindow()
        {
            this.InitializeComponent();

            this.CommandBindings.Add(new CommandBinding(SwitchFocus, (sender, e) => this.SwitchActiveContent()));
            this.CommandBindings.Add(new CommandBinding(ActivateTerminal, (sender, e) => this.SwitchTerminalVisibility()));
            this.CommandBindings.Add(new CommandBinding(ActivateFileExplorer, (sender, e) => this.ActivateHamburgerMenuItem(this.FileExplorerItem)));
            this.CommandBindings.Add(new CommandBinding(ActivateProperty, (sender, e) => this.ActivateHamburgerMenuItem(this.PropertyItem)));
            this.CommandBindings.Add(new CommandBinding(ActivateClipboardHistory, (sender, e) => this.ActivateHamburgerMenuItem(this.ClipboardHistoryItem)));
            this.CommandBindings.Add(new CommandBinding(ActivateGrep, (sender, e) => this.ActivateHamburgerMenuItem(this.GrepItem)));

            this.DataContextChanged += this.Window_DataContextChanged;
            this.Loaded += this.Window_Loaded;
            this.Closed += this.Window_Closed;
            ((Style)this.HamburgerMenu.Resources["__FileExplorerItem"]).Setters.Add(new EventSetter() { Event = MouseDoubleClickEvent, Handler = new MouseButtonEventHandler(this.FileExplorerItem_MouseDoubleClick) });
            ((Style)this.HamburgerMenu.Resources["__FileExplorerItem"]).Setters.Add(new EventSetter() { Event = KeyDownEvent, Handler = new KeyEventHandler(this.FileExplorerItem_KeyDown) });
            ((Style)this.HamburgerMenu.Resources["__GrepItem"]).Setters.Add(new EventSetter() { Event = MouseDoubleClickEvent, Handler = new MouseButtonEventHandler(this.GrepItem_MouseDoubleClick) });
            ((Style)this.HamburgerMenu.Resources["__ClipboardHistoryItem"]).Setters.Add(new EventSetter() { Event = MouseDoubleClickEvent, Handler = new MouseButtonEventHandler(this.ClipboardHistoryItem_MouseDoubleClick) });
            ((Style)this.TerminalTabControl.Resources["__TerminalTabItem"]).Setters.Add(new EventSetter() { Event = MouseRightButtonDownEvent, Handler = new MouseButtonEventHandler(this.TabItem_MouseRightButtonDown) });
            ((Style)this.TextEditorTabControl.Resources["__TextEditorTabItem"]).Setters.Add(new EventSetter() { Event = MouseRightButtonDownEvent, Handler = new MouseButtonEventHandler(this.TabItem_MouseRightButtonDown) });
            ((Style)this.TextEditorTabControl.Resources["__TextEditor"]).Setters.Add(new EventSetter() { Event = PreviewKeyDownEvent, Handler = new KeyEventHandler(this.TextEditor_PreviewKeyDown) });
            this.Flyouts.Items.OfType<Flyout>().ForEach(item => item.IsOpenChanged += this.Flyout_IsOpenChanged);
            this.HamburgerMenu.ItemInvoked += this.HamburgerMenu_ItemInvoked;
            this.ContentSplitter.DragCompleted += this.ContentSplitter_DragCompleted;
            this.TextEditorTabControl.SelectionChanged += this.TextEditorTabControl_SelectionChanged;
            this.TerminalTabControl.SelectionChanged += this.TerminalTabControl_SelectionChanged; ;
            this.EncodingComboBox.SelectionChanged += this.StatusComboBox_SelectionChanged;
            this.LanguageComboBox.SelectionChanged += this.StatusComboBox_SelectionChanged;
            this.GoToLineInput.ValueChanged += this.GoToLineInput_ValueChanged;
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ViewModelBase oldViewModel)
                oldViewModel.Disposed -= this.ViewModel_Disposed;
            if (e.NewValue is ViewModelBase newViewModel)
                newViewModel.Disposed += this.ViewModel_Disposed;
        }

        private void ViewModel_Disposed(object sender, EventArgs e)
        {
            ((ViewModelBase)sender).Disposed -= this.ViewModel_Disposed;
            this.Dispatcher.InvokeAsync(() => this.Close());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // フックメソッドの登録
            this._handleSource = (HwndSource)PresentationSource.FromVisual(this);
            this._handleSource.AddHook(this.WndProc);

            // システムメニューの構築
            var sequence = 0u;
            User32_Gdi.MENUITEMINFO createMenuItem(bool isSeparater = false)
                => new User32_Gdi.MENUITEMINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(User32_Gdi.MENUITEMINFO)),
                    fMask = isSeparater ? User32_Gdi.MenuItemInfoMask.MIIM_FTYPE : User32_Gdi.MenuItemInfoMask.MIIM_STATE | User32_Gdi.MenuItemInfoMask.MIIM_ID | User32_Gdi.MenuItemInfoMask.MIIM_STRING,
                    fType = isSeparater ? User32_Gdi.MenuItemType.MFT_SEPARATOR : User32_Gdi.MenuItemType.MFT_MENUBARBREAK,
                    fState = User32_Gdi.MenuItemState.MFS_ENABLED,
                    wID = ++sequence,
                    hSubMenu = IntPtr.Zero,
                    hbmpChecked = IntPtr.Zero,
                    hbmpUnchecked = IntPtr.Zero,
                    dwItemData = UIntPtr.Zero,
                    dwTypeData = new StrPtrAuto(string.Empty), // 必ず何らかの文字で初期化するように、空の場合は空文字を
                    cch = 0,
                    hbmpItem = IntPtr.Zero
                };
            var hMenu = User32_Gdi.GetSystemMenu(this._handleSource.Handle, false);
            this._lpmiiShowMenuBar = createMenuItem();
            this._lpmiiShowToolBar = createMenuItem();
            this._lpmiiShowSideBar = createMenuItem();
            this._lpmiiShowStatusBar = createMenuItem();
            var lpmiiSeparater = createMenuItem(true);
            User32_Gdi.InsertMenuItem(hMenu, (uint)SystemMenuIndex.ShowMenuBar, true, ref this._lpmiiShowMenuBar);
            User32_Gdi.InsertMenuItem(hMenu, (uint)SystemMenuIndex.ShowToolBar, true, ref this._lpmiiShowToolBar);
            User32_Gdi.InsertMenuItem(hMenu, (uint)SystemMenuIndex.ShowSideBar, true, ref this._lpmiiShowSideBar);
            User32_Gdi.InsertMenuItem(hMenu, (uint)SystemMenuIndex.ShowStatusBar, true, ref this._lpmiiShowStatusBar);
            User32_Gdi.InsertMenuItem(hMenu, (uint)SystemMenuIndex._Separater_, true, ref lpmiiSeparater);

            // 表示位置の復元
            if (this.RestorePlacement && SettingsService.Instance.System.SaveWindowPlacement && SettingsService.Instance.System.WindowPlacement.HasValue)
            {
                var lpwndpl = SettingsService.Instance.System.WindowPlacement.Value;
                if (lpwndpl.showCmd == ShowWindowCommand.SW_SHOWMINIMIZED)
                    lpwndpl.showCmd = ShowWindowCommand.SW_SHOWNORMAL;
                User32_Gdi.SetWindowPlacement(this._handleSource.Handle, ref lpwndpl);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // フックメソッドの解除
            this._handleSource.RemoveHook(this.WndProc);

            // 表示位置の記憶
            var settings = SettingsService.Instance.System;
            if (settings.SaveWindowPlacement && this._handleSource.IsDisposed == false)
            {
                var lpwndpl = new User32_Gdi.WINDOWPLACEMENT();
                User32_Gdi.GetWindowPlacement(this._handleSource.Handle, ref lpwndpl);
                settings.WindowPlacement = lpwndpl;
            }
        }

        private async void FileExplorerItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            var node = (FileNodeViewModel)((TreeViewItem)sender).DataContext;
            if (node.IsEmpty)
                return;

            if (File.Exists(node.FileName))
            {
                await this.ViewModel.LoadEditor(new[] { node.FileName });
                e.Handled = true;
            }
        }

        private void FileExplorerItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled)
                return;

            switch (e.Key)
            {
                case Key.Enter:
                    var node = (FileNodeViewModel)((TreeViewItem)sender).DataContext;
                    if (node.IsEmpty)
                    {
                        e.Handled = true;
                        return;
                    }
                    if (Directory.Exists(node.FileName))
                    {
                        node.IsExpanded = !node.IsExpanded;
                        e.Handled = true;
                        return;
                    }
                    if (File.Exists(node.FileName))
                    {
                        this.ViewModel.LoadEditor(new[] { node.FileName });
                        e.Handled = true;
                        return;
                    }
                    break;
            }
        }

        private async void GrepItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            dynamic content = ((ListBoxItem)sender).Content;
            var path = (string)content.Path;
            var line = (int)content.Line;
            await this.ViewModel.LoadEditor(new[] { path });
            this.ViewModel.ActiveEditor.Line = line;
            while (this.ActiveTextEditor == null)
                await Task.Delay(100);
            this.ActiveTextEditor.ScrollToCaret();
            e.Handled = true;
        }

        private async void ClipboardHistoryItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            var text = ((ListBoxItem)sender).Content.ToString();
            while (this.ActiveTextEditor == null)
                await Task.Delay(100);
            if (string.IsNullOrEmpty(text) == false)
                this.ActiveTextEditor.TextArea.Selection.ReplaceSelectionWithText(text);
            await this.Dispatcher.InvokeAsync(() => this.ActiveTextEditor.Focus());
            this.ActiveTextEditor.ScrollToCaret();
            e.Handled = true;
        }

        private void TabItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            ((DragablzItem)sender).GetParent<DraggableTabControl>().SelectedItem = ((DragablzItem)sender).Content;
            e.Handled = true;
        }
            
        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled)
                return;

            switch (e.Key)
            {
                case Key.Escape:
                    if (this.ActiveStatusComboBox != null)
                    {
                        this.ActiveStatusComboBox.IsDropDownOpen = false;
                        e.Handled = true;
                    }
                    return;
            }
        }

        private void Flyout_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            if ((sender as Flyout)?.IsOpen != false)
                return;

            this.Dispatcher.InvokeAsync(() => this.ActiveTextEditor?.Focus());
        }

        private void HamburgerMenu_ItemInvoked(object sender, HamburgerMenuItemInvokedEventArgs e)
        {
            if (MouseButtonState.Pressed == Mouse.LeftButton ||
                MouseButtonState.Pressed == Mouse.RightButton ||
                MouseButtonState.Pressed == Mouse.MiddleButton ||
                MouseButtonState.Pressed == Mouse.XButton1 ||
                MouseButtonState.Pressed == Mouse.XButton2)
                return;

            if (e.IsItemOptions)
            {
                // オプション項目は選択状態にならないように調整する
                this.HamburgerMenu.SelectedItem = this.HamburgerMenu.Content;
                this.HamburgerMenu.SelectedOptionsItem = null;
                this.OptionsFlyout.IsOpen = true;
            }
            else
            {
                this.ActivateHamburgerMenuItem(e.InvokedItem);
            }
        }

        private void ContentSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            // ターミナルが開かれている場合は何もしない
            if (this.TerminalContentRow.Height.Value != 0)
                return;

            this.IsVisibleTerminalContent = !this.IsVisibleTerminalContent;
            this.TerminalContentRow.Height = new GridLength(100, GridUnitType.Star);
        }

        private void TextEditorTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Handled || e.Source != e.OriginalSource)
                return;

            this.Dispatcher.InvokeAsync(() => this.ActiveTextEditor?.Focus());
            e.Handled = true;
        }

        private void TerminalTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ComboBox.SelectionChanged が伝播してくるためイベントの発生源を確認
            if (e.Handled || e.Source != e.OriginalSource)
                return;

            this.Dispatcher.InvokeAsync(() => this.ActiveCommandBox?.Focus());
            e.Handled = true;
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // HACK: コンボボックスの値変更時に非同期処理を行うコマンドを実行
            // 実行されるメソッドは非同期処理のため遅延が発生する。
            // 加えてコマンドを経由しているためメソッドの実行結果は取得できない。
            // 上記は View 層ではコンボボックスの値は常に元に戻しておくこととし、
            // 変更後の値の適用は ViewModel 層とのバインディングに一任する。

            var comboBox = (ComboBox)sender;
            if (comboBox.IsDropDownOpen == false)
                return;

            var encoding = (Encoding)this.EncodingComboBox.SelectedValue;
            var language = (string)this.LanguageComboBox.SelectedValue;

            comboBox.IsDropDownOpen = false;
            if (comboBox.Equals(this.EncodingComboBox))
                comboBox.SelectedIndex = comboBox.Items.IndexOf(this.ViewModel.ActiveEditor.Encoding);
            else if (comboBox.Equals(this.LanguageComboBox))
                comboBox.SelectedIndex = comboBox.Items.IndexOf(this.ViewModel.ActiveEditor.SyntaxDefinition?.Name);

            this.ViewModel.ReloadCommand.Execute(new Tuple<Encoding, string>(encoding, language));
        }

        private void GoToLineInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.GoToLineFlyout.IsOpen == false || e.NewValue.HasValue == false)
                return;
            this.ActiveTextEditor.ScrollToCaret();
        }

        private TextEditor GetTextEditor(int index)
        {
            try
            {
                // HACK: 選択されたタブ内のコントロールを取得
                // ItemsSource を使用する (具体的には ViewModel 等をバインドする) 場合、
                // Item に関係するあらゆるプロパティに上記の要素の参照が設定されるため、
                // 子要素の UIElement のインスタンスを直接取得する方法が無い。(仕様)
                // 親要素の VisualTree をたどり ContentPreseneter を取得し、内包する UIElement を要素名から探す。
                // ただし VisualTree からは一度も描画されていないエレメントを取得できないため注意が必要。
                var presenter = this.TextEditorTabControl.GetChild<ContentPresenter>(index);
                return presenter?.ContentTemplate?.FindName("TextEditor", presenter) as TextEditor;
            }
            catch
            {
                return null;
            }
        }

        private ComboBox GetCommandBox(int index)
        {
            try
            {
                var presenter = this.TerminalTabControl.GetChild<ContentPresenter>(index);
                return presenter?.ContentTemplate?.FindName("CommandBox", presenter) as ComboBox;
            }
            catch
            {
                return null;
            }
        }

        private void ActivateHamburgerMenuItem(object targetItem)
        {
            this.Dispatcher.InvokeAsync(() =>
                {
                    SettingsService.Instance.System.ShowSideBar = true;

                    var isOpened = double.IsNaN(this.HamburgerMenu.Width);
                    var isSelected = targetItem?.Equals(this.HamburgerMenu.Content) == true;
                    if (isOpened == false)
                    {
                        // 閉じた状態の場合
                        // ・選択された項目をアクティブにする
                        // ・メニューを開く
                        this.HamburgerMenu.Content = targetItem;
                        this.HamburgerMenu.SelectedItem = targetItem;
                        this.HamburgerMenu.Width = double.NaN;
                    }
                    else if (isSelected == false)
                    {
                        // 開いた状態かつアクティブでない項目が選択された場合
                        // ・選択された項目をアクティブにする
                        this.HamburgerMenu.Content = targetItem;
                        this.HamburgerMenu.SelectedItem = targetItem;
                    }
                    else
                    {
                        // 開いた状態かつアクティブな項目が選択された場合
                        // ・選択された項目のアクティブにする
                        // ・メニューを閉じる
                        this.HamburgerMenu.Content = null;
                        this.HamburgerMenu.SelectedItem = null;
                        this.HamburgerMenu.Width = this.HamburgerMenu.HamburgerWidth;
                    }

                    this.HamburgerMenu.IsPaneOpen = false;
                    this.HamburgerMenuColumn.Width = GridLength.Auto;
                });
        }

        private void SwitchActiveContent()
        {
            if (Keyboard.FocusedElement != this.ActiveTextEditor?.TextArea)
                this.Dispatcher.InvokeAsync(() => this.ActiveTextEditor?.Focus());
            else
                this.Dispatcher.InvokeAsync(() => this.ActiveCommandBox?.Focus());
        }

        private void SwitchTerminalVisibility()
        {
            if (Keyboard.FocusedElement != this.ActiveCommandBox?.Template.FindName("PART_EditableTextBox", this.ActiveCommandBox) as TextBox)
            {
                this.IsVisibleTerminalContent = true;
                if (this.ViewModel.Terminals.Any())
                    this.Dispatcher.InvokeAsync(() => this.ActiveCommandBox?.Focus());
                else
                    this.ViewModel.AddTerminalCommand.Execute(null);
            }
            else
            {
                this.IsVisibleTerminalContent = false;
                this.Dispatcher.InvokeAsync(() => this.ActiveTextEditor?.Focus());
            }
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((User32_Gdi.WindowMessage)msg)
            {
                case User32_Gdi.WindowMessage.WM_INITMENUPOPUP:
                {
                    var settings = SettingsService.Instance.System;
                    var hMenu = User32_Gdi.GetSystemMenu(this._handleSource.Handle, false);
                    this._lpmiiShowMenuBar.dwTypeData.Assign(Properties.Resources.Command_ShowMenuBar);
                    this._lpmiiShowToolBar.dwTypeData.Assign(Properties.Resources.Command_ShowToolBar);
                    this._lpmiiShowSideBar.dwTypeData.Assign(Properties.Resources.Command_ShowSideBar);
                    this._lpmiiShowStatusBar.dwTypeData.Assign(Properties.Resources.Command_ShowStatusBar);
                    this._lpmiiShowMenuBar.fState = settings.ShowMenuBar ? User32_Gdi.MenuItemState.MFS_CHECKED : User32_Gdi.MenuItemState.MFS_ENABLED;
                    this._lpmiiShowToolBar.fState = settings.ShowToolBar ? User32_Gdi.MenuItemState.MFS_CHECKED : User32_Gdi.MenuItemState.MFS_ENABLED;
                    this._lpmiiShowSideBar.fState = settings.ShowSideBar ? User32_Gdi.MenuItemState.MFS_CHECKED : User32_Gdi.MenuItemState.MFS_ENABLED;
                    this._lpmiiShowStatusBar.fState = settings.ShowStatusBar ? User32_Gdi.MenuItemState.MFS_CHECKED : User32_Gdi.MenuItemState.MFS_ENABLED;
                    User32_Gdi.SetMenuItemInfo(hMenu, (uint)SystemMenuIndex.ShowMenuBar, true, in this._lpmiiShowMenuBar);
                    User32_Gdi.SetMenuItemInfo(hMenu, (uint)SystemMenuIndex.ShowToolBar, true, in this._lpmiiShowToolBar);
                    User32_Gdi.SetMenuItemInfo(hMenu, (uint)SystemMenuIndex.ShowSideBar, true, in this._lpmiiShowSideBar);
                    User32_Gdi.SetMenuItemInfo(hMenu, (uint)SystemMenuIndex.ShowStatusBar, true, in this._lpmiiShowStatusBar);
                    break;
                }

                case User32_Gdi.WindowMessage.WM_SYSCOMMAND:
                {
                    var settings = SettingsService.Instance.System;
                    var wID = wParam.ToInt32();
                    if (this._lpmiiShowMenuBar.wID == wID)
                        settings.ShowMenuBar = !settings.ShowMenuBar;
                    if (this._lpmiiShowToolBar.wID == wID)
                        settings.ShowToolBar = !settings.ShowToolBar;
                    if (this._lpmiiShowSideBar.wID == wID)
                        settings.ShowSideBar = !settings.ShowSideBar;
                    if (this._lpmiiShowStatusBar.wID == wID)
                        settings.ShowStatusBar = !settings.ShowStatusBar;
                    break;
                }
            }
            return IntPtr.Zero;
        }

        public class MainWindowInterTabClient : IInterTabClient
        {
            INewTabHost<Window> IInterTabClient.GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
            {
                var viewModel = WorkspaceViewModel.Instance.AddWindow(null, false, false);
                var view = new MainWindow() { DataContext = viewModel, RestorePlacement = false };
                if (SettingsService.Instance.System.ShowSingleTab == false)
                {
                    // HACK: IsHeaderPanelVisible = false の状態でフローティングを行うと例外が発生する現象への対策
                    // ドラッグ移動中(マウスの左ボタンが押下されている間)はタブを表示する。
                    // また既存のタブコントロールにドッキングされた場合に備え Closed イベントも監視する。
                    //
                    // Dragablz/Dragablz/TabablzControl.cs | 6311e72 on 16 Aug 2017 | Line 1330:
                    //   _dragablzItemsControl.InstigateDrag(interTabTransfer.Item, newContainer =>

                    void View_MoveEnd(object sender, EventArgs e)
                    {
                        SettingsService.Instance.System.ShowSingleTab = false;
                        ((Window)sender).PreviewMouseLeftButtonUp -= View_MoveEnd;
                        ((Window)sender).Closed -= View_MoveEnd;
                    }
                    SettingsService.Instance.System.ShowSingleTab = true;
                    view.PreviewMouseLeftButtonUp += View_MoveEnd;
                    view.Closed += View_MoveEnd;
                }
                return new NewTabHost<Window>(view, view.TextEditorTabControl);
            }

            TabEmptiedResponse IInterTabClient.TabEmptiedHandler(TabablzControl tabControl, Window window)
                => TabEmptiedResponse.CloseWindowOrLayoutBranch;
        }

        public class LocalizationWrapper : ICSharpCode.AvalonEdit.Search.Localization
        {
            // スタイルに定義されない言語リソースを上書きする
            public override string NoMatchesFoundText => Properties.Resources.Message_NotifyNoMatchesText;
            public override string ErrorText => $"{Properties.Resources.Message_NotifyErrorText}: ";
        }
    }
}
