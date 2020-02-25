using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;

namespace SurviveCoreServer
{
    public class Server : IDisposable
    {
        private const int port = 8888;
        private const int maxplayers = 10;

        private NetManager server;
        private Dictionary<NetPeer, Client> clients;
        
        
        
        public Server()
        {
            clients = new Dictionary<NetPeer, Client>();
            EventBasedNetListener listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.Start(port);

            NetDataWriter writer = new NetDataWriter();
            
            listener.ConnectionRequestEvent += request =>
            {
                if(server.PeersCount < maxplayers)
                    request.AcceptIfKey("SurvivalGameNetwork");
                else
                    request.Reject();
            };

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("We got connection: {0}", peer.EndPoint);
                clients.Add(peer, new Client());            
                //writer.Put("Hello client!");                                // Put some string
                //peer.Send(writer, DeliveryMethod.ReliableOrdered);
            };

            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                clients.Remove(peer);
            };

            listener.NetworkReceiveEvent += (peer, reader, method) =>
            {
                switch ((PacketType)reader.GetInt())
                {
                    case PacketType.PositionUpdate:
                        clients[peer].Position = new Vector3(reader.GetFloat(),reader.GetFloat(),reader.GetFloat());
                        break;
                    case PacketType.ChunkRequest:
                        
                    default:
                        Console.WriteLine("Received bad package!");
                        break;
                }
            };
            
        }

        public void Run()
        {
            while (!Console.KeyAvailable)
            {
                server.PollEvents();
                //server.SendToAll(writer, DeliveryMethod.ReliableOrdered);
                Thread.Sleep(15);
            }
        }
        
        public void Dispose()
        {
            server?.Stop();
        }
    }

    public class Client
    {
        public Vector3 Position;
    }
    
}