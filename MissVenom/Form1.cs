using ARSoft.Tools.Net.Dns;
using HttpServer;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WhatsAppApi.Helper;

namespace MissVenom
{
    public partial class Form1 : Form
    {
        private TcpClient s_internal;
        private TcpClient s_external;
        private TcpListener tcpl;
        private List<byte[]> bufferedMessages = new List<byte[]>();

        private string targetIP;

        public static string resolveHost(string hostname)
        {
            IPHostEntry ips = Dns.GetHostEntry(hostname);
            if (ips != null && ips.AddressList != null && ips.AddressList.Length > 0)
            {
                return ips.AddressList.First().ToString();
            }
            return null;
        }

        private void SetRegIpForward()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", true);
                if (key != null)
                {
                    key.SetValue("IPEnableRouter", 1);
                    key.Close();
                    this.AddListItem("Updated registry to forward traffic");
                }
            }
            catch (Exception ex)
            {
                this.AddListItem("ERROR: Could not update registry. " + ex.Message);
            }
        }

        private byte[] getCertificate()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), @"all.whatsapp.net.pfx");
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        private byte[] getClientCertificate()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), @"server.v2.crt");
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        protected void startDnsServer()
        {
            try
            {
                DnsServer server = new DnsServer(System.Net.IPAddress.Any, 10, 10, onDnsQuery);
                this.AddListItem("Started DNS proxy...");
                server.Start();
            }
            catch (Exception e)
            {
                this.AddListItem("ERROR STARTING DNS SERVER: " + e.Message);
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        delegate void AddListItemCallback(String text);
        delegate void AddLogItemCallback(byte[] data, bool toClient);

        private void AddListItem(String data)
        {
            if (this.textBox1.InvokeRequired)
            {
                AddListItemCallback a = new AddListItemCallback(AddListItem);
                this.Invoke(a, new object[] { data });
            }
            else
            {
                this.textBox1.AppendText(data + "\r\n");
                this.textBox1.DeselectAll();
                //log to file
                try
                {
                    File.AppendAllLines("MissVenom.log", new String[] { data });
                }
                catch (Exception e)
                {

                }
            }
        }

        private void onHttpsRequest(object sender, RequestEventArgs e)
        {
            String url;
            //dev, for test on localhost:
            if (e.Request.Uri.Authority.EndsWith(".whatsapp.net") && !e.Request.Uri.Authority.Equals("cert.whatsapp.net", StringComparison.InvariantCultureIgnoreCase))
            {
                //use original
                url = e.Request.Uri.AbsoluteUri;
            }
            else
            {
                //return certificate
                byte[] cert = this.getClientCertificate();
                e.Response.ContentType = new HttpServer.Headers.ContentTypeHeader("application/x-x509-ca-cert");
                e.Response.Body.Write(cert, 0, cert.Length);
                return;
            }

            HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
            try
            {
                request.Method = e.Request.Method;
                if (e.Request.ContentType != null)
                {
                    request.ContentType = e.Request.ContentType.HeaderValue;
                }
                else
                {
                    request.ContentType = "application/x-www-form-urlencoded";
                }
                if (e.Request.ContentLength != null)
                {
                    request.ContentLength = long.Parse(e.Request.ContentLength.HeaderValue);
                }
                if (e.Request.Headers["User-Agent"] != null)
                {
                    request.UserAgent = e.Request.Headers["User-Agent"].HeaderValue;
                }
                if (e.Request.Headers["Accept"] != null)
                {
                    request.Accept = e.Request.Headers["Accept"].HeaderValue;
                }
                if (e.Request.Headers["Authorization"] != null)
                {
                    request.Headers.Add("Authorization", e.Request.Headers["Authorization"].HeaderValue);
                }
                if (e.Request.Headers["Accept-Encoding"] != null)
                {
                    request.Headers.Add("Accept-Encoding", e.Request.Headers["Accept-Encoding"].HeaderValue);
                }
                if (e.Request.Body.Length > 0)
                {
                    //copy body
                    byte[] req = new byte[e.Request.Body.Length];
                    e.Request.Body.Read(req, 0, (int)e.Request.Body.Length);
                    Stream reqStream = request.GetRequestStream();
                    reqStream.Write(req, 0, req.Length);
                }
            }
            catch (Exception ex)
            {
                this.AddListItem("Warning: " + ex.Message);
            }
            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                e.Response.ContentType.Value = response.ContentType;
                e.Response.ContentLength.Value = response.ContentLength;
                if (response.Headers["WWW-Authenticate"] != null)
                {
                    e.Response.Add(new HttpServer.Headers.StringHeader("WWW-Authenticate", response.Headers["WWW-Authenticate"].ToString()));
                }

                Stream responseStream = response.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);
                String data = responseReader.ReadToEnd();

                byte[] data2 = Encoding.Default.GetBytes(data);

                e.Response.Body.Write(data2, 0, data2.Length);

                this.AddListItem("USERAGENT:" + e.Request.Headers["User-Agent"].HeaderValue);
                this.AddListItem("REQUEST:  " + e.Request.Uri.AbsoluteUri);
                this.AddListItem("RESPONSE: " + data);
                this.AddListItem(" ");
            }
            catch (Exception ex)
            {
                this.AddListItem("HTTPS REQUEST ERROR: " + ex.Message);
            }
        }

        static System.Net.IPAddress GetIP()
        {
            IPHostEntry host;
            System.Net.IPAddress localIP = null;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (System.Net.IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip;
                }
            }
            return localIP;
        }

        public string[] GetAllIPs()
        {
            List<string> res = new List<string>();
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (System.Net.IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    res.Add(ip.ToString());
                }
            }
            return res.ToArray();
        }

        static DnsMessageBase onDnsQuery(DnsMessageBase message, System.Net.IPAddress clientAddress, ProtocolType protocol)
        {
            message.IsQuery = false;

            DnsMessage query = message as DnsMessage;
            DnsMessage answer = null;

            if ((query != null) && (query.Questions.Count == 1))
            {
                //HOOK:
                //resolve v.whatsapp.net and sro.whatsapp.net
                if (query.Questions[0].RecordType == RecordType.A
                    &&
                    (
                        query.Questions[0].Name.Equals("v.whatsapp.net", StringComparison.InvariantCultureIgnoreCase)//registration
                            ||
                        query.Questions[0].Name.Equals("sro.whatsapp.net", StringComparison.InvariantCultureIgnoreCase)//contact sync
                            ||
                        query.Questions[0].Name.Equals("cert.whatsapp.net", StringComparison.InvariantCultureIgnoreCase)//certificate provider
                    )
                    )
                {
                    query.ReturnCode = ReturnCode.NoError;
                    System.Net.IPAddress localIP = GetIP();
                    if (localIP != null)
                    {
                        query.AnswerRecords.Add(new ARecord(query.Questions[0].Name, 30, localIP));
                        return query;
                    }
                }
                // send query to upstream server
                DnsQuestion question = query.Questions[0];
                answer = DnsClient.Default.Resolve(question.Name, question.RecordType, question.RecordClass);

                // if got an answer, copy it to the message sent to the client
                if (answer != null)
                {
                    foreach (DnsRecordBase record in (answer.AnswerRecords))
                    {
                        query.AnswerRecords.Add(record);
                    }
                    foreach (DnsRecordBase record in (answer.AdditionalRecords))
                    {
                        query.AnswerRecords.Add(record);
                    }

                    query.ReturnCode = ReturnCode.NoError;
                    return query;
                }
            }
            // Not a valid query or upstream server did not answer correct
            message.ReturnCode = ReturnCode.ServerFailure;
            return message;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //this.SetRegIpForward();
            this.targetIP = GetIP().ToString();
            if (String.IsNullOrEmpty(this.targetIP))
            {
                this.AddListItem("DNS ERROR: Could not find your local IP address");
                return;
            }

            //start DNS server
            Thread srv = new Thread(new ThreadStart(startDnsServer));
            srv.IsBackground = true;
            srv.Start();

            //start HTTPS server
            var certificate = new X509Certificate2(this.getCertificate(), "banana");
            try
            {
                var listener = (SecureHttpListener)HttpServer.HttpListener.Create(System.Net.IPAddress.Any, 443, certificate);
                listener.UseClientCertificate = true;
                listener.RequestReceived += onHttpsRequest;
                listener.Start(5);
                this.AddListItem("SSL proxy started");
                this.AddListItem("Listening...");
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                this.AddListItem("SOCKET ERROR");
                this.AddListItem("Make sure port 443 is not in use");
                this.AddListItem(ex.Message);
            }
            catch (Exception ex)
            {
                this.AddListItem("GENERAL ERROR");
                this.AddListItem(ex.Message);
            }
            this.AddListItem(" ");

            string[] ips = this.GetAllIPs();
            if (ips.Length > 1)
            {
                this.AddListItem("WARNING: Multiple IP addresses found: " + String.Join(" ,", ips));
            }

            //start tcp relay
            //Thread tcpr = new Thread(new ThreadStart(startTcpRelay));
            //tcpr.IsBackground = true;
            //tcpr.Start();
            this.AddListItem("Set your DNS address on your phone to " + ips.First() + " (Settings->WiFi->Static IP->DNS) and go to https://cert.whatsapp.net in your phone's browser to install the root certificate");
        }

        private void startTcpRelay()
        {
            IPAddress ip = GetIP();
            this.tcpl = new TcpListener(IPAddress.Any, 5222);
            tcpl.Start();
            this.AddListItem("Started TCP relay, waiting for connection...");
            this.s_internal = this.tcpl.AcceptTcpClient();
            byte[] intbuf = new byte[this.s_internal.ReceiveBufferSize];
            this.s_internal.GetStream().BeginRead(intbuf, 0, intbuf.Length, onReceiveIntern, intbuf);

            this.AddListItem("Client connected!"); 
            this.s_external = new TcpClient();
            this.s_external.Connect("c.whatsapp.net", 5222);
            byte[] extbuf = new byte[this.s_external.ReceiveBufferSize];
            this.s_external.GetStream().BeginRead(extbuf, 0, extbuf.Length, onReceiveExtern, extbuf);

            //start external processor thread
            Thread extp = new Thread(new ThreadStart(externalProcessor));
            extp.IsBackground = true;
            extp.Start();

            //process synchronous
            this.processConnection(ref this.s_internal, ref this.s_external);

            this.AddListItem("WARNING: Internal socket disconnected");
        }

        private void onReceiveExtern(IAsyncResult result)
        {

        }

        private void onReceiveIntern(IAsyncResult result)
        {

        }

        private void processConnection(ref TcpClient from, ref TcpClient to)
        {
            NetworkStream stream = from.GetStream();
            byte[] buffer = new byte[1024];
            //process internal data
            while (from.Connected)
            {
                try
                {
                    int i = stream.Read(buffer, 0, buffer.Length);
                    if (i > 0)
                    {
                        while (i > 0)
                        {
                            //trim
                            int ii = buffer.Length - 1;
                            while (buffer[ii] == 0)
                                --i;
                            byte[] bar = new byte[i + 1];
                            Array.Copy(buffer, bar, i + 1);

                            to.Client.Send(bar);
                            //this.bufferedMessages.Add(bar);
                            this.AddListItem(WhatsAppApi.WhatsApp.SYSEncoding.GetString(bar));
                            i = stream.Read(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }
                }
                catch (Exception e)
                {
                    this.AddListItem(e.Message);
                }
            }
        }

        private void externalProcessor()
        {
            //process synchronous
            this.processConnection(ref this.s_external, ref this.s_internal);

            this.AddListItem("WARNING: External socket disconnected");
        }
    }
}
