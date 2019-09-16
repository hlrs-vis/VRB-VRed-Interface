using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BuisnessLayer.Manager;
using BuisnessLayer.Results;
using BuisnessLayer.Results.Events;
using BuisnessLayer.ViewModels.Control;

namespace BuisnessLayer.Base
{

	public class VrManager
	{

        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Declare the delegate (if using non-generic pattern).
        public delegate void TrackingChangedEventHandler(object sender, TrackingChangedEventArgs e);
        public delegate void TrackingChangedControllerEventHandler(object sender, TrackingChangedEventArgs e, Tracker tracker);
        // Declare the event.
        public event TrackingChangedEventHandler TrackingChangedEvent;
        public event TrackingChangedControllerEventHandler TrackingChangedController;

        // Wrap the event in a protected virtual method
        // to enable derived classes to raise the event.
        protected virtual void RaiseSampleEvent(TrackingChangedEventArgs eventArgs)
		{
			// Raise the event by using the () operator.
			TrackingChangedEvent?.Invoke(this, eventArgs);
		}

        public virtual void RaiseControllerEvent(TrackingChangedEventArgs eventArgs, Tracker tracker)
        {
            // Raise the event by using the () operator.
            TrackingChangedController?.Invoke(this, eventArgs, tracker);
        }

        public int userId;
		public string session;
        public TcpSocketManager tcpSocketManager;

		public VrManager(int pUserId, string pSessionId, TcpSocketManager pTcpSocketManager)
		{
			userId = pUserId;
			session = pSessionId;
            tcpSocketManager = pTcpSocketManager;
		}

		public async Task<BaseResult> StartHmdTrackingAsync(CancellationToken ct)
		{
            try
            {
                return await Task.Run(() => StartHmdTracking(ct));
            }
            catch(Exception e)
            {
                log.Fatal("Method StartHmdTrackingAsync is Crashed!" + e);
                return new BaseResult(e);
            }
		}

		private BaseResult StartHmdTracking(CancellationToken ct)
		{
			try
			{
				HmdTracking hmdTracking = new HmdTracking(userId, session, tcpSocketManager);
				hmdTracking.TrackingChangedEvent += HmdTrackingOnTrackingChangedEvent;
				hmdTracking.StartTracking(ct);
				return new BaseResult();
			}
			catch (Exception e)
			{
                log.Fatal("Method StartHmdTracking is Crashed!" + e);
                return new BaseResult(e);
			}
		}
        public static void CloseVredEngine()
        {
            try
            {
                Process[] GetPArry = Process.GetProcesses();
                foreach (Process testProcess in GetPArry)
                {
                    string ProcessName = testProcess.ProcessName;

                    ProcessName = ProcessName.ToLower();
                    if (ProcessName.CompareTo("vredpro") == 0)
                        testProcess.Kill();
                }
            }
            catch (Exception e)
            {
                log.Fatal("Method CloseVredEngine is Crashed!" + e);
            }
        }

        public void StartVredEngine()
        {
            try
            {
                System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string pathToVredScript = exePath + "Plugins/Sociate.py";
                string pathToScene = exePath + "Scenes/HLRS.vpb";
                pProcess.StartInfo.FileName = pathToScene;
                //pProcess.StartInfo.Arguments = exePath + "Plugins\\Sociate.py";

                pProcess.Start();
            }
            catch (Exception e)
            {
                log.Info("Die 'VREDPro.exe'sollte unter folgendem Installationpfad liegen: C:/Program Files/Autodesk/VREDPro-10/Bin/Win64/VREDPro.exe");
            }
        }

		private void HmdTrackingOnTrackingChangedEvent(object sender, TrackingChangedEventArgs e)
		{
			RaiseSampleEvent(e);
		}

        private void ControllerTrackingOnTrackingChangedEvent(object sender, TrackingChangedEventArgs e, Tracker tracker)
        {
            RaiseControllerEvent(e, tracker);
        }

        public async Task<BaseResult> StartControllerTrackingAsync(CancellationToken ct)
        {
            try
            {
                return await Task.Run(() => StartControllerTracking(ct));
            }
            catch (Exception e)
            {
                return new BaseResult(e);
            }
        }

        public BaseResult StartControllerTracking(CancellationToken ct)
		{
			try
			{
				ControllerTracking controllerTracking = new ControllerTracking(userId, session, tcpSocketManager);
                controllerTracking.TrackingChangedEventController += ControllerTrackingOnTrackingChangedEvent;
                controllerTracking.StartTracking(ct);
                return new BaseResult();
			}
			catch (Exception e)
			{
                log.Fatal("Method StartControllerTracking is Crashed!" + e);

                RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.LeftController);
                RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.RightController);
                return new BaseResult(e);
			}
		}
	}
}
