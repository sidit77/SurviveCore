using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using LiteNetLib;
using SurviveCore.DirectX;
using SurviveCore.Network;
using SurviveCore.Particles;
using WinApi.User32;

namespace SurviveCore
{
    public class MultiplayerSurvivalGame : IDisposable
    {
        private ParticleRenderer particlerenderer;

        private Vector3 lastMovement;
     
        private Camera camera;

        private Client client;
        private NetManager networkClient;
        private int playerid;

        private Dictionary<int, Player> players;
        
        public string DebugText =>
            $"Pos: [{(int) MathF.Round(camera.Position.X)}|{(int) MathF.Round(camera.Position.Y)}|{(int) MathF.Round(camera.Position.Z)}]\n" + 
            $"Players:\n {players.Values.Select(p => "   " + p.Name).Aggregate((i, j) => $"{i}\n{j}")}";
        
        public MultiplayerSurvivalGame(Client c, NetManager nc, EventBasedNetListener ebnl, int pid, Dictionary<int, Player> p)
        {
            client = c;
            client.OnDispose += Dispose;

            networkClient = nc;
            playerid = pid;
            players = p;
            
            PacketProcessor packetProcessor = new PacketProcessor();
            ebnl.NetworkReceiveEvent += packetProcessor.ReadAllPackets;
            
            packetProcessor.Subscribe(PacketType.PlayerJoinedEvent, (peer, reader) => players.Add(reader.GetInt(), reader.GetPlayer()));
            packetProcessor.Subscribe(PacketType.PlayerLeftEvent, (peer, reader) => players.Remove(reader.GetInt()));

            particlerenderer = new ParticleRenderer(c.Dx.Device);
            camera = new Camera(75f * (float) Math.PI / 180, (float) c.ScreenSize.Width / c.ScreenSize.Height,
                0.3f, 620.0f);
            
            Random r = new Random(345);
            for (int i = 0; i < 1000; i++)
            {
                particlerenderer.AddParticle(new Vector3(r.NextFloat(-100, 100),r.NextFloat(0, 100),r.NextFloat(-100, 100)), 0.3f, Color.Coral);
            }
            
        }

        public void Network()
        {
            networkClient.PollEvents();
        }
        
        public void Render()
        {
            camera.Aspect = (float)client.ScreenSize.Width / client.ScreenSize.Height;
            camera.Update(Settings.Instance.UpdateCamera);

            particlerenderer.Render(client.Dx.Context, camera);
        }

        public void Update(InputManager.InputState input)
        {
            
            Vector3 movement = Vector3.Zero;
            if (input.IsKey(VirtualKey.W))
                movement += camera.Forward;
            if (input.IsKey(VirtualKey.S))
                movement += camera.Back;
            if (input.IsKey(VirtualKey.A))
                movement += camera.Left;
            if (input.IsKey(VirtualKey.D))
                movement += camera.Right;
            movement.Y = 0;
            movement = movement.LengthSquared() > 0 ? Vector3.Normalize(movement) : Vector3.Zero;

            if (input.IsKey(VirtualKey.SPACE))
                movement += Vector3.UnitY;
            if (input.IsKey(VirtualKey.SHIFT))
                movement -= Vector3.UnitY;
            movement *= input.IsKey(VirtualKey.CONTROL) ? 0.3f : 0.06f;
            camera.Position += lastMovement = Vector3.Lerp(lastMovement, movement, 0.03f);

            if (input.MouseCaptured)
            {
                var mpos = input.DeltaMousePosition;
                camera.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (1f / 600) * mpos.X);

                const float anglediff = 0.001f;
                float angle = MathF.Acos(Vector3.Dot(camera.Forward, Vector3.UnitY));
                camera.Rotation *= Quaternion.CreateFromAxisAngle(camera.Right,
                    MathHelper.Clamp((1f / 600) * mpos.Y + angle, anglediff, MathF.PI - anglediff) - angle);
                
            }


        }

        public void Dispose()
        {
            client.OnDispose -= Dispose;
            
            networkClient?.Stop();
            
            particlerenderer?.Dispose();
            Console.WriteLine("Dispose");
        }
    }
}