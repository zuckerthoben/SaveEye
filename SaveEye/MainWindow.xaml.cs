using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.Timers;
using System;
using Settings = SaveEye.Properties.Settings;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Resources;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace SaveEye
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon _NotifyIcon;
        private Icon _Icon;
        private string _ToolTip = "SaveEye";
        private DispatcherTimer _TriggerTimer; // Timer for the time between each trigger
        private EyeScreen eyeScreen;

        private MenuItem _Open;
        private MenuItem _EyeScreen;
        private MenuItem _Pause;
        private MenuItem _Exit;

        public string _Settings { get; set; }
        public string _Startup { get; set; }
        public string _SaveAndClose { get; set; }
        
        ResourceManager rm;
        
        public MainWindow()
        {
            this.DataContext = this;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            InitializeComponent();

            this.InitLocalization();

            this.LoadSettings();

            this._Icon = GetIconFromResource();

            this.InitTimer();

            this.InitTimer();

            this.InitTray();
            
            this.InitContextMenu();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception) e.ExceptionObject;
            System.Windows.MessageBox.Show(ex.Message);
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitLocalization()
        {

            if (CultureInfo.CurrentUICulture.Name != "de-DE")
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en-US");
            }
            else
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("de-DE");
            }
           
            rm = new ResourceManager("SaveEye.Properties.Resources", Assembly.GetExecutingAssembly());

            _SettingsTextBlock.Text = rm.GetString("Settings");
            _StartWithWindowsCheckBox.Content = rm.GetString("Startup");
            _SaveButton.Content = rm.GetString("SaveAndClose");
        }
        

        /// <summary>
        /// Load the user settings, e.g. selected Colors
        /// </summary>
        private void LoadSettings()
        {
          
            this._StartWithWindowsCheckBox.IsChecked = Settings.Default._StartWithWindows;
        }

        /// <summary>
        /// Initialize the Timer that triggers the EyeScreen
        /// </summary>
        private void InitTimer()
        {
            this._TriggerTimer = new DispatcherTimer();
            this._TriggerTimer.Tick += _TriggerTimer_Ticked;
            this._TriggerTimer.Interval = new TimeSpan(0,20,0); // 20 Minutes
            this._TriggerTimer.IsEnabled = true;
            this._ToolTip = "SaveEye" + Environment.NewLine + rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString();

        }

        /// <summary>
        /// Triggered by _TriggerTimer, the EyeScreen will be opened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _TriggerTimer_Ticked(object sender, EventArgs e)
        {
            this.OpenEyeScreen();
        }

        /// <summary>
        /// Creates an EyeScreen for each screen and shows them
        /// </summary>
        private void OpenEyeScreen()
        {
            // Get Screens
            Screen[] allScreens = Screen.AllScreens;

            foreach (var item in allScreens)
            {
                
                eyeScreen = new EyeScreen(item);
                if (item == Screen.PrimaryScreen)
                {
                    // Raise only one event instead of one per screen
                    eyeScreen._RaiseToolTip += eyeScreen__RaiseToolTip;   
                }

                // Set Window to FullScreen
                Rectangle r = item.Bounds;
                eyeScreen.Left = r.Left;
                eyeScreen.Top = r.Top;
                eyeScreen.Width = r.Width;
                eyeScreen.Height = r.Height;
                eyeScreen.Show();
            }
        }

        /// <summary>
        /// Show a Tooltip for the next execution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void eyeScreen__RaiseToolTip(object sender, Events.RaiseToolTipEventArgs e)
        {
            this._TriggerTimer.Interval = new TimeSpan(0, 20, 0);
            this._NotifyIcon.ShowBalloonTip(e._DisplayDuration, "SaveEye", e._Text, ToolTipIcon.None);
            this._NotifyIcon.Text = "SaveEye" + Environment.NewLine + rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString();
        }
      
        #region Tray

        /// <summary>
        /// Initializes the TrayIcon
        /// </summary>
        private void InitTray()
        {
            this._NotifyIcon = new NotifyIcon()
            {
                ContextMenu = new System.Windows.Forms.ContextMenu(),
                ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(),
                Icon = this._Icon,
                Text = this._ToolTip,
                BalloonTipText = this._ToolTip,
                Visible = true
            };

            this._NotifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
            this._NotifyIcon.DoubleClick += _NotifyIcon_DoubleClick;
        }
        
        /// <summary>
        /// Initialisize the Context Menu: Create Menu Items and add them to the Contextmenu
        /// </summary>
        private void InitContextMenu()
        {
            if (this._NotifyIcon != null)
            {
                this._Open = new MenuItem(rm.GetString("Open_Button"), OpenClick);
                this._EyeScreen = new MenuItem(rm.GetString("EyeScreen_Button"), EyeScreenClick);
                this._Pause = new MenuItem(rm.GetString("Pause_Button"), PauseClick);
                this._Exit = new MenuItem(rm.GetString("Exit_Button"), ExitClick);
                
                this._NotifyIcon.ContextMenu.MenuItems.Add(this._Open);
                this._NotifyIcon.ContextMenu.MenuItems.Add(this._EyeScreen);
                this._NotifyIcon.ContextMenu.MenuItems.Add(this._Pause);
                this._NotifyIcon.ContextMenu.MenuItems.Add(this._Exit);
            }
        }

        /// <summary>
        /// Executed when you click the Pause-Button in the ContextMenu of the TrayIcon. Pauses the execution of EyeScreens
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PauseClick(object sender, System.EventArgs e)
        {
            if (this._TriggerTimer.IsEnabled)
            {
                this._TriggerTimer.IsEnabled = false;
                this._NotifyIcon.ShowBalloonTip(5, "SaveEye", rm.GetString("IsPaused"), ToolTipIcon.Warning);
                this._NotifyIcon.Icon = SaveEye.Properties.Resources.iconSmallPaused;
                this._Pause.Text = rm.GetString("Resume_Button");
                this._NotifyIcon.Text = rm.GetString("IsPaused");
                this._Icon = this._NotifyIcon.Icon; 
            }
            else
            {
                this._TriggerTimer.Interval = new TimeSpan(0,20,0); // 20 Minutes
                this._TriggerTimer.IsEnabled = true;
                this._NotifyIcon.Icon = SaveEye.Properties.Resources.iconSmall;
                this._Pause.Text = rm.GetString("Pause_Button");
                this._NotifyIcon.ShowBalloonTip(3, "SaveEye", rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString() + " Uhr", ToolTipIcon.None);
                this._NotifyIcon.Text = "SaveEye" + Environment.NewLine + rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString();
                this._Icon = this._NotifyIcon.Icon;
            }
            
        }

        /// <summary>
        /// Executed when clicked on the EyeScreen-Button 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EyeScreenClick(object sender, System.EventArgs e)
        {
            this.OpenEyeScreen();
        }

        /// <summary>
        /// Executed when clicked on the Open Button in the ContextMenu from the TrayIcon. Opens the configuration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenClick(object sender, System.EventArgs e)
        {
            this.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// Executed when clicked on the Exit Button in the ContextMenu from the TrayIcon. Terminates the Application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitClick(object sender, System.EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Executed when doubleclicked on the TrayIcon. Shows/Hides the MainWindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _NotifyIcon_DoubleClick(object sender, System.EventArgs e)
        {
            ToggleWindowVisibility();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        #endregion

        #region GUI

        private void ToggleWindowVisibility()
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                this.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                this.Visibility = System.Windows.Visibility.Visible;
                this.Topmost = true;
            }
        }

        #endregion

        /// <summary>
        /// Hides the Window instead of Closing it. Closing would terminate the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = System.Windows.Visibility.Hidden;
        }

        /// <summary>
        /// Executed when the Save-Button on the Configuration screen is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveEye.Properties.Settings.Default.Save();

            this.Close();
        }

        /// <summary>
        /// Shall register the application in Windows startup if checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _StartWithWindowsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (this._StartWithWindowsCheckBox.IsChecked == true)
            {
                RegisterInStartup(true);
                Settings.Default._StartWithWindows = true;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// Shall remove the application from Windows startup if unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _StartWithWindowsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this._StartWithWindowsCheckBox.IsChecked == false)
            {
                RegisterInStartup(false);
                Settings.Default._StartWithWindows = false;
                Settings.Default.Save();

            }
        }

        #region util

        /// <summary>
        /// Put Quotes at the beginning and the end of a string to 
        /// </summary>
        /// <param name="input">The string without quotes</param>
        /// <returns>The string with quotes</returns>
        private string Quoterize(string input)
        {
            string output;

            // If the string already has quotes somewhere, remove them. No double, triple, quadruple (and so on) quoting possible then
            input.Replace(@"""", "");
            
            output = @"""" + input + @"""";

            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool CheckStartup()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                 ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (registryKey.GetValue("SaveEye") == null)
            {
                return false;
            }
            else if (registryKey.GetValue("SaveEye").ToString() == Quoterize(System.Windows.Forms.Application.ExecutablePath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Registers the Application in Windows Startup
        /// </summary>
        /// <param name="isChecked"></param>
        private void RegisterInStartup(bool isChecked)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (isChecked)
            {
                if (!CheckStartup())
                {
                    registryKey.SetValue("SaveEye", Quoterize(System.Windows.Forms.Application.ExecutablePath)); 
                }
                
            }
            else
            {
                if (CheckStartup())
                {
                    registryKey.DeleteValue("SaveEye");    
                }
            }
        }

        /// <summary>
        /// Get the Icon from the Resources
        /// </summary>
        /// <returns>The Icon</returns>
        private Icon GetIconFromResource()
        {
            Icon icon = SaveEye.Properties.Resources.iconSmall;

            return icon;
        }

        #endregion

    }
}
