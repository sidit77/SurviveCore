using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace SurviveCore.Network
{
    public class PacketProcessor
    {

        private readonly Dictionary<PacketType, Action<NetPeer, NetPacketReader>> handler;
        private readonly NetDataWriter writer;
        
        public PacketProcessor()
        {
            handler = new Dictionary<PacketType, Action<NetPeer, NetPacketReader>>();
            writer = new NetDataWriter();
        }
        
        public void ReadAllPackets(NetPeer peer, NetPacketReader reader, DeliveryMethod method)
        {
            while (reader.AvailableBytes > 0)
            {
                ReadPacket(peer, reader, method);
            }
            reader.Recycle();
        }

        private void ReadPacket(NetPeer peer, NetPacketReader reader, DeliveryMethod method)
        {
            PacketType type = (PacketType) reader.GetInt();
            handler[type].Invoke(peer, reader);
        }

        public void Subscribe(PacketType type, Action<NetPeer, NetPacketReader> action)
        {
            handler.Add(type, action);
        }

        public void Send(NetPeer peer, PacketType type, Action<NetDataWriter> action, DeliveryMethod method)
        {
            writer.Reset();
            writer.Put((int)type);
            action.Invoke(writer);
            peer.Send(writer, method);
        }
        
    }
}