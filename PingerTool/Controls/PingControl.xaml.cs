using System;
using System.Net;
using PingerTool.Classes;
using System.Windows.Media;
using System.Windows.Controls;

namespace PingerTool.Controls
{
    public partial class PingControl : UserControl
    {
        private PingControlModel _Model;

        #region Initialiser
        public PingControl()
        {
            InitializeComponent();
            _Model = (PingControlModel)DataContext;
        }
        #endregion Initialiser
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