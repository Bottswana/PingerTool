using System;
using System.Net;
using System.Timers;
using System.Windows;
using FontAwesome.WPF;
using System.Net.Sockets;
using PingerTool.Classes;
using PingerTool.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Net.NetworkInformation;

namespace PingerTool.Controls
{
    public partial class PingControl : UserControl, IDisposable
    {
        private static int _MaxContainerLines = 50;
        private bool _DisposedValue = false;
        private bool _Running = false;
        private long _Count = 1;

        private PingControlModel _Model;
        private Timer _Timer;
        private App _AppRef;
        private Ping _Ping;

        #region Initialiser
        public PingControl()
        {
            InitializeComponent();
            _Timer = new Timer(1000);
            _Timer.Elapsed += _Timer_Elapsed;

            _AppRef = App.GetApp();
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
            if( _Running ) return; // Only run timer callback one at a time
            try
            {
                _Running = true;
                var PingInfo = _Ping.Send(_Model.Address, _AppRef.TimeoutValue);
                if( !_Running ) return; // This accounts for timers which were Disabled while the Ping request was pending
                switch( PingInfo.Status )
                {
                    case IPStatus.Success:
                        var Time = (PingInfo.RoundtripTime < 1) ? "<1ms" : $"={PingInfo.RoundtripTime}ms";
                        if( _Model.Address.AddressFamily == AddressFamily.InterNetworkV6 ) _Model.DisplayContents += $"Reply from {PingInfo.Address}: time{Time} count={_Count}\n";
                        else _Model.DisplayContents += $"Reply from {PingInfo.Address}: bytes={PingInfo.Buffer.Length} time{Time} TTL={PingInfo.Options.Ttl} count={_Count}\n";
                        _Model.LastContact = DateTime.Now.ToString();

                        if( PingInfo.RoundtripTime > _AppRef.WarningTimeframe ) _Model.Colour = Brushes.DarkOrange;
                        else _Model.Colour = Brushes.Green;
                        _Count++;
                        break;
                    
                    case IPStatus.DestinationHostUnreachable:
                        _Model.DisplayContents += $"Reply from {PingInfo.Address}: Destination host unreachable.\n";
                        _Model.Colour = Brushes.Maroon;
                        _Count = 1;
                        break;

                     case IPStatus.DestinationNetworkUnreachable:
                        _Model.DisplayContents += $"Reply from {PingInfo.Address}: Destination net unreachable.\n";
                        _Model.Colour = Brushes.Maroon;
                        _Count = 1;
                        break;
                       
                    case IPStatus.TimedOut:
                        _Model.DisplayContents += $"Request Timed Out.\n";
                        _Model.Colour = Brushes.Maroon;
                        _Count = 1;
                        break;

                    default:
                        _Model.DisplayContents += $"Unknown message type: {PingInfo.Status.ToString()}\n";
                        _Model.Colour = Brushes.Maroon;
                        _Count = 1;
                        break;
                }

                _TrimContainer();
                _Running = false;
                return;
            }
            catch( Exception Ex )
            {
                _Model.DisplayContents += $"PING: transmit failed. General failure.\n";
                _AppRef.Log.Debug(Ex, "Ping Transmit Failed");
                _Model.Colour = Brushes.Maroon;
                _Running = false;
                _Count = 1;
            }
        }

        /// <summary>
        /// Trim the text container to keep it under our max lines.
        /// </summary>
        private void _TrimContainer()
        {
            var TotalNewlines = 0;
            if( _Model.DisplayContents != null )
            {
                for( var i=0; i < _Model.DisplayContents.Length; i++ ) if( _Model.DisplayContents[i] == '\n' ) TotalNewlines++;
                if( TotalNewlines > _MaxContainerLines )
                {
                    for( var i=0; i < _Model.DisplayContents.Length; i++ )
                    {
                        if( _Model.DisplayContents[i] != '\n' ) continue; // If not a newline, continue looping over characters
                        if( TotalNewlines > _MaxContainerLines+1 ) { TotalNewlines--; continue; } // Continue until we make TotalNewlines = 5

                        // Strip all characters from 0 to our last newline location, which truncates the string to our max line count
                        _Model.DisplayContents = _Model.DisplayContents.Substring(i+1);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Event for Unloading Control
        /// </summary>
        private void _UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _AppRef.Log.Debug("PingControl is being disposed");
            Dispose();
        }

        /// <summary>
        /// Event for Pause Button
        /// </summary>
        private void _Pause_Click(object sender, RoutedEventArgs e)
        {
            if( _Timer.Enabled ) PauseControl();
            else ResumeControl();
        }

        /// <summary>
        /// Event for Edit Button
        /// </summary>
        private void _Edit_Click(object sender, RoutedEventArgs e)
        {
            var ResolveWindow = (MainWindow)_AppRef?.MainWindow;
            if( ResolveWindow != null )
            {
                var AddDialog = new AddDialog(ResolveWindow, _Model.Address, _Model.DisplayName);
                    AddDialog.Owner = ResolveWindow;
                    AddDialog.ShowDialog();
            }
        }

        /// <summary>
        /// Event for Delete Button
        /// </summary>
        private void _Delete_Click(object sender, RoutedEventArgs e)
        {
            if( MessageBox.Show($"Are you sure you wish to delete this check?\n{_Model.DisplayName}", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes )
            {
                var ResolveWindow = (MainWindow)_AppRef?.MainWindow;
                if( ResolveWindow != null )
                {
                    ResolveWindow.RemovePingElement(_Model.Address);
                }
            }
        }
        #endregion Window Events

        #region Public Methods
        /// <summary>
        /// Pause the ping timer on this control
        /// </summary>
        /// <returns>True if successful, False if already paused</returns>
        public bool PauseControl()
        {
            if( !_Timer.Enabled ) return false;
            _Model.DisplayContents = "Ping Control Paused\n";
            _Model.PauseIcon = FontAwesomeIcon.Play;
            _Model.Colour = Brushes.DarkOrange;

            _Timer.Enabled = false;
            _Running = false;
            return true;
        }

        /// <summary>
        /// Resume this paused ping control
        /// </summary>
        /// <returns>True if successful, False if not paused</returns>
        public bool ResumeControl()
        {
            if( _Timer.Enabled ) return false;
            _Model.PauseIcon = FontAwesomeIcon.Pause;
            _Model.DisplayContents = "";
            _Count = 1;
            
            _Timer.Enabled = true;
            return true;
        }
        #endregion Public Methods
    }

    public class PingControlModel : ViewModel
    {
        #region Private Properties
        private FontAwesomeIcon _PauseIcon = FontAwesomeIcon.Pause;
        private string _LastContact = "Never";
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

        public FontAwesomeIcon PauseIcon
        {
            get
            {
                return _PauseIcon;
            }
            set
            {
                if( _PauseIcon != value )
                {
                    _PauseIcon = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion Public Properties
    }
}