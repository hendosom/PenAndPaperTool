using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// StateObject-Klasse
    /// Status der Objekte...
    /// ... für das Lesen der Client-Daten
    /// ... ASYNCHRON
    /// </summary>
    public class StateObject
    {
        public Socket workSocket = null; // Client socket
        public const int BufferSize = 1024; // Größe des erhaltenen Buffers
        public byte[] buffer = new byte[BufferSize]; // Receive Buffer
        public StringBuilder sb = new StringBuilder(); // Erhaltener DatenString
    }
}

//Fragen zum klären:
// Was ist ein Socket?
// Was ist ein Buffer?
// Wofür brauch man beides?
