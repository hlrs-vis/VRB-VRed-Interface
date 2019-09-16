using System;
using System.Text.RegularExpressions;
using System.Windows.Input;
using BuisnessLayer.Manager;
using BuisnessLayer.ViewModels.Base;
using BuisnessLayer.ViewModels.Control;

namespace BuisnessLayer.ViewModels.Rooms
{
	public class CreateRoomViewModel : CommonBase
	{
        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly RoomsViewModel _roomsViewModel;

		public CreateRoomViewModel(RoomsViewModel roomsViewModel)
		{
			_roomsViewModel = roomsViewModel;

			CreateRoomCommand = new CreateRoomCommand(this);
            IsValid = true;
            CheckValidation = false;

		}

		private string _roomName;

		public string RoomName
		{
			get => _roomName;
			set
			{
				_roomName = value;
				RaisePropertyChanged("RoomName");


				Validate();
			}
		}

		private bool _isValid;
		public bool IsValid
		{
			get => _isValid;
			set
			{
				if (value == _isValid)
					return;

				_isValid = value;
				RaisePropertyChanged("IsValid");
			}
		}


		private bool _checkValidation;
		public bool CheckValidation
		{
			get => _checkValidation;
			set
			{
				_checkValidation = value;
				RaisePropertyChanged("CheckValidation");
			}
		}

		public void Validate()
		{
            CheckValidation = true;
			// check if the value is null or Empty
			IsValid = !string.IsNullOrEmpty(RoomName);
		}

		#region Command Methods

		public void CreateRoom()
		{
            Validate();

            if (IsValid == false)
                return;

			_roomsViewModel.CreateRoom();

		}

		#endregion

		#region Commands

		public ICommand CreateRoomCommand { get; private set; }


		#endregion


	}

	public class CreateRoomCommand : ICommand
	{
		private readonly CreateRoomViewModel _viewModel;

		public CreateRoomCommand(CreateRoomViewModel viewModel)
		{
			_viewModel = viewModel;
		}
		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_viewModel.CreateRoom();
		}

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}
	}

}