#region

using System;
using System.Net.Sockets;
using Game.Data;

#endregion

namespace Game.Comm
{
    public class SocketSession : Session
    {
        public SocketSession(string name, Socket socket, Processor processor) : base(name, processor)
        {
            Socket = socket;
        }

        public Socket Socket { get; private set; }

        protected override void Close()
        {
            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
            }
            catch(Exception e)
            {
                Global.Logger.Warn(e);
            }
        }

        public override bool Write(Packet packet)
        {
#if DEBUG || CHECK_LOCKS
            Global.Logger.Info("Sending: " + packet.ToString(32));
#endif

            byte[] packetBytes = packet.GetBytes();
            int ret;
            if (Socket == null)
                return false;

            try
            {
                ret = Socket.Send(packetBytes, packetBytes.Length, SocketFlags.None);
            }
            catch(Exception)
            {
                return false;
            }
            return ret > 0;
        }
    }
}