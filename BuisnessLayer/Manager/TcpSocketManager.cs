using BuisnessLayer.Base;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using BuisnessLayer.Base;
using BuisnessLayer.Results;
using DataModels.Base;
using BuisnessLayer.ViewModels.Rooms;
using System.Diagnostics;
using System.Threading;
using BuisnessLayer.ViewModels.Control;
using BuisnessLayer.ViewModels;

[assembly: log4net.Config.XmlConfigurator(Watch=true)]

namespace BuisnessLayer.Manager
{

	public class TcpSocketManager
	{

        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private CancellationTokenSource ctsSetClipePlane;
        public TcpClient TcpClient;
		private string _ipAdressServer;
		private int _portServer;
		private List<SessionID> RoomList = new List<SessionID>();
        private MainWindowViewModel _mainWindowViewModel;
        private Queue<string> TeleportQueue;
        public int clientID { get; set; }
        public TcpSocketManager(string ip, int port, MainWindowViewModel mainWindowViewModel)
		{
			_ipAdressServer = ip;
			_portServer = port;
            _mainWindowViewModel = mainWindowViewModel;
            TcpClient = new TcpClient(_ipAdressServer, _portServer);
            ctsSetClipePlane = new CancellationTokenSource();
        }

		public BaseResult Connect()
		{
			try
			{
				if (TcpClient.Connected) // direkt nach dem Verbindungsaufbau wird als erstes mal ein Byte ausgetauscht.
				{
					// Sends data immediately upon calling NetworkStream.Write.
					TcpClient.NoDelay = true;
					LingerOption lingerOption = new LingerOption(false, 0);
					TcpClient.LingerState = lingerOption;

					NetworkStream s = TcpClient.GetStream();
					Byte[] data = new Byte[256];
					data[0] = 1;
					try
					{
						s.Write(data, 0, 1);
					}
					catch (System.IO.IOException e)
					{
						// probably socket closed
						return new BaseResult(e);
					}

					int numRead = 0;
					try
					{
						numRead = s.Read(data, 0, 1);

					}
					catch (System.IO.IOException e)
					{
						// probably socket closed
						return new BaseResult(e);
					}

					return new BaseResult();
				}
				else
				{
					Exception e = new Exception();
					return new BaseResult(e);
				}
			}
			catch (Exception e)
			{
                log.Fatal("Connect Method is Crashed!" +e);

                return new BaseResult(e);
			}

		}

		public BaseResult send_msg(Message message)
		{
			try
			{
                message.sender = clientID;
                int len = message.message.buf.Length + (4 * 4);
				Byte[] data = new Byte[len];
                ByteSwap.swapTo((uint)message.sender, data, 1 * 4); // An welcher Stelle kommt der Sender rein???
                ByteSwap.swapTo((uint)message.Type, data, 2 * 4);
				ByteSwap.swapTo((uint)message.message.buf.Length, data, 3 * 4);
				message.message.buf.CopyTo(data, 4 * 4);
				TcpClient.GetStream().Write(data, 0, len);

				return new BaseResult();

			}
			catch (Exception e)
			{
                log.Fatal("Method send_msg is Crashed!" + e);

                return new BaseResult(e);
			}
		}
		
		public BaseResult sendInitialMessage(UserModel userModel)
		{
			try
			{
				//Init MessageBuffer
				MessageBuffer mb = new MessageBuffer();
				mb.add(userModel.Name);
				mb.add(userModel.UserIpAdress);
				mb.add(userModel.Email);

				//mb.add(true);

				Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_CONTACT);
                send_msg(msg);
				return new BaseResult();
			}
			catch (Exception e)
			{
                log.Fatal("Method sendInitialMessage is Crashed!" + e);

                return  new BaseResult(e);
			}
		}
		
		public ValueResult<SessionID> sendSetNewSessionMessage(int userId, string sessionName)
		{
			try
			{
				MessageBuffer mb = new MessageBuffer();
                var sid = new SessionID(userId, sessionName, false);
                SharedStateSerializer.serialize(ref mb, sid);
				mb.add(userId);
				Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_REQUEST_NEW_SESSION);
                send_msg(msg);
                ValueResult<SessionID> currentSession = new ValueResult<SessionID>();

                currentSession.Value = sid;
                return currentSession;
			}
			catch (Exception e)
			{
                log.Fatal("Method sendSetNewSessionMessage is Crashed!" + e);

                return new ValueResult<SessionID>(e);
			}
		}

        public async Task<BaseResult> Read()
        {
            try
            {
                var buffer = new byte[64000];
                var ns = TcpClient.GetStream();

                while (true)
                {
                    int len = 0;
                  
                    while (len < 16)
                    {
                        if (ns.DataAvailable)
                        {
                            var numRead = await ns.ReadAsync(buffer, len, 16 - len);
                            if (numRead == 0) return new BaseResult();
                            len += numRead;
                        }
          
                    }
                    int msgType = BitConverter.ToInt32(buffer, 2 * 4);
                    int length = BitConverter.ToInt32(buffer, 3 * 4);
                    length = (int)ByteSwap.swap((uint)length);
                    msgType = (int)ByteSwap.swap((uint)msgType);
                    len = 0;
                    var msgBuffer = new byte[length];
                    while (len < length)
                    {
                        try
                        {
                            var numRead = await ns.ReadAsync(msgBuffer, len, length - len);
                            if (numRead == 0) return new BaseResult();
                            len += numRead;
                        }
                        catch (System.IO.IOException e)
                        {
          
                        }
               
                    }

                    Message m = new Message(new MessageBuffer(msgBuffer), (Message.MessagesType)msgType);

                    switch (m.Type)
                    {
                        case Message.MessagesType.COVISE_MESSAGE_VRBC_SEND_SESSIONS:
                            int sessioncount = m.message.readInt();
                            MessageHandler.RoomList.Clear();
                            RoomList.Clear();
                            for (int i = 0; i < sessioncount; i++)
                            {
                                SessionID room = new SessionID();
                                SharedStateSerializer.deserialize(ref m.message, ref room);
                                if (room.m_isPrivate == false)
                                {
                                    RoomList.Add(room);
                                }
                                else
                                {
                                    RoomsViewModel.privateSession = room;
                                }
                            }
                            MessageHandler.addCurrentRooms(RoomList);
                            break;

                        case Message.MessagesType.COVISE_MESSAGE_VRBC_SET_SESSION:
                            SessionID currentSession = new SessionID();
                            SharedStateSerializer.deserialize(ref m.message, ref currentSession);
                            MessageHandler.addCurrentSession(currentSession);
                            SharedStateManager.Instance.update(new SessionID(clientID, clientID.ToString(), true), currentSession, false, false);
                            break;

                        case Message.MessagesType.COVISE_MESSAGE_VRB_REGISTRY_ENTRY_CHANGED:
                            int senderID = m.message.readInt();
                            string className = m.message.readString();
                            string variable = m.message.readString();
                            MessageBuffer mb4 = m.message.readMessageBuffer();
                            SharedStateManager.Instance.updateSharedState(className, variable, mb4);
                            break;
                            

                        case Message.MessagesType.COVISE_MESSAGE_VRB_SET_USERINFO:
                            int sessionSwitchUserId = m.message.readInt();
                            break;
                        case Message.MessagesType.COVISE_MESSAGE_VRB_QUIT:
                            int disconnectedUserId = m.message.readInt();
                            string msgDisconnectedUser = "DISCONNECTEDUSER:" + disconnectedUserId.ToString() + "|";
                            SendDisconnecteUser(msgDisconnectedUser);
                            break;

                        case Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE:
                            int msgIdentifyer = m.message.readInt();

                            // Head Position
                            if(msgIdentifyer == 100)
                            {
                                int senderId = m.message.readInt();
                                string ipAddress = m.message.readString();
                                int positionX = m.message.readInt();
                                int positionY = m.message.readInt();
                                int positionZ = m.message.readInt();
                                int rotationX = m.message.readInt();
                                int rotationY = m.message.readInt();
                                int rotationZ = m.message.readInt();

                                string msgHead = "HEAD:" + senderId.ToString() + " " + positionX.ToString() + " " + positionY.ToString() + " " + positionZ.ToString() + " " + rotationX.ToString() + " " + rotationY + " " + rotationZ.ToString() + "|";
                                SendHmdPosition(msgHead);
                                break;
                            }

                            //ControllerPosition
                            if (msgIdentifyer == 101)
                            {
                                int senderId = m.message.readInt();
                                int handId = m.message.readInt();
                                int Controller_PositionX = m.message.readInt();
                                int Controller_PositionY = m.message.readInt();
                                int Controller_PositionZ = m.message.readInt();

                                int Controller_RotationX = m.message.readInt();
                                int Controller_RotationY = m.message.readInt();
                                int Controller_RotationZ = m.message.readInt();

                                int Controller_OffSetRotationZ = m.message.readInt();

                                var ControllerDataIntArray = new int[] { senderId, msgType, handId, Controller_PositionX, Controller_PositionY, Controller_PositionZ, Controller_RotationX, Controller_RotationY, Controller_RotationZ };
                                string msgController = "CONTROLLER:" + senderId.ToString() + " " + handId.ToString() + " " + Controller_PositionX.ToString() + " " + Controller_PositionY.ToString() + " " + Controller_PositionZ + " " + Controller_RotationX + " " + Controller_RotationY + " " + Controller_RotationZ + " " + Controller_OffSetRotationZ + "|";
                                SendControllerPosition(msgController);
                                break;
                            }
                            if(msgIdentifyer == 103)
                            {
                                int disconnectedUserId_SessionChanged = m.message.readInt();
                                string msgDisconnectedUser2 = "DISCONNECTEDUSER:" + disconnectedUserId_SessionChanged.ToString() + "|";
                                SendDisconnecteUser(msgDisconnectedUser2);
                                break;
                            }
                            if(msgIdentifyer ==102)
                            {
                                int buttonId = m.message.readInt();
                                int handId = m.message.readInt();
                                int senderId = m.message.readInt();
                                // TODO need unter Type für Controller solange solte es so gehen
                                if (senderId < 100)
                                {
                                    int[] intArrayControllerInput = new int[3];
                                    intArrayControllerInput[0] = buttonId;
                                    intArrayControllerInput[1] = handId;
                                    intArrayControllerInput[2] = senderId;
                                    string msgControllerInput = "CONTROLLERINPUT:" + buttonId.ToString() + " " + handId.ToString() + " " +senderId.ToString() +"|";
                                    SendControllerInput(msgControllerInput);
                                    break;
                                }
                                break;
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {

                log.Fatal("Method Read is Crashed!" + e);
                return new BaseResult();
            }
        }

        //public async Task SendClipPlaneToVred()
        //{
        //    await Task.Run(() => _mainWindowViewModel.communicationToVredManager.BeginSendlipPlane());
        //}

        public void SendTeleportToVred(string msg)
        {
            _mainWindowViewModel.communicationToVredManager.BeginSendTeleport(msg);
        }

        public void SendHmdPosition(string msg)
        {
            _mainWindowViewModel.communicationToVredManager.BeginSend(msg);
        }

        public void SendControllerInput(string msg)
        {
            _mainWindowViewModel.communicationToVredManager.BeginSend(msg);
        }

        public void SendControllerPosition(string msg)
        {
            _mainWindowViewModel.communicationToVredManager.BeginSend(msg);
        }

        public void SendDisconnecteUser(string msg)
        {
            _mainWindowViewModel.communicationToVredManager.BeginSend(msg);
        }


        //public BaseResult SubscribeVariable(SessionID sessionId, int senderId, TcpSocketManager tcpSocket)
        //{
        //    try
        //    {
        //        SharedState<MessageBuffer> globalValue = new SharedState<MessageBuffer>("ClipPlane", SharedStateType.ALWAYS_SHARE);
        //        MessageBuffer mb = globalValue.OnSessionEnter(sessionId, senderId);
        //        Message m = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_REGISTRY_SUBSCRIBE_VARIABLE, senderId);
        //        tcpSocket.send_msg_with_SenderId(m);
        //        return new BaseResult();
        //    }
        //    catch (Exception e)
        //    {
        //        log.Fatal("Method SubscribeVariable is Crashed!" + e);
        //        return new BaseResult(e);
        //    }
        //}

        //public BaseResult ChangeVariable(SessionID sessionId, int senderId, TcpSocketManager tcpSocket, string variableName, string variablValue)
        //{
        //    try
        //    {
        //        SharedState<MessageBuffer> globalValue = new SharedState<MessageBuffer>(variableName, SharedStateType.ALWAYS_SHARE);
        //        MessageBuffer mb = globalValue.ChangeSharedState(sessionId, senderId, variablValue);
        //        Message m = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_REGISTRY_SET_VALUE, senderId);
        //        tcpSocket.send_msg_with_SenderId(m);
        //        return new BaseResult();
        //    }
        //    catch (Exception e)
        //    {
        //        log.Fatal("Method ChangeVariable is Crashed!" + e);

        //        return new BaseResult(e);
        //    }
        //}

        //public BaseResult ChangeVariable(SessionID sessionId, int senderId, TcpSocketManager tcpSocket, string variableName, float[] variablValue)
        //{
        //    try
        //    {
        //        SharedState<MessageBuffer> globalValue = new SharedState<MessageBuffer>(variableName, SharedStateType.ALWAYS_SHARE);
        //        MessageBuffer mb = globalValue.ChangeSharedState(sessionId, senderId, variablValue);
        //        Message m = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_REGISTRY_SET_VALUE, senderId);
        //        tcpSocket.send_msg_with_SenderId(m);
        //        return new BaseResult();
        //    }
        //    catch (Exception e)
        //    {
        //        log.Fatal("Method ChangeVariable is Crashed!" + e);

        //        return new BaseResult(e);
        //    }
        //}

		public ValueResult<UserModel> ReadUser(ValueResult<UserModel> user)
		{
            try
            {
                var buffer = new byte[64000];
                var ns = TcpClient.GetStream();

                while (true)
                {
                    int len = 0;
                    while (len < 16)
                    {
                        int numRead;
                        try
                        {
                            numRead = ns.Read(buffer, len, 16 - len);
                        }
                        catch (System.IO.IOException)
                        {
                            // probably socket closed
                            return new ValueResult<UserModel>();
                        }
                        len += numRead;
                    }

                    int msgType = BitConverter.ToInt32(buffer, 2 * 4);
                    int length = BitConverter.ToInt32(buffer, 3 * 4);
                    length = (int)ByteSwap.swap((uint)length);
                    msgType = (int)ByteSwap.swap((uint)msgType);
                    len = 0;
                    while (len < length)
                    {
                        int numRead;
                        try
                        {
                            numRead = ns.Read(buffer, len, length - len);
                        }
                        catch (System.IO.IOException)
                        {
                            // probably socket closed
                            return new ValueResult<UserModel>();
                        }
                        len += numRead;
                    }
                    Message m = new Message(new MessageBuffer(buffer), (Message.MessagesType)msgType);
                    switch (m.Type)
                    {
                        case Message.MessagesType.COVISE_MESSAGE_VRB_GET_ID:
                            clientID = m.message.readInt();
                            user.Value.UserId = clientID;
                            return user;
                    }
                }
            }
			catch(Exception e)
            {
                log.Fatal("Method ReadUser is Crashed!" + e);
                return null;
            }
		}

		// Method to Close the Socket
		public void CloseSocket()
		{
            try
            {
                MessageBuffer mb = new MessageBuffer();
                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_PLOT);
                MessageBuffer mb2 = new MessageBuffer();
                Message msg2 = new Message(mb2, Message.MessagesType.COVISE_MESSAGE_VRB_QUIT);
                send_msg(msg);
                send_msg(msg2);
                
                TcpClient.Close();
            }
            catch(Exception e)
            {
                log.Fatal("Method CloseSocket is Crashed!" + e);
            }
		}
	}
}
