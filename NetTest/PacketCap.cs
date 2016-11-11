using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcap.AirPcap;
using SharpPcap.WinPcap;
using PacketDotNet;
using System.Threading;
using System.Windows.Forms;
using System.IO;


namespace NetTest
{
    class PacketCap
    {
        private LibPcapLiveDevice livedevice;
        private static CaptureFileWriterDevice captureFileWriter;


        public PacketCap(int iDevice)
        {
            //获取网络设备
            var devices = LibPcapLiveDeviceList.Instance;

            var device = devices[iDevice];
            
            // Register our handler function to the 'packet arrival' event
            device.OnPacketArrival +=
                new PacketArrivalEventHandler(device_OnPacketArrival);

            // Register our handler function to the 'capture stop' event
            device.OnCaptureStopped +=
                new CaptureStoppedEventHandler(device_PcapOnCaptureStopped);
            //device.Filter = "host" + device;
            
            // Open the device for capturing
            int readTimeoutMilliseconds = 1000;
            if (device is AirPcapDevice)
            {
                // NOTE: AirPcap devices cannot disable local capture
                var airPcap = device as AirPcapDevice;
                airPcap.Open(SharpPcap.WinPcap.OpenFlags.DataTransferUdp, readTimeoutMilliseconds);
            }
            else if(device is WinPcapDevice)
            {
                var winPcap = device as WinPcapDevice;
                winPcap.Open(SharpPcap.WinPcap.OpenFlags.DataTransferUdp | SharpPcap.WinPcap.OpenFlags.NoCaptureLocal, readTimeoutMilliseconds);
            }
            else if (device is LibPcapLiveDevice)
            {
                livedevice = device;
                var livePcapDevice = device as LibPcapLiveDevice;
                livePcapDevice.Open(DeviceMode.Normal, readTimeoutMilliseconds);
            }
            else
            {
                throw new System.InvalidOperationException("unknown device type of " + device.GetType().ToString());
            }
        
        }

        /// <SUMMARY>
        /// Dumps each received packet to a pcap file
        /// 
        /// </SUMMARY>
        private static void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            // write the packet to the file
            captureFileWriter.Write(e.Packet);
        }

        /// <summary>
        /// Close the pcap file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void device_PcapOnCaptureStopped(object sender, CaptureStoppedEventStatus status)
        {
            // close the pcap file
            captureFileWriter.Close();
        }
        
        public void Start(string strFile)
        {
           // livedevice.Filter = "host" + livedevice;
            string file = strFile;
            // open the output file
            captureFileWriter = new CaptureFileWriterDevice(livedevice, file);
            // Start the capturing process
            livedevice.StartCapture();         
        }

        public void Stop()
        {

            // Stop the capturing process
            if (livedevice.Started)
            {
                livedevice.StopCapture();
            }
                                    
            // Close the pcap device
            //livedevice.Close();
        }
    }
}
