using System;
using System.Net;
using System.Linq;
using System.Timers;
using System.Windows;
using FontAwesome.WPF;
using System.Net.Sockets;
using PingerTool.Classes;
using PingerTool.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Collections.ObjectModel;

namespace PingerTool.Controls
{
    public partial class PingControl : UserControl, IDisposable
    {
        private List<double> _PlotPoints = new List<double>();
        private bool _DisposedValue = false;
        private PingControlModel _Model;
        private bool _Running = false;
        private MainWindow _Window;
        private Timer _Timer;
        private Ping _Ping;

        #region Initialiser
        public PingControl()
        {
            InitializeComponent();
            _Ping = new Ping();
            _Timer = new Timer(1000);
            _Timer.Elapsed += _Timer_Elapsed;
            
            _Window = (MainWindow)App.GetApp()?.MainWindow;
            if( _Window == null )
            {
                throw new Exception("Parent window does not exist");
            }
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Our ViewModel isnt immediately available when the object is instanced, so set it once the event for it fires
            _Model = (PingControlModel)DataContext;
            _Timer.Start();
        }

        protected virtual void Dispose(bool disposing)
        {
            if( !_DisposedValue )
            {
                if( disposing )
                {
                    _Window.Server?.SendWebsocketMessage(_Model, "DELETED");
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
        private void _Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if( _Running ) return; // Only run timer callback one at a time
            try
            {
                _Running = true;
                var PingInfo = _Ping.Send(_Model.Address, _Window.Timeframes[1]);
                if( !_Running ) return; // This accounts for timers which were Disabled while the Ping request was pending

                var Message = "";
                _PlotOnGraph(PingInfo);
                switch( PingInfo.Status )
                {
                    case IPStatus.Success:
                        var Time = ( PingInfo.RoundtripTime < 1 ) ? "<1ms" : $"={PingInfo.RoundtripTime}ms";
                        if( _Model.Address.AddressFamily == AddressFamily.InterNetworkV6 ) Message = $"Reply from {PingInfo.Address}: time{Time} count={_Model.Count}";
                        else Message = $"Reply from {PingInfo.Address}: bytes={PingInfo.Buffer.Length} time{Time} TTL={PingInfo.Options.Ttl} count={_Model.Count}";

                        _Model.LastContact = DateTime.Now.ToString();
                        if( PingInfo.RoundtripTime > _Window.Timeframes[0] )
                        {
                            _Model.Colour = Brushes.DarkOrange;
                            _HandleWarning();
                        }
                        else
                        {
                            _Model.Colour = Brushes.Green;
                            _Model.TimeoutFailures = 0;
                            _Model.WarningFailures = 0;
                        }

                        _Model.Count++;
                        break;

                    case IPStatus.DestinationHostUnreachable:
                        Message = $"Reply from {PingInfo.Address}: Destination host unreachable.";
                        _Model.Colour = Brushes.Maroon;
                        _HandleTimeout();
                        _Model.Count = 1;
                        break;

                     case IPStatus.DestinationNetworkUnreachable:
                        Message = $"Reply from {PingInfo.Address}: Destination net unreachable.";
                        _Model.Colour = Brushes.Maroon;
                        _HandleTimeout();
                        _Model.Count = 1;
                        break;
                       
                    case IPStatus.TimedOut:
                        Message = $"Request Timed Out.";
                        _Model.Colour = Brushes.Maroon;
                        _HandleTimeout();
                        _Model.Count = 1;
                        break;

                    default:
                        Message = $"Unknown message type: {PingInfo.Status.ToString()}";
                        _Model.Colour = Brushes.Maroon;
                        _HandleTimeout();
                        _Model.Count = 1;
                        break;
                }

                if( _Model.DisplayLines.Count() > App.MaxContainerLines ) _Model.DisplayLines.RemoveAt(0);
                _Window.Server?.SendWebsocketMessage(_Model, Message);
                _Model.DisplayLines.Add(Message);
                _Running = false;
                return;
            }
            catch( Exception )
            {
                var Message = $"PING: transmit failed. General failure.";

                if( _Model.DisplayLines.Count() > App.MaxContainerLines ) _Model.DisplayLines.RemoveAt(0);
                _Window.Server?.SendWebsocketMessage(_Model, Message);
                _Model.DisplayLines.Add(Message);

                _Model.Colour = Brushes.Maroon;
                _HandleTimeout();
                _Running = false;
                _Model.Count = 1;
            }
        }

        /// <summary>
        /// Event for Unloading Control
        /// </summary>
        private void _UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            App.GetApp().Log.Debug("PingControl is being disposed");
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
            new AddDialog(_Window, _Model.Address, _Model.DisplayName) {
                Owner = _Window
            }.ShowDialog();
        }

        /// <summary>
        /// Event for Delete Button
        /// </summary>
        private void _Delete_Click(object sender, RoutedEventArgs e)
        {
            if( MessageBox.Show($"Are you sure you wish to delete this check?\n{_Model.DisplayName}", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes )
            {
                _Window.RemovePingElement(_Model.Address);
            }
        }

        /// <summary>
        /// Event for Graph Button
        /// </summary>
        private void _Graph_Click(object sender, RoutedEventArgs e)
        {
            _Model.ShowGraph = !_Model.ShowGraph;
            _Window.Proj?.TriggerSaveStatus();
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
            _Timer.Enabled = false;

            _PlotPoints.Clear();
            _Model.DisplayLines.Clear();
            _Model.DisplayLines.Add("Ping Control Paused.");

            Dispatcher.Invoke( () => GraphTarget.PlotBars(_PlotPoints) );
            _Model.PauseIcon = FontAwesomeIcon.Play;
            _Model.Colour = Brushes.DarkOrange;
            _Model.TimeoutFailures = 0;
            _Model.WarningFailures = 0;
            _Running = false;

            _Window.Server?.SendWebsocketMessage(_Model, "");
            return true;
        }

        /// <summary>
        /// Resume this paused ping control
        /// </summary>
        /// <returns>True if successful, False if not paused</returns>
        public bool ResumeControl()
        {
            if( _Timer.Enabled ) return false;
            _Timer.Enabled = true;

            _Model.PauseIcon = FontAwesomeIcon.Pause;
            _Model.DisplayLines.Clear();
            _Model.Count = 1;
            return true;
        }

        /// <summary>
        /// The IP Address of this Ping Control
        /// </summary>
        public IPAddress PingAddress
        {
            get
            {
                return _Model.Address;
            }
        }
        #endregion Public Methods

        #region Private Methods
        private void _PlotOnGraph(PingReply Info)
        {
            _Model.Width = (int)Math.Ceiling(GraphHost.ActualWidth/4);
            var Offset = ( _Model.Width > 400 ) ? (int)Math.Floor((double)(_Model.Width-400)/10)+10 : 0;
            if( _PlotPoints.Count > _Model.Width+Offset )
            {
                _PlotPoints.RemoveRange(0, (_PlotPoints.Count - (_Model.Width+Offset)));
            }

            _PlotPoints.Add((Info.RoundtripTime < 1) ? 1 : Info.RoundtripTime);
            Dispatcher.Invoke( () => GraphTarget.PlotBars(_PlotPoints) );
        }

        private void _HandleWarning()
        {
            if( _Window.Timeframes[2] > 0 && _Model.WarningFailures < _Window.Timeframes[2] )
            {
                _Model.TimeoutFailures = 0;
                _Model.WarningFailures++;
            }
        }

        private void _HandleTimeout()
        {
            if( _Window.Timeframes[3] > 0 && _Model.TimeoutFailures < _Window.Timeframes[3] )
            {
                _Model.WarningFailures = 0;
                _Model.TimeoutFailures++;
            }
        }
        #endregion Private Methods
    }

    public class PingControlModel : ViewModel
    {
        #region Private Properties
        private ObservableCollection<string> _DisplayLines = new ObservableCollection<string>();
        private FontAwesomeIcon _PauseIcon = FontAwesomeIcon.Pause;
        private string _LastContact = "Never";
        private Brush _Colour = Brushes.Gray;
        private bool _Alerting = false;
        private bool _ShowGraph = true;
        private string _DisplayName;
        private IPAddress _Address;
        private int _Width = 400;
        private int _Count = 1;
        #endregion Private Properties

        #region Public Properties
        /// <summary>
        /// If this condition has been notified via Spark Integration
        /// </summary>
        public bool HasNotifiedBySpark { get; set; }

        /// <summary>
        /// Number of times the control has experienced a high round trip to the host
        /// </summary>
        public int WarningFailures { get; set; }

        /// <summary>
        /// Number of times the control has failed to ping the remote host
        /// </summary>
        public int TimeoutFailures { get; set; }

        /// <summary>
        /// Ping Window Contents
        /// </summary>
        public ObservableCollection<string> DisplayLines
        {
            get
            {
                return _DisplayLines;
            }
        }

        /// <summary>
        /// Main Window Model
        /// </summary>
        public MainWindowModel Window
        {
            get
            { 
                return ((MainWindow)App.GetApp().MainWindow).Model;
            } 
        }

        /// <summary>
        /// Number of consecutive completed pings
        /// </summary>
        public int Count
        {
            get
            {
                return _Count;
            }
            set
            {
                if( _Count != value )
                {
                    _Count = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Display Graph
        /// </summary>
        public bool ShowGraph
        {
            get
            {
                return _ShowGraph;
            }
            set
            {
                if( _ShowGraph != value )
                {
                    _ShowGraph = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Width of Chart Object
        /// </summary>
        public int Width
        {
            get
            {
                return _Width;
            }
            set
            {
                if( _Width != value )
                {
                    _Width = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// If this control is alerting
        /// </summary>
        public bool Alerting
        {
            get
            {
                return _Alerting;
            }
            set
            {
                if( _Alerting != value )
                {
                    _Alerting = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Name for this Ping Control
        /// </summary>
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

        /// <summary>
        /// Human readable string of last known contact from address
        /// </summary>
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

        /// <summary>
        /// Target IP address to ping
        /// </summary>
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

        /// <summary>
        /// Colour to use for Display box and status icon
        /// </summary>
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

        /// <summary>
        /// Icon of the Pause/Play Button
        /// </summary>
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