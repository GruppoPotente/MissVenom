using ARSoft.Tools.Net.Dns;
using HttpServer;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using WhatsAppApi.Helper;

namespace MissVenom
{
    public partial class Form1 : Form
    {
        private static TcpClient s_internal;
        private static TcpClient s_external;
        private TcpListener tcpl;
        private string password = string.Empty;
        private static BinTreeNodeReader reader = new BinTreeNodeReader(WhatsAppApi.Helper.DecodeHelper.getDictionary());

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
                if (e.Request.Connection != null)
                {
                    request.Connection = e.Request.Connection.HeaderValue;
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
                byte[] rawdata = new byte[responseStream.Length];
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
                    string filename = e.Request.Uri.AbsoluteUri.Split('/').Last();
                    this.AddListItem("Saving media file to " + filename);
                    FileStream f = File.OpenWrite(filename);
                    f.Write(rawdata, 0, rawdata.Length);
                    f.Close();
                }

                responseStream.Read(rawdata, 0, rawdata.Length);
                String data = WhatsAppApi.WhatsApp.SYSEncoding.GetString(rawdata);

                //try to find password
                if (e.Request.Uri.Authority == "v.whatsapp.net")
                {
                    JavaScriptSerializer jss = new JavaScriptSerializer();
                    RegResponse reg = jss.Deserialize<RegResponse>(data);
                    if (reg.status == "ok" && !String.IsNullOrEmpty(reg.pw))
                    {
                        this.AddListItem("FOUND PASSWORD!: " + reg.pw);
                        this.password = reg.pw;
                    }
                }
                e.Response.Body.Write(rawdata, 0, rawdata.Length);

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
                    query.Questions[0].Name.EndsWith(".whatsapp.net", StringComparison.InvariantCultureIgnoreCase)//rewrite ALL whatsapp.net subdomains
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

        private void button1_Click(object sender, EventArgs e)
        {
            //disable stuff
            this.button1.Enabled = false;
            this.textBox2.Enabled = false;

            //start
            this.password = this.textBox2.Text;

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
            Thread tcpr = new Thread(new ThreadStart(startTcpRelay));
            tcpr.IsBackground = true;
            tcpr.Start();

            this.AddListItem("Set your DNS address on your phone to " + ips.First() + " (Settings->WiFi->Static IP->DNS) and go to https://cert.whatsapp.net in your phone's browser to install the root certificate");
        }

        private void startTcpRelay()
        {
            this.tcpl = new TcpListener(IPAddress.Any, 5222);
            tcpl.Start();
            this.AddListItem("Started TCP relay, waiting for connection...");

            try
            {
                while (true)
                {
                    s_internal = this.tcpl.AcceptTcpClient();
                    s_internal.ReceiveBufferSize = 1024;
                    byte[] intbuf = new byte[s_internal.ReceiveBufferSize];
                    this.AddListItem("Client connected!");
                    if (s_external != null)
                    {
                        s_external.Close();
                    }
                    s_external = new TcpClient();
                    s_external.Connect("c2.whatsapp.net", 5222);
                    s_external.ReceiveBufferSize = 1024;
                    byte[] extbuf = new byte[s_external.ReceiveBufferSize];

                    WhatsAppApi.Helper.Encryption.encryptionIncoming = null;
                    WhatsAppApi.Helper.Encryption.encryptionOutgoing = null;

                    s_internal.GetStream().BeginRead(intbuf, 0, intbuf.Length, onReceiveIntern, intbuf);
                    s_external.GetStream().BeginRead(extbuf, 0, extbuf.Length, onReceiveExtern, extbuf);
                }
            }
            catch (Exception e)
            {
                this.AddListItem("TCPRELAY STOPPED: " + e.Message);
            }
        }

        private void onReceiveExtern(IAsyncResult result)
        {
            try
            {
                byte[] buffer = result.AsyncState as byte[];
                buffer = trimBuffer(buffer);
                logRawData(buffer, "rx");
                try
                {
                    this.decodeInTree(buffer);
                    s_internal.GetStream().Write(buffer, 0, buffer.Length);
                }
                catch (Exception e)
                {
                    //invalidate buffer and force reauth
                    if (e.Message == "Received encrypted message, encryption key not set" && !String.IsNullOrEmpty(this.password))
                    {
                        this.AddListItem("Invalidated!");
                        buffer = Convert.FromBase64String(this.password);
                    }
                    s_internal.GetStream().Write(buffer, 0, buffer.Length);
                }
                
                buffer = new byte[1024];
                s_external.GetStream().BeginRead(buffer, 0, buffer.Length, onReceiveExtern, buffer);
            }
            catch (IndexOutOfRangeException e)
            {

            }
            catch (Exception e)
            {
                this.AddListItem("TCP EXT ERROR: " + e.Message);
            }
        }

        private void onReceiveIntern(IAsyncResult result)
        {
            try
            {
                byte[] buffer = result.AsyncState as byte[];
                buffer = trimBuffer(buffer);
                logRawData(buffer, "tx");
                try
                {
                    if (!(buffer[0] == 'W' && buffer[1] == 'A'))//don't bother decoding WA stream start
                    {
                        this.decodeOutTree(buffer);
                    }
                    s_external.GetStream().Write(buffer, 0, buffer.Length);
                }
                catch (Exception e)
                {
                    //invalidate buffer and force reauth
                    if (e.Message == "Received encrypted message, encryption key not set" && !String.IsNullOrEmpty(this.password))
                    {
                        this.AddListItem("Invalidated!");
                        buffer = Convert.FromBase64String(this.password);
                    }
                    s_external.GetStream().Write(buffer, 0, buffer.Length);
                }
                
                buffer = new byte[1024];
                s_internal.GetStream().BeginRead(buffer, 0, buffer.Length, onReceiveIntern, buffer);
            }
            catch (IndexOutOfRangeException e)
            {

            }
            catch (Exception e)
            {
                this.AddListItem("TCP INT ERROR: " + e.Message);
            }
        }

        private static byte[] trimBuffer(byte[] buffer)
        {
            //trim null bytes
            int i = buffer.Length - 1;
            while (buffer[i] == 0)
                --i;
            byte[] bar = new byte[i + 1];
            Array.Copy(buffer, bar, i + 1);
            return bar;
        }

        private static void logRawData(byte[] data, string prefix)
        {
            string dat = Convert.ToBase64String(data);
            File.AppendAllLines("b64raw.log", new string[] { dat });
            dat = WhatsAppApi.WhatsApp.SYSEncoding.GetString(data);
            File.AppendAllLines("raw.log", new string[] { dat });
        }

        private void decodeInTree(byte[] data)
        {
            try
            {
                ProtocolTreeNode node = reader.nextTree(data, true);
                
                while (node != null)
                {
                    File.AppendAllLines("xmpp.log", new string[] { node.NodeString("rx") });

                    //look for challengedata and forge key
                    if (node.tag.Equals("challenge", StringComparison.InvariantCultureIgnoreCase) && !String.IsNullOrEmpty(this.password))
                    {
                        this.AddListItem("ChallengeKey received, forging key...");
                        byte[] challengeData = node.GetData();
                        byte[] pass = Convert.FromBase64String(this.password);
                        Rfc2898DeriveBytes r = new Rfc2898DeriveBytes(pass, challengeData, 16);
                        byte[] key = r.GetBytes(20);
                        reader.Encryptionkey = key;
                        //reset static keys
                        WhatsAppApi.Helper.Encryption.encryptionIncoming = null;
                        WhatsAppApi.Helper.Encryption.encryptionOutgoing = null;
                    }

                    node = reader.nextTree(null, true);
                }
            }
            catch (IncompleteMessageException e)
            { }
            catch (Exception e)
            {
                this.AddListItem("INDECODER ERROR: " + e.Message);
                throw e;
            }
        }

        private void decodeOutTree(byte[] data)
        {
            try
            {
                ProtocolTreeNode node = reader.nextTree(data, false);
                while (node != null)
                {
                    File.AppendAllLines("xmpp.log", new string[] { node.NodeString("tx") });
                    node = reader.nextTree(null, false);
                }
            }
            catch (IncompleteMessageException e)
            { }
            catch (Exception e)
            {
                this.AddListItem("OUTDECODER ERROR: " + e.Message);
                throw e;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //add instructions
            this.AddListItem("Enter your password (optional) and click Start to start MissVenom");
        }
    }
}
