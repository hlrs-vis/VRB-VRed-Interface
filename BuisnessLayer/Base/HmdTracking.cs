using BuisnessLayer.Manager;
using BuisnessLayer.Base;
using BuisnessLayer.Results;
using BuisnessLayer.Results.Events;
using BuisnessLayer.ViewModels.Control;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Valve.VR;

namespace BuisnessLayer.Base
{
	class HmdTracking
	{

        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Declare the delegate (if using non-generic pattern).
        public delegate void TrackingChangedEventHandler(object sender, TrackingChangedEventArgs e);

        private TcpSocketManager tcpSocketManager;
        // Declare the event.
        public event TrackingChangedEventHandler TrackingChangedEvent;

		// Wrap the event in a protected virtual method
		// to enable derived classes to raise the event.
		protected virtual void RaiseSampleEvent(TrackingChangedEventArgs eventArgs)
		{
			// Raise the event by using the () operator.
			TrackingChangedEvent?.Invoke(this, eventArgs);
		}

		private CVRSystem m_pHMD;
		private bool bAcquireTrackingDataByWaitingForVREvents = false;
		private int userId;
		private string sessionId;
        private bool communicationProtocolUdp = false;
       

        public HmdTracking(int pUserId, string pSessionId, TcpSocketManager pTcpSocketManager)
		{
			userId = pUserId;
			sessionId = pSessionId;
            tcpSocketManager = pTcpSocketManager;
            EVRInitError eError = EVRInitError.None;
            m_pHMD = OpenVR.Init(ref eError, EVRApplicationType.VRApplication_Background);
            
        }

		public void StartTracking(CancellationToken token)
		{
			try
			{

                if (m_pHMD != null && !token.IsCancellationRequested) //lighthouseTracking
                {
                    RaiseSampleEvent(new TrackingChangedEventArgs(true));

                    while (this.RunProdcedure(bAcquireTrackingDataByWaitingForVREvents, token) && !token.IsCancellationRequested)
                    {

                    }
                    RaiseSampleEvent(new TrackingChangedEventArgs(false));
                }
                else
                {
                    log.Warn("SteamVr is not Running");
                    RaiseSampleEvent(new TrackingChangedEventArgs(false));
                }
            }
			catch (Exception e)
			{
                log.Warn("StartTracking in HmdTracking wurde beendet!" + e);

                RaiseSampleEvent(new TrackingChangedEventArgs(e));
			}
		}

		public bool RunProdcedure(bool bWaitForEvents, CancellationToken token)
		{
			// Either A) wait for events, such as hand controller button press, before parsing...
			if (bWaitForEvents)
			{
				// Process VREvent
				VREvent_t event_T = new VREvent_t();

				while (m_pHMD.PollNextEvent(ref event_T, event_T.eventType))
				{
					try
					{
						if (!ProcessVREvent(event_T))
						{
							RaiseSampleEvent(new TrackingChangedEventArgs(new Exception()));

						}
						ParseTrackingFrameAsync(token);
					}
					catch (Exception e)
					{
                        log.Warn("RunProcedure in Hmd Tracking wurde beendet!");
                        RaiseSampleEvent(new TrackingChangedEventArgs(new Exception()));
					}

				}
			}
			else
			{
				ParseTrackingFrameAsync(token);
			}

			return true;
		}

		private async Task ParseTrackingFrameAsync(CancellationToken token)
		{
			try
			{
				for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
				{
                    if(!token.IsCancellationRequested)
                    {
                        TrackedDevicePose_t[] trackedDevicePose = new TrackedDevicePose_t[10];

                        if (!m_pHMD.IsTrackedDeviceConnected(i))
                            continue;

                        ETrackedDeviceClass trackedDeviceClass = m_pHMD.GetTrackedDeviceClass(i);

                        switch (trackedDeviceClass)
                        {
                            case ETrackedDeviceClass.HMD:

                                m_pHMD.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, trackedDevicePose);

                                HmdVector3_t position = GetPosition(trackedDevicePose[0].mDeviceToAbsoluteTracking);// devicePose->mDeviceToAbsoluteTracking);
                                HmdVector3_t rotation = GetRotationEuler(trackedDevicePose[0].mDeviceToAbsoluteTracking);

                                int positionX = (int)position.v0;
                                int positionY = (int)position.v1;
                                int positionZ = (int)position.v2 * (-1);

                                int rotationX = (int)rotation.v0 + 360;
                                int rotationY = (int)rotation.v1 + 90;
                                int rotationZ = (int)rotation.v2 * (-1);

                                if (ControlViewModel.teleporOffset[0] != -1)
                                {
                                    lock (ControlViewModel.teleporOffset)
                                    {
                                        ControlViewModel.transOffSetHeadX = ControlViewModel.teleporOffset[0];
                                        ControlViewModel.transOffSetHeadY = ControlViewModel.teleporOffset[1];
                                        ControlViewModel.transOffSetHeadZ = ControlViewModel.teleporOffset[2];
                                        ControlViewModel.rotOffSetHeadZ = ControlViewModel.teleporOffset[3];
                                    }
                                }

                                if (communicationProtocolUdp == false)
                                {
                                    MessageBuffer mb = new MessageBuffer();
                                    int type = 100;
                                    string ipAdress = ServerManager.clientIpAddressForTracking;
                                    mb.add(type);
                                    mb.add(userId);
                                    mb.add(ipAdress);
                                    mb.add(positionX + ControlViewModel.transOffSetHeadX);
                                    mb.add(positionY + ControlViewModel.transOffSetHeadY);
                                    mb.add(positionZ + ControlViewModel.transOffSetHeadZ);
                                    mb.add(rotationX - ControlViewModel.rotOffSetHeadZ);
                                    mb.add(rotationY);
                                    mb.add(rotationZ);
                                    Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                    lock (tcpSocketManager.send_msg(msg))
                                    {
                                        tcpSocketManager.send_msg(msg);
                                    }
                                   
                                    Thread.Sleep(20);
                                    break;
                                }
                                break;
                        }
                    }
					
				}

			}
			catch(Exception e)
			{
                log.Warn("ParseTrackingFrameAsync in Hmd Tracking wurde beendet!");

                RaiseSampleEvent(new TrackingChangedEventArgs(e));
			}
		}

        // Convert an Float-Array into an Byte-Array
        static byte[] ConvertFloatToByteArray(float[] floats)
		{
			byte[] ret = new byte[floats.Length * 4];// a single float is 4 bytes/32 bits

			for (int i = 0; i < floats.Length; i++)
			{
				ret = BitConverter.GetBytes(floats[i]);
			}
			return ret;
		}

		// Calculate the current HMD-Rotation Value
		public HmdQuaternion_t GetRotation(HmdMatrix34_t matrix)
		{
			HmdQuaternion_t q;

			q.w = Math.Sqrt(Math.Max(0, 1 + matrix.m0 + matrix.m5 + matrix.m10)) / 2;
			q.x = Math.Sqrt(Math.Max(0, 1 + matrix.m0 - matrix.m5 - matrix.m10)) / 2;
			q.y = Math.Sqrt(Math.Max(0, 1 - matrix.m0 + matrix.m5 - matrix.m10)) / 2;
			q.z = Math.Sqrt(Math.Max(0, 1 - matrix.m0 - matrix.m5 + matrix.m10)) / 2;

			var tempValueX = matrix.m9 - matrix.m6;
			var tempValueY = matrix.m2 - matrix.m8;
			var tempValueZ = matrix.m4 - matrix.m1;

			if (tempValueX < 0)
				q.x = -q.x;
			if (tempValueY < 0)
				q.y = -q.y;
			if (tempValueZ < 0)
				q.z = -q.z;

			return q;
		}

		// Get the vector representing the position
		public HmdVector3_t GetPosition(HmdMatrix34_t matrix)
		{
			HmdVector3_t vector;

			vector.v0 = matrix.m3 * 1000;
			vector.v1 = matrix.m7 * 1000;
			vector.v2 = matrix.m11 * 1000;

			return vector;
		}

		private bool ProcessVREvent(VREvent_t event_T)
		{
			switch (event_T.eventType)
			{
				case (uint)EVREventType.VREvent_Quit:
					return false;


				case (uint)EVREventType.VREvent_ProcessQuit:
					return false;

				case (uint)EVREventType.VREvent_QuitAborted_UserPrompt:
					return false;


				case (uint)EVREventType.VREvent_QuitAcknowledged:
					return false;

				default:
					break;
			}

			return true;
		}

		// Calculate the current HMD-Translations Value
		public HmdVector3_t GetRotationEuler(HmdMatrix34_t matrix)
		{
			HmdVector3_t v = new HmdVector3_t();

			float r2d = 180 / (float)Math.PI;

			if (matrix.m0 == 1 || matrix.m0 == -1)
			{
				v.v0 = -((-((float)Math.Atan2(matrix.m2, matrix.m11)) * r2d));
				v.v1 = 0;
				v.v2 = 0;
			}
			else
			{
				v.v0 = -(((-(float)Math.Atan2(matrix.m8, matrix.m0)) * r2d));
				v.v1 = -(((float)Math.Atan2(matrix.m6, matrix.m5)) * r2d);
				v.v2 = (((float)Math.Asin(matrix.m4)) * r2d);
			}
			return v;
		}
	}
}
