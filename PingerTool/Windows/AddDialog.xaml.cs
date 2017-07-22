using System;
using System.Net;
using System.Windows;
using PingerTool.Classes;

namespace PingerTool.Windows
{
	public partial class AddDialog
	{
        private AddDialogModel _Model;
		private MainWindow _Window;

        #region Initialiser
		public AddDialog(MainWindow Window)
		{
            InitializeComponent();

            _Model = (AddDialogModel)DataContext;
			_Window = Window;
		}
        #endregion Initialiser

		#region Window Events
        /// <summary>
        /// Event handler for clicking the Save Button
        /// </summary>
		private void _Save_Click(object sender, RoutedEventArgs e)
		{
			if( _Model.DisplayName.Length > 0 && _Model.IPAddress.Length > 0 )
			{
                // We need to convert the text IP to the IPAddress class.
                if( IPAddress.TryParse(_Model.IPAddress, out IPAddress ParsedAddress) )
                {
                    // Add Ping Check
                    if( _Window.CreatePingElement(_Model.DisplayName, ParsedAddress) ) Close();
                    else MessageBox.Show("This IP Address already has a check associated to it", "Whoops", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Invalid IP
                    MessageBox.Show("The IP Address you entered is invalid", "Whoops", MessageBoxButton.OK, MessageBoxImage.Information);
                }
			}
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