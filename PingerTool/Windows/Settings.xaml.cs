using System;
using System.Net;
using System.Windows;
using PingerTool.Classes;

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
            _Model = new SettingsModel()
            {
                WarningThreshold = App.GetApp().WarningTimeframe,
                PingTimeout = App.GetApp().TimeoutValue

            };

            DataContext = _Model;
            _Window = Window;
		}
        #endregion Initialiser

		#region Window Events
        /// <summary>
        /// Event handler for clicking the Save Button
        /// </summary>
		private void _Save_Click(object sender, RoutedEventArgs e)
		{
            // Save General Settings
            if( _Model.WarningThreshold < 1 || _Model.PingTimeout < 1 )
            {
                MessageBox.Show("Please enter a valid Ping Timeout or Warning Threshold value", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var Ref = App.GetApp();
            Ref.WarningTimeframe = _Model.WarningThreshold;
            Ref.TimeoutValue = _Model.PingTimeout;
            
            // Save Webserver Settings


            // Close Window
            _Window.Proj?.TriggerSaveStatus();
            Close();
		}

		/// <summary>
		/// Event handler for clicking the Discard Button
		/// </summary>
		private void _Discard_Click(object sender, RoutedEventArgs e)
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