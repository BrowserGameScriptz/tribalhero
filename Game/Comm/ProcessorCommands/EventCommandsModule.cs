#region

using Game.Data;
using Game.Util;
using Game.Util.Locking;
using Persistance;

#endregion

namespace Game.Comm.ProcessorCommands
{
    class EventCommandsModule : CommandModule
    {
        private readonly IDbManager dbManager;

        public EventCommandsModule(IDbManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public override void RegisterCommands(Processor processor)
        {
            processor.RegisterEvent(Command.OnDisconnect, EventOnDisconnect);
            processor.RegisterEvent(Command.OnConnect, EventOnConnect);
        }

        private void EventOnConnect(Session session, Packet packet)
        {
        }

        private void EventOnDisconnect(Session session, Packet packet)
        {
            if (session == null || session.Player == null)
                return;

            using (Concurrency.Current.Lock(session.Player))
            {
                Global.Channel.Unsubscribe(session);

                // If player is logged in under new session already, then don't bother changing their session info
                if (session.Player.Session != session)
                    return;

                session.Player.Session = null;
                session.Player.SessionId = string.Empty;
                session.Player.LastLogin = SystemClock.Now;
                dbManager.Save(session.Player);
            }
        }
    }
}