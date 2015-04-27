using System.Net.Sockets;
#if __ANDROID__
using Java.Net;
using Socket = Java.Net.Socket;
#endif 

namespace Disa.Framework
{
    #if __ANDROID__
    public class Socket
    {
        private readonly Java.Net.Socket _socket;

        public Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            _socket = new Java.Net.Socket();
        }

        public int ReceiveTimeout
        {
            get { return _socket.SoTimeout; }
            set
            {
                _socket.SoTimeout = value;
            }
        }

        public int SendTimeout
        {
            get { return 0; }
            set{}
        }

        public void Connect(string host, int port)
        {
            _socket.Connect(new InetSocketAddress(host, port));
        }

        public bool Connected
        {
            get { return _socket.IsConnected; }
        }

        public void Close()
        {
            _socket.Close();
        }

        public void Send(byte[] bytes)
        {
            _socket.OutputStream.Write(bytes, 0, bytes.Length);
            _socket.OutputStream.Flush();
        }

        public int Receive(byte[] buffer, int offset, int size, SocketFlags flags)
        {
            return _socket.InputStream.Read(buffer, offset, size);
        }
    }
    #else
    public class Socket
    {
        private readonly Socket _socket;

        public Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            _socket = new Socket(addressFamily, socketType, protocolType);
        }

        public int ReceiveTimeout
        {
            get { return _socket.ReceiveTimeout; }
            set
            {
                _socket.ReceiveTimeout = value;
            }
        }

        public int SendTimeout
        {
            get { return _socket.SendTimeout; }
            set
            {
                _socket.SendTimeout = value;
            }
        }

        public void Connect(string host, int port)
        {
            _socket.Connect(host, port);
        }

        public bool Connected
        {
            get { return _socket.Connected; }
        }

        public void Close()
        {
            _socket.Close();
        }

        public void Send(byte[] bytes)
        {
            _socket.Send(bytes);
        }

        public int Receive(byte[] buffer, int offset, int size, SocketFlags flags)
        {
            return _socket.Receive(buffer, offset, size, flags);
        }
    }
    #endif
}