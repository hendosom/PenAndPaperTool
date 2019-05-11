using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    /// <summary>
    /// Statusobjekte für empfangene Daten vom verbundenen Gerät
    /// </summary>
    class StateObject
    {
        //an den workSocket wird die Verbindung zum Server (in diesem Fall) übergeben.
        public Socket workSocket = null;
        public const int BufferSize = 256;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }
}
