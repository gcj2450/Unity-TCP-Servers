namespace Network
{
	internal static class Log
	{
		private enum LogLevel
		{
			Info,
			Debug,
			Warn,
			Error
		}

		internal static void Info(string formater, params object[] args)
		{
			LogInternal(LogLevel.Info, string.Format(formater, args));
		}

		internal static void Info(object msg)
		{
			LogInternal(LogLevel.Info, msg.ToString());
		}

		internal static void Debug(string formater, params object[] args)
		{
			LogInternal(LogLevel.Debug, string.Format(formater, args));
		}

		internal static void Debug(object msg)
		{
			LogInternal(LogLevel.Debug, msg.ToString());
		}

		internal static void Warn(string formater, params object[] args)
		{
			LogInternal(LogLevel.Warn, string.Format(formater, args));
		}

		internal static void Warn(object msg)
		{
			LogInternal(LogLevel.Warn, msg.ToString());
		}

		internal static void Error(string formater, params object[] args)
		{
			LogInternal(LogLevel.Error, string.Format(formater, args));
		}

		internal static void Error(object msg)
		{
			LogInternal(LogLevel.Error, msg.ToString());
		}

		private static void LogInternal(LogLevel level, string msg)
		{
			LogHandlerRegister.LogHandler logHandler = null;
			switch (level)
			{
			case LogLevel.Info:
				logHandler = LogHandlerRegister.InfoHandler;
				break;
			case LogLevel.Debug:
				logHandler = LogHandlerRegister.DebugHandler;
				break;
			case LogLevel.Warn:
				logHandler = LogHandlerRegister.WarnHandler;
				break;
			case LogLevel.Error:
				logHandler = LogHandlerRegister.ErrorHandler;
				break;
			}
			if (logHandler != null)
			{
				logHandler(msg);
			}
		}
	}
}
