using BuisnessLayer.Manager;
using BuisnessLayer.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace BuisnessLayer.Base
{
    public abstract class SharedStateBase
    {
        public delegate void callBack();
        private string variableName = "";
        private string m_className = "SharedState";
        private callBack cb = null;
        protected bool valueChanged = false;

        private SessionID sessionID = new SessionID(); ///the session to send updates to 
        private bool muted = false;
        private float syncInterval = 0.1f; ///how often messages get sent. if >= 0 messages will be sent immediately
        private double lastUpdateTime = 0.0;
        private MessageBuffer m_valueData;
        public SharedStateBase(string name, SharedStateType mode, string className = "SharedState")
        {
            m_className = className;
            variableName = name;
            var news = SharedStateManager.Instance.add(this, mode);
            sessionID = news.Item1;
            muted = news.Item2;
        }
        ~SharedStateBase()
        {
            SharedStateManager.Instance.remove(this);
            SharedStateManager.Instance.unsubscribeVar(m_className, variableName);
        }

        //! let the SharedState call the given function when the registry entry got changed from the server
        public void setUpdateFunction(callBack function)
        {
            cb = function;
        }

        //! returns true if the last value change was made by an other client
        bool valueChangedByOther() { return valueChanged; }
        public string getName() { return variableName; }

        //! is called from the SahredStateManager when the entry got changed from the server
        public void update(MessageBuffer mb)
        {
            deserializeValue(mb);
            if (cb != null)
            {
                cb.Invoke();
            }
        }

        public abstract void deserializeValue(MessageBuffer mb);


        public void setID(SessionID id)
        {
            sessionID = id;
        }
    public void setMute(bool m)
        {
            muted = m;
        }
        public bool getMute() { return muted; }
        ///resubscribe to the local registry and the vrb after sessionID has changed
        public void resubscribe(SessionID id)
        {
            SharedStateManager.Instance.unsubscribeVar(m_className, variableName);
            SharedStateManager.Instance.subscribeVar(id, m_className, variableName, m_valueData);
        }
        //unmute and send value to vrb
        public void becomeMaster()
        {
            muted = false;
            if (m_valueData != null )
            {
                SharedStateManager.Instance.setVar(sessionID, m_className, variableName, m_valueData, muted);
            }
        }
        protected void subscribe(MessageBuffer val)
        {
            SharedStateManager.Instance.subscribeVar(sessionID, m_className, variableName, val);
        }
    protected void setVar(MessageBuffer val)
        {
            m_valueData = val;
            SharedStateManager.Instance.setVar(sessionID, m_className, variableName, val, muted);
        }

    }
    public class SharedState<T> : SharedStateBase where T : class
    {
        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const string className = "SharedState";
        private T m_value;
        public delegate void OnValueChange(T newValue);

        public SharedState(string pName,  T pValue = default(T), SharedStateType pMode = SharedStateType.USE_COUPLING_MODE) : base(pName, pMode)
        {
            m_value = pValue;
            MessageBuffer mb = new MessageBuffer();
            SharedStateSerializer.serializeWithType(ref mb, m_value);
            subscribe(mb);
        }
        public void val(T value)
        {
            if (value != m_value)
            {
                m_value = value;
                push();
            }
        }
        public T val()
        {
            return m_value;
        }
        public override void deserializeValue(MessageBuffer mb)
        {
            SharedStateSerializer.deserializeWithType<T>(ref mb, ref m_value);
        }
        void push()
        {
            valueChanged = false;
            MessageBuffer data = new MessageBuffer();
            SharedStateSerializer.serializeWithType(ref data, m_value);
            setVar(data);
        }
    }


    public enum SharedStateType
    {
        USE_COUPLING_MODE, //0
        NEVER_SHARE, //1
        ALWAYS_SHARE,//2
        SHARE_WITH_ALL//3
    }


}