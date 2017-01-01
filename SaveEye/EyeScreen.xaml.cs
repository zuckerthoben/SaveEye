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
        private DispatcherTimer _LookAwayTimer; // Timer for the time you should look away from your screen
        private DispatcherTimer _KeepAliveTimer; // Keeps the EyeScreen in Front

        public event EventHandler<RaiseToolTipEventArgs> _RaiseToolTip;
        public Screen _ParentScreen { get; set; }
        public Color _BackgroundColor { get; set; }
        public static DependencyProperty _TextColorProperty = DependencyProperty.Register("_TextColor", typeof(SolidColorBrush), typeof(EyeScreen));
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
            rm = new ResourceManager("SaveEye.Properties.Resources", Assembly.GetExecutingAssembly());

            _LookAwayTextBlock.Text = rm.GetString("LookAway");
            _AutoCloseTextBlock.Text = rm.GetString("AutoClose");
            _CloseButton.Content = rm.GetString("EarlyClose");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentScreen"></param>
        /// <param name="backgroundColor"></param>
        /// <param name="textColor"></param>
        public EyeScreen(Screen parentScreen)
        {
            rm = new ResourceManager("SaveEye.Properties.Resources", Assembly.GetExecutingAssembly());

            // The screen where the Window lives
            this._ParentScreen = parentScreen;

            this.InitializeComponent();
            
            this.InitKeepAliveTimer();
            
            this.InitLookAwayTimer();

            this.InitLocalization();
        }
  
        #endregion
      
        public SolidColorBrush _TextColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(_TextColorProperty);
            }
            set
            {
                this.SetValue(_TextColorProperty, value);
            }
        }

        private void InitKeepAliveTimer()
        {
            this._KeepAliveTimer = new DispatcherTimer();
            this._KeepAliveTimer.Tick += _KeepAliveTimer_Tick;
            this._KeepAliveTimer.Interval = new TimeSpan(0,0,1);
            this._KeepAliveTimer.IsEnabled = true;
        }

        void _KeepAliveTimer_Tick(object sender, EventArgs e)
        {
            this.Topmost = true;
        }

       

        /// <summary>
        /// 
        /// </summary>
        private void InitLookAwayTimer()
        {
            this._LookAwayTimer = new DispatcherTimer();
            this._LookAwayTimer.Tick += _LookAwayTimer_Tick;
            
            this._LookAwayTimer.Interval = new TimeSpan(0,0,30); // 30 Sek
            this._LookAwayTimer.IsEnabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _LookAwayTimer_Tick(object sender, EventArgs e)
        {
            if (this._RaiseToolTip != null)
            {
                // Has Subscriber(s)
                if (this._ParentScreen == Screen.PrimaryScreen)
                {
                    // Raise only event, instead of one per screen
                    this._LookAwayTimer.Interval = new TimeSpan(0, 20 , 0);
                    this._RaiseToolTip(this, new RaiseToolTipEventArgs(rm.GetString("Closed") + Environment.NewLine + rm.GetString("NextExecution") + DateTime.Now.AddMinutes(20).ToShortTimeString(), 5));
                }
            }
            
            this._LookAwayTimer.Stop();
            this._KeepAliveTimer.Stop();
            this.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this._LookAwayTimer.Stop();
            this._KeepAliveTimer.Stop();
            this.Close();
        }
    }
}
