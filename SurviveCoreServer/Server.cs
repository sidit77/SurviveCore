using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using LiteNetLib;
using SurviveCore.Network;

namespace SurviveCore
{
    public class Server : IDisposable
    {

        public const int maxnamechars = 20;
        //public const int networkkey = 'S' << 24 | 'G' << 16 | 'N' << 8 | 'K';
        public const int port = 8888;
        
        private const int maxplayers = 10;

        private NetManager server;
        private Dictionary<int, (NetPeer peer, Player data)> clients;
        
        
        
        public Server()
        {
            clients = new Dictionary<int, (NetPeer, Player)>();
            
            EventBasedNetListener listener = new EventBasedNetListener();
            server = new NetManager(listener);
            

            PacketProcessor packetProcessor = new PacketProcessor();

            listener.NetworkReceiveEvent += packetProcessor.ReadAllPackets;
            
            listener.ConnectionRequestEvent += request =>
            {
                if(server.PeersCount > maxplayers)
                    request.Reject();
                else
                    request.AcceptIfKey("SurvivalGameNetwork");
            };

            listener.PeerConnectedEvent += peer => Console.WriteLine("We got connection: {0}", peer.EndPoint);

            packetProcessor.Subscribe(PacketType.JoinRequest, (peer, reader) =>
            {
                string name = reader.GetString(maxnamechars);
                
                if(clients.ContainsKey(peer.Id) || clients.Values.Any(p => p.Item2.Name.Equals(name)))
                    peer.Disconnect();
                else
                {
                    Player nplayer = new Player{
                        Name = name,
                        Position = new Vector3(0, 0, 0)
                    };
                    Console.WriteLine($"{name} has joined the game");
                    foreach (var (netPeer, _) in clients.Values)
                    {
                        packetProcessor.Send(netPeer, PacketType.PlayerJoinedEvent, writer =>
                        {
                            writer.Put(peer.Id);
                            writer.Put(nplayer);
                        }, DeliveryMethod.ReliableOrdered);
                    }
                    clients.Add(peer.Id, (peer, nplayer));
                    
                    packetProcessor.Send(peer, PacketType.JoinResponse, writer =>
                    {
                        writer.Put(peer.Id);
                        writer.Put(clients.Count);
                        foreach (var (netPeer, player) in clients.Values)
                        {
                            writer.Put(netPeer.Id);
                            writer.Put(player);
                        }
                    }, DeliveryMethod.ReliableOrdered);
                    
                    
                }
                
            });
            
            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                if (clients.ContainsKey(peer.Id))
                {
                    Console.WriteLine($"{clients[peer.Id].data.Name} has left the game");
                    clients.Remove(peer.Id);
                    foreach (var (netPeer, _) in clients.Values)
                        packetProcessor.Send(netPeer, PacketType.PlayerLeftEvent, writer => writer.Put(peer.Id), DeliveryMethod.ReliableOrdered);
                }

            };

            server.Start(port);
            
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

    public class Player
    {
        public Vector3 Position;
        public string Name;
    }
    
}