using System;
using System.Net;
using System.Windows;
using PingerTool.Classes;

namespace PingerTool.Windows
{
    public partial class AddDialog
    {
        private bool _EditMode = false;
        private AddDialogModel _Model;
        private MainWindow _Window;
        private IPAddress _OrigIP;

        #region Initialiser
        public AddDialog(MainWindow Window, IPAddress Address = null, string DisplayName = null)
        {
            InitializeComponent();

            _Model = (AddDialogModel)DataContext;
            _Window = Window;

            // Populate info if in edit mode
            if( Address != null && DisplayName != null )
            {
                _Model.IPAddress = Address.ToString();
                _Model.DisplayName = DisplayName;
                _OrigIP = Address;
                _EditMode = true;
            }
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
                var AddressString = ( _Model.IPAddress.Contains(",") ) ? _Model.IPAddress.Split(',')[1] : _Model.IPAddress;
                if( IPAddress.TryParse(AddressString.Trim(), out IPAddress ParsedAddress) )
                {
                    if( _EditMode )
                    {
                        // Edit existing check
                        if( _OrigIP.Equals(ParsedAddress) )
                        {
                            // Just update name
                            _Window.UpdatePingElementName(ParsedAddress, _Model.DisplayName);
                            Close();
                        }
                        else
                        {
                            // Update address (and/or name)
                            _Window.UpdatePingElementName(_OrigIP, _Model.DisplayName);
                            if( _Window.UpdatePingElementAddress(_OrigIP, ParsedAddress) ) Close();
                            else MessageBox.Show("This IP Address already has a check associated to it", "Whoops", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        // Add ping Check
                        if( _Window.CreatePingElement(_Model.DisplayName, ParsedAddress) ) Close();
                        else MessageBox.Show("This IP Address already has a check associated to it", "Whoops", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Invalid IP
                    MessageBox.Show("The IP Address you entered is invalid", "Whoops", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
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