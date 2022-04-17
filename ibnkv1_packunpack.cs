using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Be.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ibnktool
{
    public class IBNKProjectV1
    {
        public string version;
        public uint globalID;
        public Dictionary<int, string> includes = new Dictionary<int, string>();
    }
    class IBNKUnpacker
    {
        public void unpackV1(string output, JInstrumentBankv1 bank)
        {
            var w = new IBNKProjectV1();
            w.version = "JAUDIO_V1";
            w.globalID = bank.globalID;

            Directory.CreateDirectory($"{output}/include/");

            for (int i = 0; i < bank.instruments.Length; i++)
            {
                if (bank.instruments[i] != null)
                {
                    File.WriteAllText($"{output}/include/Inst_{i}.json", JsonConvert.SerializeObject(bank.instruments[i], Formatting.Indented));
                    w.includes[i] = $"include/Inst_{i}.json";
                }
                util.consoleProgress("Unpacking IBNK", i + 1, bank.instruments.Length, true);
            }

            Console.WriteLine();
            Console.WriteLine("Writing ibnk.json");
            File.WriteAllText($"{output}/ibnk.json", JsonConvert.SerializeObject(w, Formatting.Indented));

            Console.WriteLine("\nDone");
        }
    }



    public class IBNKPacker
    {

        public void packV1(IBNKProjectV1 prj, string baseFolder, BeBinaryWriter output)
        {
            var newIBNK = new JInstrumentBankv1();
            newIBNK.globalID = prj.globalID;
            newIBNK.instruments = new JInstrument[0xF0];

            var x = 0;
            foreach (KeyValuePair<int, string> inc in prj.includes)
            {
                x++;
                util.consoleProgress("Loading instruments...\t\t\t", x, prj.includes.Count, true);
                var pjC = File.ReadAllText($"{baseFolder}/{inc.Value}");
                var Ins = JsonConvert.DeserializeObject<JToken>(pjC);
                if ((bool)Ins["Percussion"] == true)
                    newIBNK.instruments[inc.Key] = JsonConvert.DeserializeObject<JPercussion>(pjC);
                else
                    newIBNK.instruments[inc.Key] = JsonConvert.DeserializeObject<JStandardInstrumentv1>(pjC);
            }
            Console.WriteLine();

            newIBNK.WriteToStream(output);



            var envelopeHashes = reduceEnvelopes(newIBNK);
            foreach (KeyValuePair<uint, JInstrumentEnvelopev1> entity in envelopeHashes)
            {
                util.padTo(output, 32);
                entity.Value.mBaseAddress = (int)output.BaseStream.Position;
                entity.Value.WriteToStream(output);
            }
            reduceEnvelopeReferences(newIBNK, envelopeHashes);


            var oscHashes = reduceOscillator(newIBNK);
            foreach (KeyValuePair<uint, JInstrumentOscillatorv1> entity in oscHashes)
            {
                util.padTo(output, 32);
                entity.Value.mBaseAddress = (int)output.BaseStream.Position;
                entity.Value.WriteToStream(output);
            }
            reduceOscillatorReferences(newIBNK, oscHashes);
            util.padTo(output, 32);
            writeAllVelocityRegions(newIBNK, output);
            util.padTo(output, 32);
            writeAllKeyRegions(newIBNK, output);

            util.padTo(output, 32);
            for (int i = 0; i < newIBNK.instruments.Length; i++)
            {
                if (newIBNK.instruments[i] != null)
                {
                    util.padTo(output, 32);
                    newIBNK.instruments[i].mBaseAddress = (int)output.BaseStream.Position;
                    if (!newIBNK.instruments[i].Percussion)
                        ((JStandardInstrumentv1)newIBNK.instruments[i]).WritetoStream(output);

                }
            }
            //--------- Percussion

            for (int i = 0; i < newIBNK.instruments.Length; i++)
            {
                if (newIBNK.instruments[i] != null)
                {
                    if (newIBNK.instruments[i].Percussion)
                    {
                        var wb = (JPercussion)(newIBNK.instruments[i]);
                        if (wb == null)
                            continue;
                        for (int xx = 0; xx < wb.Sounds.Length; xx++)
                        {
                            if (wb.Sounds[xx] == null)
                                continue;
                            util.padTo(output, 32);
                            wb.Sounds[xx].mBaseAddress = (int)output.BaseStream.Position;
                            wb.Sounds[xx].WriteToStream(output);
                        }
                    }
                }
            }

            util.padTo(output, 32);

            output.Flush();


            for (int i = 0; i < newIBNK.instruments.Length; i++)
            {
                if (newIBNK.instruments[i] != null)
                {
                    util.padTo(output, 32);

                    if (newIBNK.instruments[i].Percussion)
                    {
                        newIBNK.instruments[i].mBaseAddress = (int)output.BaseStream.Position;
                        ((JPercussion)newIBNK.instruments[i]).WriteToStream(output);

                    }
                }
            }


            util.padTo(output, 32); // final padding
            newIBNK.size = (uint)output.BaseStream.Length;

            output.BaseStream.Position = 0;
            newIBNK.WriteToStream(output);
            output.Flush();
        }



        public void writeAllVelocityRegions(JInstrumentBankv1 bank, BeBinaryWriter wr)
        {
            for (int regret = 0; regret < bank.instruments.Length; regret++)
            {
                util.consoleProgress("Velocity Regions... \t\t\t", regret + 1, bank.instruments.Length, true);
                if (bank.instruments[regret] != null)
                {
                    var cInsTmp = bank.instruments[regret];
                    if (!cInsTmp.Percussion)
                    {
                        var insStd = (JStandardInstrumentv1)cInsTmp;
                        for (int kr = 0; kr < insStd.keys.Length; kr++)
                        {
                            var vrs = insStd.keys[kr];
                            for (int lkr = 0; lkr < vrs.Velocities.Length; lkr++)
                            {
                                var VELR = vrs.Velocities[lkr];
                                VELR.mBaseAddress = (int)wr.BaseStream.Position;
                                VELR.WriteToStream(wr);
                                wr.Flush();
                            }
                        }
                    }
                    else
                    {
                        var insStd = (JPercussion)cInsTmp;
                        for (int kr = 0; kr < insStd.Sounds.Length; kr++)
                        {
                            var vrs = insStd.Sounds[kr];
                            if (vrs == null)
                                continue;
                            for (int lkr = 0; lkr < vrs.Velocities.Length; lkr++)
                            {
                                var VELR = vrs.Velocities[lkr];
                                VELR.mBaseAddress = (int)wr.BaseStream.Position;
                                VELR.WriteToStream(wr);
                                wr.Flush();
                            }
                        }
                    }
                }
            }
            Console.WriteLine();
        }

        public void writeAllKeyRegions(JInstrumentBankv1 bank, BeBinaryWriter wr)
        {
            for (int regret = 0; regret < bank.instruments.Length; regret++)
            {
                util.consoleProgress("Key Regions...     \t\t\t", regret + 1, bank.instruments.Length, true);
                if (bank.instruments[regret] != null)
                {
                    var cInsTmp = bank.instruments[regret];
                    if (!cInsTmp.Percussion)
                    {
                        var insStd = (JStandardInstrumentv1)cInsTmp;
                        for (int kr = 0; kr < insStd.keys.Length; kr++)
                        {
                            var VELR = insStd.keys[kr];
                            VELR.mBaseAddress = (int)wr.BaseStream.Position;
                            VELR.WriteToStream(wr);
                            wr.Flush();
                        }
                    }
                    else
                    {
                        var insStd = (JPercussion)cInsTmp;
                        for (int kr = 0; kr < insStd.Sounds.Length; kr++)
                        {
                            var vrs = insStd.Sounds[kr];
                            if (vrs == null)
                                continue;
                            for (int lkr = 0; lkr < vrs.Velocities.Length; lkr++)
                            {
                                var VELR = vrs.Velocities[lkr];
                                VELR.mBaseAddress = (int)wr.BaseStream.Position;
                                VELR.WriteToStream(wr);
                                wr.Flush();
                            }
                        }
                    }
                }
            }
            Console.WriteLine();
        }


        public void reduceOscillatorReferences(JInstrumentBankv1 bank, Dictionary<uint, JInstrumentOscillatorv1> hashtable)
        {
            for (int i = 0; i < bank.instruments.Length; i++)
            {
                util.consoleProgress("Reducing oscrefs...\t\t\t", i + 1, bank.instruments.Length, true);
                var inst = bank.instruments[i];
                if (inst != null && inst.Percussion == false)
                {
                    var realIns = (JStandardInstrumentv1)inst;
                    if (realIns.oscillatorA != null)
                    {
                        realIns.oscillatorA = hashtable[realIns.oscillatorA.mHash];
                    }
                    if (realIns.oscillatorB != null)
                    {
                        realIns.oscillatorB = hashtable[realIns.oscillatorB.mHash];
                    }
                }
            }
            Console.WriteLine();
        }


        public void reduceEnvelopeReferences(JInstrumentBankv1 bank, Dictionary<uint, JInstrumentEnvelopev1> hashtable)
        {
            for (int i = 0; i < bank.instruments.Length; i++)
            {
                util.consoleProgress("Reducing envRefs...\t\t\t", i + 1, bank.instruments.Length, true);
                var inst = bank.instruments[i];
                if (inst != null && !inst.Percussion)
                {
                    var realIns = (JStandardInstrumentv1)inst;
                    if (realIns.oscillatorA != null)
                    {
                        realIns.oscillatorA.Attack = hashtable[realIns.oscillatorA.Attack.mHash];
                        realIns.oscillatorA.Release = hashtable[realIns.oscillatorA.Release.mHash];
                    }
                    if (realIns.oscillatorB != null)
                    {
                        realIns.oscillatorB.Attack = hashtable[realIns.oscillatorB.Attack.mHash];
                        realIns.oscillatorB.Release = hashtable[realIns.oscillatorB.Release.mHash];
                    }
                }
            }
            Console.WriteLine();
        }



        public Dictionary<uint, JInstrumentOscillatorv1> reduceOscillator(JInstrumentBankv1 bank)
        {
            Dictionary<uint, JInstrumentOscillatorv1> oscHashes = new Dictionary<uint, JInstrumentOscillatorv1>();
            for (int i = 0; i < bank.instruments.Length; i++)
            {
                util.consoleProgress("Reducing oscillator...\t\t\t", i + 1, bank.instruments.Length, true);
                var inst = bank.instruments[i];
                if (inst != null && inst.Percussion == false)
                {
                    var realIns = (JStandardInstrumentv1)inst;
                    if (realIns.oscillatorA != null)
                    {
                        var atkHsh = getOscillatorHash(realIns.oscillatorA);
                        oscHashes[atkHsh] = realIns.oscillatorA;
                        realIns.oscillatorA.mHash = atkHsh;

                    }
                    if (realIns.oscillatorB != null)
                    {
                        var atkHsh = getOscillatorHash(realIns.oscillatorB);
                        oscHashes[atkHsh] = realIns.oscillatorB;
                        realIns.oscillatorB.mHash = atkHsh;
                    }
                }
            }
            foreach (KeyValuePair<uint, JInstrumentOscillatorv1> entity in oscHashes)
                entity.Value.mHash = entity.Key;

            Console.WriteLine();
            return oscHashes;

        }


        // FUCK THIS // 
        public Dictionary<uint, JInstrumentEnvelopev1> reduceEnvelopes(JInstrumentBankv1 bank)
        {
            Dictionary<uint, JInstrumentEnvelopev1> envelopeHashes = new Dictionary<uint, JInstrumentEnvelopev1>();
            for (int i = 0; i < bank.instruments.Length; i++)
            {
                util.consoleProgress("Reducing envelopes...\t\t\t", i + 1, bank.instruments.Length, true);
                var inst = bank.instruments[i];
                if (inst != null && inst.Percussion == false)
                {
                    var realIns = (JStandardInstrumentv1)inst;
                    if (realIns.oscillatorA != null)
                    {
                        var atkHsh = getEnvelopeHash(realIns.oscillatorA.Attack);
                        var relHsh = getEnvelopeHash(realIns.oscillatorA.Release);
                        realIns.oscillatorA.Attack.mHash = atkHsh;
                        realIns.oscillatorA.Release.mHash = relHsh;
                        envelopeHashes[atkHsh] = realIns.oscillatorA.Attack;
                        envelopeHashes[relHsh] = realIns.oscillatorA.Release;

                    }
                    if (realIns.oscillatorB != null)
                    {
                        var atkHsh = getEnvelopeHash(realIns.oscillatorB.Attack);
                        var relHsh = getEnvelopeHash(realIns.oscillatorB.Release);
                        realIns.oscillatorB.Attack.mHash = atkHsh;
                        realIns.oscillatorB.Release.mHash = relHsh;
                        envelopeHashes[atkHsh] = realIns.oscillatorB.Attack;
                        envelopeHashes[relHsh] = realIns.oscillatorB.Release;
                    }
                }
            }
            foreach (KeyValuePair<uint, JInstrumentEnvelopev1> entity in envelopeHashes)
                entity.Value.mHash = entity.Key;

            Console.WriteLine();
            return envelopeHashes;

        }

        private uint getOscillatorHash(JInstrumentOscillatorv1 env)
        {
            var ww = new MemoryStream(new byte[0x20]);
            var bw = new BeBinaryWriter(ww);
            env.WriteToStream(bw);
            ww.Position = 0;
            var bytes = new byte[0x20];
            ww.Read(bytes, 0, 0x20);
            var ret = crc32.ComputeChecksum(bytes);
            bw.Dispose();
            ww.Close();
            ww.Dispose();
            return ret;
        }

        private uint getEnvelopeHash(JInstrumentEnvelopev1 env)
        {
            var ww = new MemoryStream(new byte[0x40]);
            var bw = new BeBinaryWriter(ww);
            env.WriteToStream(bw);
            ww.Position = 0;
            var bytes = new byte[0x20];
            ww.Read(bytes, 0, 0x20);
            var ret = crc32.ComputeChecksum(bytes);
            bw.Dispose();
            ww.Close();
            ww.Dispose();
            return ret;
        }


    }


}
