using System;
using log4net;
using log4net.Core;
using log4net.Layout;
using log4net.Config;
using log4net.Appender;
using log4net.Repository.Hierarchy;

namespace PingerTool.Classes
{
	#region Log Configurator
	class LogInitiator
	{
		public static void ConfigureLog(string LogFileName, string LogLevel)
		{
			// Generic Configuration
			Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
			hierarchy.Root.RemoveAllAppenders();

			var PatternLayout = new PatternLayout()
			{
				ConversionPattern = "%date{dd MMM yyyy HH:mm:ss} [%-10c] %-5p:	%m%n"
			};

			// Get Logging Level
			var LoggingLevel = hierarchy.LevelMap[LogLevel];
			if( LoggingLevel == null )
			{
				LoggingLevel = Level.All;
			}

			// Configure Event Log
			var EventLog = new EventLogAppender()
			{
				ApplicationName = "PingerTool",
				Layout			= PatternLayout,
				Threshold		= Level.Warn
			};

			// Configure File Log
			var LogFile = new FileAppender()
			{
				File			= LogFileName,
				LockingModel	= new FileAppender.MinimalLock(),
				Layout			= PatternLayout,
				#if DEBUG
				Threshold		= Level.Debug,
				#else
				Threshold		= LoggingLevel,
				#endif
				AppendToFile	= true
			};

			// Activate Logging
			PatternLayout.ActivateOptions();
			EventLog.ActivateOptions();
			LogFile.ActivateOptions();

			BasicConfigurator.Configure(new IAppender[] { EventLog, LogFile });
		}
	}
	#endregion Log Configurator

	public class Log
	{
		private ILog Logger;

		public Log(string LogName)
		{
			Logger = LogManager.GetLogger(LogName);
		}

		#region Log Methods
		public void Debug(string formatString, params object[] paramList)
		{
			Logger.Debug(string.Format(formatString, paramList));
		}

		public void Debug(Exception Ex, string formatString, params object[] paramList)
		{
			Logger.Debug(string.Format(formatString, paramList), Ex);
		}

		public void Info(string formatString, params object[] paramList)
		{
			Logger.Info(string.Format(formatString, paramList));
		}

		public void Info(Exception Ex, string formatString, params object[] paramList)
		{
			Logger.Info(string.Format(formatString, paramList), Ex);
		}

		public void Warn(string formatString, params object[] paramList)
		{
			Logger.Warn(string.Format(formatString, paramList));
		}

		public void Warn(Exception Ex, string formatString, params object[] paramList)
		{
			Logger.Warn(string.Format(formatString, paramList), Ex);
		}

		public void Error(string formatString, params object[] paramList)
		{
			Logger.Error(string.Format(formatString, paramList));
		}

		public void Error(Exception Ex, string formatString, params object[] paramList)
		{
			Logger.Error(string.Format(formatString, paramList), Ex);
		}

		public void Fatal(string formatString, params object[] paramList)
		{
			Logger.Fatal(string.Format(formatString, paramList));
		}

		public void Fatal(Exception Ex, string formatString, params object[] paramList)
		{
			Logger.Fatal(string.Format(formatString, paramList), Ex);
		}
		#endregion Log Methods
	}
}