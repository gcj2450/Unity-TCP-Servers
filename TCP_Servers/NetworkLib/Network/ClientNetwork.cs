using System;
using System.Net.Sockets;

namespace Network
{
	public class ClientNetwork
	{
		private class ConnectAsyncResult
		{
			public Exception Ex;

			public Connector Conn;
		}

		public delegate void ClientNetworkConnectedHandler(ILocal local, Exception e);

		public delegate void ClientNetworkDisconnectedHandler();

		public delegate void ClientNetworkMessageReceivedHandler(Message msg);

		public ClientNetworkConnectedHandler ConnectorConnected;

		public ClientNetworkDisconnectedHandler ConnectorDisconnected;

		public ClientNetworkMessageReceivedHandler ConnectorMessageReceived;

		private Connector connector;

		private ConnectAsyncResult defferedConnected = null;

		private readonly string hostIp;

		private readonly int hostPort;

		private readonly Socket sysSocket;

		public bool Connected
		{
			get
			{
				return connector != null && connector.Connected;
			}
		}

		public ClientNetwork(string ip, int port)
		{
			hostIp = ip;
			hostPort = port;
			sysSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
			{
				SendTimeout = 500,
				ReceiveTimeout = 500,
				NoDelay = true
			};
		}

		public void Connect()
		{
			if (!Connected)
			{
				Log.Info("BeginReceive sysSocket.Connect(hostIp, hostPort);");
				try
				{
					sysSocket.Connect(hostIp, hostPort);
				}
				catch (Exception ex)
				{
					Log.Error("ClientNetwork BeginReceive throw exp:{0}", ex);
					defferedConnected = new ConnectAsyncResult
					{
						Ex = ex
					};
					return;
				}
				defferedConnected = new ConnectAsyncResult
				{
					Conn = new Connector(sysSocket, 0)
				};
			}
		}

		public void SendData(byte[] buffer)
		{
			if (connector != null && connector.Connected)
			{
				connector.Push(buffer, buffer.Length);
			}
		}

		public void SendDatav(params byte[][] buffers)
		{
			if (connector != null && connector.Connected)
			{
				connector.Pushv(buffers);
			}
		}

		public void Poll()
		{
			try
			{
				if (defferedConnected != null)
				{
					connector = defferedConnected.Conn;
					connector.BeginReceive();
					if (ConnectorConnected != null)
					{
						ConnectorConnected(defferedConnected.Conn, defferedConnected.Ex);
					}
					defferedConnected = null;
				}
				RefreshMessageQueue();
				RefreshClient();
			}
			catch (Exception msg)
			{
				Log.Error(msg);
			}
		}

		public void Close()
		{
			if (Connected)
			{
				connector.Close();
				if (ConnectorDisconnected != null)
				{
					ConnectorDisconnected();
				}
			}
		}

		public void SetClientRc4Key(string key)
		{
			if (connector == null)
			{
			}
		}

		public void Dispose()
		{
			Close();
		}

		private void RefreshClient()
		{
			if (Connected)
			{
				if (!connector.DefferedClose)
				{
					connector.Send();
				}
				if (connector.DefferedClose)
				{
					Close();
				}
			}
		}

		private void RefreshMessageQueue()
		{
			if (!Connected)
			{
				return;
			}
			connector.ProcessMessageQueue(delegate(IRemote c, Message msg)
			{
				if (ConnectorMessageReceived != null)
				{
					ConnectorMessageReceived(msg);
				}
			});
		}
	}
}
