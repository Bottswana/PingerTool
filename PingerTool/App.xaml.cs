using System;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using System.Reflection;
using PingerTool.Classes;
using PingerTool.Windows;
using LukeSkywalker.IPNetwork;

namespace PingerTool
{
	public partial class App
	{
        public int WarningTimeframe = 2000;
        public int TimeoutValue = 2000;
		public Log Log;

		#region Initialisation
		[STAThread]
		public static void Main()
		{
			// Run Application
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Helpers.ExceptionHandler);
			var Application = new App();
				Application.InitializeComponent();
				Application.Run();
		}

		public App()
		{
			// Init Logger
			try
			{
                var ThisDir = AppDomain.CurrentDomain.BaseDirectory;
				LogInitiator.ConfigureLog($"{ThisDir}\\PingerTool.log", "Info");
			}
			catch( Exception )
			{
				MessageBox.Show($"Unable to initialise logfile, please ensure the directory is writable:\n{AppDomain.CurrentDomain.BaseDirectory}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				ExitApplication(true);
				return;
			}

			// Initial Log Information
			Log = new Log("PingerTool");
			Log.Info("-- Starting log file");

			Log.Info("PingerTool - Version {0}", Helpers.GetApplicationVersion());
            #if DEBUG
			Log.Info("Development Build, Compiled {0}", Helpers.GetLinkerTime());
            #else
			Log.Info("Release Build, Compiled {0}", Helpers.GetLinkerTime());
            #endif
		}
        #endregion Initialisation

        #region Public Methods
		/// <summary>
		/// Get Application Instance
		/// </summary>
		/// <returns>App Instance</returns>
		public static App GetApp()
		{
			return (App)Application.Current;
		}

		/// <summary>
		/// Exit application
		/// </summary>
		/// <param name="ForceQuit">False for graceful shutdown, true for forced</param>
		public void ExitApplication( bool ForceQuit = false )
		{
			if( ForceQuit )
			{
				Environment.Exit(-1);
			}
			else
			{
				if( ConfirmApplicationShutdown() )
                {
				    // Exit Application
				    Application.Current.Shutdown();
                }
			}
		}

        /// <summary>
        /// Confirm if an application shutdown is permitted
        /// </summary>
        /// <returns>True if allowed, false otherwise</returns>
        public bool ConfirmApplicationShutdown()
        {
			// Check if we need to save changes to our project
			var MainWindow = (MainWindow)Windows[0];
			if( MainWindow.Proj.SaveNeeded )
			{
				var Question = MessageBox.Show("Do you wish to save changes to this project before exiting?", "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
				if( Question == MessageBoxResult.Yes )
				{
					if( MainWindow.Proj.sCurrentFile == null )
					{
						// Open file is untitled, so open save as box
				        var FileDialog = new OpenFileDialog()
				        {
				            Filter			= "Project Files (*.pingtool)|*.pingtool",
				            Title			= "Save Project As",
				            DefaultExt		= ".pingtool",
					        CheckPathExists = true,
					        ValidateNames	= true,
					        AddExtension	= true
				        };

						if( FileDialog.ShowDialog() != true ) { return false; }
						if( !MainWindow.Proj.SaveProject(FileDialog.FileName) )
						{
							// Save Error
							MessageBox.Show("A save error has occoured, please check you have permission to save to this location.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
							return false;
						}
					}
					else
					{
						if( !MainWindow.Proj.SaveProject(MainWindow.Proj.sCurrentFile) )
						{
							// Save Error
							MessageBox.Show("A save error has occoured, please check you have permission to save to this location.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
						}
					}
				}
				else if( Question == MessageBoxResult.Cancel )
				{
                    return false;
				}
			}

            // Allow shutdown
            return true;
        }
        #endregion Public Methods

        #region Application Events
		/// <summary>
		/// Windows Logoff/Shutdown
		/// </summary>
		private void _Application_SessionEnding( object sender, SessionEndingCancelEventArgs e )
		{
			if( !ConfirmApplicationShutdown() && e != null )
            {
                // Cancel Shutdown
                e.Cancel = true;
            }
		}
        #endregion Application Events
    }

    static class Helpers
	{
        #region Global Helper Methods
		public static void ExceptionHandler( object sender, UnhandledExceptionEventArgs e )
		{
			var Exception = ( e.ExceptionObject as Exception );

			var EmergencyLog = log4net.LogManager.GetLogger("DPMServer");
			EmergencyLog.Fatal("Fatal Application Exception", Exception);
		}

        public static bool ContainsAddress( this IPNetwork Net, string Address )
        {
            // Get Bytes
            var LowerEnd = Net.FirstUsable.GetAddressBytes();
            var UpperEnd = Net.LastUsable.GetAddressBytes();

            // Parse IP
            if( System.Net.IPAddress.TryParse(Address, out System.Net.IPAddress ParsedAddress) )
            {
                var addressBytes = ParsedAddress.GetAddressBytes();
                bool Lower = true, Upper = true;

                for( int i = 0; i < LowerEnd.Length && (Lower || Upper); i++ )
                {
                    if( (Lower && addressBytes[i] < LowerEnd[i]) || (Upper && addressBytes[i] > UpperEnd[i]) )
                    {
                        return false;
                    }

                    Lower &= (addressBytes[i] == LowerEnd[i]);
                    Upper &= (addressBytes[i] == UpperEnd[i]);
                }

                return true;
            }
            else
            {
                throw new ArgumentException("Invalid Address");
            }
        }

		public static string SHA256Hash( string RawData )
		{
			var HashFactory = new System.Security.Cryptography.SHA256Managed();

			var HashData = HashFactory.ComputeHash(Encoding.UTF8.GetBytes(RawData));
			return Convert.ToBase64String(HashData);
		}

		public static string GetApplicationVersion()
		{
			Assembly execAssembly = Assembly.GetCallingAssembly();
			AssemblyName name = execAssembly.GetName();
			return string.Format("{0:0}.{1:0}.{2:0}",
				name.Version.Major.ToString(),
				name.Version.Minor.ToString(),
				name.Version.Build.ToString()
			);
		}

		public static DateTime GetLinkerTime()
		{
			Assembly assembly = Assembly.GetCallingAssembly();

			var filePath = assembly.Location;
			const int c_PeHeaderOffset = 60;
			const int c_LinkerTimestampOffset = 8;

			var buffer = new byte[2048];

			using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				stream.Read(buffer, 0, 2048);

			var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
			var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

			var linkTimeUtc = epoch.AddSeconds(secondsSince1970);
			var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, TimeZoneInfo.Local);
			return localTime;
		}

		static readonly DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0);
		public static DateTime FromUnixStamp( long secondsSinceepoch )
		{
			return epochStart.ToUniversalTime().AddSeconds(secondsSinceepoch).ToLocalTime();
		}

		public static long ToUnixStamp( DateTime dateTime )
		{
			return (long)( dateTime.ToUniversalTime() - epochStart ).TotalSeconds;
		}
		
		public static long ToUnixStamp( DateTime? dateTime )
		{
			if( dateTime == null ) return 0;
		
			var timeObj = (DateTime)dateTime;
			return (long)( timeObj.ToUniversalTime() - epochStart ).TotalSeconds;
		}

		public static long CurrentUnixStamp
		{
			get 
			{
				return (long)( DateTime.UtcNow - epochStart ).TotalSeconds;
			}
		}
        #endregion Global Helper Methods
	}
}