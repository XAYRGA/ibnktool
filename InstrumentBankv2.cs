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
        private const int PERC = 0x50455243; // Percussion Table
        private const int Perc = 0x50657263; // Percussion 
        private const int SENS = 0x53454E53; // Sensor effect
        private const int RAND = 0x52414E44; // Random Effect
        private const int OSCT = 0x4F534354; // OSCillator Table
        private const int Osci = 0x4F736369; // Oscillator
        private const int INST = 0x494E5354; // INStrument Table
        private const int Inst = 0x496E7374; // Instrument
        private const int IBNK = 0x49424E4B; // Instrument BaNK
        private const int ENVT = 0x454E5654; // ENVelope Table
        private const int PMAP = 0x504D4150;
        private const int Pmap = 0x506D6170;
        private const int LIST = 0x4C495354;

        int Boundaries = 0;
        private int iBase = 0;
        private int OscTableOffset = 0;
        private int EnvTableOffset = 0;
        private int RanTableOffset = 0;
        private int SenTableOffset = 0;
        private int ListTableOffset = 0;
        private int PmapTableOffset = 0;

        public JInstrumentEnvelopev2[] Envelopes;
        public JInstrumentOscillatorv2[] Oscillators;
        
        private int findChunk(BeBinaryReader read, int chunkID, bool immediate = false)
        {
            if (!immediate) 
                read.BaseStream.Position = iBase;

            while (true)
            {
                var pos = (int)read.BaseStream.Position - iBase; 
                var i = read.ReadInt32(); 
                if (i == chunkID) 
                    return pos; 
                else if (pos > (Boundaries))
                    return 0;
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

    public class JInstrumentOscillatorv2
    {
        [JsonIgnore]
        public int mBaseAddress = 0;
        [JsonIgnore]
        public uint mHash = 0;


        public byte Target;
        public float Rate;
        public int AttackEnvelopeID;
        public int ReleaseEnvelopeID;
        public float Width;
        public float Vertex;

        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
            mBaseAddress = (int)reader.BaseStream.Position;
            Target = reader.ReadByte();
            reader.ReadBytes(3);
            Rate = reader.ReadSingle();
            var envA = reader.ReadUInt32();
            var envB = reader.ReadUInt32();
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
            wr.Write(Target);
            wr.Write(new byte[0x3]);
            wr.Write(Rate);
            wr.Write(AttackEnvelopeID);
            wr.Write(ReleaseEnvelopeID);
            wr.Write(Width);
            wr.Write(Vertex);
            wr.Write(new byte[8]);
        }

    }




}
