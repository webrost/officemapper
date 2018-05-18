using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Web;

namespace OfficeMapper.Lib
{
    public class TcpHelper
    {
        public static bool PortIsAvailable(string ip, int port)
        {
            try
            {
                TcpClient client = new TcpClient(ip, port);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        }
    }

