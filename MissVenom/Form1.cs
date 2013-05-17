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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MissVenom
{
    public partial class Form1 : Form
    {
        const int PacketBufferSize = 65536;

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
                }
            }
            catch (Exception ex)
            {
                this.AddListItem("ERROR: Could not update registry. " + ex.Message);
            }
        }

        private byte[] getCertificate()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), @"server.pfx");
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        protected void startDnsServer()
        {
            try
            {
                DnsServer server = new DnsServer(IPAddress.Any, 10, 10, onDnsQuery);
                this.AddListItem("Started DNS proxy...");
                server.Start();
            }
            catch (Exception e)
            {
                this.AddListItem("ERROR STARTING DNS SERVER: " + e.Message);
            }
        }

        protected void startTcpSniffer()
        {
            try
            {

            }
            catch (Exception e)
            {
                this.AddListItem("TCP SNIFFER ERROR: " + e.Message);
            }
            try
            {
                string targethost = "50.22.231.45";// resolveHost("c.whatsapp.net");
                if (string.IsNullOrEmpty(targethost))
                {
                    throw new Exception("Could not resolve host");
                }
                IPEndPoint src = new IPEndPoint(GetIP(), 5222);
                IPEndPoint dst = new IPEndPoint(IPAddress.Parse(targethost), 5222);
                this.AddListItem("Started TCP sniffer...");
                byte[] PacketBuffer = new byte[PacketBufferSize];
                try
                {
                    Socket tcpSocket = new Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Raw, System.Net.Sockets.ProtocolType.IP);
                    try
                    {
                        tcpSocket.Bind(src);
                        tcpSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP, System.Net.Sockets.SocketOptionName.HeaderIncluded, 1);
                        tcpSocket.IOControl(unchecked((int)0x98000001), new byte[4] { 1, 0, 0, 0 }, new byte[4]);
                        while (true)
                        {
                            System.IAsyncResult ar = tcpSocket.BeginReceive(PacketBuffer, 0, PacketBufferSize, System.Net.Sockets.SocketFlags.None, new System.AsyncCallback(CallReceive), this);
                            while (tcpSocket.Available == 0)
                            {
                                System.Threading.Thread.Sleep(10);
                            }
                            int Size = tcpSocket.EndReceive(ar);
                            ExtractBuffer(ref PacketBuffer);
                        }
                    }
                    finally
                    {
                        if (tcpSocket != null)
                        {
                            tcpSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                            tcpSocket.Close();
                        }
                    }
                }
                finally
                {
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                this.AddListItem("TCP SNIFFER: Thread aborted");
            }
            catch (System.Exception E)
            {
                System.Windows.Forms.MessageBox.Show(E.ToString());
            }
        }

        public virtual void CallReceive(System.IAsyncResult ar)
        {
            //ExtractBuffer();
        }

        protected void ExtractBuffer(ref byte[] PacketBuffer)
        {
            IPPacket IP = new IPPacket(ref PacketBuffer);
            if (IP.TCP != null && (IP.TCP.DestinationPort == 5222 || IP.TCP.SourcePort == 5222) && IP.TCP.PacketData.Length > 0)
            {
                string SourceAddress = IP.SourceAddress.ToString();
                string DestinationAddress = IP.DestinationAddress.ToString();
                //string Data = System.Text.RegularExpressions.Regex.Replace(System.Text.Encoding.ASCII.GetString(IP.TCP.PacketData), @"[^a-zA-Z_0-9\.\@\- ]", "");
                string Data = WhatsAppApi.WhatsApp.SYSEncoding.GetString(IP.TCP.PacketData);

                this.AddLogItem(IP.TCP.PacketData);
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        delegate void AddListItemCallback(String text);
        delegate void AddLogItemCallback(byte[] data);

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

        private void AddLogItem(byte[] data)
        {
            if (this.textBox1.InvokeRequired)
            {
                AddLogItemCallback a = new AddLogItemCallback(AddLogItem);
                this.Invoke(a, new object[] { data });
            }
            else
            {
                string strout = WhatsAppApi.WhatsApp.SYSEncoding.GetString(data);
                File.AppendAllLines("log.txt", new string[] {strout});
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

        static IPAddress GetIP()
        {
            IPHostEntry host;
            IPAddress localIP = null;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
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
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    res.Add(ip.ToString());
                }
            }
            return res.ToArray();
        }

        static DnsMessageBase onDnsQuery(DnsMessageBase message, IPAddress clientAddress, ProtocolType protocol)
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
                    query.Questions[0].Name.Equals("v.whatsapp.net", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    query.ReturnCode = ReturnCode.NoError;
                    IPAddress localIP = GetIP();
                    if (localIP != null)
                    {
                        query.AnswerRecords.Add(new ARecord("v.whatsapp.net", 30, localIP));
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
            if (GetIP() == null)
            {
                this.AddListItem("DNS ERROR: Could not find your local IP address");
                return;
            }

            //start DNS server
            Thread srv = new Thread(new ThreadStart(startDnsServer));
            srv.IsBackground = true;
            srv.Start();

            //start TCP proxy
            Thread tcpproxy = new Thread(new ThreadStart(startTcpSniffer));
            tcpproxy.IsBackground = true;
            tcpproxy.Start();

            //start HTTPS server
            var certificate = new X509Certificate2(this.getCertificate(), "banana");
            try
            {
                var listener = (SecureHttpListener)HttpServer.HttpListener.Create(IPAddress.Any, 443, certificate);
                listener.UseClientCertificate = true;
                listener.RequestReceived += onHttpsRequest;
                listener.Start(5);
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

            this.AddListItem("Set your DNS address on your phone to " + ips.First() + " (Settings->WiFi->Static IP->DNS)");

        }
    }
}
