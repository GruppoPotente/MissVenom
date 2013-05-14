using HttpServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
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
        private byte[] getCertificate()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), @"server.pfx");
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        //protected void startDnsServer()
        //{

        //}

        public Form1()
        {
            InitializeComponent();
            
            // TODO!
            //start DNS server
            //Thread srv = new Thread(new ThreadStart(startDnsServer));
            //srv.IsBackground = true;
            //srv.Start();
            ///DnsServer srv = new DnsServer(IPAddress.Any, 10, 10, ProcessQuery);
            ///srv.Start();


            //start HTTPS server
            var certificate = new X509Certificate2(this.getCertificate(), "banana");
            try
            {
                var listener = (SecureHttpListener)HttpServer.HttpListener.Create(IPAddress.Any, 443, certificate);
                listener.UseClientCertificate = true;
                listener.RequestReceived += OnRequest;
                listener.Start(5);
                this.AddListItem("Listening...");
            }
            catch (System.Net.Sockets.SocketException e)
            {
                this.AddListItem("SOCKET ERROR");
                this.AddListItem("Make sure port 443 is not in use");
                this.AddListItem(e.Message);
            }
            catch (Exception e)
            {
                this.AddListItem("GENERAL ERROR");
                this.AddListItem(e.Message);
            }
            this.AddListItem(" ");
        }

        delegate void AddListItemCallback(String text);

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

        private void OnRequest(object sender, RequestEventArgs e)
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
    }
}
