using BuisnessLayer.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BuisnessLayer.Manager
{

    public class CommunicationToVredManager
    {
        private static readonly log4net.ILog log = LogHelper.GetLogger();
        public static bool isConnected;
        public TcpClient tcpClient;
        public static Queue<string> DataQueueClipPlane;
        public static Queue<string> DataQueueTeleport;
        public CancellationToken ct;
        Thread ConnectionListenerThread;

        public CommunicationToVredManager()
        {
            DataQueueClipPlane = new Queue<string>();
            DataQueueTeleport = new Queue<string>();
        }
        
        private void checkAsyncTcpListener()
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    NetworkStream networkStream = tcpClient.GetStream();
                    string data = "Are Connected?|";
                    this.BeginSend(data);
                    networkStream.Flush();

                    Thread.Sleep(1000);
                }
                if (ConnectionListenerThread != null && ConnectionListenerThread.IsAlive)
                {
                    ConnectionListenerThread.Abort();
                    ConnectionListenerThread = null;
                }
            }
            catch (Exception)
            {
                log.Error("Connection to Server is crashed!");
            }
            finally
            {
                Thread.ResetAbort();
            }
        }
        public BaseResult Connect(string ip, int port, CancellationToken pCt)
        {

            try
            {
                ct = pCt;
                if (!ct.IsCancellationRequested)
                {
                    tcpClient = new TcpClient(ip, port);

                    IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                    TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections().Where(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint) && x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint)).ToArray();

                    ConnectionListenerThread = new Thread(checkAsyncTcpListener);
                    ConnectionListenerThread.Start();

                    if (tcpConnections != null && tcpConnections.Length > 0)
                    {
                        TcpState stateOfConnection = tcpConnections.First().State;
                        if (stateOfConnection == TcpState.Established)
                        {
                            Console.WriteLine("Connection to Server with IP: " + ip + " and Port: " + port + " was succesfull!");
                            Thread.Sleep(100);
                            isConnected = true;
                            
                        }
                    }
                    return new BaseResult(); 
                }

                return new BaseResult();

            }
            catch (Exception e)
            {
                log.Info("Wait until connection to Vred is ready....");
                isConnected = false;
                return new BaseResult(e);
            }
        }

        // Method to start sending Messages
        public async Task BeginSendlipPlane()
        {
            try
            {
                while (!isConnected)
                {
                   
                }

                while (isConnected && DataQueueClipPlane.Count != 0)
                {
                    log.Info(isConnected);
                    if (DataQueueClipPlane.Count > 0)
                    {
                        string data2 = "";
                        lock (DataQueueClipPlane)
                        {
                            if (DataQueueClipPlane.Count != 0)
                            {
                                data2 = DataQueueClipPlane.Dequeue();
                            }
                        }
                        string identifyer = "|";
                        data2 = data2 + identifyer;
                        var bytes2 = Encoding.ASCII.GetBytes(data2);
                        var ns2 = tcpClient.GetStream();
                        ns2.BeginWrite(bytes2, 0, bytes2.Length, EndSend, bytes2);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("FATAL| CAN NOT SEND A TCP MESSAGE. PLEASE CHECK YOUR INTERNET-CONNECTION!" + ex.ToString());
            }
        }

        public void BeginSendTeleport(string data)
        {
            try
            {
                if (isConnected && !ct.IsCancellationRequested)
                {
                    if (DataQueueTeleport.Count > 0)
                    {
                        string data2 = DataQueueTeleport.Last();
                        DataQueueTeleport.Clear();
                        var bytes2 = Encoding.ASCII.GetBytes(data);
                        var ns = tcpClient.GetStream();
                        ns.BeginWrite(bytes2, 0, bytes2.Length, EndSend, bytes2);
                    }
                    else
                    {
                        var bytes = Encoding.ASCII.GetBytes(data);
                        var ns = tcpClient.GetStream();
                        ns.BeginWrite(bytes, 0, bytes.Length, EndSend, bytes);
                    }
                }
                else
                {
                    DataQueueTeleport.Enqueue(data);
                }
            }
            catch (Exception e)
            {
                log.Error("BeginSendTeleport in CommunicationToVredManager crashed!" + e.ToString());
            }
        }


        // Method to start sending Messages
        public void BeginSend(string data)
        {
            try
            {
                if (isConnected && !ct.IsCancellationRequested)
                {
                    var bytes = Encoding.ASCII.GetBytes(data);
                    var ns = tcpClient.GetStream();
                    ns.BeginWrite(bytes, 0, bytes.Length, EndSend, bytes);
                }
;
            }
            catch (Exception e)
            {
                log.Error("BeginSend in CommunicationToVredManager crashed!" + e.ToString());
            }
        }


        // Method to cancel sending Messages
        public void EndSend(IAsyncResult result)
        {
            try
            {
                var bytes = (byte[])result.AsyncState;
            }
            catch (Exception ex)
            {
                log.Error("EndSend in CommunicationToVredManager crashed!" + ex.ToString());
            }
        }
    }
}
