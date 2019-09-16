using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuisnessLayer.Base
{
    public class SessionID
    {
        public int m_owner = 0;
        public string m_name = "";
        public bool m_isPrivate = true;
        public SessionID() { }
        public SessionID(int id, bool isPrivate = true)
        {
            m_owner = id;
            m_isPrivate = isPrivate;
        }
        public SessionID(int id, string name, bool isPrivate)
        {
            m_owner = id;
            m_name = name;
            m_isPrivate = isPrivate;
        }


        string toText()
        {
            string state = "private";
            if (!m_isPrivate)
            {
                state = "public";
            }
            return m_name + "   owner: " + m_owner.ToString() + "  " + state;
        }
    }
}
