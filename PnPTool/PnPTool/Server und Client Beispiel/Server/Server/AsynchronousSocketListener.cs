using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class AsynchronousSocketListener
    {
        //Thread -> Prozess / Ausführungsstrang / - reihenfolge
        //Nicht vererbare Klasse -> Wartet auf den Eintritt eines Signals, um es abzuarbeiten
        //das "false" legt fest, ob ein Signal "signalisiert" wurde. In diesem Fall suchen wir noch, also gibt es kein signalisiertes Signal.
        //Thread Signal
        public static ManualResetEvent allDone = new ManualResetEvent(false);


        //Konstruktor. -> Startet das Listening!
        public AsynchronousSocketListener() {
            Console.WriteLine("DNS deines Computers: " + Dns.GetHostName().ToString());
            Console.WriteLine("Enter to start Listening");
            Console.Read();

            StartListening(); }

        /// <summary>
        /// Start des Listenings
        /// </summary>
        public static void StartListening()
        {
            // Lokaler Endpukt wird hergestellt
            // DNS-Name des Computers
            // run the Listener "GerritThinkPad"
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());   //-> IPHostEntry: stellt ContainerKlasse für Adressinformationen von Internethosts bereit.
                                                                            //-> GetHostEntry: löst einen Hostnamen/IP-Adresse eine IPHostEntry Instanz.
                                                                            //-> GetHostName: ruft den Hostnamen des lokalen Computers ab

            IPAddress ipAddress = ipHostInfo.AddressList[0];                //-> IPAddress: stellt eine IP bereit
                                                                            //-> AddressList[]: Ruft oder legt eine liste von IP-Adressen ab, die einem host zugeordnet sind

            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);    //-> IPEndPoint: Stellt einen Netzwerkpunkt als IP-Adresse und Portnummer dar

            //Erstellen eines TCP/IP socket
            Socket listener = new Socket(ipAddress.AddressFamily,           //-> Socket: Implementiert die Berkeley-Socket-Schnittstelle (siehe: Socket)
                SocketType.Stream, ProtocolType.Tcp);                       //-> AddressFamily: Ruft die Addressfamilie der IP-Adresse ab (siehe: Addressfamily)
                                                                            //-> SocketType: Gibt den Typ des Sockets an, der eine Socket-Instanz darstellt
                                                                            //-> Stream: Unterstützt zuverlässige bidirektionale, verbindungsbasierte Bytestreams
                                                                            //           ohne die Duplizierung von Daten und ohne Beibehaltung von Grenzen.
                                                                            //           Ein Socket dieses Typs kommuniziert mit einem einzigen Peer und erfordert
                                                                            //           eine Verbindung vor dem Beginn der Kommunikation. (siehe: Peer)
                                                                            //           Stream verwendet das Transmission Control-Protokoll (TCP) und InterNetwork.AddressFamily
                                                                            //-> ProtocolType: Gibt die Protokolle an, die die Socket-Klasse unterstützt
                                                                            //-> TCP: Transmission Control-Protocol
                                                                     
            int testZähler = 0;


            //Lokaler Endpunkt wird an den Socket gebunden
            //Start des Listenings für incoming connections (eingehende Verbindungen)
            try
            {
                listener.Bind(localEndPoint);                               //-> Bind: Ordnet einem Socket einen lokalen Endpunkt zu
                listener.Listen(100);                                       //-> Listen(int backlog): Stelle einen Socket in einen Wartezustand (siehe: Backlog)

                while (true)
                {
                    //Event wird auf einen "nicht signalisierten" Status gesetzt)
                    allDone.Reset();                                        //-> .Reset: Zustand des Ereignisses wird auf "nicht signalisiert festgelegt,
                                                                            //           sodass Threads blockiert werden können.
                    //Start des asynchronen Sockets um für Verbindungen zu hören
                    Console.WriteLine("Waiting for a connection...");
                    testZähler++;
                    Console.WriteLine(testZähler);
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), //-> .BeginAccept: Beginnt einen asynchronen Vorgang, um eine eingehende Verbindung anzunehmen
                        listener);                                          //-> new AsyncCallback: Verweit auf eine Methode, die aufgerufen wird, sobald ein 
                                                                            //                      asynchroner Vorgang abgeschlossen ist.

                    allDone.WaitOne();                                      //-> .WaitOne: Blockiert den aktuellen Thread, damit das WaitHandle ein Signal empfängt
                                                                            //             WaitHandle: Kapselt betriebssystemspezifische Objekte, die auf exklusiven Zugriff auf gemeinsam genutzte Ressourcen warten.
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + " " + e.StackTrace.ToString());
            }
            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }
        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signalisiert dem MainThread, dass er fortfahren kann
            allDone.Set();                                                  //-> .Set: Legt den Zustand des Ereignisses aus "signalisiert" fest und ermöglicht so
                                                                            //         einem oder mehreren Threads das Fortfahren

            //Get Socket, der die Client Anfrage behandelt
            Socket listener = (Socket)ar.AsyncState;                        //-> AsyncState: Ruft ein benutzerdefiniertes Objekt ab, das einen asynchronen Vorgang
                                                                            //               qualifiziert oder Informationen darüber enthält
                                                                            
            Socket handler = listener.EndAccept(ar);                        //-> EndAccept: Nimmt eingehenden Verbindungsversuch asynchron an und erstellt
                                                                            //              ein neueen Socket remote-Host-Communication Handler (Falsche Übersetzung?)

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer,                              //-> BeginReceive: Beginnt asynchronen Datenempfang aus verbundenem Socket. 
                0, 
                StateObject.BufferSize,
                0,                                                          //-> socketFlags: Gibt das Verhalten Beim Senden/Empfangen von Sockets an (0 = None) -> https://docs.microsoft.com/de-de/dotnet/api/system.net.sockets.socketflags?view=netframework-4.7.2
                new AsyncCallback(ReadCallback), 
                state);
        }

        public static void ReadCallback(IAsyncResult ar)                    //-> IAsyncResult: Stellt den Status eines asynchronen Vorgangs dar
        {
            String content = String.Empty;


            //Abrufen des Objektstatus und SocketHandlers
            //des asynchronen Statusobjekts
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            //Lesen der Daten des Client Socket
            int bytesRead = handler.EndReceive(ar);                         //-> .EndReceive: Beendet einen ausstehenden Lesevorgang 

            if (bytesRead > 0)
            {
                //Zwischenspeichern der Daten
                //Falls es mehr gibt, damit nichts verloren geht
                state.sb.Append(Encoding.ASCII.GetString(state.buffer,      //-> .Append: Fügt eine Kopie der angegebenen Zeichenfolge an diese Instanz an
                    0, bytesRead));                                         //-> ASCII: Zeichencodierung
                                                                            //-> GetString: Die Bytefolge aus dem angegebenen Array wird in einen String umgewandelt
                
                //Es wird nach der File-Endung gesucht (In diesem Fall <EOF>)
                //Gibt es das nicht, wird nach mehr Daten gesucht
                content = state.sb.ToString();

                if (content.IndexOf("<EOF>") > -1)
                {
                    //Alle Daten wurden gelesen.
                    //Diese werden auf der Konsole ausgegeben
                    Console.WriteLine("Read {0} bytes from socket. \n Data: {1}", content.Length, content);
                    //Die Daten werden zu dem Clienten zurückgesendet (ECHO)
                    Send(handler, content);

                }
                else
                {
                    //Nicht alle Daten wurden empfangen. GET MOOOORE
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }
        }
        public static void Send(Socket handler, String data)
        {
            //Daten aus dem String werden in Bytes mit ASCII codiert
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            //Beginnen mit der Rücksendung zum verbundenen Gerät
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                //Erhalten des Sockets des Statusobjekts
                Socket handler = (Socket)ar.AsyncState;

                //Rücksenden der Daten zum verbundenen Gerät
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);


                handler.Shutdown(SocketShutdown.Both);                        //-> Shutdown: Beendet Senden und Empfangen
                handler.Close();                                              //-> Close: Schließt die Socket Verbindung und gibt alle Ressourcen frei
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + " ", e.StackTrace.ToString());
            }
        }
    }
}
