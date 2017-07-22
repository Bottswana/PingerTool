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

            _Model = (SettingsModel)DataContext;
			_Window = Window;
		}
        #endregion Initialiser

		#region Window Events
        /// <summary>
        /// Event handler for clicking the Save Button
        /// </summary>
		private void _Save_Click(object sender, RoutedEventArgs e)
		{
            // Not implemented yet
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