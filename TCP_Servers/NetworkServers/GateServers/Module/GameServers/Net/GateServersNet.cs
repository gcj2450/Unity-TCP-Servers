using NetworkServers.NetworkServers;
using NetworkServers.NetworkServers.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkServers.NetworkServers.Module.GameServers.Net
{
    class GateServersNet : Singleton<GateServersNet>
    {
        public GateServersNet()
        {
            //将操作码以及事件添加到MessageCenter中的字典
            MessageCenter.instance.AddObserver(OperateCode.CONNECTGATE_REQ, CONNECTGATE_REQ);
        }

        public void CONNECTGATE_REQ(object data, Socket client)
        {
            MessageCenter.dealCount++;

            //找到发消息的GameServers存入allServers
            for (int i = 0; i < GateManager.allClients.Count; i++)
            {
                if (GateManager.allClients[i].Client.RemoteEndPoint.ToString() == client.RemoteEndPoint.ToString())
                {
                    ClientPeer tempClientPeer = GateManager.allClients[i];
                    //将服务器添加进服务器列表
                    GateManager.allServers.Add(tempClientPeer);
                    Console.WriteLine("----------------------------------------------");
                    Console.WriteLine("已将" + tempClientPeer.Client.RemoteEndPoint.ToString() + "添加进服务器列表");
                    //并从用户列表和用户字典删除
                    GateManager.allClients.Remove(tempClientPeer);
                    GateManager.allClientsDic.Remove(tempClientPeer.Client.RemoteEndPoint.ToString());
                    Console.WriteLine("已将" + tempClientPeer.Client.RemoteEndPoint.ToString() + "从用户列表和用户字典移除");
                    Console.WriteLine("----------------------------------------------");
                }
            }


        }
    }

}
