﻿/*
The MIT License (MIT)

Copyright (c) 2013, David Suarez

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace MavLinkNet
{
    public class MavLinkUdpTransport
    {
        public int UdpListeningPort = 14551;
        public int UdpTargetPort = 14550;
        public byte MavlinkSystemId = 200;
        public byte MavlinkComponentId = 1;
        public IPAddress TargetIpAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
        public int HeartBeatUpdateRateMs = 1000;
        public MavLinkState UavState = new MavLinkState();

        public event PacketReceivedDelegate OnPacketReceived;

        private ConcurrentQueue<byte[]> mReceiveQueue = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<UasMessage> mSendQueue = new ConcurrentQueue<UasMessage>();
        private AutoResetEvent mReceiveSignal = new AutoResetEvent(true);
        private AutoResetEvent mSendSignal = new AutoResetEvent(true);
        private MavLinkWalker mMavLink = new MavLinkWalker();
        private UdpClient mUdpClient;


        public void Initialize()
        {
            InitializeMavLink();
            InitializeUdpListener(UdpListeningPort);
            InitializeUdpSender(TargetIpAddress, UdpTargetPort);
        }

        private void InitializeMavLink()
        {
            mMavLink.PacketReceived += HandlePacketReceived;
        }

        private void InitializeUdpListener(int port)
        {
            // Create UDP listening socket on port
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
            mUdpClient = new UdpClient(ep);

            mUdpClient.BeginReceive(
                new AsyncCallback(ReceiveCallback), ep);

            ThreadPool.QueueUserWorkItem(
                new WaitCallback(ProcessReceiveQueue), null);
        }

        private void InitializeUdpSender(IPAddress targetIp, int targetPort)
        {
            ThreadPool.QueueUserWorkItem(
               new WaitCallback(ProcessSendQueue), new IPEndPoint(targetIp, targetPort));
        }


        // __ Receive _________________________________________________________


        private void ReceiveCallback(IAsyncResult ar)
        {
            IPEndPoint ep = ar.AsyncState as IPEndPoint;
            mReceiveQueue.Enqueue(mUdpClient.EndReceive(ar, ref ep));
            mUdpClient.BeginReceive(new AsyncCallback(ReceiveCallback), ar);

            // Signal processReceive thread
            mReceiveSignal.Set();
        }

        private void ProcessReceiveQueue(object state)
        {
            while (true)
            {
                byte[] buffer;

                if (mReceiveQueue.TryDequeue(out buffer))
                {
                    mMavLink.ProcessReceivedBytes(buffer);
                }
                else
                {
                    // Empty queue, sleep until signalled
                    mReceiveSignal.WaitOne();
                }
            }
        }


        // __ Send ____________________________________________________________


        private void ProcessSendQueue(object state)
        {
            while (true)
            {
                UasMessage msg;

                if (mSendQueue.TryDequeue(out msg))
                {
                    SendMavlinkMessage(state as IPEndPoint, msg);
                }
                else
                {
                    // Queue is empty, sleep until signalled
                    mSendSignal.WaitOne();
                }
            }
        }

        private void SendMavlinkMessage(IPEndPoint ep, UasMessage msg)
        {
            byte[] buffer = mMavLink.SerializeMessage(msg, MavlinkSystemId, MavlinkComponentId, true);
            
            

            mUdpClient.Send(buffer, buffer.Length, ep);
        }


        // __ Heartbeat _______________________________________________________


        public void BeginHeartBeatLoop()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(HeartBeatLoop), null);
        }

        private void HeartBeatLoop(object state)
        {
            while (true)
            {
                foreach (UasMessage m in UavState.GetHeartBeatObjects())
                {
                    SendMessage(m);
                }

                Thread.Sleep(HeartBeatUpdateRateMs);
            }
        }


        // __ MavLink events __________________________________________________


        private void HandlePacketReceived(object sender, MavLinkPacket e)
        {
            if (OnPacketReceived != null) OnPacketReceived(sender, e);
        }


        // __ API _____________________________________________________________


        public void SendMessage(UasMessage msg)
        {
            mSendQueue.Enqueue(msg);

            // Signal send thread
            mSendSignal.Set();
        }
    }
}
