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

		public Settings(MainWindow Window)
		{
            InitializeComponent();

            _Model = (SettingsModel)DataContext;
			_Window = Window;
		}

		#region Window Events
        /// <summary>
        /// Event handler for clicking the Save Button
        /// </summary>
		private void _Save_Click(object sender, RoutedEventArgs e)
		{
            /*
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
			}*/
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

        #endregion Private Properties

        #region Public Properties

        #endregion Public Properties
    }
}