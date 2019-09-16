using BuisnessLayer.Manager;
using BuisnessLayer.ViewModels.Base;
using BuisnessLayer.ViewModels.Control;
using BuisnessLayer.ViewModels.Login;
using BuisnessLayer.ViewModels.Rooms;
using System;
using System.Windows;

namespace BuisnessLayer.ViewModels
{
    public class MainWindowViewModel : CommonBase
	{
        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly double _preWindowWidth;
		private readonly double _preWindowHeight;


		public MainWindowViewModel()
		{
			WindowHeight = _preWindowHeight = 600.0;
			WindowWidth = _preWindowWidth = 700.0;

			var desktopWorkingArea = SystemParameters.WorkArea;
			WindowLeft = desktopWorkingArea.Width / 2 - WindowWidth / 2;
			WindowTop = desktopWorkingArea.Height / 2 - WindowHeight / 2;

			HeaderVisible = true;
			FooterVisible = true;

			LoginViewModel = new LoginViewModel(this);

			ShowControlView = false;
		}


        public ReceiverFromVred receiverFromVred { get; set; }
		public LoginViewModel LoginViewModel { get; set; }
		public ServerManager ServerManager { get; set; }
        public CommunicationToVredManager communicationToVredManager { get; set; }
        private RoomsViewModel _roomsViewModel;
		public RoomsViewModel RoomsViewModel
		{
			get => _roomsViewModel;
			set
			{
				_roomsViewModel = value;
				RaisePropertyChanged("RoomsViewModel");
			}
		}


		private ControlViewModel _controlViewModel;
		public ControlViewModel ControlViewModel
		{
			get => _controlViewModel;
			set
			{
				_controlViewModel = value;
				RaisePropertyChanged("ControlViewModel");
			}
		}

		private bool _showControlView;
		public bool ShowControlView
		{
			get => _showControlView;
			set
			{
				_showControlView = value;
				RaisePropertyChanged("ShowControlView");
			}
		}


		/// <summary>
		/// Backendfield for Slide-Animation Propertie ChangeView
		/// </summary>
		private string _changeView;
		/// <summary>
		/// Property to start the slide animation from the Login to the Rooms and return
		/// if value == "Rooms" -> Page Slide forward from Login to Rooms (Rooms to left)
		/// if value == "Login" -> Page Slide back from Rooms to Login (Room to right)
		/// Public getter
		/// Private setter
		/// </summary>
		public string ChangeView
		{
			get => _changeView;
			private set
			{
				_changeView = value;
				RaisePropertyChanged("ChangeView");
			}
		}


		private double _windowWidth;
		public double WindowWidth
		{
			get => _windowWidth;
			set
			{
				_windowWidth = value;
				RaisePropertyChanged("WindowWidth");
			}
		}

		private double _windowHeight;
		public double WindowHeight
		{
			get => _windowHeight;
			set
			{
				_windowHeight = value;
				RaisePropertyChanged("WindowHeight");
			}
		}



		private double _windowLeft;
		public double WindowLeft
		{
			get => _windowLeft;
			set
			{
				_windowLeft = value;
				RaisePropertyChanged("WindowLeft");
			}
		}

		private double _windowTop;
		public double WindowTop
		{
			get => _windowTop;
			set
			{
				_windowTop = value;
				RaisePropertyChanged("WindowTop");
			}
		}

		private bool _headerVisible;
		public bool HeaderVisible
		{
			get => _headerVisible;
			set
			{
				_headerVisible = value;
				RaisePropertyChanged("HeaderVisible");
			}
		}

		private bool _footerVisible;
		public bool FooterVisible
		{
			get => _footerVisible;
			set
			{
				_footerVisible = value;
				RaisePropertyChanged("FooterVisible");
			}
		}




		private double _preWindowTop;
		private double _preWindowLeft;
		private void MoveToControllerView()
		{
			WindowHeight = 300;
			WindowWidth = 350;

			_preWindowLeft = WindowLeft;
			_preWindowTop = WindowTop;

			var desktopWorkingArea = SystemParameters.WorkArea;
			WindowLeft = desktopWorkingArea.Right - WindowWidth;
			WindowTop = desktopWorkingArea.Bottom - WindowHeight;

			ShowControlView = true;

			HeaderVisible = false;
			FooterVisible = false;
		}

		private void LeaveControllerView()
		{
			WindowHeight = _preWindowHeight;
			WindowWidth = _preWindowWidth;

			ShowControlView = false;

			WindowLeft = _preWindowLeft;
			WindowTop = _preWindowTop;

			HeaderVisible = true;
			FooterVisible = true;
		}

		/// <summary>
		/// Change the page to the wish page. 
		/// </summary>
		/// <param name="to"></param>
		/// <param name="from">if from will not stated the Method will go forward to the next page</param>
		public void ChangePage(Page to, Page from = Page.Forward)
		{
			if (from == Page.Forward)
			{
				switch (to)
				{
					case Page.Rooms:
						ChangeView = "Rooms";
						break;
					case Page.Control:
						MoveToControllerView();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(to), to, null);
				}
			}
			else if (from == Page.Control && to == Page.Rooms)
			{
				LeaveControllerView();
				_roomsViewModel.HideCreateRoomModal();
			}
			else if (from == Page.Rooms && to == Page.Login)
			{
				ChangeView = "Login";
			}

		}
	}

	public enum Page
	{
		Login = 0,
		Rooms = 1,
		Control = 2,
		Forward = 3
	}
}
