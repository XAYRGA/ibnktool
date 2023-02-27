using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Be.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ibnktool
{
    public class IBNKProjectV2
    {
        public string version;
        public uint globalID;
        public string InstrumentManifsest = "instruments.json";
        public string OscillatorManifest = "oscillator.json";
        public string RandomEffectsManifest = "randeff.json";
        public string SensorEffectsManifest = "senseff.json";
        public string EnvelopesManifest = "env.json";
        public string PercussionMapsManifest = "percmap.json";
        public string PercussionsManifest = "percussions.json";
        public string List = "_list.json";

    }

    class IBNKUnpackerv2
    {

        public Dictionary<int, string> Instruments = new Dictionary<int, string>();
        public Dictionary<int, string> Oscillators = new Dictionary<int, string>();
        public Dictionary<int, string> RandomEffects = new Dictionary<int, string>();
        public Dictionary<int, string> SensorEffects = new Dictionary<int, string>();
        public Dictionary<int, string> Envelopes = new Dictionary<int, string>();
        public Dictionary<int, string> PercussionMaps = new Dictionary<int, string>();
        public Dictionary<int, string> Percussions = new Dictionary<int, string>();
        public Dictionary<int, string> List = new Dictionary<int, string>();

        private Dictionary<JInstrumentv2, string> ListPathLookup = new Dictionary<JInstrumentv2, string>(); // Weirdest list i've made to date. 

        public void unpackV2(string output, InstrumentBankv2 bank)
        {
            var w = new IBNKProjectV2();
            w.version = "JAUDIO_V2";
            w.globalID = (uint)bank.id;


            Directory.CreateDirectory($"{output}/Instruments/");
            Directory.CreateDirectory($"{output}/Oscillators/");
            Directory.CreateDirectory($"{output}/Envelopes/");
            Directory.CreateDirectory($"{output}/RandomEffects/");
            Directory.CreateDirectory($"{output}/SensorEffects/");
            Directory.CreateDirectory($"{output}/PercussionMaps/");
            Directory.CreateDirectory($"{output}/Percussions/");


            for (int i = 0; i < bank.Instruments.Length; i++)
            {
                if (bank.Instruments[i] != null)
                {
                    File.WriteAllText($"{output}/Instruments/INST_{i}.json", JsonConvert.SerializeObject(bank.Instruments[i], Formatting.Indented));
                    Instruments[i] = $"Instruments/INST_{i}.json";
                    ListPathLookup[bank.Instruments[i]] = $"Instruments/INST_{i}.json";
                }
                util.consoleProgress("Unpacking Instruments", i + 1, bank.Instruments.Length, true);
            }
            Console.WriteLine();

            for (int i = 0; i < bank.Oscillators.Length; i++)
            {
                if (bank.Oscillators[i] != null)
                {
                    File.WriteAllText($"{output}/Oscillators/OSCI_{i}.json", JsonConvert.SerializeObject(bank.Oscillators[i], Formatting.Indented));
                    Oscillators[i] = $"Oscillators/OSCI_{i}.json";
                }
                util.consoleProgress("Unpacking Oscillators", i + 1, bank.Oscillators.Length, true);

            }
            Console.WriteLine();

            for (int i = 0; i < bank.Envelopes.Length; i++)
            {
                if (bank.Envelopes[i] != null)
                {
                    File.WriteAllText($"{output}/Envelopes/ENV_{i}.json", JsonConvert.SerializeObject(bank.Envelopes[i], Formatting.Indented));
                    Envelopes[i] = $"Envelopes/ENV_{i}.json";
                }
                util.consoleProgress("Unpacking Envelopes", i + 1, bank.Envelopes.Length, true);
            }
            Console.WriteLine();

            for (int i = 0; i < bank.RandEffects.Length; i++)
            {
                if (bank.RandEffects[i] != null)
                {
                    File.WriteAllText($"{output}/RandomEffects/RAND_{i}.json", JsonConvert.SerializeObject(bank.RandEffects[i], Formatting.Indented));
                    RandomEffects[i] = $"RandomEffects/RAND_{i}.json";
                }
                util.consoleProgress("Unpacking RandEffects", i + 1, bank.RandEffects.Length, true);
            }
            Console.WriteLine();

            for (int i = 0; i < bank.SenseEffects.Length; i++)
            {
                if (bank.SenseEffects[i] != null)
                {
                    File.WriteAllText($"{output}/SensorEffects/SENS_{i}.json", JsonConvert.SerializeObject(bank.SenseEffects[i], Formatting.Indented));
                    SensorEffects[i] = $"SensorEffects/SENS_{i}.json";
                }
                util.consoleProgress("Unpacking SenseEffects", i + 1, bank.SenseEffects.Length, true);
            }
            Console.WriteLine();
            for (int i = 0; i < bank.PercussionMaps.Length; i++)
            {
                if (bank.PercussionMaps[i] != null)
                {
                    File.WriteAllText($"{output}/PercussionMaps/PMAP_{i}.json", JsonConvert.SerializeObject(bank.PercussionMaps[i], Formatting.Indented));
                    PercussionMaps[i] = $"PercussionMaps/PMAP_{i}.json";
            
                }
                util.consoleProgress("Unpacking PercussionMaps", i + 1, bank.PercussionMaps.Length, true);
            }
            Console.WriteLine();

            for (int i = 0; i < bank.Percussions.Length; i++)
            {
                if (bank.Percussions[i] != null)
                {
                    File.WriteAllText($"{output}/Percussions/PERC_{i}.json", JsonConvert.SerializeObject(bank.Percussions[i], Formatting.Indented));
                    Percussions[i] = $"Percussions/PERC_{i}.json";
                    ListPathLookup[bank.Percussions[i]] = $"Percussions/PERC_{i}.json";
                }
                util.consoleProgress("Unpacking Percussions", i + 1, bank.Percussions.Length, true);
            }

            Console.WriteLine();

            for (int i=0; i < bank.List.Length; i++)
            {
                var cLI = bank.List[i];
                if (cLI == null )
                {
                    List[i] = null;
                    continue;
                }
                var str = ListPathLookup[cLI];
                List[i] = str; 
            }

            File.WriteAllText($"{output}/{w.InstrumentManifsest}", JsonConvert.SerializeObject(Instruments, Formatting.Indented));
            File.WriteAllText($"{output}/{w.OscillatorManifest}", JsonConvert.SerializeObject(Oscillators, Formatting.Indented));
            File.WriteAllText($"{output}/{w.RandomEffectsManifest}", JsonConvert.SerializeObject(RandomEffects, Formatting.Indented));
            File.WriteAllText($"{output}/{w.SensorEffectsManifest}", JsonConvert.SerializeObject(SensorEffects, Formatting.Indented));
            File.WriteAllText($"{output}/{w.EnvelopesManifest}", JsonConvert.SerializeObject(Envelopes, Formatting.Indented));
            File.WriteAllText($"{output}/{w.PercussionsManifest}", JsonConvert.SerializeObject(Percussions, Formatting.Indented));
            File.WriteAllText($"{output}/{w.PercussionMapsManifest}", JsonConvert.SerializeObject(PercussionMaps, Formatting.Indented));
            File.WriteAllText($"{output}/{w.List}", JsonConvert.SerializeObject(List, Formatting.Indented));

            Console.WriteLine();
            Console.WriteLine("Writing ibnk.json");
            File.WriteAllText($"{output}/ibnk.json", JsonConvert.SerializeObject(w, Formatting.Indented));

            Console.WriteLine("\nDone");
        }
    }





}
