using System;
using System.Threading.Tasks;
using System.Linq;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Net;
#if __ANDROID__
using Com.Squareup.Okhttp;
using Com.Disa.Okhttpcustom;
#endif

namespace Disa.Framework
{
    public class OkHttpClient : IDisposable
    {
        #if __ANDROID__
        private readonly Com.Squareup.Okhttp.OkHttpClient _client;
        private string _userAgent = "Disa";
        public static readonly MediaType MediaTypePlain = 
            MediaType.Parse("text/plain; charset=utf-8");
        #else
        private readonly WebClient _client;
        #endif

        public int Timeout 
        {
            #if __ANDROID__
            get
            {
                return _client.ConnectTimeout;
            }
            set
            {
                _client.SetConnectTimeout(value, Java.Util.Concurrent.TimeUnit.Milliseconds);
            }
            #else
            get
            {
                return _client.Timeout;
            }
            set
            {
                _client.Timeout = value;
            }
            #endif
        }

        public int ReadWriteTimeout 
        {
            #if __ANDROID__
            get
            {
                return _client.ReadTimeout;
            }
            set
            {
                _client.SetReadTimeout(value, Java.Util.Concurrent.TimeUnit.Milliseconds);
                _client.SetWriteTimeout(value, Java.Util.Concurrent.TimeUnit.Milliseconds);
            }
            #else
            get
            {
                return _client.ReadWriteTimeout;
            }
            set
            {
                _client.ReadWriteTimeout = value;
            }
            #endif
        }

        public string UserAgent
        {
            #if __ANDROID__
            get
            {
                return _userAgent;
            }
            set
            {
                _userAgent = value;
            }
            #else
            get
            {
                for (int i = 0; i < _client.Headers.AllKeys.Length; i++)
                {
                    if (_client.Headers.GetKey(i) != "User-Agent")
                        continue;
                    var values = _client.Headers.GetValues(i);
                    if (values != null && values.Any())
                    {
                        return values.First();
                    }
                }
                return null;
            }
            set
            {
                _client.Headers.Remove("User-Agent");
                _client.Headers.Add("User-Agent", value);
            }
            #endif
        }

        public OkHttpClient()
        {
            #if __ANDROID__
            _client = new Com.Squareup.Okhttp.OkHttpClient();
            #else
            _client = new WebClient();
            #endif
        }

        public byte[] DownloadData(string url)
        {
            #if __ANDROID__
            var request = new Request.Builder()
                .Url(Utils.ConvertToUrlEscapingIllegalCharacters(url))
                .Get()
                .Header("User-Agent", _userAgent)
                .Build();
            var response = _client.NewCall(request).Execute();
            return response.Body().Bytes();
            #else
            return _client.DownloadData(url);
            #endif
        }

        public Task<byte[]> DownloadDataAsync(string url)
        {
            return Task<byte[]>.Factory.StartNew(() =>
            {
                return DownloadData(url);
            });
        }

        public string DownloadString(string url)
        {
            #if __ANDROID__
            var request = new Request.Builder()
                .Url(Utils.ConvertToUrlEscapingIllegalCharacters(url))
                .Get()
                .Header("User-Agent", _userAgent)
                .Build();
            var response = _client.NewCall(request).Execute();
            return response.Body().String();
            #else
            return _client.DownloadString(url);
            #endif
        }

        public Task<string> DownloadStringAsync(string url)
        {
            return Task<string>.Factory.StartNew(() =>
            {
                return DownloadString(url);
            });
        }

        public string UploadString(string url, string data)
        {
            #if __ANDROID__
            var request = new Request.Builder()
                .Url(Utils.ConvertToUrlEscapingIllegalCharacters(url))
                .Header("User-Agent", _userAgent)
                .Post(RequestBody.Create(MediaTypePlain, data))
                .Build();
            var response = _client.NewCall(request).Execute();
            return response.Body().String();
            #else
            return _client.UploadString(url, data);
            #endif
        }

        public Task<string> UploadStringAsync(string url, string data)
        {
            return Task<string>.Factory.StartNew(() =>
            {
                return UploadString(url, data);
            });
        }

        public Stream DownloadDataStream(string url)
        {
            #if __ANDROID__
            var request = new Request.Builder()
                .Url(Utils.ConvertToUrlEscapingIllegalCharacters(url))
                .Get()
                .Header("User-Agent", _userAgent)
                .Build();
            var response = _client.NewCall(request).Execute();
            return response.Body().ByteStream();
            #else
            throw new NotImplementedException();
            #endif
        }

        public Task<Stream> DownloadDataStreamAsync(string url)
        {
            return Task<Stream>.Factory.StartNew(() =>
            {
                return DownloadDataStream(url);
            });
        }

        public string DeleteString(string url)
        {
            #if __ANDROID__
            var request = new Request.Builder()
                .Url(Utils.ConvertToUrlEscapingIllegalCharacters(url))
                .Delete()
                .Header("User-Agent", _userAgent)
                .Build();
            var response = _client.NewCall(request).Execute();
            return response.Body().String();
            #else
            return _client.UploadString(url, "DELETE", string.Empty);
            #endif
        }

        public Task<string> DeleteStringAsync(string url)
        {
            return Task<string>.Factory.StartNew(() =>
            {
                return DeleteString(url);
            });
        }

        public class FormFile 
        {
            public string Name { get; set; }

            public string ContentType { get; set; }

            public string FilePath { get; set; }

            public byte[] Bytes { get; set; }
        }

        public class UploadMultiPartException : Exception
        {
            public HttpStatusCode Status { get; private set; }

            public UploadMultiPartException(HttpStatusCode status)
            {
                Status = status;
            }
        }

        #if __ANDROID__

        private class ProgressListener : Java.Lang.Object, IProgressRequestBodyProgressListener
        {
            private readonly Action<double> _progress;

            public ProgressListener(Action<double> progress)
            {
                _progress = progress;
            }

            public void Transferred(double p0)
            {
                _progress(p0);
            }
        }

        private string PostMultiPartOkhttp(string url, Dictionary<string, object> parameters, Action<double> progress = null)
        {
            var memStreams = new List<MemoryStream>();
            try
            {
                var requestBodyBuilder = new MultipartBuilder().Type(MultipartBuilder.Form);

                foreach (var parameter in parameters)
                {
                    var formFile = parameter.Value as FormFile;
                    if (formFile == null)
                    {
                        requestBodyBuilder.AddPart(Headers.Of("Content-Disposition", "form-data; name=\"" + parameter.Key + "\""),
                            RequestBody.Create(null, parameter.Value as string));
                    }
                    else
                    {
                        var headers = Headers.Of("Content-Disposition", "form-data; name=\"" + parameter.Key + "\"; filename=\"" + formFile.Name + "\"");
                        var mediaType = string.IsNullOrWhiteSpace(formFile.ContentType) ? null : MediaType.Parse(formFile.ContentType);

                        if (formFile.Bytes != null)
                        {
                            var ms = new MemoryStream(formFile.Bytes);
                            requestBodyBuilder.AddPart(headers, new ProgressRequestBody(ms, formFile.Bytes.Length, mediaType, 
                                new ProgressListener(progressPercentage =>
                                {
                                    if (progress != null)
                                    {
                                        progress(progressPercentage);
                                    }
                                })));
                            memStreams.Add(ms);
                        }
                        else if (formFile.FilePath != null)
                        {
                            requestBodyBuilder.AddPart(headers, new ProgressRequestBody(new Java.IO.File(formFile.FilePath), mediaType, 
                                new ProgressListener(progressPercentage =>
                                {
                                    if (progress != null)
                                    {
                                        progress(progressPercentage);
                                    }
                                })));
                        }
                        else
                        {
                            throw new NotSupportedException("You need to specify the bytes or the file path");
                        }
                    }
                }

                var requestBody = requestBodyBuilder.Build();

                var request = new Request.Builder()
                    .Url(Utils.ConvertToUrlEscapingIllegalCharacters(url))
                    .Header("User-Agent", _userAgent)
                    .Post(requestBody)
                    .Build();
                var response = _client.NewCall(request).Execute();

                if (!response.IsSuccessful)
                    throw new UploadMultiPartException((HttpStatusCode)response.Code());

                var body = response.Body().String();

                return body;
            }
            finally
            {
                foreach (var ms in memStreams)
                {
                    try
                    {
                        ms.Dispose();
                    }
                    catch
                    {
                        // fall-through
                    }
                }
            }
        }

        #endif

        //FIXME: this doesn't actually use webclient :/
        private string PostMultiPartWebClient(string url, Dictionary<string, object> parameters, Action<double> progress = null)
        {
            try
            {
                string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                byte[] boundaryBytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.ContentType = "multipart/form-data; boundary=" + boundary;
                request.Method = "POST";
                request.KeepAlive = true;
                request.Credentials = System.Net.CredentialCache.DefaultCredentials;
                request.SendChunked = true;
                request.Timeout = Timeout;

                if (parameters != null && parameters.Count > 0)
                {

                    using (Stream requestStream = request.GetRequestStream())
                    {

                        foreach (KeyValuePair<string, object> pair in parameters)
                        {

                            requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                            if (pair.Value is FormFile)
                            {
                                FormFile file = pair.Value as FormFile;
                                string header = "Content-Disposition: form-data; name=\"" + pair.Key + "\"; filename=\"" + file.Name + "\"\r\nContent-Type: " + file.ContentType + "\r\n\r\n";
                                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(header);
                                requestStream.Write(bytes, 0, bytes.Length);
                                byte[] buffer = new byte[32768];
                                int bytesRead;
                                if (file.FilePath != null)
                                {
                                    // upload from file
                                    using (FileStream fileStream = File.OpenRead(file.FilePath))
                                    {
                                        long totalBytesRead = 0;
                                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                                        {
                                            requestStream.Write(buffer, 0, bytesRead);
                                            totalBytesRead += bytesRead;
                                            if (progress != null)
                                            {
                                                progress((double)totalBytesRead / (double)fileStream.Length * 100.0);
                                            }
                                        }
                                    }
                                }
                                else if (file.Bytes != null)
                                {
                                    requestStream.Write(file.Bytes, 0, file.Bytes.Length);
                                }
                            }
                            else
                            {
                                string data = "Content-Disposition: form-data; name=\"" + pair.Key + "\"\r\n\r\n" + pair.Value;
                                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
                                requestStream.Write(bytes, 0, bytes.Length);
                            }
                        }

                        byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                        requestStream.Write(trailer, 0, trailer.Length);
                    }
                }

                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream))
                        return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                var errorResponse = ex.Response as HttpWebResponse;
                throw new UploadMultiPartException(errorResponse.StatusCode);
            }
        }

        public string PostMultiPart(string url, Dictionary<string, object> parameters, Action<double> progress = null)
        {
            #if __ANDROID__
            return PostMultiPartOkhttp(url, parameters, progress);
            #else
            return PostMultiPartWebClient(url, parameters, progress);
            #endif
        }

        public Task<string> PostMultiPartAsync(string url, Dictionary<string, object> parameters, Action<double> progress = null)
        {
            return Task<string>.Factory.StartNew(() =>
            {
                return PostMultiPart(url, parameters, progress);
            });
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
