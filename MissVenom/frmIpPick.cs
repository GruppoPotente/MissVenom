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
        private string[] _ipAddresses;

        public frmIpPick(string[] ipAddresses)
        {
            InitializeComponent();
            if (ipAddresses != null && ipAddresses.Any())
            {
                _ipAddresses = ipAddresses;
                var bindableIp = (from ip in _ipAddresses select new { Ip = ip.ToString() }).ToList();
                grdIp.DataSource = bindableIp;
                grdIp.Refresh();
            }
        }

        public string SelectedIP()
        {
            return _ipAddresses[grdIp.CurrentRow.Index];
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            if (grdIp.SelectedRows != null && grdIp.SelectedRows.Count == 1)
                DialogResult = System.Windows.Forms.DialogResult.OK;
            else
                DialogResult = System.Windows.Forms.DialogResult.Abort;
        }
    }
}
