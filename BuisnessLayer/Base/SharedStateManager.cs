using BuisnessLayer.Manager;
using BuisnessLayer.Base;
using DataModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuisnessLayer.Base
{
    public sealed class SharedStateManager
    {
        private static SharedStateManager instance = null;
        private static readonly object padlock = new object();
        private List<SharedStateBase> useCouplingMode = new List<SharedStateBase>(), alwaysShare = new List<SharedStateBase>(), neverShare = new List<SharedStateBase>(), shareWithAll = new List<SharedStateBase>();
        private SessionID m_privateSessionID = new SessionID();
        private SessionID m_publicSessionID= new SessionID();
        private int clientId = -1;
        private bool m_muted = false;
        private TcpSocketManager tcpSocket = null;

        SharedStateManager()
        {
        }
        public Tuple<SessionID, bool> add(SharedStateBase sharedState, SharedStateType mode)
        {
            switch (mode)
            {
                case SharedStateType.USE_COUPLING_MODE:
                    useCouplingMode.Add(sharedState);
                    return new Tuple<SessionID, bool>(m_publicSessionID, m_muted);
                case SharedStateType.NEVER_SHARE:
                    neverShare.Add(sharedState);
                    return new Tuple<SessionID, bool>(m_privateSessionID, false);
                case SharedStateType.ALWAYS_SHARE:
                    alwaysShare.Add(sharedState);
                    return new Tuple<SessionID, bool>(m_publicSessionID, false);
                case SharedStateType.SHARE_WITH_ALL:
                    shareWithAll.Add(sharedState);
                    string name = "all";
                    SessionID sid = new SessionID(0, name, false);
                    return new Tuple<SessionID, bool>(sid, false);
            }

            return new Tuple<SessionID, bool>(m_privateSessionID, true);
        }

        public void remove(SharedStateBase sharedState)
        {
            useCouplingMode.Remove(sharedState);
            alwaysShare.Remove(sharedState);
            neverShare.Remove(sharedState);
            shareWithAll.Remove(sharedState);
        }
        public void update(SessionID privateSessionID, SessionID publicSessionID, bool muted, bool force)
        {

            if (m_privateSessionID != privateSessionID || force)
            {
                foreach (var sharedState in neverShare)
                {
                    sharedState.resubscribe(privateSessionID);
                    sharedState.setID(privateSessionID);
                }
            }

            if (m_publicSessionID != publicSessionID || force)
            {
                foreach (var sharedState in alwaysShare)
                {
                    sharedState.resubscribe(publicSessionID);
                    sharedState.setID(publicSessionID);
                }
                foreach (var sharedState in useCouplingMode)
                {
                    sharedState.resubscribe(publicSessionID);
                    sharedState.setID(publicSessionID);
                }
            }

            if (m_muted != muted || force)
            {
                foreach (var sharedState in useCouplingMode)
                {
                    sharedState.setMute(muted);
                }
            }

            m_privateSessionID = privateSessionID;
            m_publicSessionID = publicSessionID;
            m_muted = muted;
        }

        public void becomeMaster()
        {
            List<SharedStateBase>[] sharedStates = new List<SharedStateBase>[4]{ useCouplingMode, alwaysShare, neverShare, shareWithAll };
            for (int i = 0; i < 4; i++)
            {
                foreach (var sharedState in sharedStates[i])
                {
                    sharedState.becomeMaster();
                }
            }
        }
        public static SharedStateManager Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new SharedStateManager();
                    }
                    return instance;
                }
            }
        }
        public void initializeSender(TcpSocketManager sender)
        {
            tcpSocket = sender;
        }
        private void sendMessage(Message msg)
        {
            if (tcpSocket == null)
            {
                return;
            }
            tcpSocket.send_msg(msg);
        }
        public void unsubscribeVar(string className, string varName)
        {
            MessageBuffer mb = new MessageBuffer();
            mb.add(clientId);
            mb.add(className);
            mb.add(varName);
            Message m = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_REGISTRY_UNSUBSCRIBE_VARIABLE);
            sendMessage(m);
        }
        public void subscribeVar(SessionID sid, string className, string varName, MessageBuffer mb)
        {
            MessageBuffer mb2 = new MessageBuffer();
            SharedStateSerializer.serialize(ref mb2, m_publicSessionID);
            mb2.add(tcpSocket.clientID);
            mb2.add(className);
            mb2.add(varName);
            mb2.add(mb);
            Message m = new Message(mb2, Message.MessagesType.COVISE_MESSAGE_VRB_REGISTRY_SUBSCRIBE_VARIABLE);
            sendMessage(m);
        }
        private void serialize(ref MessageBuffer mb, SessionID sid)
        {
            mb.add(sid.m_owner);
            mb.add(sid.m_name);
            mb.add(sid.m_isPrivate);
        }
        public void setVar(SessionID sid, string className, string varName, MessageBuffer mb, bool muted = false)
        {
            MessageBuffer mb2 = new MessageBuffer();
            SharedStateSerializer.serialize(ref mb2, m_publicSessionID);
            mb2.add(clientId);
            mb2.add(className);
            mb2.add(varName);
            mb2.add(mb);
            Message m = new Message(mb, Message.MessagesType.COVISE_MESSAGE_VRB_REGISTRY_SET_VALUE);
            sendMessage(m);
        }
        public void updateSharedState(string className, string variable, MessageBuffer val)
        {
            if (className != "SharedState")
            {
                return;
            }
            List<SharedStateBase>[] sharedStates = new List<SharedStateBase>[4] { useCouplingMode, alwaysShare, neverShare, shareWithAll };
            for (int i = 0; i < 4; i++)
            {
                var st = sharedStates[i].Find(s => s.getName() == variable);
                if (st != null)
                {
                    st.update(val);
                    return;
                }
            }
        }
    }
}
