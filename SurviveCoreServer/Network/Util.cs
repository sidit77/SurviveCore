using System.Numerics;
using LiteNetLib;
using LiteNetLib.Utils;

namespace SurviveCore.Network
{
    public static class Util
    {
        public static Player GetPlayer(this NetPacketReader reader)
        {
            return new Player()
            {
                Name = reader.GetString(Server.maxnamechars),
                Position = reader.GetVector3()
            };
        }
        
        public static void Put(this NetDataWriter writer, Player p)
        {
            writer.Put(p.Name);
            writer.Put(p.Position);
        }

        public static Vector3 GetVector3(this NetPacketReader reader)
        {
            return new Vector3(reader.GetFloat(),reader.GetFloat(),reader.GetFloat());
        }
        
        public static void Put(this NetDataWriter writer, Vector3 v)
        {
            writer.Put(v.X);
            writer.Put(v.Y);
            writer.Put(v.Z);
        }
    }
}