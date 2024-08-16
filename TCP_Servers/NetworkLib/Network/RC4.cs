using System.Text;

namespace Network
{
	internal class RC4
	{
		private readonly byte[] mBox;

		private static readonly Encoding Encode = Encoding.Default;

		private const int mBoxLength = 256;

		private int x;

		private int y;

		public RC4(string pass)
		{
			mBox = GetKey(Encode.GetBytes(pass));
			x = 0;
			y = 0;
		}

		public void Encrypt(byte[] data, int len, int offset = 0)
		{
			if (data != null)
			{
				for (int i = offset; i < offset + len && i < data.Length; i++)
				{
					x = (x + 1) % 256;
					y = (y + mBox[x]) % 256;
					byte b = mBox[x];
					mBox[x] = mBox[y];
					mBox[y] = b;
					byte b2 = mBox[(mBox[x] + mBox[y]) % 256];
					data[i] ^= b2;
				}
			}
		}

		private static byte[] GetKey(byte[] pass)
		{
			byte[] array = new byte[256];
			for (int i = 0; i < 256; i++)
			{
				array[i] = (byte)i;
			}
			int num = 0;
			for (int j = 0; j < 256; j++)
			{
				num = (num + array[j] + pass[j % pass.Length]) % 256;
				byte b = array[j];
				array[j] = array[num];
				array[num] = b;
			}
			return array;
		}
	}
}
