#region

using System;
using System.Net.Sockets;

#endregion

namespace Game.Comm
{
    public class SocketSession : Session
    {
        private readonly object writeLock = new object();

        public SocketSession(string name, Socket socket, Processor processor)
                : base(name, processor)
        {
            Socket = socket;
        }

        public Socket Socket { get; private set; }

        public override void Write(Packet packet)
        {
            lock (writeLock)
            {
                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug("Sending IP[{0}] {1}", Name, packet.ToString());
                }

                byte[] packetBytes = packet.GetBytes();
                int totalBytesSent = 0;

                while (totalBytesSent < packetBytes.Length)
                {
                    try
                    {
                        int bytesSent = Socket.Send(packetBytes, totalBytesSent, packetBytes.Length - totalBytesSent, SocketFlags.None);

                        // bytesSent 0 means there was an error sending the packet
                        if (bytesSent == 0)
                        {
                            break;
                        }

                        totalBytesSent += bytesSent;
                    }
                    catch(SocketException e)
                    {
                        if (e.SocketErrorCode == SocketError.WouldBlock)
                        {
                            try
                            {
                                if (Logger.IsDebugEnabled)
                                {
                                    Logger.Debug(e, "Socket would block. Setting it to blocking and trying again {0}", Name);
                                }

                                Socket.Blocking = true;
                            }
                            catch(SocketException e2)
                            {
                                Logger.Warn(e, "Socket unhandled exception when trying to block {0} {1}", e2.SocketErrorCode, e2.Message);
                                return;
                            }
                            catch(Exception e2)
                            {
                                Logger.Warn(e, "General unhandled exception when trying to block socket {0}", e2.Message);
                            }

                            continue;
                        }

                        if (e.SocketErrorCode == SocketError.ConnectionReset || e.SocketErrorCode == SocketError.TimedOut)
                        {
                            if (Logger.IsDebugEnabled)
                            {
                                Logger.Debug(e, "Socket error handled packetLength[{0}] socketErrorCode[{1}]", packetBytes.Length, e.SocketErrorCode);
                            }
                        }
                        else
                        {
                            Logger.Warn(e, "Socket exception with unhandled error {0} {1}", e.SocketErrorCode, e.Message);
                        }

                        return;
                    }
                    catch(ObjectDisposedException)
                    {
                        // This exception happens if the client disconnects and we shut down the socket but then still try to send data
                        return;
                    }
                    catch(Exception e)
                    {
                        Logger.Warn(e, "Unhandled exception when trying to send data to socket {0}", e.Message);
                        return;
                    }
                }

                try
                {
                    if (Socket.Blocking)
                    {
                        Socket.Blocking = false;
                    }
                }
                catch(Exception e)
                {
                    Logger.Warn(e, "Failed to reset socket blocking status");
                }
            }
        }
    }
}