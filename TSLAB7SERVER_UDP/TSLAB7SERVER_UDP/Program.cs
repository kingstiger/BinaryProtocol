using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections;

namespace TSLAB7SERVER_UDP
{
	class Program
	{
		public static List<Task> tasks = new List<Task>();
		public static List<Host> hosts = new List<Host>();
        public static Task z_ID_do_odeslania;
		public static UdpClient server = new UdpClient(8080);

		public static void SendToHost(Host host, PDU pdu)
		{
			byte[] buffer = pdu.ToByteArray();
            /*foreach(byte i in buffer)
            {
                Console.Write(i + " ");
            }
            Console.WriteLine();*/
			server.Send(buffer, buffer.Length, host.Endpoint);
		}

		public static void SendToHost(IPEndPoint host, PDU pdu)
		{
			byte[] buffer = pdu.ToByteArray();
			server.Send(buffer, buffer.Length, host);
		}

        public static void PrintPDU(PDU pdu)
        {
            if(pdu.GetData() == "")
            Console.WriteLine("{0}, {1}, {2}", pdu.GetOp(), pdu.GetAns(), pdu.GetSessionId());

            else
            Console.WriteLine("{0}, {1}, {2}, {3}", pdu.GetOp(), pdu.GetAns(), pdu.GetSessionId(), pdu.GetData());
        }

		public static bool IsHostNew(IPEndPoint endpoint)
		{
            string socket = endpoint.Address.ToString() + ':' + endpoint.Port.ToString();
            foreach (Host h in hosts)
            {
                if (socket == h.Endpoint.Address.ToString() + ':' + h.Endpoint.Port.ToString())
                    return false;
            }
			return true;
		}

        public static bool IsHostNew(string sesid)
        {
            foreach(Host h in hosts)
            {
                if (h.GetSessionId() == sesid)
                    return false;
            }
            return true;
        }

        public static int FindHostIndex(IPEndPoint endpoint)
		{
            string socket = endpoint.Address.ToString() + ':' + endpoint.Port.ToString();
            for (int i = 0; i < hosts.Count; ++i)
			{
				if (socket == hosts[i].Endpoint.Address.ToString() + ':' + hosts[i].Endpoint.Port.ToString())
					return i;
			}

			return -1;
		}

		public static int FindHostIndex(string sessid)
		{
			for (int i = 0; i < hosts.Count; ++i)
			{
				if (hosts[i].GetSessionId() == sessid)
					return i;
			}

			return -1;
		}

        /*public static void PrintBuf(byte[] buffer)
        {
            foreach (byte b in buffer)
                Console.Write(b);

            Console.WriteLine();
        }*/

        public static void ExecTask(Task t)
        {
            int index;

            if (IsHostNew(t.from))
            {
                if (t.pdu.GetOp() == "-recvid") //Prośba o przydzielenie sessid
                {
                    Host temp = new Host(t.from);
                    hosts.Add(temp);
                    t.pdu.SetAns("OK");

                    index = FindHostIndex(t.from);
                    if (index != -1)
                    {
                        t.pdu.SetSessionId(hosts[index].GetSessionId());
                        SendToHost(hosts[index], t.pdu);
                        Console.WriteLine("EXECTASK> Nowy klient wysłał zapytanie o przydzielenie sesji, i przydzielono mu Id -> " + hosts[index].GetSessionId());
                        PrintPDU(t.pdu);
                        Console.WriteLine("EXECTASK> Zostało wysłane!\n\n");
                        return;
                    }

                    PrintPDU(t.pdu);
                    return;
                }
                else //Inna opreacja niż recvid
                {
                    t.pdu.SetAns("NOK");
                    SendToHost(t.from, t.pdu);
                    Console.WriteLine("EXECTASK> Nowy klient wysłał zapytanie, ale nie ma ID sesji!");
                    PrintPDU(t.pdu);
                    Console.WriteLine("EXECTASK> Zostało wysłane!\n\n");
                    return;
                }


            } //Sprawdzanie, czy klient jest nowy

            if (t.pdu.GetOp() == "-recvid") //Prośba o przydzielenie sessid dla istniejącego hosta
            {
                t.pdu.SetAns("NOK");
                SendToHost(t.from, t.pdu);
                Console.WriteLine("EXECTASK> Ten klient ma już ID sesji!");
            }

            if (t.pdu.GetOp() == "-invite")  //Host 1 zaprasza hosta 2 do komunikacji
            {
                if (t.pdu.GetAns() == "")
                {
                    index = FindHostIndex(t.pdu.GetSessionId());

                    if (index == -1) //Próba zaproszenia hosta który nie ma id sesji
                    {
                        t.pdu.SetAns("NOK");
                        SendToHost(t.from, t.pdu);

                        Console.WriteLine("EXECTASK> Klient wyslal zaproszenie do nieistniejacego hosta!");
                    }
                    else //Poprawne zaproszenie
                    {
                        z_ID_do_odeslania = new Task(t.pdu, t.from);
                        t.pdu.SetSessionId(hosts[FindHostIndex(t.from)].GetSessionId());
                        SendToHost(hosts[index], t.pdu);
                        Console.WriteLine("EXECTASK> Klient wyslal zaproszenie do hosta " + hosts[index].GetSessionId());
                    }
                }

                else if (t.pdu.GetAns() == "OK") //Klient przyjął zaproszenie
                {
                    index = FindHostIndex(t.pdu.GetSessionId());
                    t.pdu.SetSessionId(hosts[FindHostIndex(t.from)].GetSessionId());
                    SendToHost(hosts[index], t.pdu);
                }
                else if (t.pdu.GetAns() == "NOK") //Klient odrzucił zaproszenie
                {
                    index = FindHostIndex(hosts[FindHostIndex(t.from)].GetSessionId());
                    t.pdu.SetSessionId(hosts[FindHostIndex(t.from)].GetSessionId());
                    SendToHost(hosts[index], t.pdu);
                }
            }


            if (t.pdu.GetOp() == "-end")
            {
                if (t.pdu.GetAns() == "")
                {
                    index = FindHostIndex(t.pdu.GetSessionId());
                    if (index == -1)
                    {
                        Console.WriteLine("EXECTASK> Klient " + hosts[FindHostIndex(t.from)].GetSessionId() +
                           " wyslał wiadomość do nieistniejącego hosta!");
                        t.pdu.SetAns("HOST_NOT_AVA");
                        t.pdu.SetSessionId("");
                        SendToHost(t.from, t.pdu);
                    }
                    else
                    {
                        z_ID_do_odeslania = new Task(t.pdu, t.from);
                        t.pdu.SetSessionId(hosts[FindHostIndex(t.from)].GetSessionId());

                        //hosts.RemoveAt(FindHostIndex(t.from));
                        SendToHost(hosts[index], t.pdu);
                    }
                } else if (t.pdu.GetAns() == "OK")
                {

                    index = FindHostIndex(hosts[FindHostIndex(z_ID_do_odeslania.from)].GetSessionId());
                    t.pdu.SetSessionId(hosts[FindHostIndex(t.from)].GetSessionId());
                    SendToHost(hosts[index], t.pdu);

                }
            }

            if (t.pdu.GetOp() == "-ping")
            {
                if (t.pdu.GetAns() == "")
                {
                    index = FindHostIndex(t.pdu.GetSessionId());
                    if (index == -1)
                    {
                        Console.WriteLine("EXECTASK> Klient " + hosts[FindHostIndex(t.from)].GetSessionId() +
                           " wyslał wiadomość do nieistniejącego hosta!");
                        t.pdu.SetAns("HOST_NOT_AVA");
                        t.pdu.SetSessionId("");
                        SendToHost(t.from, t.pdu);
                    }
                    else
                    {
                        z_ID_do_odeslania = new Task(t.pdu, t.from);
                        t.pdu.SetSessionId(hosts[FindHostIndex(t.from)].GetSessionId());
                        SendToHost(hosts[index], t.pdu);
                    }
                }
                else if (t.pdu.GetAns() == "OK")
                {
                    index = FindHostIndex(hosts[FindHostIndex(z_ID_do_odeslania.from)].GetSessionId());
                    t.pdu.SetSessionId(hosts[FindHostIndex(t.from)].GetSessionId());
                    SendToHost(hosts[index], t.pdu);
                }
            }

            if (t.pdu.GetOp() == "-msg")
            {
                if (t.pdu.GetAns() == "") //Klient wysłał wiadomość do drugiego klienta
                {
                    index = FindHostIndex(t.pdu.GetSessionId());

                    if (index == -1) //Klient wysłał wiadomość do nieistniejącej sesji
                    {
                        Console.WriteLine("EXECTASK> Klient " + hosts[FindHostIndex(t.from)].GetSessionId() +
                            " wyslał wiadomość do nieistniejącego hosta!");
                        t.pdu.SetAns("HOST_NOT_AVA");
                        t.pdu.SetSessionId("");
                        SendToHost(t.from, t.pdu);
                    }

                    else
                    {
                        z_ID_do_odeslania = new Task(t.pdu, t.from);
                        t.pdu.SetSessionId(hosts[FindHostIndex(t.from)].GetSessionId());
                        SendToHost(hosts[index], t.pdu);
                    }
                }


                if (t.pdu.GetAns() == "OK")  //Potwierdzenie wiadomości
                {
                    index = FindHostIndex(t.pdu.GetSessionId());
                    t.pdu.SetSessionId(hosts[FindHostIndex(t.from)].GetSessionId());
                    SendToHost(hosts[index], t.pdu);
                }
            }

            PrintPDU(t.pdu);
            Console.WriteLine("EXECTASK> Zostało wysłane!\n\n");
        }


		public static void Recieve()
		{
			PDU pdu = new PDU();
			BitArray bits;
			byte[] buffer = new byte[1024];

            while (true)
			{
				IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                Console.WriteLine("RECIEVE> Oczekiwanie na dane...");
				buffer = server.Receive(ref from);
                Console.WriteLine("RECIEVE> Odebrano bajtow: " + buffer.Length);

                if (buffer.Length > 0)
                {
                    bits = Bits.ToBitArray(buffer);
                    pdu.FromBitArray(bits);
                    tasks.Add(new Task(pdu, from));
                    Console.WriteLine("RECIEVE> Dodano zadanie!");
                    PrintPDU(pdu);
                }
			}
		}

		static void Main()
		{
			Console.WriteLine("MAIN> Serwer uruchomiony!");
            Thread recieve = new Thread(Recieve);
            recieve.Start();

            while (true)
			{
				if(tasks.Count > 0)
				{
                    Console.WriteLine("MAIN> Nowe zadanie!");
					ExecTask(tasks.First());
					tasks.RemoveAt(0);
				}
			}
		}
	}
}
