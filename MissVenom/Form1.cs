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
            //var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), @"server.pfx");
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), @"all.whatsapp.net.pfx");
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
            }
        }

        private void onHttpsRequest(object sender, RequestEventArgs e)
        {
            String url = "https://v.whatsapp.net" + e.Request.Uri.PathAndQuery;
            HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
            try
            {
                request.UserAgent = e.Request.Headers["User-Agent"].HeaderValue;
            }
            catch (Exception ex)
            {
                this.AddListItem("Warning: " + ex.Message);
            }
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            e.Response.ContentType.Value = response.ContentType;
            e.Response.ContentLength.Value = response.ContentLength;

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
                //resolve v.whatsapp.net
                if (query.Questions[0].RecordType == RecordType.A
                    &&
                    (
                        query.Questions[0].Name.Equals("v.whatsapp.net")//rewrite registry host
                        ||
                        query.Questions[0].Name.Equals("c.whatsapp.net")//rewrite tcp hosts
                        ||
                        query.Questions[0].Name.Equals("c1.whatsapp.net")
                        ||
                        query.Questions[0].Name.Equals("c2.whatsapp.net")
                        ||
                        query.Questions[0].Name.Equals("c3.whatsapp.net")
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
            this.SetRegIpForward();
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

            this.AddListItem("Set your DNS address on your phone to " + ips.First() + " (Settings->WiFi->Static IP->DNS)");
        }

        private void startTcpRelay()
        {
            IPAddress ip = GetIP();
            this.tcpl = new TcpListener(GetIP(), 5222);
            tcpl.Start();
            this.AddListItem("Started TCP relay, waiting for connection...");
            this.s_internal = this.tcpl.AcceptTcpClient();
            this.AddListItem("Client connected!"); 
            this.s_external = new TcpClient("c.whatsapp.net", 5222);

            //start external processor thread
            Thread extp = new Thread(new ThreadStart(externalProcessor));
            extp.IsBackground = true;
            extp.Start();

            //process synchronous
            this.processConnection(ref this.s_internal, ref this.s_external);

            this.AddListItem("WARNING: Internal socket disconnected");
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
                            to.Client.Send(buffer);
                            this.bufferedMessages.Add(buffer);
                            this.AddListItem(WhatsAppApi.WhatsApp.SYSEncoding.GetString(buffer));
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
