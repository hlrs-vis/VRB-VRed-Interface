using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using BuisnessLayer.ViewModels.Base;

namespace BuisnessLayer.ViewModels.Login
{
	public class UserIpViewModel : CommonBase
	{
		public UserIpViewModel()
		{
			ShowValidationMessage = false;
			UserIpList = new List<string>();
			ReadUserIps();

			SelectUserIpIsVisible = true;

			if (UserIpList.Count == 1)
				SelectUserIpIsVisible = false;
		}

		private List<string> _userIpList;
		public List<string> UserIpList
		{
			get => _userIpList;
			set
			{
				_userIpList = value;
				RaisePropertyChanged("UserIpList");
			}
		}


		private string _selectedUserIp;
		public string SelectedUserIp
		{
			get => _selectedUserIp;
			set
			{
				_selectedUserIp = value;
				RaisePropertyChanged("SelectedUserIp");

				Validate();
			}
		}

		private bool _selectUserIpIsVisible;
		public bool SelectUserIpIsVisible
		{
			get => _selectUserIpIsVisible;
			set
			{
				_selectUserIpIsVisible = value;
				RaisePropertyChanged("SelectUserIpIsVisible");
			}
		}

		private bool _showValidationMessage;
		public bool ShowValidationMessage
		{
			get => _showValidationMessage;
			set
			{
				_showValidationMessage = value;
				RaisePropertyChanged("ShowValidationMessage");
			}
		}


		private void ReadUserIps()
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (var ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					UserIpList.Add(ip.ToString());
				}
			}

			if (UserIpList.Count > 0)
				SelectedUserIp = UserIpList.First();
		}

		public void Validate()
		{
			ShowValidationMessage = string.IsNullOrEmpty(SelectedUserIp);
		}
	}
}