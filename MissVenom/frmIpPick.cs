using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace MissVenom
{
    public partial class frmIpPick : Form
    {
        public string[] IpAddresses { get; set; }

        public frmIpPick()
        {
            InitializeComponent();
        }

        public void RefreshIpList()
        {
            if (IpAddresses != null && IpAddresses.Any())
            {
                //var bindableIp = (from ip in IpAddresses select new { Address = ip }).ToList();
                this.lbIpAddresses.DataSource = this.IpAddresses;
            }
        }

        public string SelectedIP()
        {
            return IpAddresses[this.lbIpAddresses.SelectedIndex];
        }

        private void lbIpAddresses_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}
