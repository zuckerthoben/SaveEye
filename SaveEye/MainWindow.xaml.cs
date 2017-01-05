using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System;
using System.ComponentModel;
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
        private NotifyIcon _notifyIcon;
        private Icon _icon;
        private string _toolTip = "SaveEye";
        private DispatcherTimer _triggerTImer; // Timer for the time between each trigger
        private EyeScreen _eyeScreen;
        private ResourceManager _rm;

        private MenuItem _openMenuItem;
        private MenuItem _eyeScreenMenuItem;
        private MenuItem _pauseMenuItem;
        private MenuItem _exitMenuItem;

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

            this._icon = GetIconFromResource();

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

            this._rm = new ResourceManager("SaveEye.Properties.Resources", Assembly.GetExecutingAssembly());

            this._SettingsTextBlock.Text = this._rm.GetString("Settings");
            this._StartWithWindowsCheckBox.Content = this._rm.GetString("Startup");
            this._SaveButton.Content = this._rm.GetString("SaveAndClose");
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
            this._triggerTImer = new DispatcherTimer();
            this._triggerTImer.Tick += this.TriggerTimer_Ticked;
            this._triggerTImer.Interval = new TimeSpan(0,20,0); // 20 Minutes
            this._triggerTImer.IsEnabled = true;

            this._toolTip = "SaveEye" + Environment.NewLine + this._rm.GetString("NextExecution") + 
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

                this._eyeScreen = new EyeScreen(item)
                {
                Left = r.Left,
                Top = r.Top,
                Width = r.Width,
                Height = r.Height,
                };

                if (item == Screen.PrimaryScreen)
                {
                    this._eyeScreen.RaiseToolTipEventHandler += this.EyeScreen__RaiseToolTip;
                }

                this._eyeScreen.Show();
            }
        }

        /// <summary>
        /// Show a Tooltip for the next execution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void EyeScreen__RaiseToolTip(object sender, Events.RaiseToolTipEventArgs e)
        {
            this._triggerTImer.Interval = new TimeSpan(0, 20, 0);
            this._notifyIcon.ShowBalloonTip(e.DisplayDuration, "SaveEye", e.Text, ToolTipIcon.None);
            this._notifyIcon.Text = "SaveEye" + Environment.NewLine + this._rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString();
        }
      
        #region Tray

        /// <summary>
        /// Initializes the TrayIcon
        /// </summary>
        private void InitTray()
        {
            this._notifyIcon = new NotifyIcon()
            {
                ContextMenu = new ContextMenu(),
                ContextMenuStrip = new ContextMenuStrip(),
                Icon = this._icon,
                Text = this._toolTip,
                BalloonTipText = this._toolTip,
                Visible = true
            };

            this._notifyIcon.ContextMenuStrip.Opening += this.ContextMenuStrip_Opening;
            this._notifyIcon.DoubleClick += this.NotifyIcon_DoubleClick;
        }
        
        /// <summary>
        /// Initialisize the Context Menu: Create Menu Items and add them to the Contextmenu
        /// </summary>
        private void InitContextMenu()
        {
            if (this._notifyIcon != null)
            {
                this._openMenuItem = new MenuItem(this._rm.GetString("Open_Button"), this.OpenClick);
                this._eyeScreenMenuItem = new MenuItem(this._rm.GetString("EyeScreen_Button"), this.EyeScreenClick);
                this._pauseMenuItem = new MenuItem(this._rm.GetString("Pause_Button"), this.PauseClick);
                this._exitMenuItem = new MenuItem(this._rm.GetString("Exit_Button"), this.ExitClick);
                
                this._notifyIcon.ContextMenu.MenuItems.Add(this._openMenuItem);
                this._notifyIcon.ContextMenu.MenuItems.Add(this._eyeScreenMenuItem);
                this._notifyIcon.ContextMenu.MenuItems.Add(this._pauseMenuItem);
                this._notifyIcon.ContextMenu.MenuItems.Add(this._exitMenuItem);
            }
        }

        /// <summary>
        /// Executed when you click the Pause-Button in the ContextMenu of the TrayIcon. Pauses the execution of EyeScreens
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PauseClick(object sender, System.EventArgs e)
        {
            if (this._triggerTImer.IsEnabled)
            {
                this._triggerTImer.IsEnabled = false;
                this._notifyIcon.ShowBalloonTip(5, "SaveEye", this._rm.GetString("IsPaused"), ToolTipIcon.Warning);
                this._notifyIcon.Icon = Properties.Resources.iconSmallPaused;
                this._pauseMenuItem.Text = this._rm.GetString("Resume_Button");
                this._notifyIcon.Text = this._rm.GetString("IsPaused");
                this._icon = this._notifyIcon.Icon; 
            }
            else
            {
                this._triggerTImer.Interval = new TimeSpan(0,20,0); // 20 Minutes
                this._triggerTImer.IsEnabled = true;
                this._notifyIcon.Icon = Properties.Resources.iconSmall;
                this._pauseMenuItem.Text = this._rm.GetString("Pause_Button");
                this._notifyIcon.ShowBalloonTip(3, "SaveEye", this._rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString() + " Uhr", ToolTipIcon.None);
                this._notifyIcon.Text = "SaveEye" + Environment.NewLine + this._rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString();
                this._icon = this._notifyIcon.Icon;
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
        private void OpenClick(object sender, EventArgs e) => 
            this.Visibility = Visibility.Visible;

        /// <summary>
        /// Executed when clicked on the Exit Button in the ContextMenu from the TrayIcon. Terminates the Application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitClick(object sender, EventArgs e) => 
            System.Windows.Application.Current.Shutdown();

        /// <summary>
        /// Executed when doubleclicked on the TrayIcon. Shows/Hides the MainWindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyIcon_DoubleClick(object sender, EventArgs e) => 
            this.ToggleWindowVisibility();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenuStrip_Opening(object sender, CancelEventArgs e)
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
        private void Window_Closing(object sender, CancelEventArgs e)
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
        private static string Quoterize(string input)
        {
            string output;

            // If the string already has quotes somewhere, remove them. No double, triple, quadruple (and so on) quoting possible then
            input = input.Replace(@"""", "");
            
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
        private static Icon GetIconFromResource() => Properties.Resources.iconSmall;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MainWindow()
        {
            // Finalizer calls Dispose(false)  
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)  
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources  
                if (this._openMenuItem != null)
                {
                    this._openMenuItem.Dispose();
                    this._openMenuItem = null;
                }

                if (this._pauseMenuItem != null)
                {
                    this._pauseMenuItem.Dispose();
                    this._pauseMenuItem = null;
                }

                if (this._eyeScreenMenuItem != null)
                {
                    this._eyeScreenMenuItem.Dispose();
                    this._eyeScreenMenuItem = null;
                }

                if (this._exitMenuItem != null)
                {
                    this._exitMenuItem.Dispose();
                    this._exitMenuItem = null;
                }
            }

            //// free native resources if there are any.  
            //if (nativeResource != IntPtr.Zero)
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}
        }

        #endregion
    }
}
