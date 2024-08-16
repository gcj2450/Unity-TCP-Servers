namespace Network
{
	internal class SwapContainer<T> where T : new()
	{
		public T In = new T();

		public T Out = new T();

		public readonly object Lock = new object();

		public void Swap()
		{
			lock (Lock)
			{
				T @in = In;
				In = Out;
				Out = @in;
			}
		}
	}
}
