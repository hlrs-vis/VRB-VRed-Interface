using System;

namespace BuisnessLayer.Results.Events
{
	public class TrackingChangedEventArgs : BaseResult
	{
		public TrackingChangedEventArgs(Exception innerException) : base(innerException)
		{
			IsAvailable = false;
		}
		public TrackingChangedEventArgs(bool isAvailable)
		{
			IsAvailable = isAvailable;
		}

		public bool IsAvailable { get; } // readonly
	}
}