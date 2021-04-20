using System.Net.Sockets;
using System.Net;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
public class UdpCustomClient
{
    UdpClient client;

    IPEndPoint endPoint;

    List<byte[]> messages = new List<byte[]>();

    CancellationTokenSource ts;
    CancellationToken ct;

    public UdpCustomClient (string ip, int port)
    {
        client = new UdpClient(8364);

        ts = new CancellationTokenSource();
        ct = ts.Token;
        try
        {
            endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            client.Connect(ip, port);

            //#pragma warning disable 4014
            //Process(client, messages, endPoint, ct);

            client.Send(new byte[1]{232}, 1);

            new Thread (()=>
            {
                while (true)
                {
                    if (ct.IsCancellationRequested) break;

                    try
                    {
                        var result = client.Receive(ref endPoint);
                        lock (messages) messages.Add(result);
                    }
                catch {}
                }
            }).Start();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public byte[][] GetMessages ()
    {
        if (messages.Count == 0) return null;

        var mmessages = messages.ToArray();
        messages.Clear();
        return mmessages;
    }

    public bool TryGetMessages (out byte[][] array)
    {
        if (messages.Count == 0)
        {
            array = null;
            return false;
        }

        array = messages.ToArray();
        messages.Clear();
        return true;
    }

    public void Send (byte[] message)
    {
        client.SendAsync(message, message.Length);
    }

    #pragma warning disable 1998
    static async Task Process (UdpClient connection, List<byte[]> storage, IPEndPoint endPoint, CancellationToken ct)
    {
        while (true)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var result = connection.Receive(ref endPoint);
                lock (storage) storage.Add(result);
            }
            catch (Exception e) 
            {
                lock (storage)
                {
                    storage.Add(Encoding.ASCII.GetBytes(e.Message));
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