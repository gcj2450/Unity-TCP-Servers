using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Network
{
	internal class Connector : IRemote, ILocal
	{
		public delegate void ConnectorMessageHandler(IRemote remote, Message msg);

		private const int HeadLen = 4;

		private Socket sysSocket;

		private ConnectorBuffer receiveBuffer = new ConnectorBuffer();

		private ConnectorBuffer sendBuffer = new ConnectorBuffer();

		private readonly SwapContainer<Queue<Message>> msgQueue = new SwapContainer<Queue<Message>>();

		private RC4 rc4Read;

		private RC4 rc4Write;

		internal bool DefferedClose { get; private set; }

		public bool Connected { get; set; }

		public int Id { get; private set; }

		public string RemoteIp
		{
			get
			{
				if (sysSocket != null)
				{
					IPEndPoint iPEndPoint = sysSocket.RemoteEndPoint as IPEndPoint;
					if (iPEndPoint != null)
					{
						return iPEndPoint.Address.ToString();
					}
				}
				return "";
			}
		}

		public int RemotePort
		{
			get
			{
				if (sysSocket != null)
				{
					IPEndPoint iPEndPoint = sysSocket.RemoteEndPoint as IPEndPoint;
					if (iPEndPoint != null)
					{
						return iPEndPoint.Port;
					}
				}
				return 0;
			}
		}

		public Connector(Socket s, int id)
		{
			sysSocket = s;
			Id = id;
			Connected = true;
		}

		public int PushBegin(int len)
		{
			if (!Connected)
			{
				return -1;
			}
			byte[] array = new byte[4];
			Helper.Int32ToByteArray(len, array, 0);
			sendBuffer.PushData(array, array.Length);
			return 4;
		}

		public int PushMore(byte[] buffer, int len, int offset = 0)
		{
			if (rc4Write != null)
			{
				rc4Write.Encrypt(buffer, len, offset);
			}
			sendBuffer.PushData(buffer, len, offset);
			return len;
		}

		public int Push(byte[] buffer, int len, int offset = 0)
		{
			if (!Connected)
			{
				return -1;
			}
			if (offset + len >= buffer.Length)
			{
				return -2;
			}
			if (rc4Write != null)
			{
				rc4Write.Encrypt(buffer, len, offset);
			}
			byte[] array = new byte[4];
			Helper.Int32ToByteArray(len, array, 0);
			sendBuffer.PushData(array, array.Length);
			sendBuffer.PushData(buffer, len, offset);
			return buffer.Length + 4;
		}

		public int Pushv(params byte[][] buffers)
		{
			if (!Connected)
			{
				return -1;
			}
			int num = 0;
			foreach (byte[] array in buffers)
			{
				num += array.Length;
			}
			if (rc4Write != null)
			{
				foreach (byte[] array2 in buffers)
				{
					rc4Write.Encrypt(array2, array2.Length);
				}
			}
			byte[] array3 = new byte[4];
			Helper.Int32ToByteArray(num, array3, 0);
			sendBuffer.PushData(array3, array3.Length);
			foreach (byte[] array4 in buffers)
			{
				sendBuffer.PushData(array4, array4.Length);
			}
			return num + 4;
		}

		public void Send()
		{
			if (sendBuffer.Length <= 0)
			{
				return;
			}
			try
			{
				int num = sysSocket.Send(sendBuffer.Buffer, sendBuffer.Start, sendBuffer.Length, SocketFlags.None);
				if (num == sendBuffer.Length)
				{
					sendBuffer.Reset();
				}
				else
				{
					sendBuffer.Consume(num);
				}
			}
			catch (SocketException ex)
			{
				if (ex.ErrorCode != 10035)
				{
					Log.Debug("Connector.SendData error connectId={0} errorCode={1} msg={2}", Id, ex.ErrorCode, ex.Message);
				}
				if (ex.ErrorCode == 10054 || ex.ErrorCode == 10053 || ex.ErrorCode == 10058)
				{
					sendBuffer.Reset();
					DefferedClose = true;
				}
			}
			catch (Exception ex2)
			{
				sendBuffer.Reset();
				Log.Warn("Connector.SendData error={0}", ex2);
				DefferedClose = true;
			}
		}

		public void BeginReceive()
		{
			Receive();
		}

		public void ProcessMessageQueue(ConnectorMessageHandler msgProcessor)
		{
			if (Connected)
			{
				if (msgQueue.Out.Count == 0)
				{
					msgQueue.Swap();
				}
				while (msgQueue.Out.Count > 0)
				{
					Message msg = msgQueue.Out.Dequeue();
					msgProcessor(this, msg);
				}
			}
		}

		public void Close()
		{
			Connected = false;
			if (sysSocket != null)
			{
				sysSocket.Close();
				sysSocket = null;
			}
			receiveBuffer.Dispose();
			sendBuffer.Dispose();
			receiveBuffer = null;
			sendBuffer = null;
		}

		private void Receive()
		{
			if (!Connected)
			{
				return;
			}
			try
			{
				if (receiveBuffer.EnsureFreeSpace(1))
				{
					sysSocket.BeginReceive(receiveBuffer.Buffer, receiveBuffer.Position, receiveBuffer.Free, SocketFlags.None, OnReceivedCallback, this);
				}
				else
				{
					DefferedClose = true;
				}
			}
			catch (SocketException ex)
			{
				Log.Debug("Connector.Receive error={0}", ex.Message);
			}
			catch (Exception ex2)
			{
				Log.Warn("Connector.Receive error={0}", ex2);
			}
		}

		private void OnReceivedCallback(IAsyncResult ar)
		{
			int num = 0;
			try
			{
				if (sysSocket != null)
				{
					num = sysSocket.EndReceive(ar);
				}
			}
			catch (ObjectDisposedException)
			{
				Log.Debug("Connector.ReceiveCallback objectDisposedException connectId={0}", Id);
				DefferedClose = true;
				return;
			}
			catch (SocketException ex2)
			{
				Log.Debug("Connector.ReceiveCallback connectId={0} errorCode={1} errorMessage={2}", Id, ex2.ErrorCode, ex2.Message);
				if (ex2.ErrorCode == 10054 || ex2.ErrorCode == 10053 || ex2.ErrorCode == 10058)
				{
					DefferedClose = true;
				}
				return;
			}
			catch (Exception)
			{
				Log.Error("Connector.ReceiveCallback exception connectId={0}", Id);
				DefferedClose = true;
				return;
			}
			if (num == 0)
			{
				Log.Debug("Connector.ReceiveCallback  read is 0 connectId={0}", Id);
				return;
			}
			receiveBuffer.Produce(num);
			while (receiveBuffer.Length >= 4)
			{
				int num2 = BitConverter.ToInt32(receiveBuffer.Buffer, receiveBuffer.Start);
				if (num2 < 0)
				{
					Log.Warn("Connector.ReceiveCallback size={0} id={1} buffer={2} bytesRead={3}", num2, Id, receiveBuffer, num);
					break;
				}
				if (receiveBuffer.Length >= num2 + 4)
				{
					byte[] array = null;
					array = new byte[num2];
					Buffer.BlockCopy(receiveBuffer.Buffer, receiveBuffer.Start + 4, array, 0, num2);
					if (rc4Read != null)
					{
						rc4Read.Encrypt(array, num2);
					}
					receiveBuffer.Consume(num2 + 4);
					if (receiveBuffer.Length == 0)
					{
						receiveBuffer.Reset();
					}
					try
					{
						MessageQueueEnqueue(array);
					}
					catch (Exception msg)
					{
						Log.Error(msg);
					}
					continue;
				}
				break;
			}
			Receive();
		}

		private void MessageQueueEnqueue(byte[] buf)
		{
			Message item = new Message(buf);
			lock (msgQueue.Lock)
			{
				msgQueue.In.Enqueue(item);
			}
		}
	}
}
