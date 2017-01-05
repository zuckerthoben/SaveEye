using System;
using System.Windows;
using SaveEye.Events;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Windows.Media;
using System.Resources;
using System.Reflection;

namespace SaveEye
{
    /// <summary>
    /// Interaction logic for EyeScreen.xaml
    /// </summary>
    public partial class EyeScreen : Window
    {
        private DispatcherTimer LookAwayTimer; // Timer for the time you should look away from your screen
        private DispatcherTimer KeepAliveTimer; // Keeps the EyeScreen in Front

        public event EventHandler<RaiseToolTipEventArgs> RaiseToolTipEventHandler;
        public Screen ParentScreen { get; set; }
        public Color BackgroundColor { get; set; }
        private static DependencyProperty TextColorProperty = DependencyProperty.Register("_TextColor", typeof(SolidColorBrush), typeof(EyeScreen));
        private ResourceManager rm;

        #region CTOR

        /// <summary>
        /// Constructor for EyeScreen-Objects
        /// </summary>
        public EyeScreen()
        {         
            this.InitializeComponent();

            this.InitLookAwayTimer();

            this.InitKeepAliveTimer();

            this.InitLocalization();
        }

        private void InitLocalization()
        {
            this.rm = new ResourceManager("SaveEye.Properties.Resources", Assembly.GetExecutingAssembly());

            this._LookAwayTextBlock.Text = this.rm.GetString("LookAway");
            this._AutoCloseTextBlock.Text = this.rm.GetString("AutoClose");
            this._CloseButton.Content = this.rm.GetString("EarlyClose");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentScreen"></param>
        /// <param name="backgroundColor"></param>
        /// <param name="textColor"></param>
        public EyeScreen(Screen parentScreen)
        {
            this.rm = new ResourceManager("SaveEye.Properties.Resources", Assembly.GetExecutingAssembly());

            // The screen where the Window lives
            this.ParentScreen = parentScreen;

            this.InitializeComponent();
            
            this.InitKeepAliveTimer();
            
            this.InitLookAwayTimer();

            this.InitLocalization();
        }
  
        #endregion
      
        public SolidColorBrush TextColor
        {
            get => (SolidColorBrush)this.GetValue(TextColorProperty);
            set => this.SetValue(TextColorProperty, value);
        }

        private void InitKeepAliveTimer()
        {
            this.KeepAliveTimer = new DispatcherTimer();
            this.KeepAliveTimer.Tick += this.KeepAliveTimer_Tick;
            this.KeepAliveTimer.Interval = new TimeSpan(0,0,1);
            this.KeepAliveTimer.IsEnabled = true;
        }

        void KeepAliveTimer_Tick(object sender, EventArgs e) => 
            this.Topmost = true;



        /// <summary>
        /// 
        /// </summary>
        private void InitLookAwayTimer()
        {
            this.LookAwayTimer = new DispatcherTimer();
            this.LookAwayTimer.Tick += this.LookAwayTimer_Tick;
            
            this.LookAwayTimer.Interval = new TimeSpan(0,0,30); // 30 Sek
            this.LookAwayTimer.IsEnabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LookAwayTimer_Tick(object sender, EventArgs e)
        {
            if (this.RaiseToolTipEventHandler != null)
            {
                // Has Subscriber(s)
                if (this.ParentScreen == Screen.PrimaryScreen)
                {
                    // Raise only event, instead of one per screen
                    this.LookAwayTimer.Interval = new TimeSpan(0, 20 , 0);
                    this.RaiseToolTipEventHandler(this, new RaiseToolTipEventArgs(this.rm.GetString("Closed") + Environment.NewLine + this.rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString(), 5));
                }
            }
            
            this.LookAwayTimer.Stop();
            this.KeepAliveTimer.Stop();
            this.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.LookAwayTimer.Stop();
            this.KeepAliveTimer.Stop();
            this.Close();
        }
    }
}
