using PcapDotNet.Core;
using PcapDotNet.Packets;
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
            //IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            //if (allDevices.Count == 0)
            //{
            //    throw new Exception("No capture devices found");
            //}
            //if (allDevices.Count > 1)
            //{
            //    Console.WriteLine("You have " + allDevices.Count + " devices");
            //    //show adapter selection dialog
            //}

            //PacketDevice device = allDevices.First();

            //            // Open the device
            //using (PacketCommunicator communicator = 
            //    device.Open(65536,                                  // portion of the packet to capture
            //                                                                // 65536 guarantees that the whole packet will be captured on all the link layers
            //                        PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
            //                        1000))                                  // read timeout
            //{
            //    BerkeleyPacketFilter filter = communicator.CreateFilter("port 5222");
            //    Console.WriteLine("Listening on " + device.Description + "...");
            //    communicator.SetFilter(filter);
            //    // start the capture
            //    communicator.ReceivePackets(0, PacketHandler);
            //}
        }

        // Callback function invoked by Pcap.Net for every incoming packet
        private static void PacketHandler(Packet packet)
        {
            
            //Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" + packet.Length);
        }
    }
}
