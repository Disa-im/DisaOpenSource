using System;
using System.Net;

namespace Disa.Framework
{
	public class WebClient : System.Net.WebClient
    {
        private int _timeout;
        private bool _timeoutSet;
        private int _readWriteTimeout;
        private bool _readWriteTimeoutSet;

		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest request = base.GetWebRequest(address);
			HttpWebRequest webRequest = request as HttpWebRequest;
			if (webRequest != null)
			{
                webRequest.AutomaticDecompression = DecompressionMethods.GZip;
                if (_timeoutSet)
                    webRequest.Timeout = _timeout;
                if (_readWriteTimeoutSet)
                    webRequest.ReadWriteTimeout = _readWriteTimeout;
			}
			return request;
		}

        public void SetUserAgent(string userAgent)
        {
            Headers.Remove("User-Agent");
            Headers.Add("User-Agent", userAgent);
        }

        public void AddHeader(string name, string value)
        {
            Headers.Remove(name);
            Headers.Add(name, value);
        }

        public int Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                _timeout = value;
                _timeoutSet = true;
            }
        }

        public int ReadWriteTimeout
        {
            get
            {
                return _readWriteTimeout;
            }
            set
            {
                _readWriteTimeout = value;
                _readWriteTimeoutSet = true;
            }
        }

        public WebClient()
		{
            Headers.Remove("Accept-Encoding");
            Headers.Add("Accept-Encoding", "gzip");
		}
    }
}

