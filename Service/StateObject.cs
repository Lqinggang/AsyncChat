using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Service
{
    public class StateObject
    {
        public Socket socket = null;
        public const int bufferSizes = 1024;
        public byte[] buffer = new byte[bufferSizes];
        public StringBuilder sb = new StringBuilder();
    }

}
