using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using FontAwesome.WPF;
using PingerTool.Windows;
using System.Windows.Media;
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
			// Reset data in MainWindow
			_Window.Title = "PingerTool - Untitled Project*";
            sCurrentFile = null;
			//_Window.ContentCtrl.Content = "";

			SaveNeeded = true;
			return true;
		}

		public bool OpenProject(string FilePath)
		{
			try
			{
				// Get FileData
				var FileObject = JsonConvert.DeserializeObject<SaveFileData>(File.ReadAllText(FilePath));

                // TODO: Restore Data

				// Restore Environment
				_Window.Title = "PingerTool - " + Path.GetFileName(FilePath);
				_AddToHistory(FilePath);

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
                // TODO: Set data to save

				// Convert into file data
				var FileData = JsonConvert.SerializeObject(new SaveFileData()
				{

				});

				// Write to file
				File.WriteAllText(FilePath, FileData);

				// Update Environment
				_Window.Title = "PingerTool - " + Path.GetFileName(FilePath);
				_AddToHistory(FilePath);

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

		/// <summary>
		/// Get array of recently opened files
		/// </summary>
		/// <returns>Array of file paths</returns>
		public List<string> GetRecentProjects()
		{
			try
			{
				var RootKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
				var TSKey = RootKey.OpenSubKey(@"Software\PingerTool", true);
				if( TSKey != null )
				{
					// Key Exists
					var RecentFiles = ((string)TSKey.GetValue("RecentFiles", "")).Split(';');
					return ( RecentFiles.Length > 0 ) ? new List<string>(RecentFiles) : new List<string>();
				}
				else
				{
					// Create new key
					TSKey = RootKey.CreateSubKey(@"Software\PingerTool");
					return new List<string>();
				}
			}
			catch( Exception Ex )
			{
				_AppRef.Log.Error(Ex, "Unable to access registry keys");
				return null;
			}
		}

		/// <summary>
		/// Add file to file history
		/// </summary>
		/// <param name="FileName">File Path</param>
		private void _AddToHistory(string FileName)
		{
			try
			{
				var RootKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
				var TSKey = RootKey.OpenSubKey(@"Software\PingerTool", true);
				if( TSKey != null )
				{
					// Update inside existing key
					var RecentFiles = ((string)TSKey.GetValue("RecentFiles", "")).Split(';');
					var NewArray = new List<string>();

					// Check we dont exist
					var ExistCount = RecentFiles.Where( q => { return q.Equals(FileName); } ).Count();
					if( ExistCount == 0 )
					{
						NewArray.Add(FileName);
						for( int i=0; i < 9; i++ )
						{
							if( i >= RecentFiles.Length || RecentFiles[i].Length == 0 ) continue;
							NewArray.Add(RecentFiles[i]);
						}

						// Save back to registry
						TSKey.SetValue("RecentFiles", String.Join(";", NewArray));
						TSKey.SetValue("LastFile", FileName);
					}
				}
				else
				{
					// Create new key
					TSKey = RootKey.CreateSubKey(@"Software\PingerTool");
					TSKey.SetValue("RecentFiles", FileName);
				}
			}
			catch( Exception Ex )
			{
				_AppRef.Log.Error(Ex, "Unable to access registry keys");
			}
		}
		#endregion Project Info
	}

    #region Save File Structure
	public class SaveFileData
	{

	}
    #endregion Save File Structure
}