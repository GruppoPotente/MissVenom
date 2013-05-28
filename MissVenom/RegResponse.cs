using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MissVenom
{
    public class RegResponse
    {
        public string status { get; set; }
        public string login { get; set; }
        public string pw { get; set; }
        public string type { get; set; }
        public string expiration { get; set; }
        public string kind { get; set; }
        public string price { get; set; }
        public string cost { get; set; }
        public string currency { get; set; }
        public string price_expiration { get; set; }
    }
}
