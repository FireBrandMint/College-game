using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine;
using System.Text;
using System.Linq;

public class UdpServer
{
    UdpClient client;

    List<KeyValuePair<string, byte[]>> IpAndMessage = new List<KeyValuePair<string, byte[]>>();

    public GlassList< IPEndPoint> IpIpendpoint = new GlassList< IPEndPoint>();

    CancellationTokenSource ts;
    CancellationToken ct;

    public UdpServer (string ip, int port)
    {
        try
        {
            client = new UdpClient(new IPEndPoint(IPAddress.Parse(ip), port));

            ts = new CancellationTokenSource();
            ct = ts.Token;
        //#pragma warning disable 4014
        //Process(client, IpAndMessage, IpIpendpoint, ct);

            Task necessary = Process(client, IpAndMessage, IpIpendpoint, ct, this);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public KeyValuePair<string, byte[]>[] ReceiveMessages ()
    {
        lock (IpAndMessage)
        {
            if (IpAndMessage.Count == 0) return null;

            var a = IpAndMessage.ToArray();
            IpAndMessage.Clear();
            return a;
        }
    }

    public bool TryReceiveMessages (out KeyValuePair<string, byte[]>[] msgs)
    {
        lock (IpAndMessage)
        {
            if (IpAndMessage.Count == 0)
            {
                msgs = null;
                return false;
            }

            var a = IpAndMessage.ToArray();
            IpAndMessage.Clear();
            msgs = a;
            return true;
        }
    }

    public void Send (byte[] message, string ip)
    {
        lock (IpAndMessage)
        {
            try
            {
                IPEndPoint gudshit;
                if (IpIpendpoint.TryGetValue(ip, out gudshit))
                {
                    client.SendAsync(message, message.Length, gudshit);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
    }

    public void Send (byte[] message, IPEndPoint ip)
    {
        lock (IpAndMessage)
        {
            try
            {
                client.SendAsync(message, message.Length, ip);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
    }

    public bool ClientListUpdated = true;

    ///<summary>
    ///<para> Gives client ip array when it's updated 1 time. </para>
    ///<para> Returns null for the vessel and then false when there's no update.</para>
    ///</summary>
    public bool GetClientsConnectedWhenNecessary (out string[] vessel)
    {
        if (ClientListUpdated)
        {
            vessel = null;
            return false;
        }
        vessel = IpIpendpoint.Keys;
        ClientListUpdated = true;

        return true;
    }

    static async Task Process (UdpClient client, List<KeyValuePair<string, byte[]>> receiver, GlassList<IPEndPoint> identifier, CancellationToken ct, UdpServer udpServer)
    {
        while (true)
        {
            try
            {
            if (ct.IsCancellationRequested) break;
                var result = await client.ReceiveAsync();
                lock (receiver)
                {
                    var ip = result.RemoteEndPoint.Address.ToString();

                    lock (identifier)
                    {
                        if (!identifier.ContainsKey(ip))
                        {
                            identifier.Add(ip, result.RemoteEndPoint);
                            lock(udpServer) udpServer.ClientListUpdated = false;
                        }
                    }

                    var buffer = result.Buffer;

                    if (!(buffer.Length == 1 && buffer[0] == 232)) receiver.Add(new KeyValuePair<string, byte[]> (ip, buffer));
                }
            }
            catch (Exception e) 
            {
                lock (receiver)
                {
                    receiver.Add(new KeyValuePair<string, byte[]> ("no one", Encoding.ASCII.GetBytes(e.Message)));
                }
            }
        }
    }

    public void Close ()
    {
        client.Close();
        client.Dispose();
        ts.Cancel();
    }
}