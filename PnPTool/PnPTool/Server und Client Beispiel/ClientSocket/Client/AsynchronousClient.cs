using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class AsynchronousClient
    {
        //Portnummer für den Server. Wird unten angeben.
        static int port = 0;

        //Die ManualResetEvents legen fest, ob ein Signal erfasst wurde oder nicht => Stati: "signalisiert" / "nicht signalisiert"
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        //Hier wird später die Antwort des Servers eingetragen.
        private static String response = String.Empty;

        public AsynchronousClient()
        {
            StartClient();
        }

        private static void StartClient() {
            //Verbindung zu einem anderen Gerät (Hier: Unser eingerichteter Server)
            try
            {
                //Eingabe des Ports
                Console.WriteLine("Geben Sie den Port an: ");
                port = Convert.ToInt32(Console.ReadLine());

                //Verbindung zu einem anderen Endgerät erschaffen
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName()); //Diese Zeile Gilt nur für diesen Computer. Sonst anderen Hostname eintragen
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                //Erstellen eines TCP/IP Socket
                Socket client = new Socket(
                    ipAddress.AddressFamily, 
                    SocketType.Stream, 
                    ProtocolType.Tcp);

                //Verbindungsherstellung zum Remote device
                client.BeginConnect(
                    remoteEP, 
                    new AsyncCallback(ConnectCallback), 
                    client);
                connectDone.WaitOne();

                //Testdaten zum remote device senden
                
                
                    Send(
                        client,
                        "Hello World <EOF>");
                    sendDone.WaitOne();



                    //Einfach zum warten...
                    //Console.WriteLine("Press Enter to go on...");
                    //Console.Read();

                    //Erhalten der Antwort vom remote device
                    Receive(client);
                    receiveDone.WaitOne();
                
                //Ausgabe der erhaltenen Antwort
                Console.WriteLine("Response received: {0}", response);

                //Freigabe des Sockets
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "Stack Trace: " + e.StackTrace.ToString());
            }

        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                //Übergabe des Sockets
                Socket client = (Socket)ar.AsyncState;

                //Verbindung wird abgeschlossen (Vollstedige Verbindung sollte dann vorhanden sein)
                client.EndConnect(ar);

                //Ausgabe, wohin der Socket connected ist -> MAC adresse oder DNS?
                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());

                //Das Event "connectDone" wird gesetzt auf "signalisiert"
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "Stack Trace: " + e.StackTrace.ToString());
            }

        }

        private static void Receive(Socket client)
        {
            try
            {
                //Der workSocket wird festgelegt (Erstellen des Statusobjekts)
                StateObject state = new StateObject();
                state.workSocket = client;

                //Begin mit dem erhalten der Daten des remote Device 
                client.BeginReceive(state.buffer, 0 , StateObject.BufferSize,0,new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "Stack Trace: " + e.StackTrace.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                //Erhalten des Statusobjekts und des Client socketsvom asynchronen Statusobjekt
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                //Daten des Remotedevice werden gelesen
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    //Speichern der bereits erhaltenen Daten -> Falls mehr kommen!
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    //Rest der Daten bekommen
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);

                }
                else
                {
                    //Alle Daten sind angekommen -> Antwort in response
                    if (state.sb.Length > 1)
                        response = state.sb.ToString();
                    //Status des Erhaltens wird auf "signalisiert" gesetzt
                    receiveDone.Set();
                }  
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "Stack Trace: " + e.StackTrace.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            //Stringdaten codiert in Bytedata mit ASCII
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            //Daten werden zum RemoteDevice gesendet
            client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);

        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                //Erhaltend es Sockets vom Statusobjekt
                Socket client = (Socket)ar.AsyncState;

                //Vervollständigen der Datensendung zum RemoteDevice
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                //Status des Sendens wird gesetzt
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "Stack Trace: " + e.StackTrace.ToString());
            }
        }
    }
}
