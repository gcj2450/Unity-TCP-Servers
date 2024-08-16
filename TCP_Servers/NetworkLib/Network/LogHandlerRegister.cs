namespace Network
{
	public static class LogHandlerRegister
	{
		public delegate void LogHandler(string msg);

		public static LogHandler InfoHandler;

		public static LogHandler DebugHandler;

		public static LogHandler WarnHandler;

		public static LogHandler ErrorHandler;
	}
}
