using BuisnessLayer.Base;
using BuisnessLayer.Manager;
using BuisnessLayer.ViewModels.Base;
using BuisnessLayer.ViewModels.Control;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuisnessLayer.ViewModels.Rooms
{
    public class RoomsViewModel : CommonBase
	{
        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly MainWindowViewModel _mainWindowViewModel;
		public int userId { get; set; }
		public string UserName { get; set; }
		public static SessionID currentSession { get; set; }
        public static SessionID privateSession { get; set; }
		public List<SessionID> AllPublicRooms= new List<SessionID>();

		public RoomsViewModel(string userName, int userId, MainWindowViewModel mainWindowViewModel)
		{

            Task.Run(() => UpdateRoomsAsync());
            this.userId = userId;
			_mainWindowViewModel = mainWindowViewModel;
			UserName = userName;
			LogoutCommand = new LogoutCommand(this);
			JoinRoomCommand = new JoinRoomCommand(this);
			ToggleCreateRoomCommand = new ToggleCreateRoomCommand(this);

            CreateRoomViewModel = new CreateRoomViewModel(this);

			CreateRoomVisibility = Visibility.Collapsed;
		}


		#region View Properties

		private List<RoomViewModel> _rooms;
		public List<RoomViewModel> Rooms
		{
			get => _rooms;
			set
			{
				_rooms = value;
				RaisePropertyChanged("Rooms");
			}
		}

		private CreateRoomViewModel _createRoomViewModel;
		public CreateRoomViewModel CreateRoomViewModel
		{
			get { return _createRoomViewModel; }
			set
			{
				_createRoomViewModel = value;
				RaisePropertyChanged("CreateRoomViewModel");
			}
		}

		private Visibility _createRoomVisibility;
		public Visibility CreateRoomVisibility
		{
			get => _createRoomVisibility;
			set
			{
				_createRoomVisibility = value;
				RaisePropertyChanged("CreateRoomVisibility");
			}
		}


		#endregion

		public void CreateRoom()
		{
            try
            {
                var result = _mainWindowViewModel.ServerManager.CreateSession(userId, CreateRoomViewModel.RoomName);
                if (result.HasException)
                {
                    throw result.InnerException;
                }

                var res = _mainWindowViewModel.ServerManager.JoinSession(RoomsViewModel.currentSession, _mainWindowViewModel.RoomsViewModel.userId, result.Value);
                SessionID currentSession = res.Value;
                _mainWindowViewModel.ControlViewModel = new ControlViewModel(_mainWindowViewModel, _mainWindowViewModel.ServerManager.tcpSocket, currentSession);
                //LogoutCommand = new LogoutCommand(this);
                _mainWindowViewModel.ChangePage(Page.Control);
            }
            catch (Exception e)
            {
                log.Error(e);
            }
		}


		public void UpdateRooms()
		{
			try
			{
                while (true)
                {
                    var allRooms = MessageHandler.getAllRoom();

                    if (allRooms.Count > 0)
                    {
                        foreach (var item in allRooms)
                        {
                            if (!item.m_isPrivate)
                            {
                                AllPublicRooms.Add(item);
                            }
                        }
                        Rooms = AllPublicRooms.Select(
                            rm => new RoomViewModel(userId, rm, _mainWindowViewModel)).ToList();
                    }

                    currentSession = MessageHandler.getSession();
                    AllPublicRooms.Clear();
                    Thread.Sleep(1000);
                }

            }
			catch (Exception e)
			{
                // Get stack trace for the exception with source file information
                var st = new StackTrace(e, true);
                // Get the top stack frame
                var frame = st.GetFrame(0);
                // Get the line number from the stack frame
                var line = frame.GetFileLineNumber();

                Console.WriteLine(line.ToString() + e.ToString());
            }
		}

		public async Task UpdateRoomsAsync()
		{
			await Task.Run(() => UpdateRooms());
		}

		public void HideCreateRoomModal()
		{
			CreateRoomVisibility = Visibility.Collapsed;
		}

		#region Command Methos
		internal void Join()
		{
			throw new NotImplementedException();
		}

		public void Logout()
		{
			// ToDo: Call Logout from ServerManager
			_mainWindowViewModel.ServerManager.Logout();
			// go to the login View
			_mainWindowViewModel.ChangePage(Page.Login, Page.Rooms);
		}

		public void ToggleCreateRoomModal()
		{
			if (CreateRoomVisibility == Visibility.Collapsed)
			{
				CreateRoomVisibility = Visibility.Visible;
				CreateRoomViewModel = new CreateRoomViewModel(this);
			}
			else
				CreateRoomVisibility = Visibility.Collapsed;
		}
		#endregion


		#region Commands

		public ICommand ToggleCreateRoomCommand { get; private set; }

		public ICommand LogoutCommand { get; private set; }

		public ICommand JoinRoomCommand { get; private set; }

        #endregion

       
	}


	public class ToggleCreateRoomCommand : ICommand
	{
		private readonly RoomsViewModel _viewModel;

		public ToggleCreateRoomCommand(RoomsViewModel viewModel)
		{
			_viewModel = viewModel;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_viewModel.ToggleCreateRoomModal();
		}

		public event EventHandler CanExecuteChanged;
	}
	public class LogoutCommand : ICommand
	{
		private readonly RoomsViewModel _viewModel;

		public LogoutCommand(RoomsViewModel viewModel)
		{
			_viewModel = viewModel;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_viewModel.Logout();
		}

		public event EventHandler CanExecuteChanged;
	}

	public class JoinRoomCommand : ICommand
	{
		private readonly RoomsViewModel _viewModel;

		public JoinRoomCommand(RoomsViewModel viewModel)
		{
			_viewModel = viewModel;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_viewModel.Join();
		}

		public event EventHandler CanExecuteChanged;
	}

}
