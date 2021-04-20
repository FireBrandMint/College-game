using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using Bindings;

public class ServerTCP : MonoBehaviour
{
    public static ServerTCP instance;
    //When the server node
    public static Action OnServerNodeAvailable = null;

    // Start is called before the first frame update
    int tf;
    int vcc;
    void Start()
    {
        tf = Application.targetFrameRate;
        vcc = QualitySettings.vSyncCount;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        instance = this;
        OnServerInitialized();
        if (OnServerNodeAvailable != null)
        {
            OnServerNodeAvailable.Invoke();
            OnServerNodeAvailable = null;
        }


    }

    protected virtual void OnServerInitialized ()
    {

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.End)) Destroy(this);
        if (!IsServerInitialized) return;

        KeyValuePair<string, byte[]>[] udpMsgs;
        if (udpSocket.TryReceiveMessages(out udpMsgs))
        {
            for (int i = 0; i< udpMsgs.Length; ++i)
            {
                int vessel;
                var um = udpMsgs[i];
                if (Client.ipIndexes.TryGetValue(um.Key, out vessel))
                {
                    ServerHandleNetworkData.HandleNetworkInformation(vessel, um.Value);
                }
            }
        }

        for (int i = 0; i < clients.Length; ++i)
        {
            var client = clients[i];
            if (client!=null) client.tick();
        }

        var pList = Player.players;

        for (int i = 0; i<pList.Count; ++i)
        {
            Player p = pList[i];

            if (p == null) continue;

            var data = p.GetCurrentBuffer().ToArray();

            if (data.Length > 1) SendDataTo(p.playerID, data);

            var udpData = p.GetCurrentUDPBuffer().ToArray();

            if (udpData.Length > 1) UdpSendDataTo(p.playerID, udpData);
        }

        OnlineEntity.Areas.ClearAllAreaBuffers();
    }

    private Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public UdpServer udpSocket;
    private byte [] buffer = new byte[1024];

    public Client[] clients = new Client[Bindings.Constants.MAX_PLAYERS];

    List<string> bannedIps = new List<string>();

    bool IsServerInitialized = false;

    public void SetupServer (string ip = "", int port = int.MaxValue)
    {
        try{
            string theIp;
            if (ip == "localhost" || ip == "default")
            {
                theIp = "127.0.0.1";
            }
            else theIp = ip;

            serverSocket.Bind(new IPEndPoint(theIp == "" ? IPAddress.Any : IPAddress.Parse(theIp), port == int.MaxValue? 5555 : port));
            serverSocket.Listen(5);
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            IsServerInitialized = true;
            udpSocket = new UdpServer(theIp == "" ? IPAddress.Any.ToString() : theIp, port == int.MaxValue? 5555 : port);
            Debug.Log("Server setup.");
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public delegate void IntFunc (int ClientIndex);

    public List<IntFunc> OnClientConnected = new List<IntFunc>();

    public void AcceptCallback(IAsyncResult ar)
    {
        if (dead) return;
        try
        {
            Debug.Log("Received callback!");
            Socket _socket = serverSocket.EndAccept(ar);
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);

            for (int i = 0; i < Constants.MAX_PLAYERS; ++i)
            {
                if (clients[i] == null || clients[i].socket == null)
                {
                    clients[i] = Client.makeStartedInstance(_socket, i);
                    var c = clients[i];
                    foreach (string ip in bannedIps) if (c.ip == ip)
                    {
                        c.CloseClient();
                        return;
                    }

                    foreach (IntFunc itf in OnClientConnected) itf.Invoke(i);
                
                    break;
                }
            
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }
    
    public void SendDataTo (int index, byte[] data)
    {
        byte [] sizeInfo = new byte[4]
        {
            (byte) data.Length,
            (byte) (data.Length >> 8),
            (byte) (data.Length >> 16),
            (byte) (data.Length >> 24)
        };

        clients[index].socket.Send(sizeInfo);
        clients[index].socket.Send(data);
    }
    public void UdpSendDataTo (int index, byte[] data)
    {
        udpSocket.Send(data, clients[index].ip);
    }
    public void BanPlayer(int index)
    {
        var c = clients[index];
        bannedIps.Add(c.ip);
        c.CloseClient();
    }
    public void SendConnectionOK (int index)
    {
        var _buffer = new PacketBuffer();
        _buffer.WriteInteger((int) ServerPackets.SConnectionOK);
        _buffer.WriteString("Sucess!");
        SendDataTo(index, _buffer.ToArray());
        _buffer.Dispose();
    }

    public void CloseServer ()
    {
        Destroy(this);
    }
    bool dead = false;
    void OnDestroy ()
    {
        dead = true;
        serverSocket.Close();
        OnlineEntity.Areas.ClearRegistry();
        foreach (Action a in OnServerClosed) a.Invoke();
        OnServerClosed.Clear();
        foreach (Client c in clients) if (c!= null) c.CloseClient();

        instance = null;

        Application.targetFrameRate = tf;
        QualitySettings.vSyncCount = vcc;
    }

    public static List<Action> OnServerClosed = new List<Action>();
}

public class Client
    {
        public static GlassList<int> ipIndexes = new GlassList<int>();
        static byte[] PingTestPacket = BitConverter.GetBytes((int)ServerPackets.PingMeasure);

        public int index;
        public string endpoint;
        public string ip;
        public Socket socket;
        public bool closing = false;
        private byte[] buffer = new byte[1024];

        public Player EntityControlled = null;

        public static Client makeStartedInstance (Socket socket, int index)
        {
            Client cl = new Client();

            cl.socket = socket;
            cl.index = index;
            cl.StartClient();
            Console.WriteLine($"Connection from {cl.ip} received.");

            return cl;
        }

        public void StartClient ()
        {
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            closing = false;
            ip = (socket.RemoteEndPoint as IPEndPoint).Address.ToString();
            ipIndexes.Add(ip, index);
            endpoint = socket.RemoteEndPoint.ToString();
        }

        private void ReceiveCallback (IAsyncResult ar)
        {
            Socket _socket = (Socket) ar.AsyncState;

            try
            {
                int received = _socket.EndReceive(ar);
                if (received <= 0)
                {
                    CloseClient(index);
                }
                else
                {
                    byte[] databuffer = new byte[received];
                    Array.Copy(buffer, databuffer, received);
                    ServerHandleNetworkData.HandleNetworkInformation(index, databuffer);
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                }
            }
            catch
            {
                CloseClient(index);
            }
        }

        public bool StartedWaitingPing = false;
        AverageCalculator PingMeasure = new AverageCalculator(5);
        public int WaitingPingTicks = 0;

        int SendPingIn = 60;

        public void tick ()
        {
            if (SendPingIn != int.MinValue)
            {
                if (SendPingIn<1)
                {
                    ServerTCP.instance.SendDataTo(index, PingTestPacket);
                    StartedWaitingPing = true;
                    SendPingIn = int.MinValue;
                }
                else --SendPingIn;
            }

            if (StartedWaitingPing)
            {
                WaitingPingTicks += 1;
            }
            else if (SendPingIn == int.MinValue)
            {
                PingMeasure.Put(WaitingPingTicks);
                if (EntityControlled != null)
                {
                    EntityControlled.ping = PingMeasure.Value();

                    using (PacketBuffer buff = new PacketBuffer())
                    {
                        buff.WriteInteger((int)ServerPackets.PingConfirmation);
                        buff.WriteInteger(EntityControlled.entityID);
                        buff.WriteInteger(PingMeasure.Value());
                        foreach (Client c in ServerTCP.instance.clients) c.socket.Send(buff.ToArray());
                    }


                }
                WaitingPingTicks = 0;
                SendPingIn = 60;
                Debug.Log($"Current ping from {ip} is {PingMeasure.Value()*16} ms!");
            }

            if (WaitingPingTicks > 2000)
            {
                CloseClient(index);
            }
        }

        public void CloseClient (int index)
        {
            closing = true;
            Console.WriteLine($"Connection {ip} has been terminated.");
            foreach (Action a in OnClientDisconnected) a.Invoke();
            OnClientDisconnected.Clear();
            socket.Close();
            ServerTCP.instance.clients[index] = null;
            ipIndexes.Remove(ip);
        }

        public void CloseClient ()
        {
            closing = true;
            Console.WriteLine($"Connection with {ip} has been terminated.");
            foreach (Action a in OnClientDisconnected) a.Invoke();
            OnClientDisconnected.Clear();
            socket.Close();
            ServerTCP.instance.clients[index] = null;
            ipIndexes.Remove(ip);
        }

        public List<Action> OnClientDisconnected = new List<Action>();
    }
