using System.Collections.Generic;
using System.Drawing;
using LiteNetLib;
using SurviveCore.DirectX;
using SurviveCore.Network;

namespace SurviveCore.Gui.Scene.Multiplayer
{
    public class MultiplayerConnectionScene : GuiScene
    {
        
        private string ip;
        private string name;
        private GuiScene previous;
        private NetManager networkClient;
        
        public MultiplayerConnectionScene(GuiScene p, string i, string n)
        {
            ip = i;
            name = n;
            previous = p;
        }

        public override void OnActivate(Client c)
        {
            base.OnActivate(c);
            c.ClearColor = Color.LightSlateGray;
            
            EventBasedNetListener listener = new EventBasedNetListener();
            PacketProcessor packetProcessor = new PacketProcessor();
            networkClient = new NetManager(listener);

            listener.NetworkReceiveEvent += packetProcessor.ReadAllPackets;

            listener.NetworkErrorEvent += (point, error) => GoBack($"Error: {error}");
            listener.PeerDisconnectedEvent += (peer, info) => GoBack($"Disconnect: {info.Reason}");

            listener.PeerConnectedEvent += peer =>
                packetProcessor.Send(peer, PacketType.JoinRequest, writer => writer.Put(name), DeliveryMethod.ReliableOrdered);

            packetProcessor.Subscribe(PacketType.JoinResponse, (peer, reader) =>
            {
                int pid = reader.GetInt();
                int playercount = reader.GetInt();
                Dictionary<int, Player> players = new Dictionary<int, Player>();
                while (playercount-- > 0)
                    players.Add(reader.GetInt(), reader.GetPlayer());
                
                listener.ClearNetworkErrorEvent();
                listener.ClearPeerConnectedEvent();
                listener.ClearNetworkReceiveEvent();
                listener.ClearPeerDisconnectedEvent();
                client.CurrentScene = new MultiplayerInGameScene(new MultiplayerSurvivalGame(client, networkClient, listener, pid, players));
            });
            
            networkClient.Start();
            networkClient.Connect(ip , Server.port, "SurvivalGameNetwork");
        }

        private void GoBack(string reason)
        {
            networkClient?.Stop();
            client.CurrentScene = previous;
        }
        
        public override void OnGui(InputManager.InputState input, GuiRenderer gui)
        {
            int w = client.ScreenSize.Width  / 2;
            int h = client.ScreenSize.Height / 2;
            gui.Text(new Point(w, h), $"Connecting to {ip} as {name}", Origin.Center, 25);
        }

        public override void OnNetworkUpdate()
        {
            base.OnNetworkUpdate();
            networkClient.PollEvents();
        }
    }
}