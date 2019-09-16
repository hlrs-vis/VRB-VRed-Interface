using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Input;
using System.Windows;
using BuisnessLayer.Manager;
using BuisnessLayer.Results;
using BuisnessLayer.ViewModels.Base;
using BuisnessLayer.ViewModels.Rooms;
using DataModels.Base;

namespace BuisnessLayer.ViewModels.Login
{
	public class LoginViewModel : CommonBase
	{
        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly MainWindowViewModel _mainWindowViewModel;

		public LoginViewModel(MainWindowViewModel mainWindowViewModel)
		{
			_mainWindowViewModel = mainWindowViewModel;

			LoginCommand = new LoginCommand(this);
			AbortLoginCommand = new AbortLoginCommand(this);

			ShowLoginModal = false;

			NameTextBoxViewModel = new ValidationTextBoxViewModel();
			EmailTextBoxViewModel = new ValidationTextBoxViewModel(new Regex(@"\A(?:[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?)\Z"));
			ServerIpTextBoxViewModel = new ValidationTextBoxViewModel();//new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b"));
			TcpPortTextBoxViewModel = new ValidationTextBoxViewModel(new Regex(@"\d{4,6}"));
			UdpPortTextBoxViewModel = new ValidationTextBoxViewModel(new Regex(@"\d{4,6}"));
			UserIpViewModel = new UserIpViewModel();

#if DEBUG
			NameTextBoxViewModel.Value = "James T. Kurk";
			EmailTextBoxViewModel.Value = "JamesTKirk@USS.Enterprise";
			ServerIpTextBoxViewModel.Value = "192.168.1.35";
			TcpPortTextBoxViewModel.Value = "31251";
			UdpPortTextBoxViewModel.Value = "31253";

#endif
		}
		private Thread _loginThread;
		//public string GetOwnIp()
		//{
		//    string hostName = Dns.GetHostName(); // Retrive the Name of HOST  
		//    Console.WriteLine(hostName);
		//    // Get the IP  
		//    string myIP = Dns.GetHostByName(hostName).AddressList[0].ToString();
		//    Console.WriteLine("My IP Address is :" + myIP);
		//    return myIP;
		//}

		public ValidationTextBoxViewModel NameTextBoxViewModel { get; set; }

		public ValidationTextBoxViewModel EmailTextBoxViewModel { get; set; }

		public ValidationTextBoxViewModel ServerIpTextBoxViewModel { get; set; }

		public ValidationTextBoxViewModel TcpPortTextBoxViewModel { get; set; }

		public ValidationTextBoxViewModel UdpPortTextBoxViewModel { get; set; }

		public UserIpViewModel UserIpViewModel { get; set; }

		private bool _showLoginModal;
		public bool ShowLoginModal
		{
			get => _showLoginModal;
			set
			{
				_showLoginModal = value;
				RaisePropertyChanged("ShowLoginModal");
			}
		}


		#region Command Methods

		public void Login()
		{
			ActivateTextBoxValidation();

			// ToDo: Validate all values
			NameTextBoxViewModel.Validate();
			//EmailTextBoxViewModel.Validate();
			ServerIpTextBoxViewModel.Validate();
			TcpPortTextBoxViewModel.Validate();
			UdpPortTextBoxViewModel.Validate();
			UserIpViewModel.Validate();

			if (!NameTextBoxViewModel.IsValid || !ServerIpTextBoxViewModel.IsValid || !TcpPortTextBoxViewModel.IsValid ||
				!UdpPortTextBoxViewModel.IsValid || string.IsNullOrEmpty(UserIpViewModel.SelectedUserIp))
				return;

			_loginThread = new Thread(AsyncLogin);
			_loginThread.Start();	
		}

		private void AsyncLogin()
		{
			ShowLoginModal = true;

			_mainWindowViewModel.ServerManager = new ServerManager(_mainWindowViewModel);
            _mainWindowViewModel.communicationToVredManager = new CommunicationToVredManager();
            _mainWindowViewModel.receiverFromVred = new ReceiverFromVred();
            ValueResult<UserModel> resultUser = _mainWindowViewModel
            .ServerManager
            .LogIn(
            NameTextBoxViewModel.Value,
            EmailTextBoxViewModel.Value,
            UserIpViewModel.SelectedUserIp,
            ServerIpTextBoxViewModel.Value,
            Convert.ToInt32(TcpPortTextBoxViewModel.Value));

            ShowLoginModal = false;
			if (resultUser.HasException)
			{
				Console.WriteLine(resultUser.InnerException.Message);
				MessageBox.Show(resultUser.InnerException.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			else if (resultUser.Value.UserId <= 0)
			{
				Console.WriteLine("Ein Fehler ist aufgetretten. Du konntest nicht angemeldet werden.");
				MessageBox.Show("Ein Fehler ist aufgetretten. Du konntest nicht angemeldet werden.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			else
			{
				// ToDo: Go to next Page
				_mainWindowViewModel.ChangePage(Page.Rooms);
				_mainWindowViewModel.RoomsViewModel = new RoomsViewModel(NameTextBoxViewModel.Value, resultUser.Value.UserId, _mainWindowViewModel);
			}
		}

		public void AbortLogin()
		{
			if (_loginThread != null && _loginThread.IsAlive)
			{
				_loginThread.Abort();
				_loginThread = null;

				ShowLoginModal = false;
			}
		}

		private void LoadNextPage()
		{

		}

		private void ActivateTextBoxValidation()
		{
			NameTextBoxViewModel.CheckValidation = true;
			EmailTextBoxViewModel.CheckValidation = true;
			ServerIpTextBoxViewModel.CheckValidation = true;
			TcpPortTextBoxViewModel.CheckValidation = true;
			UdpPortTextBoxViewModel.CheckValidation = true;
		}

		#endregion

		#region Command Implementations

		public ICommand LoginCommand { get; private set; }

		public ICommand AbortLoginCommand { get; private set; }

		#endregion
	}

	public class LoginCommand : ICommand
	{
		private readonly LoginViewModel _viewModel;

		public LoginCommand(LoginViewModel viewModel)
		{
			_viewModel = viewModel;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_viewModel.Login();
		}

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}
	}

	public class AbortLoginCommand : ICommand
	{
		private readonly LoginViewModel _viewModel;

		public AbortLoginCommand(LoginViewModel viewModel)
		{
			_viewModel = viewModel;
		}
		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_viewModel.AbortLogin();
		}

		public event EventHandler CanExecuteChanged;
	}
}
