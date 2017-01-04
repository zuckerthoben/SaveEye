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
    public partial class MainWindow : Window, IDisposable
    {
        private NotifyIcon notifyIcon;
        private Icon icon;
        private string toolTip = "SaveEye";
        private DispatcherTimer triggerTImer; // Timer for the time between each trigger
        private EyeScreen eyeScreen;
        private ResourceManager rm;

        private MenuItem OpenMenuItem;
        private MenuItem EyescreenMenuItem;
        private MenuItem PauseMenuItem;
        private MenuItem ExitMenuItem;

        //public string _Settings { get; set; }
        //public string Startup { get; set; }
        //public string SaveAndClose { get; set; }
        
        
        public MainWindow()
        {
            this.DataContext = this;

            AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;

            InitializeComponent();

            this.LoadApplication();


        }

        private void LoadApplication()
        {
            this.InitLocalization();

            this.LoadSettings();

            this.icon = GetIconFromResource();

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
            var ex = (Exception) e.ExceptionObject;
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

            this.rm = new ResourceManager("SaveEye.Properties.Resources", Assembly.GetExecutingAssembly());

            this._SettingsTextBlock.Text = this.rm.GetString("Settings");
            this._StartWithWindowsCheckBox.Content = this.rm.GetString("Startup");
            this._SaveButton.Content = this.rm.GetString("SaveAndClose");
        }


        /// <summary>
        /// Load the user settings, e.g. selected Colors
        /// </summary>
        private void LoadSettings() => this._StartWithWindowsCheckBox.IsChecked = Settings.Default._StartWithWindows;

        /// <summary>
        /// Initialize the Timer that triggers the EyeScreen
        /// </summary>
        private void InitTimer()
        {
            this.triggerTImer = new DispatcherTimer();
            this.triggerTImer.Tick += this.TriggerTimer_Ticked;
            this.triggerTImer.Interval = new TimeSpan(0,20,0); // 20 Minutes
            this.triggerTImer.IsEnabled = true;

            this.toolTip = "SaveEye" + Environment.NewLine + this.rm.GetString("NextExecution") + 
                DateTime.Now.AddMinutes(20).ToShortTimeString();
        }

        /// <summary>
        /// Triggered by _TriggerTimer, the EyeScreen will be opened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TriggerTimer_Ticked(object sender, EventArgs e) => this.OpenEyeScreen();

        /// <summary>
        /// Creates an EyeScreen for each screen and shows them
        /// </summary>
        private void OpenEyeScreen()
        {
            // Get Screens
            var allScreens = Screen.AllScreens;

            foreach (var item in allScreens)
            {
                var r = item.Bounds;

                this.eyeScreen = new EyeScreen(item)
                {
                Left = r.Left,
                Top = r.Top,
                Width = r.Width,
                Height = r.Height,
                };

                if (item == Screen.PrimaryScreen)
                {
                    this.eyeScreen.RaiseToolTipEventHandler += this.EyeScreen__RaiseToolTip;
                }

                this.eyeScreen.Show();
            }
        }

        /// <summary>
        /// Show a Tooltip for the next execution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void EyeScreen__RaiseToolTip(object sender, Events.RaiseToolTipEventArgs e)
        {
            this.triggerTImer.Interval = new TimeSpan(0, 20, 0);
            this.notifyIcon.ShowBalloonTip(e.DisplayDuration, "SaveEye", e.Text, ToolTipIcon.None);
            this.notifyIcon.Text = "SaveEye" + Environment.NewLine + this.rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString();
        }
      
        #region Tray

        /// <summary>
        /// Initializes the TrayIcon
        /// </summary>
        private void InitTray()
        {
            this.notifyIcon = new NotifyIcon()
            {
                ContextMenu = new System.Windows.Forms.ContextMenu(),
                ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(),
                Icon = this.icon,
                Text = this.toolTip,
                BalloonTipText = this.toolTip,
                Visible = true
            };

            this.notifyIcon.ContextMenuStrip.Opening += this.ContextMenuStrip_Opening;
            this.notifyIcon.DoubleClick += this.NotifyIcon_DoubleClick;
        }
        
        /// <summary>
        /// Initialisize the Context Menu: Create Menu Items and add them to the Contextmenu
        /// </summary>
        private void InitContextMenu()
        {
            if (this.notifyIcon != null)
            {
                this.OpenMenuItem = new MenuItem(this.rm.GetString("Open_Button"), this.OpenClick);
                this.EyescreenMenuItem = new MenuItem(this.rm.GetString("EyeScreen_Button"), this.EyeScreenClick);
                this.PauseMenuItem = new MenuItem(this.rm.GetString("Pause_Button"), this.PauseClick);
                this.ExitMenuItem = new MenuItem(this.rm.GetString("Exit_Button"), this.ExitClick);
                
                this.notifyIcon.ContextMenu.MenuItems.Add(this.OpenMenuItem);
                this.notifyIcon.ContextMenu.MenuItems.Add(this.EyescreenMenuItem);
                this.notifyIcon.ContextMenu.MenuItems.Add(this.PauseMenuItem);
                this.notifyIcon.ContextMenu.MenuItems.Add(this.ExitMenuItem);
            }
        }

        /// <summary>
        /// Executed when you click the Pause-Button in the ContextMenu of the TrayIcon. Pauses the execution of EyeScreens
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PauseClick(object sender, System.EventArgs e)
        {
            if (this.triggerTImer.IsEnabled)
            {
                this.triggerTImer.IsEnabled = false;
                this.notifyIcon.ShowBalloonTip(5, "SaveEye", this.rm.GetString("IsPaused"), ToolTipIcon.Warning);
                this.notifyIcon.Icon = Properties.Resources.iconSmallPaused;
                this.PauseMenuItem.Text = this.rm.GetString("Resume_Button");
                this.notifyIcon.Text = this.rm.GetString("IsPaused");
                this.icon = this.notifyIcon.Icon; 
            }
            else
            {
                this.triggerTImer.Interval = new TimeSpan(0,20,0); // 20 Minutes
                this.triggerTImer.IsEnabled = true;
                this.notifyIcon.Icon = Properties.Resources.iconSmall;
                this.PauseMenuItem.Text = this.rm.GetString("Pause_Button");
                this.notifyIcon.ShowBalloonTip(3, "SaveEye", this.rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString() + " Uhr", ToolTipIcon.None);
                this.notifyIcon.Text = "SaveEye" + Environment.NewLine + this.rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString();
                this.icon = this.notifyIcon.Icon;
            }
            
        }

        /// <summary>
        /// Executed when clicked on the EyeScreen-Button 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EyeScreenClick(object sender, System.EventArgs e) => 
            this.OpenEyeScreen();

        /// <summary>
        /// Executed when clicked on the Open Button in the ContextMenu from the TrayIcon. Opens the configuration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenClick(object sender, System.EventArgs e) => 
            this.Visibility = Visibility.Visible;

        /// <summary>
        /// Executed when clicked on the Exit Button in the ContextMenu from the TrayIcon. Terminates the Application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitClick(object sender, System.EventArgs e) => 
            System.Windows.Application.Current.Shutdown();

        /// <summary>
        /// Executed when doubleclicked on the TrayIcon. Shows/Hides the MainWindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyIcon_DoubleClick(object sender, System.EventArgs e) => 
            this.ToggleWindowVisibility();

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
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();

            this.Close();
        }

        /// <summary>
        /// Shall register the application in Windows startup if checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartWithWindowsCheckBox_Checked(object sender, RoutedEventArgs e)
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
        private void StartWithWindowsCheckBox_Unchecked(object sender, RoutedEventArgs e)
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
            var registryKey = Registry.CurrentUser.OpenSubKey
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
            var registryKey = Registry.CurrentUser.OpenSubKey
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
        private Icon GetIconFromResource() => Properties.Resources.iconSmall;

        public void Dispose()
        {
            this.OpenMenuItem.Dispose();
            this.PauseMenuItem.Dispose();
            this.EyescreenMenuItem.Dispose();
            this.ExitMenuItem.Dispose();
        }

        #endregion

    }
}
