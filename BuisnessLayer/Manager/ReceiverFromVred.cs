using BuisnessLayer.Results;
using BuisnessLayer.ViewModels.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BuisnessLayer.Manager
{
    public class ReceiverFromVred
    {
        private static readonly log4net.ILog log = LogHelper.GetLogger();
        TcpListener listener;
        public CancellationToken ct;
        public ReceiverFromVred()
        {
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 3222);
        }

        public BaseResult StartListen(ControlViewModel controlViewModel, CancellationToken pCt)
        {
            try
            {
                ct = pCt;
                // Start listening for client requests.
                listener.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;

                // Enter the listening loop.
                while (!ct.IsCancellationRequested)
                {
                    log.Info("Waiting for a connection... ");

                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    data = null;
                    NetworkStream stream = client.GetStream();

                    int i;
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        data = Encoding.ASCII.GetString(bytes, 0, i);
                        var msg = data.Split(':');
                        Console.WriteLine(msg);
                        switch (msg[0])
                        {
                            case "TELEPORT":
                               
                                string[] value = data.Split(' ');

                                for (int j = 0; j < value.Length; j++)
                                {
                                    string[] temp = value[j].Split('.');
                                    value[j] = temp[0];
                                }

                                lock (ControlViewModel.teleporOffset)
                                {
                                    ControlViewModel.teleporOffset[0] = Int32.Parse(value[1]);
                                    ControlViewModel.teleporOffset[1] = Int32.Parse(value[2]);
                                    ControlViewModel.teleporOffset[2] = Int32.Parse(value[3]);
                                    ControlViewModel.teleporOffset[3] = Int32.Parse(value[4]);
                                } 
                                break;
                            case "CLIPPLANE":
                                controlViewModel.updateClipPlane(msg);
                                break;
                        }
                    }
                    return new BaseResult();

                }
                return new BaseResult();
            }
            catch(Exception e)
            {
                Close();
                log.Warn("Something goes wrong in StartListen" + e);
                return new BaseResult(e);
            }
        }
        public void Close()
        {
            listener.Stop();
        }
    }
}
