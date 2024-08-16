using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
	public class ServerNetwork
	{
		public delegate void ServerNetworkClientConnectedHandler(IRemote remote);

		public delegate void ServerNetworkClientDisconnectedHandler(IRemote remote);

		public delegate void ServerNetworkClientMessageReceivedHandler(IRemote remote, Message msg);

		public ServerNetworkClientConnectedHandler OnClientConnected;

		public ServerNetworkClientDisconnectedHandler OnClientDisconnected;

		public ServerNetworkClientMessageReceivedHandler OnClientMessageReceived;

		private readonly Dictionary<int, Connector> clientConnectorsDict = new Dictionary<int, Connector>();

		private readonly SwapContainer<Queue<Connector>> toAddClientConnectors = new SwapContainer<Queue<Connector>>();

		private readonly SwapContainer<Queue<Connector>> toRemoveClientConnectors = new SwapContainer<Queue<Connector>>();

		private int nextClientConnectorId = 1;

		private Socket listenSocket;

		public int ClientCount
		{
			get
			{
				return clientConnectorsDict.Count;
			}
		}

		public ServerNetwork(int port)
		{
			listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
			listenSocket.Listen(10);
		}

		public void BeginAccept()
		{
			listenSocket.BeginAccept(OnAcceptedCallback, null);
		}

		public void Poll()
		{
			try
			{
				ProcessClientConnectorsMessageQueue();
				RefreshClientList();
			}
			catch (Exception msg)
			{
				Log.Error(msg);
			}
		}

		internal void CloseClient(Connector connector, NetworkCloseMode mode)
		{
			if (connector == null)
			{
				Log.Warn("ServerNetwork.CloseClient socket is null");
				return;
			}
			lock (toRemoveClientConnectors.Lock)
			{
				toRemoveClientConnectors.In.Enqueue(connector);
				Log.Debug("ServerNetwork.CloseClient connectId={0}", mode, connector.Id);
			}
		}

		public void Dispose()
		{
			if (listenSocket != null)
			{
				listenSocket.Close();
			}
			Log.Debug("ServerSocket.Dispose");
		}

		private void OnAcceptedCallback(IAsyncResult ar)
		{
			try
			{
				Socket socket = listenSocket.EndAccept(ar);
				socket.SendTimeout = 500;
				socket.ReceiveTimeout = 500;
				socket.NoDelay = true;
				int id = nextClientConnectorId;
				Interlocked.Exchange(ref nextClientConnectorId, (nextClientConnectorId + 1 >= int.MaxValue) ? 1 : (nextClientConnectorId + 1));
				while (clientConnectorsDict.ContainsKey(nextClientConnectorId))
				{
					Interlocked.Exchange(ref nextClientConnectorId, (nextClientConnectorId + 1 >= int.MaxValue) ? 1 : (nextClientConnectorId + 1));
				}
				Connector item = new Connector(socket, id);
				lock (toAddClientConnectors.Lock)
				{
					toAddClientConnectors.In.Enqueue(item);
				}
				listenSocket.BeginAccept(OnAcceptedCallback, null);
			}
			catch (Exception)
			{
			}
		}

		private void ProcessClientConnectorsMessageQueue()
		{
			foreach (Connector value in clientConnectorsDict.Values)
			{
				value.ProcessMessageQueue(delegate(IRemote c, Message msg)
				{
					if (OnClientMessageReceived != null)
					{
						OnClientMessageReceived(c, msg);
					}
				});
			}
		}

		private void RefreshClientList()
		{
			if (toAddClientConnectors.Out.Count == 0)
			{
				toAddClientConnectors.Swap();
				foreach (Connector item in toAddClientConnectors.Out)
				{
					if (clientConnectorsDict.ContainsKey(item.Id))
					{
						Log.Warn("ServerNetwork.RefreshClientList connector exist id={0}", item.Id);
						return;
					}
					clientConnectorsDict.Add(item.Id, item);
					if (OnClientConnected != null)
					{
						OnClientConnected(item);
					}
					item.BeginReceive();
				}
				toAddClientConnectors.Out.Clear();
			}
			if (toRemoveClientConnectors.Out.Count == 0)
			{
				toRemoveClientConnectors.Swap();
				foreach (Connector item2 in toRemoveClientConnectors.Out)
				{
					if (clientConnectorsDict.ContainsKey(item2.Id))
					{
						clientConnectorsDict.Remove(item2.Id);
						item2.Close();
						if (OnClientDisconnected != null)
						{
							OnClientDisconnected(item2);
						}
					}
				}
				toRemoveClientConnectors.Out.Clear();
			}
			foreach (Connector value in clientConnectorsDict.Values)
			{
				value.Send();
				if (value.DefferedClose)
				{
					CloseClient(value, NetworkCloseMode.DefferedClose);
				}
			}
		}
	}
}
