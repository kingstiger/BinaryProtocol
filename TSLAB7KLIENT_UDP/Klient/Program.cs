using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections;
using System.Threading;

namespace Klient

{
    
    static class SMTH
    {


        static public BitArray ToBitArray(byte[] p)
        {
            BitArray wynik = new BitArray(0);
            foreach (byte b in p)
            {
                byte[] t = new byte[1];
                t[0] = b;
                BitArray temp = new BitArray(t);
                int exx = b;
                temp = temp.Odwroc();
                wynik = wynik.Append(temp);
            }
            return wynik;
        }

        public static BitArray Odwroc(this BitArray bits)
        {
            BitArray stib = new BitArray(bits.Length, false);
            for (int i = 0; i < bits.Length; i++)
            {
                stib[i] = bits[bits.Length - i - 1];
            }
            return stib;
        }
        
        public static BitArray Append(this BitArray current, BitArray after)
        {
            bool[] bools = new bool[current.Count + after.Count];
            current.CopyTo(bools, 0);
            after.CopyTo(bools, current.Count);
            return new BitArray(bools);
        }
    
        public static BitArray NewStrToBitArr(this string text)
        {
            BitArray bits;
            byte[] p = Encoding.ASCII.GetBytes(text);
            bits = ToBitArray(p);
            return bits;
        }
        
        public static byte[] BitToByte(this BitArray bits)
        {
            BitArray temp;
            byte[] wynik;
            if (bits.Length % 8 != 0)
            {
                temp = new BitArray(bits.Length % 8, false);
                temp = temp.Append(bits);
            }
            else
            {
                temp = bits;
            }
            wynik = new byte[temp.Length / 8];
            int byteIndex = 0, bitIndex = 0;
            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                    wynik[byteIndex] |= (byte)(1 << (7 - bitIndex));

                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }
            return wynik;
        }

    }

    
    
    public class Klient
    {
        UdpClient udpklient;
        public string adres_serwera = "";
        IPAddress IPserwera;
        IPEndPoint EP, EPto;
        public BitArray received, tosend;
        public byte[] recv;
        public PDU FROMRECEIVED;
        public int rec;
        public string input = "";
        static List<PDU> toBeSent = new List<PDU>();

        static string s = "";
        bool p = true, ending = true;

        Klient ()
        {
            Console.WriteLine("Wpisz adres serwera, z ktorym bedziesz sie laczyc:");
            adres_serwera = Console.ReadLine();
            IPserwera = IPAddress.Parse(adres_serwera);
            EP = new IPEndPoint(IPserwera, 8080);
            EPto = new IPEndPoint(IPAddress.Any, 0);
            recv = new byte[1024];
            udpklient = new UdpClient(27015, EP.AddressFamily);
            FROMRECEIVED = new PDU("", "", 0, "", "");
            received = new BitArray(0);
            toBeSent.Add(new PDU("-recvid", "", 0, "", ""));
        }
        static PDU RunCommand()
        {

            string command = "";
            while (command == "")
            {
                command = Console.ReadLine();
            }
            if(command == "tak" || command == "nie")
            {
                s = command;
            }
            PDU toSend = new PDU("", "", 0, "", "");
            BitArray output = new BitArray(0);
            string temp = "", data;
            int i = 0;

            
            for (i = 0; i < command.Length; i++)
            {
                if (command[i] == ' ') break;
                temp = temp + command[i];
            }
            ++i;

            if (temp == "-ping")
            {
                toSend.SetOp("-ping"); //100
                temp = "";

                if (command[i] == '-') i++;
                for (; i<command.Length; ++i)
                {
                    temp = temp + command[i];
                }
                toSend.SetAns("");
                toSend.SetSessionId(temp);
                toSend.SetData("");

            }
            else if (temp == "-invite")
            {
                toSend.SetOp("-invite"); //001;
                temp = "";
                if (command[i] == '-') i++;
                for (; i < command.Length; ++i)
                {
                    temp = temp + command[i];
                }
                data = temp;
                toSend.SetSessionId(data);
                toSend.SetAns("");
                toSend.SetData("");
            }
            else if (temp == "-msg")
            {
                toSend.SetOp("-msg"); //010
                temp = "";

                if (command[i] == '-') i++;
                for (; command[i] != ' '; ++i)
                {
                    temp = temp + command[i];
                }
                ++i;

                toSend.SetSessionId(temp);

                temp = "";

                if (command[i] == '-') i++;
                for (; i < command.Length; ++i)
                {
                    temp = temp + command[i];
                }

                toSend.SetAns("");
                toSend.SetData(temp);
                

                //dalsze operacje ustalające tablicę bitów...
                
            }
            else if (temp == "-end")
            {
                toSend.SetOp("-end"); //011
                temp = "";

                if (command[i] == '-') i++;
                for (; i < command.Length; ++i)
                {
                    temp = temp + command[i];
                }

                toSend.SetSessionId(temp);
                toSend.SetData("");
                toSend.SetAns("");
                //dalsze operacje ustalające tablicę bitów...
                
            }
            return toSend;
        }

        //PDUtoBitArr -> konwersja programowego pakietu na tablice bitow
        //BitToByte -> konwersja tablicy bitow na dajace sie wyslac bajty
        //ToBitArray -> konwersja odebranych bajtow na tablice bitow
        //BitArrtoPDU -> konwersja tablicy bitow na latwiejszy do odczytania pakiet

        public void Sending() // metoda ktora wysyla
        {

            byte[] buffer;
            if (toBeSent[0].GetOp() != "" && toBeSent.Count > 0)
            {
                buffer = toBeSent[0].PDUtoBitArr().BitToByte();
                udpklient.Send(buffer, buffer.Length, EP);
                Console.WriteLine("Wyslano:");
                toBeSent[0].PrintPDU();
            }
            if (toBeSent[0].GetOp() == "-end" && toBeSent[0].GetAns() == "")
            {
                ending = false;
            }
            toBeSent.RemoveAt(0);


        }

        public void Odbieranie()
        {
            while (true)
            {
                recv = null;
                recv = udpklient.Receive(ref EPto); //odbior (obojetnie skad)
                received = SMTH.ToBitArray(recv); //odebrane bajty -> tablica bitow
                FROMRECEIVED = FROMRECEIVED.BitArrtoPDU(received); //wrzucam ja do PDU
                Console.WriteLine("Otrzymano:"); //wypisuje co wyszlo, tak kontrolnie
                FROMRECEIVED.PrintPDU();


                if (recv.Length > 0) //jesli mam cokolwiek to tu wchodze
                {
                    if (FROMRECEIVED.GetOp() == "-msg" && FROMRECEIVED.GetAns() == "OK")
                    {
                        Console.WriteLine("Host " + FROMRECEIVED.GetSessionId() + " odebral wiadomosc"); //biore ID z "TOSEND", bo skoro tam wysylam, to musze miec jakies id, do ktorego wysylam
                    }
                    else if (FROMRECEIVED.GetOp() == "-msg" && FROMRECEIVED.GetAns() == "")
                    {
                        Console.WriteLine("Nowa wiadomosc:");
                        Console.WriteLine(FROMRECEIVED.GetData());
                        toBeSent.Add(new PDU("-msg", "OK", 0, "", FROMRECEIVED.GetSessionId()));
                        Sending();
                    }

                    if (FROMRECEIVED.GetOp() == "-ping")
                    {
                        if (FROMRECEIVED.GetAns() == "OK")
                        {
                            Console.WriteLine("Host " + FROMRECEIVED.GetSessionId() + " jest dostepny");
                        }
                        else if (FROMRECEIVED.GetAns() == "")
                        {
                            toBeSent.Add(new PDU("-ping", "OK", 0, "", ""));
                            Sending();
                        }
                        else if (FROMRECEIVED.GetAns() == "HOST_NOT_AVA")
                        {
                            Console.WriteLine("Host " + FROMRECEIVED.GetSessionId() + "nie jest dostepny");
                        }
                    }

                    if (FROMRECEIVED.GetOp() == "-invite")
                    {
                        if (FROMRECEIVED.GetAns() == "OK")
                        {
                            Console.WriteLine("Host " + FROMRECEIVED.GetSessionId() + " zgadza sie na komunikacje");
                        }
                        else if (FROMRECEIVED.GetAns() == "NOK")
                        {
                            Console.WriteLine("Host " + FROMRECEIVED.GetSessionId() + " nie zgadza się na komunikacje");
                        }
                        else if (FROMRECEIVED.GetAns() == "")
                        {
                            bool temp = true;
                            Console.WriteLine("Host " + FROMRECEIVED.GetSessionId() + " zaprasza do komunikacji, zgodzic sie? (tak/nie)");
                            while (temp)
                            {
                                if (s == "")
                                    s = Console.ReadLine();
                                if (s == "tak")
                                {
                                    toBeSent.Add(new PDU("-invite", "OK", 0, "", FROMRECEIVED.GetSessionId()));

                                    temp = false;
                                    Console.WriteLine("Serio, zgadzamy sie na komunikacje? :(");
                                    Console.WriteLine("(Pytanie retoryczne, nie klikaj nic!");
                                    Sending();
                                }
                                else if (s == "nie")
                                {
                                    toBeSent.Add(new PDU("-invite", "NOK", 0, "", FROMRECEIVED.GetSessionId()));
                                    temp = false;
                                    Console.WriteLine("yay, nie zgadzamy sie na komunikacje! :D");
                                    Sending();
                                }
                                else
                                {
                                    Console.WriteLine("Zla komenda, chcesz sie zgodzic? (tak/nie)");
                                    s = "";
                                }
                                
                            }
                        }
                    }

                    if (FROMRECEIVED.GetOp() == "-end")
                    {
                        if (FROMRECEIVED.GetAns() == "OK")
                        {
                            Console.WriteLine("Host " + FROMRECEIVED.GetSessionId() + " zgodzil sie zakonczyc komunikacje");
                            
                        }
                        else if (FROMRECEIVED.GetAns() == "")
                        {
                            Console.WriteLine("Host " + FROMRECEIVED.GetSessionId() + " konczy komunikacje");
                            toBeSent.Add(new PDU("-end", "OK", 0, "", FROMRECEIVED.GetSessionId()));
                            
                        }
                    }
                }
            }
        }

        

       static public void Okno()
        {
            Klient klient = new Klient();
            Console.WriteLine("Klient rozpoczal dzialanie");
            char t;
            Thread odbjur = new Thread(klient.Odbieranie);
            
            while (klient.p)
            {

                Console.WriteLine("a) zrob cos, zacznij dzialac, masz cale zycie przed soba");
                Console.WriteLine("b) wyjdz");
                t = (char)Console.Read();
                switch(t)
                {
                    case 'a':
                        odbjur.Start();
                        while (klient.ending)
                        {
                            if (toBeSent.Count > 0)
                                klient.Sending();
                            else if (toBeSent.Count == 0)
                            {
                                PDU cmd;
                                Console.WriteLine("No dawaj, napisz do niego");
                                cmd = RunCommand();
                                if (cmd.GetOp() == "-end")
                                    klient.ending = false;
                                toBeSent.Add(cmd);
                            }
                        }
                        odbjur.Join();
                        Console.WriteLine("Koniec komunikacji, mozesz isc wypic kawe :) ");
                        break;
                    case 'b':
                        Console.WriteLine("Na pewno chcesz wyjsc? (T/N)");
                        char w = (char)Console.Read();
                        if(w == 'T' || w == 't')
                        {
                            klient.p = false;
                            break;
                        } else if (w == 'N' || w == 'n')
                        {
                            continue;
                        }
                        break;
                }
            }
        }

        static void Main(string[] args)
        {
            Okno();

        }
    }
}
