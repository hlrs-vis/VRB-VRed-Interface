using BuisnessLayer.Manager;
using System;
using System.Collections.Generic;

namespace BuisnessLayer.Base
{
    public static class MessageHandler
	{
        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static List<SessionID> RoomList = new List<SessionID>();
		public static SessionID Session = new SessionID();

		public static void addCurrentRooms(List<SessionID> ListOfAllCurrentRooms)
		{
            try
            {
                RoomList = new List<SessionID>(ListOfAllCurrentRooms);
            }
            catch (Exception e)
            {
                log.Error("addCurrentRooms Method is crashed!" + e);
            }
		}

		public static void addCurrentSession(SessionID pSession)
		{
			Session = pSession;
		}

		public static List<SessionID> getAllRoom()
		{
			return RoomList;
		}

		public static SessionID getSession()
		{
			return Session;
		}
	}
}
