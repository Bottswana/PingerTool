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
		private MainWindow _Window;
		public string sCurrentFile;
		public bool SaveNeeded;
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
			_Window.Title = "PingerTool - Untitled Project*";
            _Window.ClearAllElements();

            sCurrentFile = null;
			SaveNeeded = true;
			return true;
		}

		public bool OpenProject(string FilePath)
		{
			try
			{
				// Get FileData
				var FileObject = JsonConvert.DeserializeObject<SaveFileData>(File.ReadAllText(FilePath));

                // Restore Data
                _AppRef.TimeoutValue = FileObject.PingTimeout;
                _AppRef.WarningTimeframe = FileObject.WarningThreshold;
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
                            _Window.CreatePingElement(PingElement.Name, Address);
                        }
                        else
                        {
                            _AppRef.Log.Warn("Unable to import PingElement due to invalid address: {0}", PingElement.Address);
                        }
                    }
                }

				// Restore Environment
				_Window.Title = "PingerTool - " + Path.GetFileName(FilePath);
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
                        Name = Element.DisplayName
                    });
                }

				// Convert into file data
				var FileData = JsonConvert.SerializeObject(new SaveFileData()
				{
                    WarningThreshold = _AppRef.WarningTimeframe,
                    PingTimeout = _AppRef.TimeoutValue,
                    PingElements = PingWindow
				});

				// Write to file
				File.WriteAllText(FilePath, FileData);

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
		/// Get last open file
		/// </summary>
		/// <returns>File path of file</returns>
		public string GetLastOpenProject()
		{
			try
			{
				var RootKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
				var TSKey = RootKey.OpenSubKey(@"Software\PingerTool", true);
				if( TSKey != null )
				{
					// Key Exists
					return (string)TSKey.GetValue("LastFile", "");
				}
				else
				{
					// Create new key
					TSKey = RootKey.CreateSubKey(@"Software\PingerTool");
					return "";
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
        }

        /// <summary>
        /// List of current Ping Elements
        /// </summary>
        public List<PingElement> PingElements { get; set; }

        /// <summary>
        /// Threshold for warning when roundtrip is > than this 
        /// </summary>
        public int WarningThreshold { get; set; }

        /// <summary>
        /// Ping timeout value
        /// </summary>
        public int PingTimeout { get; set; }
        #endregion Save File Structure
	}
}