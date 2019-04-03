using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSLAB7SERVER_UDP
{
	static class Bits
    {
        static public BitArray ToBitArray(byte[] p)
        {
            BitArray wynik = new BitArray(0);
            foreach (byte b in p)
            {
                byte[] t = new byte[1];
                t[0] = b;
                BitArray temp = new BitArray(t);
                wynik = wynik.Append(temp.odwroc());
            }
            return wynik;
        }

        /*public static BitArray Prepend(this BitArray current, BitArray before)
		{
			var bools = new bool[current.Count + before.Count];
			before.CopyTo(bools, 0);
			current.CopyTo(bools, before.Count);
			return new BitArray(bools);
		}*/

		public static BitArray Append(this BitArray current, BitArray after)
		{
			var bools = new bool[current.Count + after.Count];
			current.CopyTo(bools, 0);
			after.CopyTo(bools, current.Count);
			return new BitArray(bools);
		}

        /*public static BitArray ToBitArrayPierwotnie(this string text)
        {
            string s;
            Encoding encoding = new UnicodeEncoding();
            s = string.Join("", encoding.GetBytes(text).Select(n => Convert.ToString(n, 2).PadLeft(8, '0')));
            var res = new BitArray(s.Select(c => c == '1').ToArray());
            return res;
        }
        */

     
     

        public static BitArray NewStrToBitArr(this string text)
        {
            BitArray bits;
            byte[] p = Encoding.ASCII.GetBytes(text);
            bits = ToBitArray(p);
            return bits;
        }
        
        /*public static BitArray ToBitArray(this string text)
        {
            string s;
            BitArray bits = new BitArray(0, false);
            Encoding encoding = new ASCIIEncoding();

            s = string.Join("", encoding.GetBytes(text).Select(n => Convert.ToString(n, 2).PadLeft(8, '0')));

            bits.Length = s.Length;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '1')
                {
                    bits[i] = true;
                }
                else
                {
                    bits[i] = false;
                }
            }
            return bits;
        }*/

        public static BitArray odwroc(this BitArray penis)
        {
            BitArray sinep = new BitArray(penis.Length, false);
            for (int i = 0; i < penis.Length; i++)
            {
                sinep[i] = penis[penis.Length - 1 - i];
            }
            return sinep;
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
        /*
        public static void PrintBits(this BitArray bits)
        {
            foreach(var b in bits)
                Console.Write(b);

            Console.WriteLine();
        }*/
	}
}
