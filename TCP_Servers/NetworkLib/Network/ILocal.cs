namespace Network
{
	public interface ILocal
	{
		string RemoteIp { get; }

		int RemotePort { get; }
	}
}
