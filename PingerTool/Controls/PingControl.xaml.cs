using System;
using System.Net;
using System.Timers;
using System.Windows;
using PingerTool.Classes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Net.NetworkInformation;

namespace PingerTool.Controls
{
    public partial class PingControl : UserControl, IDisposable
    {
        private bool _DisposedValue = false;
        private PingControlModel _Model;
        private bool _Running = false;
        private Timer _Timer;
        private Ping _Ping;

        #region Initialiser
        public PingControl()
        {
            InitializeComponent();
            _Timer = new Timer(1000);
            _Timer.Elapsed += _Timer_Elapsed;

            _Ping = new Ping();
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Our ViewModel isnt immediately available when the object is instanced, so set it once the event for it fires
            _Model = (PingControlModel)DataContext;
            _Timer.Start();
        }

        protected virtual void Dispose( bool disposing )
        {
            if( !_DisposedValue )
            {
                if( disposing )
                {
                    // Dispose of Managed Objects
                    _Timer.Dispose();
                    _Ping.Dispose();
                }

                _DisposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion Initialiser

        #region Window Events
        /// <summary>
        /// Main Timer Callback
        /// </summary>
        private void _Timer_Elapsed( object sender, ElapsedEventArgs e )
        {
            // TODO: trim container over x lines
            // TODO: Last Contact

            if( _Running ) return; // Only run timer callback one at a time
            try
            {
                _Running = true;
                var PingInfo = _Ping.Send(_Model.Address, 2000);
                switch( PingInfo.Status )
                {
                    case IPStatus.Success:
                         var Time = (PingInfo.RoundtripTime < 1) ? "<1ms" : $"={PingInfo.RoundtripTime}ms";
                        _Model.DisplayContents += $"Reply from {PingInfo.Address}: bytes={PingInfo.Buffer.Length} time{Time} TTL={PingInfo.Options.Ttl}\n";
                        _Model.Colour = Brushes.Green;
                        break;
                    
                    case IPStatus.DestinationHostUnreachable:
                        _Model.DisplayContents += $"Reply from {PingInfo.Address}: Destination host unreachable.\n";
                        _Model.Colour = Brushes.Maroon;
                        break;

                     case IPStatus.DestinationNetworkUnreachable:
                        _Model.DisplayContents += $"Reply from {PingInfo.Address}: Destination net unreachable.\n";
                        _Model.Colour = Brushes.Maroon;
                        break;
                       
                    case IPStatus.TimedOut:
                        _Model.DisplayContents += $"Request Timed Out.\n";
                        _Model.Colour = Brushes.Maroon;
                        break;

                    default:
                        _Model.DisplayContents += $"Unknown message type: {PingInfo.Status.ToString()}\n";
                        _Model.Colour = Brushes.Maroon;
                        break;
                }

                _Running = false;
                return;
            }
            catch( Exception Ex )
            {
                _Model.DisplayContents += $"PING: transmit failed. General failure.\n";
                App.GetApp().Log.Debug(Ex, "Ping Transmit Failed");
                _Model.Colour = Brushes.Maroon;
                _Running = false;
            }
        }

        /// <summary>
        /// Event for Unloading Control
        /// </summary>
		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			App.GetApp().Log.Debug("PingControl is being disposed");
			Dispose();
		}
        #endregion Window Events
    }

    public class PingControlModel : ViewModel
    {
        #region Private Properties
        private string _LastContact = "Just Now";
        private Brush _Colour = Brushes.Gray;

        private string _DisplayContents;
        private string _DisplayName;
        private IPAddress _Address;
        #endregion Private Properties

        #region Public Properties
        public string DisplayContents
        {
            get
            {
                return _DisplayContents;
            }
            set
            {
                if( _DisplayContents != value )
                {
                    _DisplayContents = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string DisplayName
        {
            get
            {
                return _DisplayName;
            }
            set
            {
                if( _DisplayName != value )
                {
                    _DisplayName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string LastContact
        {
            get
            {
                return _LastContact;
            }
            set
            {
                if( _LastContact != value )
                {
                    _LastContact = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public IPAddress Address
        {
            get
            {
                return _Address;
            }
            set
            {
                if( _Address != value )
                {
                    _Address = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Brush Colour
        {
            get
            {
                return _Colour;
            }
            set
            {
                if( _Colour != value )
                {
                    _Colour = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion Public Properties
    }
}