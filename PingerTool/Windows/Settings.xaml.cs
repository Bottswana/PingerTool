using System;
using System.Windows;
using PingerTool.Classes;
using System.Threading.Tasks;
using LukeSkywalker.IPNetwork;
using System.Collections.ObjectModel;

namespace PingerTool.Windows
{
    public partial class Settings
    {
        private SettingsModel _Model;
        private MainWindow _Window;

        #region Initialiser
        public Settings(MainWindow Window)
        {
            InitializeComponent();
            _Window = Window;

            // Configure ViewModel
            _Model = new SettingsModel()
            {
                EnableWebserver = ( _Window.Server != null ) ? true : false,
                EnableSpark = ( _Window.Spark != null ) ? true : false,
                EnableNotification = _Window.Notification,
                WarningThreshold = _Window.Timeframes[0],
                GraphHeight = _Window.Model.GraphHeight,
                PingTimeout = _Window.Timeframes[1],
                WarningNoti = _Window.Timeframes[2],
                TimeoutNoti = _Window.Timeframes[3]
            };

            // Fetch Webserver Settings
            if( _Window.Server != null )
            {
                _Model.AllowedSubnet = String.Join(",", _Window.Server.AllowedSubnets);
                _Model.Username = _Window.Server.AuthDetails[0];
                _Model.BindAddress = _Window.Server.BindAddress;
                _Model.EnableAuth = _Window.Server.AuthEnabled;
            }

            // Fetch Spark Settings
            if( _Window.Spark != null )
            {
                _Model.SparkWarnThreshold = _Window.Spark.WarningThreshold;
                _Model.SparkTimeThreshold = _Window.Spark.TimeoutThreshold;
                _Model.SparkCircles.Add(_Window.Spark.SelectedCircleId);
                Circles.SelectedItem = _Window.Spark.SelectedCircleId;
            }

            // Bind Datacontext
            DataContext = _Model;
        }
        #endregion Initialiser

        #region Window Events
        /// <summary>
        /// Event handler for clicking the Save Button
        /// </summary>
        private void _Save_Click(object sender, RoutedEventArgs e)
        {
            // Password Safety Warning
            if( _Model.EnableAuth && _Model.Username.Length > 0 && Password.Password.Length > 0 )
            {
                var Warning = "The password feature is basic and is sent and stored unencrypted\nPlease ensure you use a password that is not used elsewhere";
                if( MessageBox.Show(Warning, "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel )
                {
                    return;
                }
            }

            // Save Time interval settings
            if( _Model.TimeoutNoti < 0 ) _Model.TimeoutNoti = 0;
            if( _Model.WarningNoti < 0 ) _Model.WarningNoti = 0;
            if ( _Model.WarningThreshold < 1 || _Model.PingTimeout < 1 )
            {
                MessageBox.Show("Please enter a valid Ping Timeout or Warning Threshold value", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if( _Model.GraphHeight <= 4 )
            {
                MessageBox.Show("Graph height must be greater than 4", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _Window.Timeframes = new int[] { _Model.WarningThreshold, _Model.PingTimeout, _Model.WarningNoti, _Model.TimeoutNoti };
            _Window.Notification = _Model.EnableNotification;
            _Window.Model.GraphHeight = _Model.GraphHeight;

            // Save Spark Settings
            if( _Model.EnableSpark )
            {
                var SelectedSpark = ( Circles.SelectedItem != null ) ? (RoomData.Rooms)Circles.SelectedItem : null;
                if( SelectedSpark == null )
                {
                    MessageBox.Show("Please select a valid Spark Circle to enable Spark support", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _Window.Spark = new Spark(SelectedSpark, _Model.SparkWarnThreshold, _Model.SparkTimeThreshold);
            }

            // Validate Webserver Config
            if( _Model.EnableWebserver )
            {
                var WebPass = Password.Password;
                if( _Model.BindAddress.Length < 0 )
                {
                    MessageBox.Show("Please enter a valid Bind Address", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if( _Model.EnableAuth && _Model.Username.Length < 1 )
                {
                    MessageBox.Show("Please enter a valid Username for authentication", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if( _Model.AllowedSubnet.Length > 0 )
                {
                    var Elements = _Model.AllowedSubnet.Split(',');
                    foreach( var Element in Elements )
                    {
                        if( !IPNetwork.TryParse(Element, out IPNetwork Net) )
                        {
                            MessageBox.Show($"Entry '{Element}' is not a valid subnet.\nPlease correct the Allowed Subnets before continuing", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }
                else if( _Model.AllowedSubnet.Length < 0 )
                {
                    _Model.AllowedSubnet = "0.0.0.0/0";
                }
            }

            // Check if we need to alter the webserver configuration
            if( _Window.Server != null && !_Model.EnableWebserver )
            {
                // Webserver is running and should be stopped
                _Window.Server.Dispose();
                _Window.Server = null;
            }
            else if( _Model.EnableWebserver )
            {
                // Get Password
                var OldPass = _Window.Server?.AuthDetails[1] ?? "";
                var tPassword = ( OldPass != null && OldPass.Length > 0 && Password.Password.Length < 1 ) ? OldPass : Helpers.SHA256Hash(Password.Password);

                // Compare current config versus new config
                var AllowedSubnets = ( _Window.Server != null ) ? String.Join(",", _Window.Server?.AllowedSubnets) : "";
                if( _Window.Server == null || _Model.BindAddress != _Window.Server.BindAddress || _Model.AllowedSubnet != AllowedSubnets 
                    || _Model.EnableAuth != _Window.Server.AuthEnabled || _Model.Username != _Window.Server.AuthDetails[0] || OldPass != tPassword )
                {
                    // Server configuration changed, restart server
                    Task.Run(() =>
                    {
                        _Window.Server?.Dispose();
                        _Window.Server = new WebServer(_Model.BindAddress, _Model.AllowedSubnet, _Model.EnableAuth, _Model.Username, tPassword);
                    });
                }
            }

            // Close Window
            _Window.Proj?.TriggerSaveStatus();
            Close();
        }

        /// <summary>
        /// Sound Test Button
        /// </summary>
        private void _SoundTest_Click(object sender, RoutedEventArgs e)
        {
            _Window.Player.Play();
        }

        /// <summary>
        /// Update the Spark Circles
        /// </summary>
        private void _Update_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async() =>
            {
                var Results = await (new Spark()).GetRooms();
                Dispatcher.Invoke(() =>
                {
                    _Model.SparkCircles.Clear();
                    foreach( var Circle in Results )
                    {
                        _Model.SparkCircles.Add(Circle);
                    }
                });
            });
        }
        #endregion Window Events
    }

    class SettingsModel : ViewModel
    {
        #region Initialiser
        public SettingsModel()
        {
            SparkCircles = new ObservableCollection<RoomData.Rooms>();
        }
        #endregion Initialiser

        #region Private Properties
        private bool _EnableNotification = false;
        private bool _EnableWebserver = false;
        private bool _EnableSpark = false;
        private bool _EnableAuth = false;
        private int _SparkWarnThreshold = 5;
        private int _SparkTimeThreshold = 5;
        private int _WarningThreshold;
        private int _GraphHeight;
        private int _PingTimeout;
        private int _WarningNoti;
        private int _TimeoutNoti;

        private string _BindAddress = "0.0.0.0:8080";
        private string _AllowedSubnet = "0.0.0.0/0";
        private string _Username = "";
        #endregion Private Properties

        #region Public Properties
        /// <summary>
        /// Graph Height
        /// </summary>
        public int GraphHeight
        {
            get
            {
                return _GraphHeight;
            }
            set
            {
                if (_GraphHeight != value)
                {
                    _GraphHeight = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Threshold of failures for Timeout notification
        /// </summary>
        public int SparkTimeThreshold
        {
            get
            {
                return _SparkTimeThreshold;
            }
            set
            {
                if (_SparkTimeThreshold != value)
                {
                    _SparkTimeThreshold = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Threshold of failures for Warning notification
        /// </summary>
        public int SparkWarnThreshold
        {
            get
            {
                return _SparkWarnThreshold;
            }
            set
            {
                if (_SparkWarnThreshold != value)
                {
                    _SparkWarnThreshold = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Spark Rooms
        /// </summary>
        public ObservableCollection<RoomData.Rooms> SparkCircles { get; private set; }

        /// <summary>
        /// Enable/Disable Spark integration
        /// </summary>
        public bool EnableSpark
        {
            get
            {
                return _EnableSpark;
            }
            set
            {
                if (_EnableSpark != value)
                {
                    _EnableSpark = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Bots Name
        /// </summary>
        public string BotName
        {
            get
            {
                return SparkPrivateTokens.NAME;
            }
        }

        /// <summary>
        /// Threshold for Timeout Notification Sound
        /// </summary>
        public int TimeoutNoti
        {
            get
            {
                return _TimeoutNoti;
            }
            set
            {
                if (_TimeoutNoti != value)
                {
                    _TimeoutNoti = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Threshold for Warning Notification Sound
        /// </summary>
        public int WarningNoti
        {
            get
            {
                return _WarningNoti;
            }
            set
            {
                if (_WarningNoti != value)
                {
                    _WarningNoti = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Enable Notification Sound
        /// </summary>
        public bool EnableNotification
        {
            get
            {
                return _EnableNotification;
            }
            set
            {
                if (_EnableNotification != value)
                {
                    _EnableNotification = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Ping Timeout Value
        /// </summary>
        public int PingTimeout
        {
            get
            {
                return _PingTimeout;
            }
            set
            {
                if( _PingTimeout != value )
                {
                    _PingTimeout = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Ping Round-Trip Warning Threshold
        /// </summary>
        public int WarningThreshold
        {
            get
            {
                return _WarningThreshold;
            }
            set
            {
                if( _WarningThreshold != value )
                {
                    _WarningThreshold = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Enable Server Authentication
        /// </summary>
        public bool EnableAuth
        {
            get
            {
                return _EnableAuth;
            }
            set
            {
                if( _EnableAuth != value )
                {
                    _EnableAuth = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Enable Webserver
        /// </summary>
        public bool EnableWebserver
        {
            get
            {
                return _EnableWebserver;
            }
            set
            {
                if( _EnableWebserver != value )
                {
                    if( value == false ) EnableAuth = false;
                    _EnableWebserver = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Webserver Bind Address
        /// </summary>
        public string BindAddress
        {
            get
            {
                return _BindAddress;
            }
            set
            {
                if( _BindAddress != value )
                {
                    _BindAddress = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Allowed subnet for Webserver
        /// </summary>
        public string AllowedSubnet
        {
            get
            {
                return _AllowedSubnet;
            }
            set
            {
                if( _AllowedSubnet != value )
                {
                    _AllowedSubnet = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Username for Authentication
        /// </summary>
        public string Username
        {
            get
            {
                return _Username;
            }
            set
            {
                if( _Username != value )
                {
                    _Username = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion Public Properties
    }
}