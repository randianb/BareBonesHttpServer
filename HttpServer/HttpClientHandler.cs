﻿// Corey Wunderlich - 2019
// www.wundervisionenvisionthefuture.com
//
// Handles receiving data from a client and notifying the server of the
// data that is available. Decodes regular Http and Websocktes
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
namespace SimpleHttpServer
{
    public delegate void HttpRequestDataCallback(HttpClientHandler client, HttpRequest packet);
    public delegate void HttpWebSocketDataCallback(HttpClientHandler client, WebSocketFrame packet);
    public delegate void HttpClientHandlerEvent(HttpClientHandler client);

    public class HttpClientHandler
    {

        public HttpRequestDataCallback HttpRequestReceived;
        public HttpWebSocketDataCallback WebSocketDataReceived;
        public HttpClientHandlerEvent ClientDisconnected;

        private bool WebSocketUpgrade = false;
        private TcpClient _client;
        private NetworkStream _stream;

        public string ClientInfo
        {
            get { return _client.Client.RemoteEndPoint.ToString(); }
        }
        public EndPoint RemoteEndPoint
        {
            get { return _client.Client.RemoteEndPoint;  }
        }
        public string IPAddress
        {
            get { return ((IPEndPoint)_client.Client.RemoteEndPoint).Address.ToString(); }
        }
        public TcpClient Client
        {
            get { return _client;  }
        }

        const int BUFFERSIZE = 1024;
        byte[] _buffer = new byte[BUFFERSIZE];
        
        public HttpClientHandler(TcpClient c)
        {
            _client = c;
            _stream = c.GetStream();
            //System.Diagnostics.Debug.WriteLine("Creating Client");
        }

        public async void BeginReadData()
        {
            try
            {
                while(true)
                {
                    int bytesread = await _stream.ReadAsync(_buffer, 0, BUFFERSIZE);
                    if (bytesread > 0)
                    {

                        if (!this.WebSocketUpgrade)
                        {
                            string msg = System.Text.Encoding.UTF8.GetString(_buffer);
                            //System.Diagnostics.Debug.WriteLine(msg);
                            HttpRequest h = new HttpRequest(msg);
                            HttpRequestReceived?.Invoke(this, h);
                        }
                        else
                        {
                            //Not handling Multiple Frames worth of data...
                            WebSocketFrame frame = new WebSocketFrame(_buffer);
                            WebSocketDataReceived?.Invoke(this, frame);
                            //Console.WriteLine(Encoding.UTF8.GetString(frame.Payload));
                        }
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("Zero Bytes");
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Client Read Aborted " + ex.ToString());
            }
            Console.WriteLine("DONE");
            ClientDisconnected?.Invoke(this);
        }
        public void UpgradeToWebsocket()
        {
            this.WebSocketUpgrade = true;
        }
        public void SendAsync(string text)
        {
            this.SendAsync(System.Text.Encoding.UTF8.GetBytes(text));
        }
        public void SendAsync(WebSocketFrame ws)
        {
            this.SendAsync(ws.GetBytes());
        }
        public void SendAsync(byte[] bytes)
        {
            //System.Diagnostics.Debug.WriteLine("Sending Data");
            if (this._client.Connected && this._stream.CanWrite)
            {
                this._stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }
        public void Send(string text)
        {
            this.Send(System.Text.Encoding.UTF8.GetBytes(text));
        }
        public void Send(byte[] bytes)
        {
            //System.Diagnostics.Debug.WriteLine("Sending Data");
            if (this._client.Connected && this._stream.CanWrite)
            {
                this._stream.Write(bytes, 0, bytes.Length);
            }
        }
        public void Send(WebSocketFrame ws)
        {
            this.Send(ws.GetBytes());
        }
        public void Send(HttpResponse hr)
        {
            this.Send(hr.GetBytes());
        }

        public void Close()
        {
            if (_client.Connected)
            {
                _client.Close();
            }
        }
    }
}
