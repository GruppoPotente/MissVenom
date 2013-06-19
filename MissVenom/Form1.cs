using ARSoft.Tools.Net.Dns;
using HttpServer;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;

namespace MissVenom
{
    public partial class Form1 : Form
    {
        const string WA_SYNC_HOST = "sro.whatsapp.net";
        const string WA_REG_HOST = "v.whatsapp.net";
        const string WA_CERT_HOST = "cert.whatsapp.net";

        private static TcpClient s_internal;
        private static TcpClient s_external;
        private TcpListener tcpl;

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
                server.ExceptionThrown += dnsServer_ExceptionThrown;
                server.Start();
            }
            catch (Exception e)
            {
                this.AddListItem(String.Format("ERROR STARTING DNS SERVER: {0}", e.Message));
            }
        }

        private void dnsServer_ExceptionThrown(object sender, ARSoft.Tools.Net.Dns.ExceptionEventArgs e)
        {
            startDnsServer();
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
                this.textBox1.AppendText(String.Format("{0}\r\n", data));
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
            if (e.Request.Uri.Authority.EndsWith(".whatsapp.net") && !e.Request.Uri.Authority.Equals(WA_CERT_HOST, StringComparison.InvariantCultureIgnoreCase))
            {
                //use original
                url = e.Request.Uri.AbsoluteUri;
            }
            else
            {
                //return certificate
                byte[] cert = this.getClientCertificate();
                e.Response.ContentType = new HttpServer.Headers.ContentTypeHeader("application/x-x509-ca-cert");
                e.Response.Add(new HttpServer.Headers.StringHeader("Content-disposition", "attachment; filename=wapi.cer"));
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
                Stream responseStream = response.GetResponseStream();
                byte[] rawdata = new byte[1024];
                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while ((read = responseStream.Read(rawdata, 0, rawdata.Length)) > 0)
                    {
                        ms.Write(rawdata, 0, read);
                    }
                    rawdata = ms.ToArray();
                }
                if (response.Headers["WWW-Authenticate"] != null)
                {
                    //contact sync auth header
                    e.Response.Add(new HttpServer.Headers.StringHeader("WWW-Authenticate", response.Headers["WWW-Authenticate"].ToString()));
                }
                if(response.Headers["X-WA-Metadata"] != null)
                {
                    //WA media type header
                    e.Response.Add(new HttpServer.Headers.StringHeader("X-WA-Metadata", response.Headers["X-WA-Metadata"]));
                    //save media:
                    string filename = e.Request.Uri.AbsolutePath.Split('/').Last();
                    this.AddListItem(String.Format("Saving media file to {0}", filename));
                    FileStream f = File.OpenWrite(filename);
                    f.Write(rawdata, 0, rawdata.Length);
                    f.Close();
                }

                responseStream.Read(rawdata, 0, rawdata.Length);
                String data = System.Text.Encoding.UTF8.GetString(rawdata);

                //try to find password
                if (e.Request.Uri.Authority == WA_REG_HOST)
                {
                    try
                    {
                        RegResponse reg = JsonConvert.DeserializeObject<RegResponse>(data);
                        if (reg.status == "ok" && !String.IsNullOrEmpty(reg.pw))
                        {
                            this.AddListItem(String.Format("FOUND PASSWORD!: {0}", reg.pw));
                        }
                    }
                    catch (Exception ex)
                    {
                        this.AddListItem(String.Format("ERROR DESERIALIZING JSON: {0}", ex.Message));
                    }
                }
                e.Response.Body.Write(rawdata, 0, rawdata.Length);

                this.AddListItem(String.Format("USERAGENT:{0}", e.Request.Headers["User-Agent"].HeaderValue));
                this.AddListItem(String.Format("REQUEST:  {0}", e.Request.Uri.AbsoluteUri));
                this.AddListItem(String.Format("RESPONSE: {0}", data));
                this.AddListItem(" ");
            }
            catch (Exception ex)
            {
                this.AddListItem(String.Format("HTTPS REQUEST ERROR: {0}", ex.Message));
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
                //resolve whatsapp.net subdomains
                if (query.Questions[0].RecordType == RecordType.A
                    &&
                    (
                        query.Questions[0].Name == WA_CERT_HOST
                        ||
                        query.Questions[0].Name == WA_REG_HOST
                        ||
                        (
                            //media files
                            query.Questions[0].Name.StartsWith("mms")
                            &&
                            query.Questions[0].Name.EndsWith("whatsapp.net")
                        )
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
                try
                {
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
                catch (Exception e)
                { }
            }
            // Not a valid query or upstream server did not answer correct
            message.ReturnCode = ReturnCode.ServerFailure;
            return message;
        }

        private void startVenom()
        {
            //do stuff
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
                this.AddListItem(String.Format("WARNING: Multiple IP addresses found: {0}", String.Join(" ,", ips)));
            }

            this.AddListItem(String.Format("Set your DNS address on your phone to {0} (Settings->WiFi->Static IP->DNS) and go to https://cert.whatsapp.net in your phone's browser to install the root certificate", ips.First()));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //start working
            Thread t = new Thread(new ThreadStart(startVenom));
            t.IsBackground = true;
            t.Start();
        }
    }
}
