namespace Network
{
	public class Helper
	{
		public static void Int32ToByteArray(int value, byte[] buf, int offset)
		{
			buf[offset + 3] = (byte)(value >> 24);
			buf[offset + 2] = (byte)(value >> 16);
			buf[offset + 1] = (byte)(value >> 8);
			buf[offset] = (byte)value;
		}
	}
}
