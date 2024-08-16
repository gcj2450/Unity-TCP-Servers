using System;

namespace Network
{
	internal class ConnectorBuffer : IDisposable
	{
		private byte[] bufferInner = new byte[1024];

		private int position;

		private int begin;

		public byte[] Buffer
		{
			get
			{
				return bufferInner;
			}
		}

		public int Start
		{
			get
			{
				return begin;
			}
		}

		public int Position
		{
			get
			{
				return position;
			}
		}

		public int Length
		{
			get
			{
				return position - begin;
			}
		}

		public int Free
		{
			get
			{
				return bufferInner.Length - position;
			}
		}

		public void PushData(byte[] data, int size, int offset = 0)
		{
			CheckResize(size);
			System.Buffer.BlockCopy(data, offset, bufferInner, position, size);
			position += size;
		}

		public void Consume(int offset)
		{
			begin += offset;
		}

		public void Produce(int offset)
		{
			position += offset;
		}

		public void Reset()
		{
			position = 0;
			begin = 0;
		}

		public bool EnsureFreeSpace(int free)
		{
			CheckResize(free);
			return true;
		}

		private void CheckResize(int size)
		{
			int num = bufferInner.Length;
			while (num - position < size)
			{
				num *= 2;
			}
			if (num > bufferInner.Length)
			{
				byte[] dst = new byte[num];
				if (position > 0 && position <= bufferInner.Length)
				{
					int count = position - begin;
					System.Buffer.BlockCopy(bufferInner, begin, dst, 0, count);
					begin = 0;
					position = count;
				}
				bufferInner = dst;
			}
		}

		public void Dispose()
		{
			Reset();
			bufferInner = null;
		}

		public override string ToString()
		{
			return string.Format("{{bufferInner.Len:{0} position:{1} begin:{2}}}", bufferInner.Length, position, begin);
		}
	}
}
