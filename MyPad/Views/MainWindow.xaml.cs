using Dragablz;
using MahApps.Metro.Controls;
using MyLib.Wpf;
using MyPad.Models;
using MyPad.ViewModels;
using MyPad.Views.Components;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
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

        public readonly static ICommand ActivateTerminal
            = Interactor.CreateRoutedCommand<MainWindow>(new InputGestureCollection { new KeyGesture(Key.OemTilde, ModifierKeys.Control, "Ctrl+@") });
        public readonly static ICommand ActivateProperty
            = Interactor.CreateRoutedCommand<MainWindow>(new InputGestureCollection { new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift) });
        public readonly static ICommand ActivateClipboardHistory
            = Interactor.CreateRoutedCommand<MainWindow>(new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift) });

        private User32_Gdi.MENUITEMINFO _lpmiiShowMenuBar;
        private User32_Gdi.MENUITEMINFO _lpmiiShowToolBar;
        private User32_Gdi.MENUITEMINFO _lpmiiShowSideBar;
        private User32_Gdi.MENUITEMINFO _lpmiiShowStatusBar;
        private HwndSource _handleSource;

        private TextEditor ActiveTextEditor
        {
            get
            {
                try
                {
                    // HACK: 選択されたタブ内のコントロールを取得
                    // ItemsSource を使用する (具体的には ViewModel 等をバインドする) 場合、
                    // Item に関係するあらゆるプロパティに上記の要素の参照が設定されるため、
                    // 子要素の UIElement のインスタンスを直接取得する方法が無い。(仕様)
                    // 親要素の VisualTree をたどり ContentPreseneter を取得し、内包する UIElement を要素名から探す。
                    // ただし VisualTree からは一度も描画されていないエレメントを取得できないため注意が必要。
                    var presenter = this.TabControl.GetVisualChild<ContentPresenter>(this.TabControl.SelectedIndex);
                    return presenter?.ContentTemplate?.FindName("TextEditor", presenter) as TextEditor;
                }
                catch
                {
                    return null;
                }
            }
        }

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

            this.CommandBindings.Add(new CommandBinding(ActivateTerminal, (sender, e) => this.ActivateHamburgerMenuItem(this.TerminalItem)));
            this.CommandBindings.Add(new CommandBinding(ActivateProperty, (sender, e) => this.ActivateHamburgerMenuItem(this.PropertyItem)));
            this.CommandBindings.Add(new CommandBinding(ActivateClipboardHistory, (sender, e) => this.ActivateHamburgerMenuItem(this.ClipboardItem)));

            this.DataContextChanged += this.Window_DataContextChanged;
            this.Loaded += this.Window_Loaded;
            this.Closed += this.Window_Closed;
            ((Style)this.TabControl.Resources["__TextEditor"]).Setters.Add(new EventSetter() { Event = PreviewKeyDownEvent, Handler = new KeyEventHandler(this.TextEditor_PreviewKeyDown) });
            ((Style)this.TabControl.Resources["__DragablzItem"]).Setters.Add(new EventSetter() { Event = PreviewMouseRightButtonDownEvent, Handler = new MouseButtonEventHandler(this.TabItem_PreviewMouseRightButtonDown) });
            ((Style)this.HamburgerMenu.Resources["__ClipboardItem"]).Setters.Add(new EventSetter() { Event = PreviewMouseDoubleClickEvent, Handler = new MouseButtonEventHandler(this.ClipboardItems_PreviewMouseDoubleClick) });
            this.HamburgerMenu.ItemClick += this.HamburgerMenu_ItemClick;
            this.TabControl.SelectionChanged += this.TabControl_SelectionChanged;
            this.Flyouts.Items.Cast<Flyout>().ForEach(item => item.IsOpenChanged += this.Flyout_IsOpenChanged);
            this.GoToLineInput.ValueChanged += this.GoToLineInput_ValueChanged;
            this.EncodingComboBox.SelectionChanged += this.StatusComboBox_SelectionChanged;
            this.LanguageComboBox.SelectionChanged += this.StatusComboBox_SelectionChanged;
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
            this.Dispatcher.InvokeAsync(() => this.Close(), DispatcherPriority.ApplicationIdle);
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((User32_Gdi.WindowMessage)msg)
            {
                case User32_Gdi.WindowMessage.WM_INITMENUPOPUP:
                    {
                        var settings = SettingsService.Instance.Window;
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
                        var settings = SettingsService.Instance.Window;
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
            if (this.RestorePlacement)
            {
                if (SettingsService.Instance.Window.SaveWindowPosition && SettingsService.Instance.Window.Placement.HasValue)
                {
                    var lpwndpl = SettingsService.Instance.Window.Placement.Value;
                    if (lpwndpl.showCmd == ShowWindowCommand.SW_SHOWMINIMIZED)
                        lpwndpl.showCmd = ShowWindowCommand.SW_SHOWNORMAL;
                    User32_Gdi.SetWindowPlacement(this._handleSource.Handle, ref lpwndpl);
                }
                else
                {
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // フックメソッドの解除
            this._handleSource.RemoveHook(this.WndProc);

            // 表示位置の記憶
            var settings = SettingsService.Instance.Window;
            if (settings.SaveWindowPosition && this._handleSource.IsDisposed == false)
            {
                var lpwndpl = new User32_Gdi.WINDOWPLACEMENT();
                User32_Gdi.GetWindowPlacement(this._handleSource.Handle, ref lpwndpl);
                settings.Placement = lpwndpl;
            }
        }

        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    if (this.ActiveStatusComboBox != null)
                    {
                        this.ActiveStatusComboBox.IsDropDownOpen = false;
                        e.Handled = true;
                        return;
                    }
                    break;
            }
        }

        private void HamburgerMenu_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (MouseButtonState.Released == Mouse.LeftButton &&
                MouseButtonState.Released == Mouse.RightButton &&
                MouseButtonState.Released == Mouse.MiddleButton &&
                MouseButtonState.Released == Mouse.XButton1 &&
                MouseButtonState.Released == Mouse.XButton2)
            {
                this.ActivateHamburgerMenuItem(e.ClickedItem);
            }
        }

        private void ClipboardItems_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var text = ((ListBoxItem)sender).Content.ToString();
            if (string.IsNullOrEmpty(text) == false)
                this.ActiveTextEditor?.TextArea.Selection.ReplaceSelectionWithText(text);
            this.ActiveTextEditor?.ScrollToCaret();
            this.Dispatcher.InvokeAsync(() => this.ActiveTextEditor?.Focus(), DispatcherPriority.Input);
        }

        private void TabItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.TabControl.SelectedItem = ((DragablzItem)sender).Content;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.Dispatcher.InvokeAsync(() => this.ActiveTextEditor?.Focus(), DispatcherPriority.Input);
        }

        private void Flyout_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            if ((sender as Flyout)?.IsOpen == false)
                this.Dispatcher.InvokeAsync(() => this.ActiveTextEditor?.Focus(), DispatcherPriority.Input);
        }

        private void GoToLineInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.GoToLineFlyout.IsOpen && e.NewValue.HasValue)
            {
                this.ActiveTextEditor.Line = (int)e.NewValue.Value;
                this.ActiveTextEditor.ScrollToCaret();
            }
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

        private void ActivateHamburgerMenuItem(object targetItem)
        {
            this.Dispatcher.InvokeAsync(() =>
                {
                    var isWidthChanged = double.IsNaN(this.HamburgerMenu.Width);
                    var isActivated = targetItem?.Equals(this.HamburgerMenu.Content) == true;

                    SettingsService.Instance.Window.ShowSideBar = true;
                    this.HamburgerMenu.Width =
                        isWidthChanged == false ? double.NaN :
                        isActivated ? this.HamburgerMenu.HamburgerWidth : this.HamburgerMenu.Width;
                    this.HamburgerMenu.Content = targetItem;
                    this.HamburgerMenu.IsPaneOpen = false;
                    this.HamburgerMenuColumn.Width = GridLength.Auto;
                },
                DispatcherPriority.ApplicationIdle);
        }

        public class MainWindowInterTabClient : IInterTabClient
        {
            INewTabHost<Window> IInterTabClient.GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
            {
                var viewModel = WorkspaceViewModel.Instance.AddWindow(null, false, false);
                var view = new MainWindow() { DataContext = viewModel, RestorePlacement = false };
                if (SettingsService.Instance.Window.ShowSingleTab == false)
                {
                    // HACK: IsHeaderPanelVisible = false の状態でフローティングを行うと例外が発生する現象への対策
                    // ドラッグ移動中(マウスの左ボタンが押下されている間)はタブを表示する。
                    // また既存のタブコントロールにドッキングされた場合に備え Closed イベントも監視する。
                    //
                    // Dragablz/Dragablz/TabablzControl.cs | 6311e72 on 16 Aug 2017 | Line 1330:
                    //   _dragablzItemsControl.InstigateDrag(interTabTransfer.Item, newContainer =>

                    void View_MoveEnd(object sender, EventArgs e)
                    {
                        SettingsService.Instance.Window.ShowSingleTab = false;
                        ((Window)sender).PreviewMouseLeftButtonUp -= View_MoveEnd;
                        ((Window)sender).Closed -= View_MoveEnd;
                    }
                    SettingsService.Instance.Window.ShowSingleTab = true;
                    view.PreviewMouseLeftButtonUp += View_MoveEnd;
                    view.Closed += View_MoveEnd;
                }
                return new NewTabHost<Window>(view, view.TabControl);
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
