using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using xayrga;
using Be.IO;

namespace ibnktool
{
    public class minimizedIBNK
    {
        public uint globalID;
    }
    class Program
    {
        static void Main(string[] args)
        {
            crc32.reset();


#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("!IBNKTOOL build in debug mode, do not push into release!");
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("dump email: bugs@xayr.ga");
            Console.ForegroundColor = ConsoleColor.Gray;
#endif

            var bb = File.OpenRead("53.bnk");
            var bw = new BeBinaryReader(bb);
            Console.WriteLine("Reading 53.bnk");
            Console.WriteLine("InstrumentBankV2 --> CreateFromStream()");
            var w = InstrumentBankv2.CreateFromStream(bw);
            Console.WriteLine($"Oscillators \t{w.Oscillators.Length}");
            Console.WriteLine($"Sensors \t{w.SenseEffects.Length}");
            Console.WriteLine($"RandEffs \t{w.RandEffects.Length}");
            Console.WriteLine($"Instruments \t{w.Instruments.Length}");
            Console.WriteLine($"PercRegions \t{w.PercussionMaps.Length}");
            Console.WriteLine($"Percussions \t{w.Percussions.Length}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Read successful.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.ReadLine();
            File.WriteAllText("test_ibnk.json", JsonConvert.SerializeObject(w, Formatting.Indented));

            if (true)
                return;
            cmdarg.cmdargs = args;

            var operation = cmdarg.assertArg(0, "Operation");
            var version = cmdarg.assertArg(1, "Version");

            util.consoleProgress_quiet = cmdarg.findDynamicFlagArgument("-quiet");

            if (operation=="unpack")
            {
                var file = cmdarg.assertArg(2, "IBNK File");
                var output = cmdarg.assertArg(3, "Output Folder");
                cmdarg.assert(!File.Exists(file), $"{file} not found.");
                var fh = File.OpenRead(file);
                var mr = new BeBinaryReader(fh);
                JInstrumentBankv1 bank = null;
                try {bank = JInstrumentBankv1.CreateFromStream(mr);}
                catch (Exception E){
#if DEBUG
                    Console.WriteLine(E.ToString());
#endif
                    cmdarg.assert($"Cannot deserialize IBNK\n\n{E.Message}");
                }

                    var unp = new IBNKUnpacker();
                unp.unpackV1(output, bank);              
            } else if ( operation=="pack")
            {

                var file = cmdarg.assertArg(2, "Project Folder");
                var output = cmdarg.assertArg(3, "Output File");

                cmdarg.assert(!File.Exists($"{file}/ibnk.json"), $"Cannot locate {file}/ibnk.json");
                var wl = File.ReadAllText($"{file}/ibnk.json");
                IBNKProjectV1 prj = null;
                try
                {
                    prj = JsonConvert.DeserializeObject<IBNKProjectV1>(wl);
                } catch (Exception E)
                {
                    cmdarg.assert($"Cannot deserialize project\n\n{E.Message}");
#if DEBUG 
                    Console.WriteLine(E.ToString());
#endif
                }

                var pck = new BeBinaryWriter(File.OpenWrite(output));
                var rpk = new IBNKPacker();
                rpk.packV1(prj, $"{file}", pck);

            } else
            {
                Console.WriteLine("ibnktool");
                Console.WriteLine("ibnktool <operation> <ibnk version> <....<");
                Console.WriteLine("ibnktool unpack 0 <input file> <output folder>");
                Console.WriteLine("ibnktool pack 0 <input folder> <output file>");
            }

        }
    }
}
