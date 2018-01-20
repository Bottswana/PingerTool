using System;
using System.Net;
using System.Linq;
using System.Media;
using System.Timers;
using System.Windows;
using Microsoft.Win32;
using FontAwesome.WPF;
using System.Reflection;
using PingerTool.Classes;
using PingerTool.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PingerTool.Windows
{
    public partial class MainWindow : Window, IDisposable
    {
        public int[] Timeframes = { 2000, 2000, 0, 5 }; // UIWarn, UITImeout, SoundWarn, SoundTimeout
        public bool Notification = false;
        public MainWindowModel Model;
        public ProjectControl Proj;
        public SoundPlayer Player;
        public WebServer Server;
        public Spark Spark;

        private bool _IsTimerRunning = false;
        private bool _IsPlayingAlert = false;
        private bool _DisposedValue = false;
        private Timer _Timer;

        #region Initialiser
        public MainWindow()
        {
            InitializeComponent();

            // Setup Local Properties
            Model = (MainWindowModel)DataContext;
            Proj = new ProjectControl(this);

            // Setup Timer
            _Timer = new Timer(500);
            _Timer.Elapsed += _Timer_Elapsed;
            _Timer.Start();

            // Setup Player
            Player = _SetupPlayer();

            // Use Fontawesome icons for ribbon
            var ColourBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#2b579a"));
            Add.LargeIcon       = ImageAwesome.CreateImageSource(FontAwesomeIcon.PlusSquare, ColourBrush);
            PauseAll.LargeIcon  = ImageAwesome.CreateImageSource(FontAwesomeIcon.Pause, ColourBrush);
            ResumeAll.LargeIcon = ImageAwesome.CreateImageSource(FontAwesomeIcon.Play, ColourBrush);
            Settings.LargeIcon  = ImageAwesome.CreateImageSource(FontAwesomeIcon.Cogs, ColourBrush);
            ShGraph.LargeIcon   = ImageAwesome.CreateImageSource(FontAwesomeIcon.BarChart, ColourBrush);
            NoGraph.LargeIcon   = ImageAwesome.CreateImageSource(FontAwesomeIcon.BarChart, Brushes.Gray);

            // Version Info
            Model.CompiledOn = Helpers.GetLinkerTime().ToString();
            #if DEBUG
            Model.VersionString = string.Format("{0} - Development Build", Helpers.GetApplicationVersion());
            #else
            Model.VersionString = string.Format("{0} - Production Build", Helpers.GetApplicationVersion());
            #endif
        }

        protected virtual void Dispose(bool disposing)
        {
            if( !_DisposedValue )
            {
                if( disposing )
                {
                    _Timer.Dispose();
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
            if( _IsTimerRunning ) return;
            _IsTimerRunning = true;

            var NeedsToAlert = false;
            foreach( var Control in Model.PingWindows )
            {
                // Update audio alerting status
                if( Notification && ((Timeframes[2] > 0 && Control.WarningFailures >= Timeframes[2]) 
                    || (Timeframes[3] > 0 && Control.TimeoutFailures >= Timeframes[3])) )
                {
                    NeedsToAlert = true;
                    Control.Alerting = true;
                    if( !_IsPlayingAlert )
                    {
                        _IsPlayingAlert = true;
                        Player.PlayLooping();
                    }
                }
                else
                {
                    Control.Alerting = false;
                }

                // Update Spark Status
                if( Spark != null && ((Spark.WarningThreshold > 0 && Control.WarningFailures >= Spark.WarningThreshold) 
                    || (Spark.TimeoutThreshold > 0 && Control.TimeoutFailures >= Spark.TimeoutThreshold)) )
                {
                    if( !Control.HasNotifiedBySpark )
                    {
                        Control.HasNotifiedBySpark = true;
                        var Lines = Control.DisplayLines;

                        var Status = ( Control.WarningFailures != 0 ) ? "HIGH RESPONSE TIME" : "TIMEOUT";
                        var BaseMessage = $"Host '{Control.DisplayName}' has entered state '{Status}'.\n\nLast contact: '{Control.LastContact}'";
                        var t = Spark.SendMessage($"ALERT: {BaseMessage}", $"**ALERT**: {BaseMessage}\n```\n{Lines[Lines.Count-4]}\n{Lines[Lines.Count-3]}\n{Lines[Lines.Count-2]}\n```");
                    }
                }
                else if( Control.HasNotifiedBySpark )
                {
                    Control.HasNotifiedBySpark = false;
                    var Lines = Control.DisplayLines;

                    var BaseMessage = $"Host '{Control.DisplayName}' has cleared alert state.\n\nLast contact: '{Control.LastContact}'";
                    var t = Spark.SendMessage($"OK: {BaseMessage}", $"**OK**: {BaseMessage}\n```\n{Lines[Lines.Count-4]}\n{Lines[Lines.Count-3]}\n{Lines[Lines.Count-2]}\n```");
                }
            }

            // Stop Audio Alert
            if( !NeedsToAlert && _IsPlayingAlert )
            {
                _IsPlayingAlert = false;
                Player.Stop(); 
            }

            // End of timer
            _IsTimerRunning = false;
        }

        /// <summary>
        /// Window Closing Event
        /// </summary>
        private void _Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if( !App.GetApp().ConfirmApplicationShutdown() && e != null )
            {
                // Abort closing of window
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Event hander for Ctrl + s Keyboard Combo
        /// </summary>
        private void _Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if( e.Key == Key.S && e.KeyboardDevice.Modifiers == ModifierKeys.Control )
            {
                // Save Combination
                Proj.SaveProject(Proj.sCurrentFile);
                return;
            }
        }

        /// <summary>
        /// Event handler for opening a new project
        /// </summary>
        private void _NewProject_Click(object sender, RoutedEventArgs e)
        {
            if( !Proj.SaveNeeded || MessageBox.Show("Are you sure you wish to create a new Project?\nAll unsaved work will be lost.", "New Project", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes )
            {
                // Create New Project
                Proj.NewProject();

                // Close Ribbon Menu
                var Backstage = (Fluent.Backstage)ribbon.Menu;
                if( Backstage != null ) Backstage.IsOpen = false;
            }
        }

        /// <summary>
        /// Event handler for opening a project
        /// </summary>
        private void _OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if( !Proj.SaveNeeded || MessageBox.Show("Are you sure you wish to open a project?\nAll unsaved work will be lost.", "Open Project", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes )
            {
                var FileDialog = new OpenFileDialog()
                {
                    Filter          = "Project Files (*.pingtool)|*.pingtool",
                    Title           = "Save Project As",
                    DefaultExt      = ".pingtool",
                    CheckPathExists = true,
                    ValidateNames   = true,
                    AddExtension    = true
                };

                if( FileDialog.ShowDialog() == true )
                {
                    if( !Proj.OpenProject(FileDialog.FileName) )
                    {
                        // Open Error
                        MessageBox.Show("Unable to open project. It may be corrupt or the file selected may not be a valid project file.", "Open Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        // Close Ribbon Menu
                        var Backstage = (Fluent.Backstage)ribbon.Menu;
                        if( Backstage != null ) Backstage.IsOpen = false;
                    }
                }
            }
        }

        /// <summary>
        /// Event hander for saving a project
        /// </summary>
        private void _SaveProject_Click(object sender, RoutedEventArgs e)
        {
            if( Proj.sCurrentFile == null )
            {
                // Open file is untitled, so open save as box
                _SaveAsProject_Click(sender, e);
                return;
            }

            if( !Proj.SaveProject(Proj.sCurrentFile) )
            {
                // Save Error
                MessageBox.Show("A save error has occoured, please check you have permission to save to this location.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                // Close Ribbon Menu
                var Backstage = (Fluent.Backstage)ribbon.Menu;
                if( Backstage != null ) Backstage.IsOpen = false;
            }
        }

        /// <summary>
        /// Event handler for saving a project with filename
        /// </summary>
        private void _SaveAsProject_Click(object sender, RoutedEventArgs e)
        {
            var FileDialog = new SaveFileDialog()
            {
                Filter          = "Project Files (*.pingtool)|*.pingtool",
                Title           = "Save Project As",
                DefaultExt      = ".pingtool",
                CheckPathExists = true,
                OverwritePrompt = true,
                ValidateNames   = true,
                AddExtension    = true
            };

            if( FileDialog.ShowDialog() == true )
            {
                if( !Proj.SaveProject(FileDialog.FileName) )
                {
                    // Save Error
                    MessageBox.Show("A save error has occoured, please check you have permission to save to this location.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    // Close Ribbon Menu
                    var Backstage = (Fluent.Backstage)ribbon.Menu;
                    if( Backstage != null ) Backstage.IsOpen = false;
                }
            }
        }

        /// <summary>
        /// Event handler for clicking the Add Button
        /// </summary>
        private void _AddCheck_Click(object sender, RoutedEventArgs e)
        {
            (new AddDialog(this) {
                Owner = this
            }).ShowDialog();
        }

        /// <summary>
        /// Event handler for clicking the Settings Button
        /// </summary>
        private void _Settings_Click(object sender, RoutedEventArgs e)
        {
            (new Settings(this) {
                Owner = this
            }).ShowDialog();
        }

        /// <summary>
        /// Event hander for clicking the pause all button
        /// </summary>
        private void _PauseAll_Click(object sender, RoutedEventArgs e)
        {
            foreach( var PingControl in FindVisualChildren<PingControl>(this) )
            {
                PingControl.PauseControl();
            }
        }

        /// <summary>
        /// Event hander for clicking the resume all button
        /// </summary>
        private void _ResumeAll_Click(object sender, RoutedEventArgs e)
        {
            foreach( var PingControl in FindVisualChildren<PingControl>(this) )
            {
                PingControl.ResumeControl();
            }
        }

        /// <summary>
        /// Event hander for clicking the graph button
        /// </summary>
        private void _GraphsOn_Click(object sender, RoutedEventArgs e)
        {
            foreach( var PingControl in Model.PingWindows )
            {
                PingControl.ShowGraph = true;
            }
        }

        /// <summary>
        /// Event hander for clicking the graph button
        /// </summary>
        private void _GraphsOff_Click(object sender, RoutedEventArgs e)
        {
            foreach( var PingControl in Model.PingWindows )
            {
                PingControl.ShowGraph = false;
            }
        }
        #endregion Window Events

        #region Private Methods
        private SoundPlayer _SetupPlayer()
        {
            const string ResourceName = "PingerTool.Resources.AlertAudio.wav";
            using( var Stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName) )
            {
                var Player = new SoundPlayer(Stream);
                Player.Load();
                return Player;
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if( depObj != null )
            {
                for( int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++ )
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if( child != null && child is T )
                    {
                        yield return (T)child;
                    }

                    foreach( T childOfChild in FindVisualChildren<T>(child) )
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        #endregion PrivateMethods

        #region Public Methods
        /// <summary>
        /// Add a new Ping Element
        /// </summary>
        /// <param name="HeaderName">Name of the Element</param>
        /// <param name="Address">IP Address to Ping</param>
        /// <param name="GraphHidden">If graph element is hidden</param>
        /// <returns>True if added, False if the element exists already</returns>
        public bool CreatePingElement(string HeaderName, IPAddress Address, bool GraphHidden = false)
        {
            // Check it does not already exist
            var Elements = Model.PingWindows.Count( q => { return q.Address.Equals(Address); });
            if( Elements != 0 ) return false;

            // Add Model to Collection
            Model.PingWindows.Add(new PingControlModel()
            {
                ShowGraph = !GraphHidden,
                DisplayName = HeaderName,
                Address = Address
            });

            // Update Column Count
            var TotalCount = Model.PingWindows.Count;
            if( TotalCount > 8 ) Model.Columns = 3; // Three windows wide, we can fit about 12 on a 1080 screen before it gets silly
            else if( TotalCount > 1 ) Model.Columns = 2;
            else Model.Columns = 1;

            Proj?.TriggerSaveStatus();
            return true;
        }

        /// <summary>
        /// Remove a existing Ping Element
        /// </summary>
        /// <param name="Address">IP Address to remove</param>
        /// <returns>True if removed, False if the element does not exist</returns>
        public bool RemovePingElement(IPAddress Address)
        {
            // Check it exists
            var Elements = Model.PingWindows.Count(q => { return q.Address.Equals(Address); });
            if( Elements == 0 ) return false;

            // Remove Elements
            var Element = Model.PingWindows.First(q => { return q.Address.Equals(Address); });
            Model.PingWindows.Remove(Element);

            // Update Column Count
            var TotalCount = Model.PingWindows.Count;
            if( TotalCount > 8 ) Model.Columns = 3; // Three windows wide, we can fit about 12 on a 1080 screen before it gets silly
            else if( TotalCount > 1 ) Model.Columns = 2;
            else Model.Columns = 1;

            Proj?.TriggerSaveStatus();
            return true;
        }

        /// <summary>
        /// Update the name of an existing Ping Element
        /// </summary>
        /// <param name="Address">IP Address of existing element</param>
        /// <param name="NewName">New name to use for element</param>
        /// <returns>True if updated, False if the element does not exist</returns>
        public bool UpdatePingElementName(IPAddress Address, string NewName)
        {
            // Check it exists
            var Elements = Model.PingWindows.Count(q => { return q.Address.Equals(Address); });
            if( Elements == 0 ) return false;

            // Update Name
            var Element = Model.PingWindows.First(q => { return q.Address.Equals(Address); });
            Element.DisplayName = NewName;

            Proj?.TriggerSaveStatus();
            return true;
        }

        /// <summary>
        /// Update the Address of an existing Ping Element
        /// </summary>
        /// <param name="Address">IP Address of existing element</param>
        /// <param name="NewAddress">New IP Address</param>
        /// <returns>True if updated, False if the element does not exist</returns>
        public bool UpdatePingElementAddress(IPAddress Address, IPAddress NewAddress)
        {
            // Check it exists
            var Elements = Model.PingWindows.Count(q => { return q.Address.Equals(Address); });
            if( Elements == 0 ) return false;

            // Check the new adddress does not exist
            Elements = Model.PingWindows.Count( q => { return q.Address.Equals(NewAddress); });
            if( Elements != 0 ) return false;

            // Update Address
            var Element = Model.PingWindows.First(q => { return q.Address.Equals(Address); });
            var ClonedModel = (PingControlModel)Element.Clone();
            Element.Address = NewAddress;

            // Notify the WebUI and trigger save update
            Server?.SendWebsocketMessage(ClonedModel, "DELETED");
            Proj?.TriggerSaveStatus();
            return true;
        }

        /// <summary>
        /// Pause the specified ping element
        /// </summary>
        /// <param name="Address">IP Address of existing element</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool PausePingElement(IPAddress Address)
        {
            foreach( var PingControl in FindVisualChildren<PingControl>(this) )
            {
                if( PingControl.PingAddress != Address ) continue;
                PingControl.PauseControl();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resume the specified ping element
        /// </summary>
        /// <param name="Address">IP Address of existing element</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ResumePingElement(IPAddress Address)
        {
            foreach( var PingControl in FindVisualChildren<PingControl>(this) )
            {
                if( PingControl.PingAddress != Address ) continue;
                PingControl.ResumeControl();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Delete all Ping Elements
        /// </summary>
        public void ClearAllElements()
        {
            Model.PingWindows.Clear();
            Model.Columns = 1;
        }

        /// <summary>
        /// Get a list of active Ping Elements
        /// </summary>
        /// <returns>List of Ping Elements</returns>
        public List<PingControlModel> GetAllElements()
        {
            return Model.PingWindows.ToList();
        }
        #endregion Public Methods
    }

    public class MainWindowModel : ViewModel
    {
        #region Initialiser
        public MainWindowModel()
        {
            PingWindows = new ObservableCollection<PingControlModel>();
        }
        #endregion Initialiser

        #region Private Properties
        private string _VersionString;
        private string _CompiledOn;
        private int _GraphHeight = 100;
        private int _Columns = 1;
        #endregion Private Properties

        #region Public Properties
        /// <summary>
        /// Ping Window Collection
        /// </summary>
        public ObservableCollection<PingControlModel> PingWindows { get; set; }

        /// <summary>
        /// Individual Graph Height
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
        /// Current Application Version
        /// </summary>
        public string VersionString
        {
            get
            {
                return _VersionString;
            }
            set
            {
                if( _VersionString != value )
                {
                    _VersionString = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Date application compiled on
        /// </summary>
        public string CompiledOn
        {
            get
            {
                return _CompiledOn;
            }
            set
            {
                if( _CompiledOn != value )
                {
                    _CompiledOn = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Number of Columns to Show
        /// </summary>
        public int Columns
        {
            get
            {
                return _Columns;
            }
            set
            {
                if( _Columns != value )
                {
                    _Columns = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion Public Properties
    }
}