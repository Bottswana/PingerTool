using System;
using System.Net;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using FontAwesome.WPF;
using PingerTool.Classes;
using PingerTool.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PingerTool.Windows
{
    public partial class MainWindow : Window
    {
        private MainWindowModel _Model;
        private App _AppRef;

        public ProjectControl Proj;
        public WebServer Server;
        public Log Log;

		#region Initialiser
		public MainWindow()
		{
			InitializeComponent();

            // Setup Local Properties
            _Model = (MainWindowModel)DataContext;
			Proj = new ProjectControl(this);
			Log = new Log("Main GUI");
			_AppRef = App.GetApp();

			// Use Fontawesome icons for ribbon
			var ColourBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#2b579a"));
            Add.LargeIcon		= ImageAwesome.CreateImageSource(FontAwesomeIcon.PlusSquare, ColourBrush);
            PauseAll.LargeIcon  = ImageAwesome.CreateImageSource(FontAwesomeIcon.Pause, ColourBrush);
            ResumeAll.LargeIcon = ImageAwesome.CreateImageSource(FontAwesomeIcon.Play, ColourBrush);
            Settings.LargeIcon	= ImageAwesome.CreateImageSource(FontAwesomeIcon.Cogs, ColourBrush);

			// Version Info
			_Model.CompiledOn = Helpers.GetLinkerTime().ToString();
			#if DEBUG
			_Model.VersionString = string.Format("{0} - Development Build", Helpers.GetApplicationVersion());
			#else
			Model.VersionString = string.Format("{0} - Production Build", Helpers.GetApplicationVersion());
			#endif
        }
		#endregion Initialiser

        #region Window Events
        /// <summary>
        /// Window Closing Event
        /// </summary>
        private void _Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            if( !_AppRef.ConfirmApplicationShutdown() && e != null )
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
		private void _NewProject_Click( object sender, RoutedEventArgs e )
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
		private void _OpenProject_Click( object sender, RoutedEventArgs e )
		{
			if( !Proj.SaveNeeded || MessageBox.Show("Are you sure you wish to open a project?\nAll unsaved work will be lost.", "Open Project", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes )
			{
				var FileDialog = new OpenFileDialog()
				{
				    Filter			= "Project Files (*.pingtool)|*.pingtool",
				    Title			= "Save Project As",
				    DefaultExt		= ".pingtool",
					CheckPathExists = true,
					ValidateNames	= true,
					AddExtension	= true
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
		private void _SaveProject_Click( object sender, RoutedEventArgs e )
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
		private void _SaveAsProject_Click( object sender, RoutedEventArgs e )
		{
			var FileDialog = new SaveFileDialog()
			{
				Filter			= "Project Files (*.pingtool)|*.pingtool",
				Title			= "Save Project As",
				DefaultExt		= ".pingtool",
				CheckPathExists = true,
				OverwritePrompt = true,
				ValidateNames	= true,
				AddExtension	= true
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
        private void _AddCheck_Click( object sender, RoutedEventArgs e )
        {
            var AddDialog = new AddDialog(this);
                AddDialog.Owner = this;
                AddDialog.ShowDialog();
        }

        /// <summary>
        /// Event handler for clicking the Settings Button
        /// </summary>
        private void _Settings_Click( object sender, RoutedEventArgs e )
        {
            var Settings = new Settings(this);
                Settings.Owner = this;
                Settings.ShowDialog();
        }

        /// <summary>
        /// Event hander for clicking the pause all button
        /// </summary>
        private void _PauseAll_Click( object sender, RoutedEventArgs e )
        {
            foreach( var PingControl in FindVisualChildren<PingControl>(this) )
            {
                PingControl.PauseControl();
            }
        }

        /// <summary>
        /// Event hander for clicking the resume all button
        /// </summary>
        private void _ResumeAll_Click( object sender, RoutedEventArgs e )
        {
            foreach( var PingControl in FindVisualChildren<PingControl>(this) )
            {
                PingControl.ResumeControl();
            }
        }
        #endregion Window Events

        #region Private Methods
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
        /// <returns>True if added, False if the element exists already</returns>
        public bool CreatePingElement(string HeaderName, IPAddress Address)
        {
            // Check it does not already exist
            var Elements = _Model.PingWindows.Count( q => { return q.Address.Equals(Address); });
            if( Elements != 0 ) return false;

            // Add Model to Collection
            _Model.PingWindows.Add(new PingControlModel()
            {
                DisplayName = HeaderName,
                Address = Address
            });

            // Update Column Count
            var TotalCount = _Model.PingWindows.Count;
            if( TotalCount > 8 ) _Model.Columns = 3; // Three windows wide, we can fit about 12 on a 1080 screen before it gets silly
            else if( TotalCount > 1 ) _Model.Columns = 2;
            else _Model.Columns = 1;

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
            var Elements = _Model.PingWindows.Count(q => { return q.Address.Equals(Address); });
            if( Elements == 0 ) return false;

            // Remove Elements
            var Element = _Model.PingWindows.First(q => { return q.Address.Equals(Address); });
            _Model.PingWindows.Remove(Element);

            // Update Column Count
            var TotalCount = _Model.PingWindows.Count;
            if( TotalCount > 8 ) _Model.Columns = 3; // Three windows wide, we can fit about 12 on a 1080 screen before it gets silly
            else if( TotalCount > 1 ) _Model.Columns = 2;
            else _Model.Columns = 1;

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
            var Elements = _Model.PingWindows.Count(q => { return q.Address.Equals(Address); });
            if( Elements == 0 ) return false;

            // Update Name
            var Element = _Model.PingWindows.First(q => { return q.Address.Equals(Address); });
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
            var Elements = _Model.PingWindows.Count(q => { return q.Address.Equals(Address); });
            if( Elements == 0 ) return false;

            // Check the new adddress does not exist
            Elements = _Model.PingWindows.Count( q => { return q.Address.Equals(NewAddress); });
            if( Elements != 0 ) return false;

            // Update Address
            var Element = _Model.PingWindows.First(q => { return q.Address.Equals(Address); });
            Element.Address = NewAddress;

            Proj?.TriggerSaveStatus();
            return true;
        }

        /// <summary>
        /// Delete all Ping Elements
        /// </summary>
        public void ClearAllElements()
        {
            _Model.PingWindows.Clear();
            _Model.Columns = 1;
        }

        /// <summary>
        /// Get a list of active Ping Elements
        /// </summary>
        /// <returns>List of Ping Elements</returns>
        public List<PingControlModel> GetAllElements()
        {
            return _Model.PingWindows.ToList();
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
        private int _Columns = 1;
        #endregion Private Properties

        #region Public Properties
        /// <summary>
        /// Ping Window Collection
        /// </summary>
        public ObservableCollection<PingControlModel> PingWindows { get; set; }

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

    public class EmptyListConverter : IValueConverter
    {
        #region Custom List length to Visibility Converter
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check the converter is being used in the intended manner
            if( value.GetType() != typeof(int) || targetType != typeof(Visibility) )
                throw new ArgumentException("Converter only valid for a int to Visibility connversion");

            // Return visible if the length of the list is >= 1, otherwise return hidden
            return ( (int)value >= 1 ) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // We cant convert from Visibility back into a Collection, thats just not feasible or needed
            throw new NotImplementedException("Converter only valid for one-way conversion");
        }
        #endregion Custom List length to Visibility Converter
    }
}