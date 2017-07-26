using System;
using System.Windows;
using PingerTool.Classes;
using LukeSkywalker.IPNetwork;

namespace PingerTool.Windows
{
    public partial class Settings
    {
        private SettingsModel _Model;
        private MainWindow _Window;

        #region Initialiser
        public Settings( MainWindow Window )
        {
            InitializeComponent();
            _Window = Window;

            // Configure ViewModel
            _Model = new SettingsModel()
            {
                EnableWebserver = ( _Window.Server != null ) ? true : false,
                WarningThreshold = App.GetApp().WarningTimeframe,
                PingTimeout = App.GetApp().TimeoutValue
            };

            if( _Window.Server != null )
            {
                _Model.AllowedSubnet = String.Join(",", _Window.Server.AllowedSubnets);
                _Model.Username = _Window.Server.AuthDetails[0];
                _Model.BindAddress = _Window.Server.BindAddress;
                _Model.EnableAuth = _Window.Server.AuthEnabled;
            }

            DataContext = _Model;
        }
        #endregion Initialiser

        #region Window Events
        /// <summary>
        /// Event handler for clicking the Save Button
        /// </summary>
        private void _Save_Click( object sender, RoutedEventArgs e )
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

            // Save General Settings
            if( _Model.WarningThreshold < 1 || _Model.PingTimeout < 1 )
            {
                MessageBox.Show("Please enter a valid Ping Timeout or Warning Threshold value", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var Ref = App.GetApp();
            Ref.WarningTimeframe = _Model.WarningThreshold;
            Ref.TimeoutValue = _Model.PingTimeout;

            // Stop Webserver (If Running)
            var OldPassword = "";
            if( _Window.Server != null )
            {
                OldPassword = _Window.Server.AuthDetails[1];
                _Window.Server.Dispose();
                _Window.Server = null;
            }
            
            // Save Webserver Settings
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

                // Start Webserver
                var tPassword = ( OldPassword != "" && WebPass.Length < 1 ) ? OldPassword : Helpers.SHA256Hash(WebPass);
                _Window.Server = new WebServer(_Model.BindAddress, _Model.AllowedSubnet, _Model.EnableAuth, _Model.Username, tPassword);
            }

            // Close Window
            _Window.Proj?.TriggerSaveStatus();
            Close();
        }

        /// <summary>
        /// Event handler for clicking the Discard Button
        /// </summary>
        private void _Discard_Click( object sender, RoutedEventArgs e )
        {
            Close();
        }
        #endregion Window Events
    }

    class SettingsModel : ViewModel
    {
        #region Private Properties
        private bool _EnableWebserver = false;
        private bool _EnableAuth = false;
        private int _WarningThreshold;
        private int _PingTimeout;

        private string _BindAddress = "0.0.0.0:8080";
        private string _AllowedSubnet = "0.0.0.0/0";
        private string _Username = "";
        #endregion Private Properties

        #region Public Properties
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