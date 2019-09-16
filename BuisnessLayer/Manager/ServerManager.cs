using BuisnessLayer.Base;
using BuisnessLayer.Base;
using BuisnessLayer.Results;
using BuisnessLayer.ViewModels;
using DataModels.Base;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BuisnessLayer.Manager
{
    public class ServerManager
	{
        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TcpSocketManager tcpSocket;
        public static string clientIpAddressForTracking = "";
        MainWindowViewModel _mainWindowViewModel;

        public ServerManager(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
        }
		public ValueResult<UserModel> LogIn(string name, string email, string clientIpAddress, string serverIpAddress, int port)
		{
			try
			{
                clientIpAddressForTracking = clientIpAddress;
                tcpSocket = new TcpSocketManager(serverIpAddress, port, _mainWindowViewModel);
                SharedStateManager.Instance.initializeSender(tcpSocket);
                var hasExceptionConnection = tcpSocket.Connect();
                ValueResult<UserModel> user = new ValueResult<UserModel>
                {
	                Value = new UserModel {Name = name, Email = email, UserIpAdress = clientIpAddress}
                };

                tcpSocket.sendInitialMessage(user.Value);

				while (user.Value.UserId == 0)
				{
					ValueResult<UserModel> tempUser = tcpSocket.ReadUser(user);
				}
                var res = Task.Run(() => tcpSocket.Read());
           
                return user;
			}
			catch (Exception e)
			{
                log.Error("Method LogIn is crashed!" + e);
                return new ValueResult<UserModel>(e);
			}
		}

        public BaseResult SendDisconnectedUser(TcpSocketManager tcpSocketManager, int userId)
        {
            try
            {
                MessageBuffer mb = new MessageBuffer();
                int type = 103;
                mb.add(type);
                mb.add(userId);
                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                tcpSocketManager.send_msg(msg);
                return new BaseResult();
            }
            catch(Exception e)
            {
                log.Error("Method SendDisconnectedUser is crashed!" + e);

                return new BaseResult(e);
            }

        }

        
        public BaseResult Logout()
		{
			try
			{
				tcpSocket.CloseSocket();
				return new BaseResult();
			}
			catch (Exception e)
			{
                log.Error("Method Logout is crashed!" + e);

                return new BaseResult(e);
			}
		}

		/// <summary>
		/// Session wird Erstelllt UND automatisch betreten!!!!!!!!!!!!!!!!!!!!!!!!! da nach der RoomsListe direkt die SET-Session  
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		public ValueResult<SessionID> CreateSession(int userId, string sessionName)
		{
			try
			{
				ValueResult<SessionID> currentSessions= tcpSocket.sendSetNewSessionMessage(userId, sessionName);
				return currentSessions;
			}
			catch (Exception e)
			{
                log.Error("Method CreateSession is crashed!" + e);

                return new ValueResult<SessionID>(e);
			}
		}

		public ValueResult<SessionID> JoinSession(SessionID currentSession, int senderId, SessionID sid)//(ValueResult<RoomModel> session)
		{
			try
			{
				// Session verlassen
				MessageBuffer mb = new MessageBuffer();
                SharedStateSerializer.serialize(ref mb, currentSession);
				Message m = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRBC_UNOBSERVE_SESSION);
				tcpSocket.send_msg(m);


				//neue Session setzen und Senden
				MessageBuffer mb2 = new MessageBuffer();
                SharedStateSerializer.serialize(ref mb2, sid);
				mb2.add(senderId);
				Message m2 = new Message(mb2, Message.MessagesType.COVISE_MESSAGE_VRBC_SET_SESSION);
				tcpSocket.send_msg(m2);

				return new ValueResult<SessionID>(sid);
			}
			catch (Exception e)
			{
                log.Error("Method JoinSession is crashed!" + e);

                return new ValueResult<SessionID>(e);
			}
		}
	}
}
