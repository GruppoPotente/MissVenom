using PcapDotNet.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MissVenom.Sniff
{
    class Capture
    {
        public Capture()
        {
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            if (allDevices.Count == 0)
            {
                throw new Exception("No capture devices found");
            }

            //and so on
        }
        
    }
}
