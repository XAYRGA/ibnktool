using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;
using Newtonsoft.Json;
namespace ibnktool
{
    internal class InstrumentBankv2
    {

        public int id;
        public int flags; 


        private const int PERC = 0x50455243; // Percussion Table
        private const int Perc = 0x50657263; // Percussion 
        private const int SENS = 0x53454E53; // Sensor effect
        private const int RAND = 0x52414E44; // Random Effect
        private const int OSCT = 0x4F534354; // OSCillator Table
        private const int INST = 0x494E5354; // INStrument Table
        private const int IBNK = 0x49424E4B; // Instrument BaNK
        private const int ENVT = 0x454E5654; // ENVelope Table
        private const int PMAP = 0x504D4150;
        private const int Pmap = 0x506D6170;
        private const int LIST = 0x4C495354;

        uint Boundaries = 0;
        private int iBase = 0;
        private int OscTableOffset = 0;
        private int EnvTableOffset = 0;
        private int RanTableOffset = 0;
        private int SenTableOffset = 0;
        private int InsTableOffset = 0;
        private int ListTableOffset = 0;
        private int PercTableOffset = 0;
        private int PmapTableOffset = 0;

        public JInstrumentEnvelopev2[] Envelopes = new JInstrumentEnvelopev2[0];
        public JInstrumentOscillatorv2[] Oscillators = new JInstrumentOscillatorv2[0];
        public JInstrumentRandEffectv2[] RandEffects = new JInstrumentRandEffectv2[0];
        public JInstrumentSenseEffectv2[] SenseEffects = new JInstrumentSenseEffectv2[0];
        
        private Dictionary<int, long> PointerMemory = new Dictionary<int, long>();

        private void storeWritebackPointer(BeBinaryWriter write,int id)
        {
            PointerMemory[id] = write.BaseStream.Position;
            write.Write(0xFFFFFFFF);
        }

        private void fillWritebackPointer(BeBinaryWriter write, int id, int pointer)
        {
            var anchor = write.BaseStream.Position;
            write.BaseStream.Position = PointerMemory[id];
            write.Write(pointer);
            write.BaseStream.Position = anchor;
        }
        
        private int findChunk(BeBinaryReader read, int chunkID, bool immediate = false)
        {
            if (!immediate) 
                read.BaseStream.Position = iBase;

            while (true)
            {
                var pos = (int)read.BaseStream.Position;
                var i = read.ReadInt32(); 
                if (i == chunkID) 
                    return pos; 
                else if (pos > (Boundaries))
                    return 0;
            }
        }


        private void loadFromStream(BeBinaryReader reader)
        {
            if (reader.ReadUInt32() != IBNK)
                throw new InvalidOperationException("Data is not IBNK");
            var ibnkSize = reader.ReadUInt32();
            Boundaries = ibnkSize;
            id = reader.ReadInt32();
            var flags = reader.ReadInt32();
            reader.ReadBytes(0x10);

            var origPos = reader.BaseStream.Position;
            EnvTableOffset = findChunk(reader, ENVT, true);
            OscTableOffset = findChunk(reader, OSCT, true);
            RanTableOffset = findChunk(reader, RAND, true);
            SenTableOffset = findChunk(reader, SENS, true);
            InsTableOffset = findChunk(reader, INST, true);
            PmapTableOffset = findChunk(reader, PMAP, true);
            PercTableOffset = findChunk(reader, PERC, true);
            ListTableOffset  = findChunk(reader, LIST, true);


            loadEnvelopes(reader, EnvTableOffset);
            loadOscillators(reader, OscTableOffset);
            loadRandEffs(reader, RanTableOffset);
            loadSensEffs(reader, SenTableOffset);
        }

        // No safety checks for validity of envelope structure due to lack of titles. Beware! 
        private void loadEnvelopes(BeBinaryReader rd, int envTableOffset)
        {
            rd.BaseStream.Position = envTableOffset;
            if (rd.ReadInt32() != ENVT)
                throw new Exception("Expected ENVT");
            var size = rd.ReadUInt32();
            var sectStart = rd.BaseStream.Position;
            var EnvStore = new Queue<JInstrumentEnvelopev2>();
         
            while ((rd.BaseStream.Position - sectStart) < size) 
                EnvStore.Enqueue(JInstrumentEnvelopev2.CreateFromStream(rd));
            
            Envelopes = new JInstrumentEnvelopev2[EnvStore.Count];
            for (int i = 0; i < Envelopes.Length; i++)
                Envelopes[i] = EnvStore.Dequeue();
        }

        private void loadOscillators(BeBinaryReader rd, int oscTableOffset)
        {
            rd.BaseStream.Position = oscTableOffset;
            if (rd.ReadInt32() != OSCT)
                throw new Exception("Expected OSCT");
            var size = rd.ReadInt32();
            var count = rd.ReadInt32();

            if (count == 0)
                return;
           
 
            Oscillators = new JInstrumentOscillatorv2[count];
            for (int i = 0; i < count; i++)
                Oscillators[i] = JInstrumentOscillatorv2.CreateFromStream(rd, 0);
        }

        private void loadRandEffs(BeBinaryReader rd, int randTableOffset)
        {
            rd.BaseStream.Position = randTableOffset;

            if (rd.ReadInt32() != RAND)
                throw new Exception("Expected RAND");
            var size = rd.ReadInt32();
            var count = rd.ReadInt32();

            if (count == 0)
                return;

            RandEffects = new JInstrumentRandEffectv2[count];
            for (int i = 0; i < count; i++)
                RandEffects[i] = JInstrumentRandEffectv2.CreateFromStream(rd);
        }


        private void loadSensEffs(BeBinaryReader rd, int sensTableOffset)
        {
            rd.BaseStream.Position = sensTableOffset;

            if (rd.ReadInt32() != SENS)
                throw new Exception("Expected RAND");
            var size = rd.ReadInt32();
            var count = rd.ReadInt32();

            if (count == 0)
                return;

            SenseEffects = new JInstrumentSenseEffectv2[count];
            for (int i = 0; i < count; i++)
                SenseEffects[i] = JInstrumentSenseEffectv2.CreateFromStream(rd);
        }

        public static InstrumentBankv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new InstrumentBankv2();
            b.loadFromStream(reader);
            return b;
        }
        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(IBNK);
            storeWritebackPointer(wr, IBNK);
            wr.Write(id);
            wr.Write(flags);
            wr.Write(new byte[0x10]); // padding 

            ///////~ ENVELOPES
            wr.Write(ENVT);
            {
                if (Envelopes.Length == 0)
                    wr.Write(0x0000000400000000);
                else
                {
                    storeWritebackPointer(wr, ENVT);
                    var anchor = wr.BaseStream.Position;
                    for (int i = 0; i < Envelopes.Length; i++)
                        Envelopes[i].WriteToStream(wr);
                    fillWritebackPointer(wr, ENVT, (int)(wr.BaseStream.Position - anchor));
                    util.padTo(wr, 0x04);
                }
            }

            wr.Write(OSCT);
            {
                if (Oscillators.Length == 0)
                    wr.Write(0x0000000400000000);
                else
                {
                    storeWritebackPointer(wr, ENVT);
                    var anchor = wr.BaseStream.Position;
                    for (int i = 0; i < Envelopes.Length; i++)
                        Envelopes[i].WriteToStream(wr);
                    fillWritebackPointer(wr, ENVT, (int)(wr.BaseStream.Position - anchor));
                    util.padTo(wr, 0x04);
                }
            }

        }
    }


    public class JInstrumentEnvelopev2
    {

        [JsonIgnore]
        public int mBaseAddress = 0;
        [JsonIgnore]
        public uint mHash = 0;

        public JEnvelopeVector[] points;
        public class JEnvelopeVector
        {
            public ushort Mode;
            public ushort Delay;
            public short Value;
        }

        private void loadFromStream(BeBinaryReader reader)
        {
        
            var origPos = reader.BaseStream.Position;
            mBaseAddress = (int)origPos;
            int count = 0;
            while (reader.ReadUInt16() < 0xB)
            {
                reader.ReadUInt32();
                count++;
            }


            count++;
            reader.BaseStream.Position = origPos;
            points = new JEnvelopeVector[count];
            for (int i = 0; i < count; i++)
                points[i] = new JEnvelopeVector { Mode = reader.ReadUInt16(), Delay = reader.ReadUInt16(), Value = reader.ReadInt16() };
        }
        public static JInstrumentEnvelopev2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentEnvelopev2();
            b.loadFromStream(reader);
            return b;
        }
        public void WriteToStream(BeBinaryWriter wr)
        {
            for (int i = 0; i < points.Length; i++)
            {
                wr.Write(points[i].Mode);
                wr.Write(points[i].Delay);
                wr.Write(points[i].Value);
            }
        }
    }


    public class JInstrumentRandEffectv2
    {
        private const int Rand = 0x52616E64;
        [JsonIgnore]
        public int mBaseAddress = 0;

        public byte Target;
        public float Floor;
        public float Ceiling;

        private void loadFromStream(BeBinaryReader reader)
        {
            mBaseAddress = (int)reader.BaseStream.Position;
            if (reader.ReadInt32() != Rand)
                throw new Exception("Expected 'Rand'");
            Target = reader.ReadByte();
            reader.ReadBytes(3);
            Floor = reader.ReadSingle();
            Ceiling = reader.ReadSingle();
        }
        public static JInstrumentRandEffectv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentRandEffectv2();
            b.loadFromStream(reader);
            return b;
        }
        
        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(Rand);
            wr.Write(Target);
            wr.Write(new byte[0x03]);
            wr.Write(Floor);
            wr.Write(Ceiling);
        }
    }




    public class JInstrumentSenseEffectv2
    {
        private const int Sens = 0x53656E73;
        [JsonIgnore]
        public int mBaseAddress = 0;

        public byte Target;
        public byte Register;
        public byte Key;
        public float Floor;
        public float Ceiling;

        private void loadFromStream(BeBinaryReader reader)
        {
            if (reader.ReadInt32() != Sens)
                throw new Exception("Expected 'Sens'");
            Target = reader.ReadByte();
            Register = reader.ReadByte();
            Key = reader.ReadByte();
            reader.ReadBytes(1);
            Floor = reader.ReadSingle();
            Ceiling = reader.ReadSingle();
        }
        public static JInstrumentSenseEffectv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentSenseEffectv2();
            b.loadFromStream(reader);
            return b;
        }
        
        public void WritetoStream(BeBinaryWriter wr)
        {
            wr.Write(Sens);
            wr.Write(Target);
            wr.Write(Register);
            wr.Write(Key);
            wr.Write((byte)0x00);
            wr.Write(Floor);
            wr.Write(Ceiling);
        }
    }

    public class JInstrumentOscillatorv2
    {
        // [00 00 00 00] target  3F 80 00 00 // 00 00 05 DC //  00 00 01 E0 // 3F 80 00 00 //  00 00 00 00 ///
        [JsonIgnore]
        public int mBaseAddress = 0;
        [JsonIgnore]
        public uint mHash = 0;

        private const int Osci = 0x4F736369; // Oscillator
        public byte Target;
        public float Rate;
        public int AttackEnvelopeID;
        public int ReleaseEnvelopeID;
        public float Width;
        public float Vertex;

        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
            mBaseAddress = (int)reader.BaseStream.Position;
            if (reader.ReadInt32() != Osci)
                throw new Exception("Expected Osci");
            Target = reader.ReadByte();
            reader.ReadBytes(3);
            Rate = reader.ReadSingle();
            AttackEnvelopeID = reader.ReadInt32();
            ReleaseEnvelopeID = reader.ReadInt32();
            Width = reader.ReadSingle();
            Vertex = reader.ReadSingle();
        }
        public static JInstrumentOscillatorv2 CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JInstrumentOscillatorv2();
            b.loadFromStream(reader, seekbase);
            return b;
        }

        public void WriteToStream(BeBinaryWriter wr)
        {
            mBaseAddress = (int)wr.BaseStream.Position;
            wr.Write(Osci);
            wr.Write(Target);
            wr.Write(new byte[0x3]);
            wr.Write(Rate);
            wr.Write(AttackEnvelopeID);
            wr.Write(ReleaseEnvelopeID);
            wr.Write(Width);
            wr.Write(Vertex);
        }

    }

    public class JInstrumentv2
    {
        private const int Inst = 0x496E7374; // Instrument

        [JsonIgnore]
        public int mBaseAddress = 0;

        public int[] OscillatorIndices;
        public int[] RandIndices;
        public JInstrumentKeyRegionv2[] Keys;
        public float Volume;
        public float Pitch;

        private void loadFromStream(BeBinaryReader reader)
        {
            if (reader.ReadInt32() != Inst)
                throw new Exception("Expected 'Inst'");
            var osciCount = reader.ReadInt32();
            OscillatorIndices = util.readInt32Array(reader, osciCount);

            var randCount = reader.ReadInt32();
            RandIndices = util.readInt32Array(reader, randCount);

            var keyRegCount = reader.ReadInt32();  
            Keys = new JInstrumentKeyRegionv2[keyRegCount];
            for (int i=0; i<keyRegCount; i++) 
                Keys[i] = JInstrumentKeyRegionv2.CreateFromStream(reader);

            Volume = reader.ReadSingle();
            Pitch = reader.ReadSingle();    
        }
        public static JInstrumentv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentv2();
            b.loadFromStream(reader);
            return b;
        }
        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(Inst);

            wr.Write(OscillatorIndices.Length);
            for (int i=0; i<OscillatorIndices.Length; i++)
                wr.Write(OscillatorIndices[i]);

            wr.Write(RandIndices.Length);
            for (int i=0; i<RandIndices.Length; i++)
                wr.Write(RandIndices[i]);

            wr.Write(Keys.Length);
            for (int j = 0; j < Keys.Length; j++)
                Keys[j].WriteToStream(wr);
            wr.Write(Volume);
            wr.Write(Pitch);
        }
    }

    public class JInstrumentKeyRegionv2
    {
        public byte BaseKey;
        public JInstrumentVelocityRegionv2[] Velocities;

        private void loadFromStream(BeBinaryReader reader)
        {
            BaseKey = reader.ReadByte();
            reader.ReadBytes(3);
            var count = reader.ReadInt32();
            Velocities = new JInstrumentVelocityRegionv2[count];
            for (int i = 0; i < count; i++) 
                Velocities[i] = JInstrumentVelocityRegionv2.CreateFromStream(reader);
        }
        public static JInstrumentKeyRegionv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentKeyRegionv2();
            b.loadFromStream(reader);
            return b;
        }

        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(BaseKey);
            for (int i=0; i <Velocities.Length; i++)
                Velocities[i].WriteToStream(wr);
        }

    }

    public class JInstrumentVelocityRegionv2
    {
        public byte Velocity;
        public short WSYSID;
        public short WAVEID;
        public float Volume;
        public float Pitch;

        private void loadFromStream(BeBinaryReader reader)
        {
           
            Velocity = reader.ReadByte();
            reader.ReadBytes(3);
            WSYSID = reader.ReadInt16();
            WAVEID = reader.ReadInt16();
            Volume = reader.ReadSingle();
            Pitch = reader.ReadSingle();
        }
        public static JInstrumentVelocityRegionv2 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentVelocityRegionv2();
            b.loadFromStream(reader);
            return b;
        }

        public void WriteToStream(BeBinaryWriter wrt)
        {
            wrt.Write(Velocity);
            wrt.Write(new byte[0x3]);
            wrt.Write(WSYSID);
            wrt.Write(WAVEID);
            wrt.Write(Volume);
            wrt.Write(Pitch);
        }
    }

}
