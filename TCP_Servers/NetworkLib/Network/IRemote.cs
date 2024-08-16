namespace Network
{
	public interface IRemote
	{
		string RemoteIp { get; }

		int RemotePort { get; }

		int Id { get; }

		bool Connected { get; }

		int Push(byte[] buffer, int len, int offset);

		int PushBegin(int len);

		int PushMore(byte[] buffer, int len, int offset);
	}
}
