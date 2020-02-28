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
            int startsize = reader.AvailableBytes;
            int length = reader.GetInt();
            PacketType type = (PacketType) reader.GetInt();
            if (handler.TryGetValue(type, out var action))
            {
                action(peer, reader);
                if(reader.AvailableBytes != startsize - length)
                    throw new ParseException($"A {type} packet was not fully consumed!");
            }
            else
            {
                while (reader.AvailableBytes > startsize - length)
                    reader.GetByte();
            }
        }

        public void Subscribe(PacketType type, Action<NetPeer, NetPacketReader> action)
        {
            handler.Add(type, action);
        }

        public void Send(NetPeer peer, PacketType type, Action<NetDataWriter> action, DeliveryMethod method)
        {
            writer.Reset();
            writer.Put(0);
            writer.Put((int)type);
            action.Invoke(writer);
            FastBitConverter.GetBytes(writer.Data, 0, writer.Length);
            peer.Send(writer, method);
        }
        
        public void Send(NetManager manager, PacketType type, Action<NetDataWriter> action, DeliveryMethod method)
        {
            writer.Reset();
            writer.Put(0);
            writer.Put((int)type);
            action.Invoke(writer);
            FastBitConverter.GetBytes(writer.Data, 0, writer.Length);
            manager.SendToAll(writer, method);
        }
        
    }
}