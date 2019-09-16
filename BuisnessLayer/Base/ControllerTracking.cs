using BuisnessLayer.Manager;
using BuisnessLayer.Results;
using BuisnessLayer.Results.Events;
using BuisnessLayer.ViewModels.Control;
using System;
using System.Threading;
using Valve.VR;

namespace BuisnessLayer.Base
{
    class ControllerTracking
	{
        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Declare the delegate (if using non-generic pattern).
        public delegate void TrackingChangedControllerEventHandler(object sender, TrackingChangedEventArgs e, Tracker tracker);
        
        // Declare the event.
		public event TrackingChangedControllerEventHandler TrackingChangedEventController;

        // Wrap the event in a protected virtual method
        // to enable derived classes to raise the event.
        protected virtual void RaiseControllerEvent(TrackingChangedEventArgs eventArgs, Tracker tracker)
        {
            // Raise the event by using the () operator.
            TrackingChangedEventController?.Invoke(this, eventArgs, tracker);
        }

        CVRSystem vr_pointer;
		public bool bAcquireTrackingDataByWaitingForVREvents = false;
		private int userId;
		private string sessionId;
		private int port;
		private string ipAdress;
        private TcpSocketManager tcpSocketManager;
        private bool someThingTracked = false;
        ETrackedControllerRole lastController;
        private bool communicationProtocolUdp = false;
        private int transOffSetX = 0;
        private int transOffSetY = 0;
        private int transOffSetZ = 0;
        private int rotOffSetZ = 0;

        public ControllerTracking(int pUserId, string pSessionId, TcpSocketManager pTcpSocketManager)
		{
			userId = pUserId;
			sessionId = pSessionId;
            tcpSocketManager = pTcpSocketManager;
			EVRInitError eError = EVRInitError.None;
			vr_pointer = OpenVR.Init(ref eError, EVRApplicationType.VRApplication_Background);
		}

		public BaseResult StartTracking(CancellationToken token)
		{
            try
            {
                if (vr_pointer != null)
                {
                    while (this.RunProdcedure(bAcquireTrackingDataByWaitingForVREvents) && !token.IsCancellationRequested)
                    {

                    }
                    RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.LeftController);
                    RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.RightController);
                    return new BaseResult();
                }
                else
                {
                    log.Warn("SteamVr is not Running");
                    RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.LeftController);
                    RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.RightController);
                    return new BaseResult(new NullReferenceException());
                }
            }
            catch (Exception e)
            {
                log.Warn("SteamVr is not Running" + e);
                return new BaseResult(e);
            }

		}

		void Shutdown()
		{
			if (vr_pointer != null)
			{
				OpenVR.Shutdown();
                log.Info("Controller Tracking wurde beendet!");
                vr_pointer = null;
			}
		}

		public bool RunProdcedure(bool bWaitForEvents)
		{
            try
            {    
                VREvent_t event_T = new VREvent_t();

                if (vr_pointer.PollNextEvent(ref event_T, event_T.eventType))
                {
                    // Process event
                    if (!ProcessVREvent(event_T))
                    {

                    }
                    ParseTrackingFrame();
                }
                else
                {
                    ParseTrackingFrame();
                }
            }          
            catch
            {
                log.Warn("RunProcedure in Controller Tracking wurde beendet!");
                RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.LeftController);
                RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.RightController);
            }
                return true;
		}

		private async void ParseTrackingFrame()
		{
			try
			{
                for (uint id = 0; id < OpenVR.k_unMaxTrackedDeviceCount; id++)
                {

                    if (vr_pointer != null)
                    {
                        TrackedDevicePose_t[] trackedDevicePose = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

                        if (!vr_pointer.IsTrackedDeviceConnected(id))
                            continue;

                        VRControllerState_t controllState = new VRControllerState_t();
                       
                        ETrackedDeviceClass trackedDeviceClass = vr_pointer.GetTrackedDeviceClass(id);

                        switch (trackedDeviceClass)
                        {
                                
                            case ETrackedDeviceClass.Controller:

                                vr_pointer.GetControllerStateWithPose(ETrackingUniverseOrigin.TrackingUniverseStanding, id, ref controllState, OpenVR.k_unMaxTrackedDeviceCount, ref trackedDevicePose[id]);

                                HmdVector3_t position = GetPosition(trackedDevicePose[id].mDeviceToAbsoluteTracking);// devicePose->mDeviceToAbsoluteTracking);
                                HmdVector3_t rotation = GetRotationEuler(trackedDevicePose[id].mDeviceToAbsoluteTracking);

           
                                int positionControllerX = (int)position.v0;
                                int positionControllerY = (int)position.v1;
                                int positionControllerZ = (int)position.v2 * (-1);

                                int rotationControllerX = (int)rotation.v0 + 180;
                                int rotationControllerY = (int)rotation.v1;
                                int rotationControllerZ = (int)rotation.v2;

                                if(ControlViewModel.teleporOffset[0] != -1)
                                {
                                    ControlViewModel.transOffSetControllerX = ControlViewModel.teleporOffset[0];
                                    ControlViewModel.transOffSetControllerY = ControlViewModel.teleporOffset[1]; 
                                    ControlViewModel.transOffSetControllerZ = ControlViewModel.teleporOffset[2];
                                    ControlViewModel.rotOffSetControllerZ = ControlViewModel.teleporOffset[3];
                                }

                                ETrackedControllerRole result = vr_pointer.GetControllerRoleForTrackedDeviceIndex(id);

                                if (result != ETrackedControllerRole.LeftHand && lastController != ETrackedControllerRole.LeftHand)
                                {
                                    RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.LeftController);
                                }

                                if (result != ETrackedControllerRole.RightHand && lastController != ETrackedControllerRole.RightHand)
                                {
                                    RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.RightController);
                                }

                                switch (result)
                                {                                  
                                    case ETrackedControllerRole.Invalid:
                                    break;
                                    case ETrackedControllerRole.LeftHand:

                                        if(communicationProtocolUdp == false)
                                        {
                                            MessageBuffer mb2 = new MessageBuffer();
                                            someThingTracked = true;
                                            int type = 101;
                                            mb2.add(type);
                                            mb2.add(userId);
                                            int handid = 1;
                                            mb2.add(handid);
                                            mb2.add(positionControllerX + ControlViewModel.transOffSetControllerX);
                                            mb2.add(positionControllerY + ControlViewModel.transOffSetControllerY);
                                            mb2.add(positionControllerZ + ControlViewModel.transOffSetControllerZ);
                                            mb2.add(rotationControllerX);
                                            mb2.add(rotationControllerY);
                                            mb2.add(rotationControllerZ);
                                            mb2.add(ControlViewModel.rotOffSetControllerZ);
                                            RaiseControllerEvent(new TrackingChangedEventArgs(true), Tracker.LeftController);
                                            Message msg = new Message(mb2, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                            lock (tcpSocketManager.send_msg(msg))
                                            {
                                                tcpSocketManager.send_msg(msg);
                                            }
                                            Thread.Sleep(20);
                                            break;
                                        }
                                        break;

                                    case ETrackedControllerRole.RightHand:

                                        if (communicationProtocolUdp == false)
                                        {
                                            MessageBuffer mb2 = new MessageBuffer();
                                            someThingTracked = true;
                                            int type = 101;
                                            mb2.add(type);
                                            mb2.add(userId);
                                            int handid = 2;
                                            mb2.add(handid);
                                            mb2.add(positionControllerX + ControlViewModel.transOffSetControllerX);
                                            mb2.add(positionControllerY + ControlViewModel.transOffSetControllerY);
                                            mb2.add(positionControllerZ + ControlViewModel.transOffSetControllerZ);
                                            mb2.add(rotationControllerX);
                                            mb2.add(rotationControllerY);
                                            mb2.add(rotationControllerZ);
                                            mb2.add(ControlViewModel.rotOffSetControllerZ);
                                            Message msg = new Message(mb2, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                            RaiseControllerEvent(new TrackingChangedEventArgs(true), Tracker.RightController);
                                            lock (tcpSocketManager.send_msg(msg))
                                            {
                                                tcpSocketManager.send_msg(msg);
                                            }
                                            Thread.Sleep(20);
                                            break;
                                        }
                                        break;
                                }       
                                break;
                                
                        }                   
                }            
            }

            }
			catch (Exception e)
			{
                log.Warn("ParseTrackingFrame in Controller Tracking wurde beendet!" +e);
                RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.LeftController);
                RaiseControllerEvent(new TrackingChangedEventArgs(false), Tracker.RightController);
            }
		}


        private bool ProcessVREvent(VREvent_t event_T)
		{
			switch (event_T.eventType)
			{
				case (uint)EVREventType.VREvent_Quit:
                    Shutdown();
                    return false;
				case (uint)EVREventType.VREvent_QuitAborted_UserPrompt:
					return false;
				case (uint)EVREventType.VREvent_QuitAcknowledged:
					return false;

                case (uint)EVREventType.VREvent_ButtonPress:

                    int msgIdentifyer = 102;

                    switch(event_T.data.controller.button)
                    {
                        case (uint)EVRButtonId.k_EButton_SteamVR_Touchpad:
                            Console.WriteLine("Touchpad");

                            ETrackedControllerRole result = vr_pointer.GetControllerRoleForTrackedDeviceIndex(event_T.trackedDeviceIndex);
                            if(result == ETrackedControllerRole.LeftHand)
                            {
                                int handid = 1;
                                MessageBuffer mb = new MessageBuffer();
                                mb.add(msgIdentifyer);
                                mb.add((int)EVRButtonId.k_EButton_SteamVR_Touchpad);
                                mb.add(userId);
                                mb.add(handid);
                                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                lock (tcpSocketManager.send_msg(msg))
                                {
                                    tcpSocketManager.send_msg(msg);
                                }
                            }
                            else
                            {
                                int handid = 2;
                                MessageBuffer mb = new MessageBuffer();
                                mb.add(msgIdentifyer);
                                mb.add((int)EVRButtonId.k_EButton_SteamVR_Touchpad);
                                mb.add(userId);
                                mb.add(handid);
                                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                lock (tcpSocketManager.send_msg(msg))
                                {
                                    tcpSocketManager.send_msg(msg);
                                }
                            }
                            return false;
                        case (uint)EVRButtonId.k_EButton_ApplicationMenu:
                            Console.WriteLine("ButttonAplication");

                            ETrackedControllerRole result2 = vr_pointer.GetControllerRoleForTrackedDeviceIndex(event_T.trackedDeviceIndex);
                            if (result2 == ETrackedControllerRole.LeftHand)
                            {
                                int handid = 1;
                                MessageBuffer mb = new MessageBuffer();
                                mb.add(msgIdentifyer);
                                mb.add((int)EVRButtonId.k_EButton_ApplicationMenu);
                                mb.add(userId);
                                mb.add(handid);
                                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                lock (tcpSocketManager.send_msg(msg))
                                {
                                    tcpSocketManager.send_msg(msg);
                                }
                            }
                            else
                            {
                                int handid = 2;
                                MessageBuffer mb = new MessageBuffer();
                                mb.add(msgIdentifyer);
                                mb.add((int)EVRButtonId.k_EButton_ApplicationMenu);
                                mb.add(userId);
                                mb.add(handid);
                                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                lock (tcpSocketManager.send_msg(msg))
                                {
                                    tcpSocketManager.send_msg(msg);
                                }
                            }
                            return false;
                        case (uint)EVRButtonId.k_EButton_System:
                            Console.WriteLine("ButtonSystem");

                            ETrackedControllerRole result3 = vr_pointer.GetControllerRoleForTrackedDeviceIndex(event_T.trackedDeviceIndex);
                            if (result3 == ETrackedControllerRole.LeftHand)
                            {
                                int handid = 1;
                                MessageBuffer mb = new MessageBuffer();
                                mb.add(msgIdentifyer);
                                mb.add((int)EVRButtonId.k_EButton_System);
                                mb.add(userId);
                                mb.add(handid);
                                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                lock (tcpSocketManager.send_msg(msg))
                                {
                                    tcpSocketManager.send_msg(msg);
                                }
                            }
                            else
                            {
                                int handid = 2;
                                MessageBuffer mb = new MessageBuffer();
                                mb.add(msgIdentifyer);
                                mb.add((int)EVRButtonId.k_EButton_System);
                                mb.add(userId);
                                mb.add(handid);
                                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                lock (tcpSocketManager.send_msg(msg))
                                {
                                    tcpSocketManager.send_msg(msg);
                                }
                            }
                            return false;
                        case (uint)EVRButtonId.k_EButton_SteamVR_Trigger:
                            Console.WriteLine("Trigger");

                            ETrackedControllerRole result4 = vr_pointer.GetControllerRoleForTrackedDeviceIndex(event_T.trackedDeviceIndex);
                            if (result4 == ETrackedControllerRole.LeftHand)
                            {
                                int handid = 1;
                                MessageBuffer mb = new MessageBuffer();
                                mb.add(msgIdentifyer);
                                mb.add((int)EVRButtonId.k_EButton_SteamVR_Trigger);
                                mb.add(userId);
                                mb.add(handid);
                                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                lock (tcpSocketManager.send_msg(msg))
                                {
                                    tcpSocketManager.send_msg(msg);
                                }

                            }
                            else
                            {
                                int handid = 2;
                                MessageBuffer mb = new MessageBuffer();
                                mb.add(msgIdentifyer);
                                mb.add((int)EVRButtonId.k_EButton_SteamVR_Trigger);
                                mb.add(userId);
                                mb.add(handid);
                                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                lock (tcpSocketManager.send_msg(msg))
                                {
                                    tcpSocketManager.send_msg(msg);
                                }
                            }
                            return false;
                        case (uint)EVRButtonId.k_EButton_Grip:
                            Console.WriteLine("Grip");
                            ETrackedControllerRole result5 = vr_pointer.GetControllerRoleForTrackedDeviceIndex(event_T.trackedDeviceIndex);
                            if (result5 == ETrackedControllerRole.LeftHand)
                            {
                                int handid = 1;
                                MessageBuffer mb = new MessageBuffer();
                                mb.add(msgIdentifyer);
                                mb.add((int)EVRButtonId.k_EButton_Grip);
                                mb.add(userId);
                                mb.add(handid);
                                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                lock (tcpSocketManager.send_msg(msg))
                                {
                                    tcpSocketManager.send_msg(msg);
                                }
                            }
                            else
                            {
                                int handid = 2;
                                MessageBuffer mb = new MessageBuffer();
                                mb.add(msgIdentifyer);
                                mb.add((int)EVRButtonId.k_EButton_Grip);
                                mb.add(userId);
                                mb.add(handid);
                                Message msg = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_MESSAGE);
                                lock (tcpSocketManager.send_msg(msg))
                                {
                                    tcpSocketManager.send_msg(msg);
                                }                               
                            }
                            return false;
                    }
                    return false;
                default:
                    return false;
            }
		}

		public HmdVector3_t GetRotationEuler(HmdMatrix34_t matrix)
		{
			try
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
			catch (Exception ex)
			{
				HmdVector3_t vt = new HmdVector3_t();
				vt.v0 = 0.0f;
				vt.v1 = 0.0f;
				vt.v2 = 0.0f;
				return vt;
			}
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
	}
}
