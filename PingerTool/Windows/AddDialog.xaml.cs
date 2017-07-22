using System;
using System.Net;
using System.Windows;
using PingerTool.Classes;
using System.Windows.Media;

namespace PingerTool.Windows
{
	public partial class AddDialog
	{
        private AddDialogModel _Model;
		private MainWindow _Window;

		public AddDialog(MainWindow Window)
		{
            InitializeComponent();

            _Model = (AddDialogModel)DataContext;
			_Window = Window;
		}

		#region Window Events
        /// <summary>
        /// Event handler for clicking the Save Button
        /// </summary>
		private void _Save_Click(object sender, RoutedEventArgs e)
		{
            /*
			try
			{
                var Model = (AddDialogModel)DataContext;
                var Password = PasswordBox.Password;

				if( Password.Length > 0 && Model.DisplayName.Length > 0 )
				{
					// Register Client
					var Task = App.GetApp().Agent.RegisterClient(IPAddress.Parse(Model.IPAddress), Password);
					Task.ContinueWith((t) =>
					{
						if( t.Result )
						{
							// Register client on Dispatcher thread
							App.Current.Dispatcher.Invoke( (Action)delegate
							{
								// Add Client
								_AppRef.Model.Clients.Add(new ComputerModel()
								{
									UUID		= App.GetApp().Agent.GetUUIDFromIP(IPAddress.Parse(Model.IPAddress)),
									Icon		= FontAwesomeIcon.Circle,
									Category	= "Registered Clients",
									DisplayName = Model.DisplayName,
									Colour		= Brushes.Gray
								});

								// Close Dialog
								this.Close();
							});
						}
						else
						{
							// Error authenticating client
							MessageBox.Show("Unable to authenticate with remote client.\nIt may be offline or your password may be incorrect", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
						}
					});
				}
				else
				{
					// Nope
					MessageBox.Show("Please enter a Client Password and Display Name", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
			catch( Exception Ex )
			{
				// Error parsing IP Address (Probably)
				MessageBox.Show("Unable to parse your IP Address\nPlease check it and try again", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
				_AppRef.Log.Error(Ex, "Unable to parse IP Address");
			}*/
		}

		/// <summary>
		/// Event handler for clicking the Discard Button
		/// </summary>
		private void _Discard_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
		#endregion Window Events
	}

    class AddDialogModel : ViewModel
    {
        #region Private Properties
        private string _DisplayName = "";
        private string _IPAddress = "";
        #endregion Private Properties

        #region Public Properties
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

        public string IPAddress
        {
            get
            {
                return _IPAddress;
            }
            set
            {
                if( _IPAddress != value )
                {
                    _IPAddress = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion Public Properties
    }
}