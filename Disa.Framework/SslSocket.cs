using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
#if __ANDROID__
using Javax.Net.Ssl;
using Java.Net;
#endif
using System.Net;

namespace Disa.Framework
{
    #if __ANDROID__
    public class SslSocket 
	{
		private readonly string _remoteHostName;
		private readonly int _remotePort;

        private Java.Net.Socket _socket;

		public SslSocket(string remoteHostName, int remotePort, bool secure)
		{
			_remoteHostName = remoteHostName;
			_remotePort = remotePort;
		}

        public SslSocket(string remoteHostName, int remotePort)
        {
            _remoteHostName = remoteHostName;
            _remotePort = remotePort;
        }

		public int Receive(byte[] buffer)
		{
			return _socket.InputStream.Read(buffer, 0, buffer.Length);
		}

		public int Send(byte[] buffer)
		{
			_socket.OutputStream.Write(buffer, 0, buffer.Length);
			_socket.OutputStream.Flush();
			return buffer.Length;
		}

        public void SetTimeout(int timeout)
        {
            _socket.SoTimeout = timeout == -1 ? 0 : timeout;
        }

		public void Close()
		{
			_socket.Close();
		}

		public void Connect(int sendTimeout = -1, int receiveTimeout = -1)
		{
			SSLSocket sslSocket = null;
            var f = SSLSocketFactory.Default as SSLSocketFactory;
            sslSocket = f.CreateSocket(_remoteHostName, _remotePort) as SSLSocket;
            _socket = sslSocket;

            _socket.SoTimeout = receiveTimeout == -1 ? 0 : receiveTimeout;

            sslSocket.StartHandshake();

			_socket.SoTimeout = 0;
		}
	}
    #else
    public class SslSocket 
    {
        private readonly string _remoteHostName;
        private readonly int _remotePort;

        private System.Net.Sockets.Socket _socket;
        private SslStream _sslStream;
        private NetworkStream _netStream;

        public SslSocket(string remoteHostName, int remotePort, bool secure)
        {
            _remoteHostName = remoteHostName;
            _remotePort = remotePort;
        }

        public SslSocket(string remoteHostName, int remotePort)
        {
            _remoteHostName = remoteHostName;
            _remotePort = remotePort;
        }

        public int Receive(byte[] buffer)
        {
            return _sslStream.Read(buffer, 0, buffer.Length);
        }

        public int Send(byte[] buffer)
        {
            _sslStream.Write(buffer);
            _sslStream.Flush();
            return buffer.Length;
        }

        public void SetTimeout(int timeout)
        {
            _socket.SendTimeout = timeout;
            _socket.ReceiveTimeout = timeout;
        }

        public void Close()
        {
            _netStream.Close();
            _sslStream.Close();
            _socket.Close();
        }

        public void Connect(int sendTimeout = -1, int receiveTimeout = -1)
        {
            _socket = new System.Net.Sockets.Socket(GetAddressFamily(IPAddress.Parse(_remoteHostName)), SocketType.Stream, ProtocolType.Tcp);
            _socket.SendTimeout = sendTimeout;
            _socket.ReceiveTimeout = receiveTimeout;
            _socket.Connect(_remoteHostName, _remotePort);
            _netStream = new NetworkStream(_socket);
            _sslStream = new SslStream(_netStream);
            _sslStream.AuthenticateAsClient(_remoteHostName);
            _socket.SendTimeout = -1;
            _socket.ReceiveTimeout = -1;
        }

        private static AddressFamily GetAddressFamily(IPAddress ipAddress)
        {
            return ipAddress.AddressFamily;
        }
    }
    #endif
}