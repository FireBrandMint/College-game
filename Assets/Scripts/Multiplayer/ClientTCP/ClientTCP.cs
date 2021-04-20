using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using Bindings;
using System.Collections.Concurrent;
using System.Threading;

public class ClientTCP : MonoBehaviour
{
    public static ClientTCP instance = null;
    public static Action OnClientNodeAvailable = null;
    public Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private byte[] _asyncBuffer = new byte[1024];

    public bool ControlledEntityHere = false;
    public int ControlledEntity;

    int ConnectionTimeoutLimit = 1200 + 30;

    bool ConnectionTimeout = false;

    public int LastPing = 0;

    public bool ClientConnected = false;

    ConcurrentBag<byte[]> packets = new ConcurrentBag<byte[]>();

    int tf;
    int vcc;

    void Start ()
    {
       instance = this;

        if (OnClientNodeAvailable != null)
        {
            OnClientNodeAvailable.Invoke();
            OnClientNodeAvailable = null;
        }

        tf = Application.targetFrameRate;
        vcc = QualitySettings.vSyncCount;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    void Update ()
    {
        byte[][] subject;

        if (udpClient.TryGetMessages(out subject))
        {
            for (int i = 0; i < subject.Length; ++i)
            {
                packets.Add(subject[i]);
            }
        }

        var pCount = packets.Count;
        if (pCount!=0)
        {
            for (int i = 0; i<pCount; ++i)
            {
                byte[] l;

                while (!packets.TryPeek(out l)) {}
                ClientHandleNetworkData.HandleNetworkInformation(l);
            }
            for (int i = 0; i<pCount; ++i)
            {
                byte[] l = null;
                while (l==null) packets.TryTake(out l);
            }
        }

        if (LastPing > ConnectionTimeoutLimit) ConnectionTimeout = true;

        if (ConnectionTimeout) Disconnect();

        if (connecting)
        {
            ++LastPing;
        } 

        if (!ClientConnected) return;

        OnlineEntity pp;

        if(ControlledEntityHere && OnlineEntity.OEntities.TryGetValue(ControlledEntity, out pp))
        {
            Player p = (Player)pp;
            p.isControlledPlayer = true;
            p.controls.tick();
        }

        ++LastPing;
    }

    bool connecting = false;
    public void ConnectToServer (string ip, int port)
    {
        Debug.Log("Connecting to server...");
        if (ip == "localhost" || ip == "default") ip = "127.0.0.1";
        ServerIp = ip;
        ServerPort = port;
        _clientSocket.BeginConnect(ip, port, new AsyncCallback(ConnectCallback), _clientSocket);

        connecting = true;
    }

    string ServerIp = null;
    int ServerPort = int.MinValue;

    Thread serverReceivingThread = null;

    UdpCustomClient udpClient;

    private void ConnectCallback (IAsyncResult ar)
    {
        try
        {
            Debug.Log("Connection callback!");
            connecting = false;
            ClientConnected = true;
            LastPing = 0;
            _clientSocket.EndConnect(ar);
            udpClient = new UdpCustomClient(ServerIp, ServerPort);
            serverReceivingThread = new Thread(() => {while (!disconnected) OnReceive();});
            serverReceivingThread.Start();
            Debug.Log("Connected to server");
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
        //while (!disconnected)
        //{
            //OnReceive();
        //}
    }

    private void OnReceive ()
    {
        byte [] _sizeinfo = new byte [4];
        byte [] _receiveBuffer = new byte[2048];

        int totalread = 0, currentread = 0;

        try
        {
            currentread = totalread = _clientSocket.Receive(_sizeinfo);
            if (totalread <= 0)
            {
                //Yeah, damm right those bytes aren't here now.
                //Debug.Log("No bytes here for now.");
            }
            else if (ConnectionTimeout)
            {
                return;
            }
            else
            {
                while (totalread < _sizeinfo.Length && currentread > 0)
                {
                    currentread = _clientSocket.Receive(_sizeinfo, totalread, _sizeinfo.Length - totalread, SocketFlags.None);
                    totalread += currentread;
                }

                int messagesize = 0;
                messagesize |= _sizeinfo[0];
                messagesize |= (_sizeinfo[1] << 8);
                messagesize |= (_sizeinfo[2] << 16);
                messagesize |= (_sizeinfo[3] << 24);

                byte[] data = new byte [messagesize];

                totalread = 0;

                currentread = totalread = _clientSocket.Receive(data,totalread,data.Length - totalread, SocketFlags.None);

                while (totalread < messagesize && currentread > 0)
                {
                    currentread = _clientSocket.Receive(data, totalread, data.Length - totalread, SocketFlags.None);
                    totalread += currentread;
                }

                packets.Add(data);
            }
        }

        catch (Exception e)
        {
            ConnectionTimeout = true;
            lock(disconnectMessage) disconnectMessage = e.Message;
            return;
        }
    }
    
    public void SendData (byte[] data)
    {
        _clientSocket.Send(data);
    }

    public void SendDataUdp (byte[] data)
    {
        udpClient.Send(data);
    }

    bool disconnected = false;

    public static List<Action> OnDisconnect = new List<Action>();

    string disconnectMessage = "";
    protected void Disconnect ()
    {
        Debug.Log("timed out");
        if (disconnectMessage!="") Debug.Log(disconnectMessage);
        disconnected = true;
        OnDisconnected();
        foreach (Action a in OnDisconnect) a.Invoke();
        OnDisconnect.Clear();
        _clientSocket.Close();
        udpClient.Close();
    }

    protected virtual void OnDisconnected()
    {

    }

    void OnDestroy ()
    {
        instance = null;
        OnDisconnected();

        Application.targetFrameRate = tf;
        QualitySettings.vSyncCount = vcc;
    }

    public void ThankYouServer ()
    {
        PacketBuffer buffer = new PacketBuffer();
        buffer.WriteInteger((int) ClientPackets.CThankYou);
        buffer.WriteString("(WHY THE HELL KEVING WROTE THAT) Thank you bruv, for letting me connect to ya server.");
        SendData(buffer.ToArray());
        buffer.Dispose();
    }
}