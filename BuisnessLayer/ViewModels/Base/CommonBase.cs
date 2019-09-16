using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuisnessLayer.ViewModels.Base
{
	public class CommonBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged; protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = this.PropertyChanged; if (handler != null)
			{
				PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName); handler(this, args);
			}
		}
	}
}
