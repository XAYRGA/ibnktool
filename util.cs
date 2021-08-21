using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;

namespace ibnktool
{
    public static class util
    {
        public static bool consoleProgress_quiet = false;
        public static void consoleProgress(string txt, int progress, int max, bool show_progress = false)
        {
            if (consoleProgress_quiet)
                return;
            var flt_total = (float)progress / max;
            Console.CursorLeft = 0;
            //Console.WriteLine(flt_total);
            Console.Write($"{txt} [");
            for (float i = 0; i < 32; i++)
                if (flt_total > (i / 32f))
                    Console.Write("#");
                else
                    Console.Write(" ");
            Console.Write("]");
            if (show_progress)
                Console.Write($" ({progress}/{max})");           
        }
        public static int padTo(BeBinaryWriter bw, int padding)
        {
            int del = 0; 
            while (bw.BaseStream.Position % padding != 0)
            {
                bw.BaseStream.WriteByte(0x00);
                bw.BaseStream.Flush();
                del++;
            }
            return del;
        }

        public static int[] readInt32Array(BeBinaryReader binStream, int count)
        {
            var b = new int[count];
            for (int i = 0; i < count; i++)
                b[i] = binStream.ReadInt32();
            return b;
        }

        public static int padToInt(int Addr, int padding)
        {
            var delta = (int)(Addr % padding);
            return (padding - delta);        
        }

    }
}
