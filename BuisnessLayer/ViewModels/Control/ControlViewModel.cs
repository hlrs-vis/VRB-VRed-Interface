using BuisnessLayer.Base;
using BuisnessLayer.Manager;
using BuisnessLayer.Results.Events;
using BuisnessLayer.ViewModels.Base;
using BuisnessLayer.ViewModels.Rooms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuisnessLayer.ViewModels.Control
{
    public class ControlViewModel : CommonBase
	{
        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly int _udpPort;
        private readonly string _udpIpAdress;
        public readonly MainWindowViewModel _mainWindowViewModel;
        public readonly int userId;
        private SessionID sessionId;
        private SharedState<List<float>> resultClipPlaneFloat;
        public readonly TcpSocketManager tcpSocketManager;
        CancellationTokenSource ctsHmdTracking;
        CancellationTokenSource ctsControllerTracking;
        CancellationTokenSource ctsConnectionToVred;
        CancellationTokenSource ctsConnectionFromVred;
        public static int[] teleporOffset = new int[4] { -1, -1, -1, -1 };
        public static bool offSetHeadIsRead = true;
        public static int transOffSetHeadX;
        public static int transOffSetHeadY;
        public static int transOffSetHeadZ;
        public static int rotOffSetHeadZ;
        public static bool offSetControllerIsRead = true;
        public static int transOffSetControllerX;
        public static int transOffSetControllerY;
        public static int transOffSetControllerZ;
        public static int rotOffSetControllerZ;
        public CommunicationToVredManager communicationToVredManagerObject;
        public ControlViewModel(MainWindowViewModel mainWindowViewModel, TcpSocketManager pTcpSocket, SessionID pSessionId)
		{
            //ToDo change hardcoded Variables
            tcpSocketManager = pTcpSocket;
            userId = pSessionId.m_owner;
            sessionId = pSessionId;
            _udpPort = Int32.Parse(mainWindowViewModel.LoginViewModel.UdpPortTextBoxViewModel.Value);
            _udpIpAdress = mainWindowViewModel.LoginViewModel.ServerIpTextBoxViewModel.Value;
            _vrManager = new VrManager(userId, pSessionId.m_name, tcpSocketManager);
            VrManager.CloseVredEngine();
            _vrManager.StartVredEngine();
            _mainWindowViewModel = mainWindowViewModel;
            LeaveRoomCommand = new LeaveRoomCommand(this);
            HeadTracking = false;
			ControllerTracking = false;
            resultClipPlaneFloat = new SharedState<List<float>>("ClipPlane", new List<float>() { 1, 2, 3, 4, 5, 6, 7 });
            resultClipPlaneFloat.setUpdateFunction(OnClipPlaneVrbUpdate);
            StartConnectToVredSocket();
            StartReceiverFromVred();
        }
        private async void OnClipPlaneVrbUpdate()
        {
            string msg = "CLIPPLANE:";
            for (int i = 0; i < 7; i++)
            {
                msg += resultClipPlaneFloat.val()[i].ToString("G9") + " ";
            }
            lock (CommunicationToVredManager.DataQueueClipPlane)
            {
                CommunicationToVredManager.DataQueueClipPlane.Enqueue(msg);
            }
            await SendClipPlaneToVred();
        }
        public async Task SendClipPlaneToVred()
        {
            await Task.Run(() => _mainWindowViewModel.communicationToVredManager.BeginSendlipPlane());
        }
        public readonly VrManager _vrManager;

        public void StartConnectToVredSocket()
        {
            Task.Run(() => StartAsyncConnectToVredSocket());
        }
        public async Task StartAsyncConnectToVredSocket()
        {
            ctsConnectionToVred = new CancellationTokenSource();
            var res = await Task.Run(() => _mainWindowViewModel.communicationToVredManager.Connect("127.0.0.1", 3221, ctsConnectionToVred.Token));

            if (res.HasException)
            {
                if (!ctsConnectionToVred.Token.IsCancellationRequested)
                {
                    ctsConnectionToVred.Cancel();
                    StartConnectToVredSocket();
                }
            }
        }

        public async Task StartReceiverFromVred()
        {
            ctsConnectionFromVred = new CancellationTokenSource();
            var res = await Task.Run(() => _mainWindowViewModel.receiverFromVred.StartListen(this, ctsConnectionFromVred.Token));
            if (res.HasException)
            {
                ctsConnectionFromVred.Cancel();
            }
            else
            {
                StartReceiverFromVred();
            }
        }

        #region View Properties

        public string RoomId { get; set; }

		private bool _headTracking;
		public bool HeadTracking
		{
			get => _headTracking;
			set
			{
				_headTracking = value;
				RaisePropertyChanged("HeadTracking");

                try
                {
                    if (_headTracking)
                    {
                        ctsHmdTracking = new CancellationTokenSource();
                        CheckHeadTracking(ctsHmdTracking.Token);
                    }

                    else
                    {
                        SetTrackingAviable(Tracker.Head, TrackingState.Default);
                        ctsHmdTracking.Cancel();
                    }
                }
                catch(NullReferenceException e)
                {

                }

			}
		}


		private bool _controllerTracking;

		public bool ControllerTracking
		{
			get => _controllerTracking;
			set
			{
				_controllerTracking = value;
				RaisePropertyChanged("ControllerTracking");

                try
                {
                    if (_controllerTracking)
                    {
                        ctsControllerTracking = new CancellationTokenSource();
                        CheckControllerTracking(ctsControllerTracking.Token);
                    }
                    else
                    {
                        SetTrackingAviable(Tracker.LeftController, TrackingState.Default);
                        SetTrackingAviable(Tracker.RightController, TrackingState.Default);
                        ctsControllerTracking.Cancel();
                    }
                }
                catch(NullReferenceException e)
                {

                }
			}
		}


		private string _isHeadAviable;
		public string IsHeadAviable
		{
			get => _isHeadAviable;
			set
			{
				_isHeadAviable = value;
				RaisePropertyChanged("IsHeadAviable");
			}
		}

		private string _isLeftControllerAviable;
		public string IsLeftControllerAviable
		{
			get => _isLeftControllerAviable;
			set
			{
				_isLeftControllerAviable = value;
				RaisePropertyChanged("IsLeftControllerAviable");
			}
		}

		private string _isRightControllerAviable;
		public string IsRightControllerAviable
		{
			get => _isRightControllerAviable;
			set
			{
				_isRightControllerAviable = value;
				RaisePropertyChanged("IsRightControllerAviable");
			}
		}

		#endregion

		public async void CheckHeadTracking(CancellationToken cts)
		{

            _vrManager.TrackingChangedEvent += VrManagerOnTrackingChangedEvent;
			var result = await _vrManager.StartHmdTrackingAsync(cts);

			if (result.HasException)
			{
				HeadTracking = false;
                log.Info("CheckHeadTracking is canceled!!!!");
            }
		}

        public string GetOwnIp()
        {
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST  
            Console.WriteLine(hostName);
            // Get the IP  
            string myIP = Dns.GetHostByName(hostName).AddressList[0].ToString();
            Console.WriteLine("My IP Address is :" + myIP);
            return myIP;
        }


        public async void CheckControllerTracking(CancellationToken cts)
        {
            _vrManager.TrackingChangedController += VrManagerOnTrackingControllerChangedEvent;
            var result = await _vrManager.StartControllerTrackingAsync(cts);

            if (result.HasException)
            {
                log.Info("CheckControllerTracking is canceled!!!!");
                ControllerTracking = false;
            }
        }


        public void updateClipPlane(string[] data)
        {

            string msg = data[1];
            string [] msgArray = msg.Split('|');
            List<float> cps = new List<float>(7);
            for (int i = 0; i < msgArray.Length - 1; i++)
            {
                float x = Convert.ToSingle(msgArray[i], CultureInfo.InvariantCulture.NumberFormat);
                cps[i] = x;
            }
            resultClipPlaneFloat.val(cps);
            //tcpSocketManager.ChangeVariable(sessionId, userId, tcpSocketManager, "ClipPlane", resultClipPlaneFloat);
        }

        private void VrManagerOnTrackingChangedEvent(object sender, TrackingChangedEventArgs e)
		{
			SetHeadTrackingAviable(e.IsAvailable ? TrackingState.Aviable : TrackingState.Error);
		}

        private void VrManagerOnTrackingControllerChangedEvent(object sender, TrackingChangedEventArgs e, Tracker tracker)
        {
            if(tracker == Tracker.LeftController)
                SetControllerTrackingAviable(e.IsAvailable ? TrackingState.Aviable : TrackingState.Error, Tracker.LeftController);
            if (tracker == Tracker.RightController)
                SetControllerTrackingAviable(e.IsAvailable ? TrackingState.Aviable : TrackingState.Error, Tracker.RightController);
     
        }

        #region Command Methods

        private void KillAllPipeInstances()
        {
            if (ctsHmdTracking != null)
            {
                ctsHmdTracking.Cancel();
            }
            if (ctsControllerTracking != null)
            {
                ctsControllerTracking.Cancel();
            }
        }

        private void KillAllTasks()
        {
            if(ctsConnectionToVred != null)
            {
                ctsConnectionToVred.Cancel();
            }
            if (ctsConnectionFromVred != null)
            {
                ctsConnectionFromVred.Cancel();
            }
        }
        public void LeaveRoom()
		{
			_mainWindowViewModel.ChangePage(Page.Rooms, Page.Control);
			try
			{
                _mainWindowViewModel.ServerManager.SendDisconnectedUser(tcpSocketManager, userId);
                _mainWindowViewModel.ServerManager.JoinSession(sessionId,userId, RoomsViewModel.privateSession);
                SharedStateManager.Instance.update(new SessionID(), new SessionID(), false, false);
                VrManager.CloseVredEngine();
                _mainWindowViewModel.receiverFromVred.Close();
                CommunicationToVredManager.isConnected = false;
                KillAllPipeInstances();
                KillAllTasks();

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

		#endregion


		#region Helping Functions

		private void SetHeadTrackingAviable(TrackingState state)
		{
			switch (state)
			{
				case TrackingState.Default:
					IsHeadAviable = "../../Assets/Images/ViveHmdDefault_300.png";
					break;
				case TrackingState.Aviable:
					IsHeadAviable = "../../Assets/Images/ViveHmdSuccess_300.png";
					break;
				case TrackingState.Error:
					IsHeadAviable = "../../Assets/Images/ViveHmdError_300.png";
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, null);
			}
		}

		private void SetControllerTrackingAviable(TrackingState state, Tracker tracker)
		{
            try
            {
                switch (state)
                {
                    case TrackingState.Default:
                        if (tracker == Tracker.LeftController)
                            IsLeftControllerAviable = "../../Assets/Images/ViveControllersDefault_300.png";

                        else if (tracker == Tracker.RightController)
                            IsRightControllerAviable = "../../Assets/Images/ViveControllersDefault_300.png";
                        break;


                    case TrackingState.Aviable:
                        if (tracker == Tracker.LeftController)
                            IsLeftControllerAviable = "../../Assets/Images/ViveControllersSuccess_300.png";

                        else if (tracker == Tracker.RightController)
                            IsRightControllerAviable = "../../Assets/Images/ViveControllersSuccess_300.png";
                        break;


                    case TrackingState.Error:
                        if (tracker == Tracker.LeftController)
                            IsLeftControllerAviable = "../../Assets/Images/ViveControllersError_300.png";

                        else if (tracker == Tracker.RightController)
                            IsRightControllerAviable = "../../Assets/Images/ViveControllersError_300.png";
                        break;
                }
            }
            catch(Exception e)
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


		private void SetTrackingAviable(Tracker traker, TrackingState trackingState)
		{
			switch (traker)
			{
				case Tracker.Head:
					SetHeadTrackingAviable(trackingState);
					break;
				case Tracker.LeftController:
				case Tracker.RightController:
					SetControllerTrackingAviable(trackingState, traker);
					break;
			}
		}

		#endregion

		#region Command

		public ICommand LeaveRoomCommand { get; private set; }

		#endregion
	}

	internal class LeaveRoomCommand : ICommand
	{
		private readonly ControlViewModel _viewModel;

		public LeaveRoomCommand(ControlViewModel viewModel)
		{
			_viewModel = viewModel;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_viewModel.LeaveRoom();
		}

		public event EventHandler CanExecuteChanged;
	}

   

	internal enum TrackingState
	{
		Default = 0,
		Aviable = 1,
		Error = 2
	}

	public enum Tracker
	{
		Head = 0,
		LeftController = 1,
		RightController = 2
	}
}
