using BuisnessLayer.Base;
using BuisnessLayer.Manager;
using BuisnessLayer.ViewModels.Base;
using BuisnessLayer.ViewModels.Control;
using System;
using System.Windows.Input;

namespace BuisnessLayer.ViewModels.Rooms
{
    public class RoomViewModel : CommonBase
    {
        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly MainWindowViewModel _mainWindowViewModel;
        private SessionID oldSession, sessionID;

        public RoomViewModel(int userId, SessionID sid, MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
            oldSession = new SessionID(userId, userId.ToString(), true);
            sessionID = sid;
            UserId = userId;
            EnterRoomCommand = new EnterRoomCommand(this);
        }
        #region View Properties

        public string RoomId
        {
            get { return sessionID.m_name; }
            set { sessionID.m_name = value; }
        }

        public int CreatedBy
        {
            get { return sessionID.m_owner; }
            set { sessionID.m_owner = value; }
        }

        public string Accessible() { return sessionID.m_isPrivate ? "Private" : "Public"; }

        public int UserId { get; set; }

		public bool isPrivate() { return sessionID.m_isPrivate; }

		#endregion

		#region Command Methods

		public void EnterRoom()
        {
            var res2 = _mainWindowViewModel.ServerManager.JoinSession(oldSession, _mainWindowViewModel.RoomsViewModel.userId, sessionID);
            SessionID currentSessionId = res2.Value;
            _mainWindowViewModel.ControlViewModel = new ControlViewModel(_mainWindowViewModel, _mainWindowViewModel.ServerManager.tcpSocket, currentSessionId);
            RoomsViewModel.currentSession = currentSessionId;
            _mainWindowViewModel.ChangePage(Page.Control);

        }


        #endregion

        #region Commands
        public ICommand EnterRoomCommand { get; private set; }
        #endregion

    }

	public class EnterRoomCommand : ICommand
    {
        private readonly RoomViewModel _viewModel;

        public EnterRoomCommand(RoomViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _viewModel.EnterRoom();
        }

        public event EventHandler CanExecuteChanged;
    }
}
