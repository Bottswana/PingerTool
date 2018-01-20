using System;
using System.IO;
using System.Net;
using Microsoft.Win32;
using Newtonsoft.Json;
using PingerTool.Windows;
using System.Collections.Generic;

namespace PingerTool.Classes
{
    public class ProjectControl
    {
        public string sCurrentFile;
        public bool SaveNeeded;

        private MainWindow _Window;
        private App _AppRef;

        #region Initialisation
        public ProjectControl(MainWindow Window)
        {
            _AppRef = App.GetApp();
            _Window = Window;

            sCurrentFile = null;
            SaveNeeded = false;

            // Open last opened file, or new workspace
            var LastFile = GetLastOpenProject();
            var FileOpen = false;

            if( LastFile.Length > 0 ) FileOpen = OpenProject(LastFile);
            if( !FileOpen ) NewProject();
        }

        /// <summary>
        /// Trigger save required status
        /// </summary>
        public void TriggerSaveStatus()
        {
            if( !SaveNeeded )
            {
                _Window.Title = _Window.Title + "*";
                SaveNeeded = true;
            }
        }
        #endregion Initialisation

        #region Project Control
        public bool NewProject()
        {
            // Clear the data from the MainWindow Model
            _Window.Timeframes = new int[] { 2000, 2000, 0, 5 };
            _Window.Title = "PingerTool - Untitled Project";
            _Window.Model.GraphHeight = 100;
            _Window.Notification = false;
            _Window.ClearAllElements();
            _Window.Spark = null;

            // Shut down webserver
            if( _Window.Server != null )
            {
                _Window.Server.Dispose();
                _Window.Server = null;
            }

            sCurrentFile = null;
            SaveNeeded = false;
            return true;
        }

        public bool OpenProject(string FilePath)
        {
            try
            {
                // Get FileData
                var FileObject = JsonConvert.DeserializeObject<SaveFileData>(File.ReadAllText(FilePath));
                _Window.ClearAllElements();
                _Window.Spark = null;

                // Shut down webserver
                if( _Window.Server != null )
                {
                    _Window.Server.Dispose();
                    _Window.Server = null;
                }

                // Restore Data
                _Window.Timeframes = FileObject.Timeframes;
                _Window.Model.GraphHeight = FileObject.GraphHeight;
                _Window.Notification = FileObject.NotificationEnabled;
                foreach( var PingElement in FileObject.PingElements )
                {
                    if( PingElement.Address == null || PingElement.Name == null )
                    {
                        _AppRef.Log.Warn("Unable to import PingElement due to invalid name or address: {0}, {1}", PingElement.Address, PingElement.Name);
                    }
                    else
                    {
                        if( IPAddress.TryParse(PingElement.Address, out IPAddress Address) )
                        {
                            _Window.CreatePingElement(PingElement.Name, Address, PingElement.GraphHidden);
                        }
                        else
                        {
                            _AppRef.Log.Warn("Unable to import PingElement due to invalid address: {0}", PingElement.Address);
                        }
                    }
                }

                // Restore Webserver
                if( FileObject.WebEnabled )
                {
                    _Window.Server = new WebServer(
                        FileObject.WebBindAddress,
                        FileObject.WebAllowedSubnets,
                        FileObject.WebAuthEnabled,
                        FileObject.WebUsername,
                        FileObject.WebPassword 
                    );
                }

                // Restore Spark
                if( FileObject.SparkEnabled )
                {
                    _Window.Spark = new Spark(
                        FileObject.SparkRoomId,
                        FileObject.SparkWarn,
                        FileObject.SparkTime
                    );
                }

                // Restore Environment
                _Window.Title = "PingerTool - " + Path.GetFileName(FilePath);
                UpdateLastOpenProject(FilePath);
                sCurrentFile = FilePath;
                SaveNeeded = false;
                return true;
            }
            catch( Exception Ex )
            {
                _AppRef.Log.Error(Ex, "Unable to open project");
                return false;
            }
        }

        public bool SaveProject(string FilePath)
        {
            try
            {
                // Get list of Ping Controls
                var PingWindow = new List<SaveFileData.PingElement>();
                foreach( var Element in _Window.GetAllElements() )
                {
                    PingWindow.Add(new SaveFileData.PingElement()
                    {
                        Address = Element.Address.ToString(),
                        GraphHidden = !Element.ShowGraph,
                        Name = Element.DisplayName
                    });
                }

                // Convert into file data
                var FileData = JsonConvert.SerializeObject(new SaveFileData()
                {
                    WebAllowedSubnets = ( _Window.Server != null ) ? String.Join(",", _Window.Server.AllowedSubnets) : null,
                    WebAuthEnabled = ( _Window.Server != null ) ? _Window.Server.AuthEnabled : false,
                    WebBindAddress = ( _Window.Server != null ) ? _Window.Server.BindAddress : null,
                    WebPassword = ( _Window.Server != null ) ? _Window.Server.AuthDetails[1] : null,
                    WebUsername = ( _Window.Server != null ) ? _Window.Server.AuthDetails[0] : null,
                    SparkRoomId = ( _Window.Spark != null ) ? _Window.Spark.SelectedCircleId : null,
                    SparkTime = ( _Window.Spark != null ) ? _Window.Spark.TimeoutThreshold   : 0,
                    SparkWarn = ( _Window.Spark != null ) ? _Window.Spark.WarningThreshold   : 0,
                    NotificationEnabled = _Window.Notification,
                    SparkEnabled = ( _Window.Spark != null ),
                    WebEnabled = ( _Window.Server != null ),
                    GraphHeight = _Window.Model.GraphHeight,
                    Timeframes = _Window.Timeframes,
                    PingElements = PingWindow
                });

                // Write to file
                File.WriteAllText(FilePath, FileData);
                UpdateLastOpenProject(FilePath);

                // Update Environment
                _Window.Title = "PingerTool - " + Path.GetFileName(FilePath);
                sCurrentFile = FilePath;
                SaveNeeded = false;
                return true;
            }
            catch( Exception Ex )
            {
                _AppRef.Log.Error(Ex, "Unable to save project");
                return false;
            }
        }
        #endregion Project Control

        #region Project Info
        /// <summary>
        /// Update the last open project file
        /// </summary>
        /// <param name="ProjectFile">File path of the file</param>
        public void UpdateLastOpenProject(string ProjectFile)
        {
            try
            {
                using( var RootKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default) )
                {
                    // Accessing existing key
                    using( var TSKey = RootKey.OpenSubKey(@"Software\PingerTool", true) )
                    { 
                        if( TSKey != null )
                        {
                            // Key Exists
                            TSKey.SetValue("LastFile", ProjectFile);
                            return;
                        }
                    }

                    // Create new key
                    using( var TSKey = RootKey.CreateSubKey(@"Software\PingerTool") )
                    {
                        // Create new key
                        TSKey.SetValue("LastFile", ProjectFile);
                    }
                }
            }
            catch( Exception Ex )
            {
                _AppRef.Log.Error(Ex, "Unable to access registry keys");
                return;
            }
        }

        /// <summary>
        /// Get last open file
        /// </summary>
        /// <returns>File path of file</returns>
        public string GetLastOpenProject()
        {
            try
            {
                using( var RootKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default) )
                {
                    // Accessing existing key
                    using( var TSKey = RootKey.OpenSubKey(@"Software\PingerTool", true) )
                    { 
                        if( TSKey != null )
                        {
                            // Key Exists
                            return (string)TSKey.GetValue("LastFile", "");
                        }
                    }

                    // Create new key
                    using( var TSKey = RootKey.CreateSubKey(@"Software\PingerTool") )
                    {
                        // Create new key
                        return "";
                    }
                }
            }
            catch( Exception Ex )
            {
                _AppRef.Log.Error(Ex, "Unable to access registry keys");
                return null;
            }
        }
        #endregion Project Info
    }

    public class SaveFileData
    {
        #region Save File Structure
        public class PingElement
        {
            public string Address { get; set; }
            public string Name { get; set; }
            public bool GraphHidden { get; set; }
        }

        /// <summary>
        /// List of current Ping Elements
        /// </summary>
        public List<PingElement> PingElements { get; set; }

        /// <summary>
        /// Height of individual graph elements
        /// </summary>
        public int GraphHeight { get; set; }

        /// <summary>
        /// Timeout and Warning Intervals
        /// </summary>
        public int[] Timeframes { get; set; }

        /// <summary>
        /// If audio notification is enabled
        /// </summary>
        public bool NotificationEnabled { get; set; }

        /// <summary>
        /// If spark integration is enabled
        /// </summary>
        public bool SparkEnabled { get; set; }

        /// <summary>
        /// Spark Circle/Room ID
        /// </summary>
        public RoomData.Rooms SparkRoomId { get; set; }

        /// <summary>
        /// Threshold for Warnings for Spark Integration
        /// </summary>
        public int SparkWarn { get; set; }

        /// <summary>
        /// Threshold for Timeouts for Spark Integration
        /// </summary>
        public int SparkTime { get; set; }

        /// <summary>
        /// Address to bind webserver to
        /// </summary>
        public string WebBindAddress { get; set; }

        /// <summary>
        /// Allowed subnets for webserver
        /// </summary>
        public string WebAllowedSubnets { get; set; }

        /// <summary>
        /// Username for webserver authentication
        /// </summary>
        public string WebUsername { get; set; }

        /// <summary>
        /// Password for webserver authentication
        /// </summary>
        public string WebPassword { get; set; }

        /// <summary>
        /// If webserver is enabled
        /// </summary>
        public bool WebEnabled { get; set; }

        /// <summary>
        /// If webserver auth is enabled
        /// </summary>
        public bool WebAuthEnabled { get; set; }
        #endregion Save File Structure
    }
}