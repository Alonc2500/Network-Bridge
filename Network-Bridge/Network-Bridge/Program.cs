﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using PcapDotNet.Packets.Arp;
using System.Net;
using System.IO;
using System.Net.Mail;
using System.Threading;

namespace Network_Bridge
{
    class Program
    {
        public static string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        private static int deviceNumber; //Global integer to pass on between threads
        private static IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine; // global list of all devices
        private static List<MyDevice> myDevices = new List<MyDevice>();


        //Capture thread
        public static void CaptureStarter()
        {
            // Take the selected adapter
            PacketDevice selectedDevice = allDevices[deviceNumber];

            myDevices.Add(new MyDevice(selectedDevice));

            // Open the device
            using (PacketCommunicator communicator =
                selectedDevice.Open(65536,                                  // portion of the packet to capture                                                                           // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000))                                  // read timeout
            {

                using (BerkeleyPacketFilter filter = communicator.CreateFilter(" "))
                {
                    // Set the filter
                    communicator.SetFilter(filter);
                }

                

                Console.WriteLine("Listening on device " + (deviceNumber + 1) + " out of " + allDevices.Count + " :  " + selectedDevice.Description + "...");

                // Start the capture
                communicator.ReceivePackets(0, PacketHandler);
                
            }
        }

        // Callback function invoked by Pcap.Net for every incoming packet
        private static void PacketHandler(Packet packet)
        {
            //Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" + packet.Length + "IP:" + packet.IpV4);
            if (packet.Ethernet.Arp != null)
            {
                CheckAddress(packet);
            }
        }

        private static void CheckAddress(Packet packet)
        {
            
            int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId; //fetch current thread ID
            string mac = packet.Ethernet.Source.ToString();
            Console.WriteLine("Device:" + threadID);

            foreach (MyDevice device in myDevices) //checks if it's a new device or if a mac should be added
            {
                if (device.ID == -1) //device not in list
                {
                    device.ID = threadID;
                    device.Addresses.Add(mac);
                }
                else
                {
                    if (device.ID == threadID) //found device
                    {
                        bool inList = false;
                        foreach (string address in device.Addresses)
                        {
                            if (address.Equals(mac))
                            {
                                inList = true;
                            }
                        }

                        if (!inList)
                        {
                            device.Addresses.Add(mac); // if device mac not in list, adds it
                        }
                    }
                }
            }
        }
        

        static void Main(string[] args)
        {

            //Opens thread for every device, to capture traffic from all devices.
            Thread[] recievers = new Thread[allDevices.Count];
            Thread.Sleep(100);
            for (int i = 0; i < allDevices.Count; i++)
            {
                deviceNumber = i;                               // sets global integer to device number to pass it on to the right thread
                recievers[i] = new Thread(CaptureStarter);     //creates thread
                recievers[i].Start();                           // starts thread
                Thread.Sleep(100);                               //thread sleeps for a while to let the just opened thread to finish it's initialisation
            }
        }


    }
}



/*
                
                for (int i = 0; i < allDevices.Count; i++)
                {
                    if (lists[i][0] == null && mac!="Broadcast")
                    {
                        lists[i][0] = mac;
                        lists[i][1] = packet.Ethernet.Source.ToString();
                    }
                    else
                    {
                        if(lists[i][0]==mac )
                        {
                            
                        }
                    }
                    
                }
                
                 if (packet.IpV4.Icmp != null)
            {
                string mac = packet.Ethernet.Source.ToString();
            }
               */