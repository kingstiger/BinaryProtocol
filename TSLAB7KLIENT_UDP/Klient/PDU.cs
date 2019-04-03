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
    public class PDU
    {
        //Wszystkie pola są stworzone z myślą o bezpośrednie skonwertowanie ich do postaci tablicy bitów
        private bool[] Op = new bool[3];
        private bool[] Ans = new bool[3];
        private UInt32 DataLength; //Dłguość danych wyrażamy w BITACH!!!!
        private string Data;
        private string SessionId;   //Maks. 3 znaki

        //Settery, gettery i inne pierdoły...

        PDU(bool[] OPE, bool[] ANSWE, UInt32 DL, string D, string SID)
        {
            this.Op = OPE;
            this.Ans = ANSWE;
            this.DataLength = DL;
            this.Data = D;
            this.SessionId = SID;
        }

        public PDU(string Operation, string Answer, UInt32 DataLength, string Data, string SessionId)
        {
            SetOp(Operation);
            SetAns(Answer);


            SetData(Data);
            this.SessionId = SessionId;
        }

        public void PrintPDU()
        {
            Console.WriteLine("*****PDU*****");
            Console.WriteLine("OPERACJA: " + GetOp());
            Console.WriteLine("OPOWIEDZ: " + GetAns());
            Console.WriteLine("DL. DANYCH: " + GetDataLength());
            Console.WriteLine("DANE: " + GetData());
            Console.WriteLine("USTAWIONE ID SESJI: " + GetSessionId());
            Console.WriteLine("**************");
        }

        public void SetOp(string text) //Metoda ustawiająca pole operacji na podstawie komendy w bashu
        {
            if (text == "-ping")
            {
                Op[0] = true;
                Op[1] = false;
                Op[2] = false;
            }
            else if (text == "-invite")
            {
                Op[0] = false;
                Op[1] = false;
                Op[2] = true;
            }
            else if (text == "-msg")
            {
                Op[0] = false;
                Op[1] = true;
                Op[2] = false;
            }
            else if (text == "-end")
            {
                Op[0] = false;
                Op[1] = true;
                Op[2] = true;
            }
            else if (text == "-recvid")
            {
                Op[0] = true;
                Op[1] = true;
                Op[2] = true;
            }
            else
            {
                Op[0] = false;
                Op[1] = false;
                Op[2] = false;
            }
        }

        public string GetData()
        {
            return Data;
        }

        public string GetOp()   //Metoda zwracająca nazwę operacji, jeżeli kod był błędny, zwraca pustego stringa
        {
            bool[] ping = { true, false, false };
            bool[] invite = { false, false, true };
            bool[] msg = { false, true, false };
            bool[] end = { false, true, true };
            bool[] recvid = { true, true, true };

            if (Op.SequenceEqual(ping))
                return "-ping";

            if (Op.SequenceEqual(invite))
                return "-invite";

            if (Op.SequenceEqual(msg))
                return "-msg";

            if (Op.SequenceEqual(end))
                return "-end";

            if (Op.SequenceEqual(recvid))
                return "-recvid";

            else return "";
        }

        
        
        public void SetAns(string text) //Metoda ustawiająca pole odpowiedzi
        {
            if (text == "OK")
            {
                Ans[0] = true;
                Ans[1] = true;
                Ans[2] = true;
            }
            else if (text == "HOST_NOT_AVA")
            {
                Ans[0] = true;
                Ans[1] = false;
                Ans[2] = true;
            }
            else if (text == "NOK")
            {
                Ans[0] = true;
                Ans[1] = false;
                Ans[2] = false;
            }
            else
            {
                Ans[0] = false;
                Ans[1] = false;
                Ans[2] = false;
            }
        }

        public string GetAns() //Metoda zwracająca nazwę odpowiedzi, jeżeli kod był błędny, zwraca pustego stringa
        {
            bool[] OK = { true, true, true };
            bool[] NOK = { true, false, false };
            bool[] HOST_NOT_AVA = { true, false, true };

            if (Ans.SequenceEqual(OK))
                return "OK";

            if (Ans.SequenceEqual(NOK))
                return "NOK";

            if (Ans.SequenceEqual(HOST_NOT_AVA))
                return "HOST_NOT_AVA";

            else return "";
        }


        public string NewBitArrToStr(BitArray bits)
        {
            string text;
            byte[] p = bits.BitToByte();
            text = Encoding.ASCII.GetString(p);
           
           return text;
        }


        public byte GetDataLength()
        {
            return (byte)DataLength;
        }

        private void SetDataLength()    //Metoda ustawiająca wartość pola długości danych
        {
            if (Data == "" || Data == null)
            {
                DataLength = (UInt32)0;
            }
            else
            {
                DataLength = (UInt32)Data.Length * 8;
            }
        }

        public void SetData(string text) //Metoda ustawiająca pole z danymi na podstawie podanego stringa
        {
            Data = text;
            if (text == "" || text == null)
            {
                Data = "";
            }
            SetDataLength();
        }

        public void SetSessionId(string text) //Metoda ustawiająca pole z id sesji
        {
            if (text.Length != 3)
                SessionId = "dft";  //Od słowa default (tak w razie czego, jak serwer wygeneruje złe id sesji
            else
                SessionId = text;
        }

        public string GetSessionId()
        {
            return SessionId;
        }

        public BitArray PDUtoBitArr() //konwersja pakietu na tablice bitow
        {
            bool[] t = new bool[6];
            for (int i = 0; i < 3; i++)
            {
                t[i] = Op[i];
                t[i + 3] = Ans[i];
            }
            
            BitArray temp = new BitArray(t);
            BitArray temp3 = new BitArray(Data.NewStrToBitArr());
            BitArray temp4 = new BitArray(SessionId.NewStrToBitArr());
            BitArray wynik = new BitArray(0, false);
            
            int[] exx = new int[1];
            exx[0] = (int)DataLength;
            BitArray temp2 = new BitArray(exx);

            wynik = wynik.Append(temp);
            if (DataLength == 0 || Data == "") //dlugosc pola DataLength zawsze rowna 32
            {

                BitArray ar = new BitArray(32, false);
                wynik = wynik.Append(ar);
            }
            else
            {
                temp2 = temp2.Odwroc();
                wynik = wynik.Append(temp2);
                wynik = wynik.Append(temp3);
            }
            if (SessionId == "" || SessionId == null) //dlugosc pola id_sesji zawsze rowna 24 (3 znaki UTF8/ASCII)
            {
                BitArray ar = new BitArray(24, false);
                wynik = wynik.Append(ar);
            }
            else
                wynik = wynik.Append(temp4);
            
            return wynik;
        }

        static public bool[] BitToBool(BitArray p)
        {
            int p2 = p.Length;
            bool[] ex = new bool[p2];
            for(int i = 0; i < p.Length; i++)
            {
                ex[i] = p[i];
            }
            return ex;
        }

        public PDU BitArrtoPDU(BitArray t)
        {
            bool[] oper = new bool[3];
            bool[] answer = new bool[3];
            bool[] data_lB = new bool[32];
            UInt32 data_l = 0;
            string data = "", sesid = "";
            int i = 0;
            if (t.Length != 0)
            {
                for (i = 0; i < 3; i++)
                {
                    oper[i] = t[i];
                    answer[i] = t[i + 3];
                }
                if (t.Length > 8)
                {
                    for (i = 6; i < 38; i++)
                    {
                        data_lB[i - 6] = t[i];
                    }
                    BitArray exx = new BitArray(data_lB);
                    exx = exx.Odwroc();
                    data_lB = BitToBool(exx);
                    data_l = (UInt32)BitToInt(data_lB);

                    if (data_l == 0)
                    {
                        data = "";
                    }
                    else
                    {
                        BitArray newBitarr = new BitArray((int)data_l, false);

                        for (i = 38; i < (int)data_l + 38; i++)
                        {
                            newBitarr[i - 38] = t[i];     //   -> ZMIANA
                        }

                        data = NewBitArrToStr(newBitarr);
                    }

                    int temp = t.Length - (int)data_l;
                    BitArray newBitarr2 = new BitArray(24, false);
                    int j = 0;

                    for (i = (int)data_l + 38; j < 24; i++)
                    {
                        newBitarr2[j] = t[i];
                        j++;
                    }
                   

                    sesid = NewBitArrToStr(newBitarr2);
                }
                PDU pdu = new PDU(oper, answer, data_l, data, sesid);
                return pdu;
            }
            else
            {
                PDU pdu = new PDU("", "", 0, "", "");
                return pdu;
            }
        }

        public int BitToInt(bool[] p)
        {
            int i = 1, wynik = 0;
            for (int j = 0; j < p.Length; j++)
            {
                if (p[j] == true)
                {
                    wynik += i;
                }
                i *= 2;
            }
            return wynik;
        }
    }

}
