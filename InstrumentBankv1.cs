using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Be.IO;
using Newtonsoft.Json;

namespace ibnktool
{
    public class JInstrumentBankv1
    {


        [JsonIgnore]
        public int mBaseAddress = 0;
        [JsonIgnore]
        public uint mHash = 0;

        private const int IBNK = 0x49424e4b;
        private const int BANK = 0x42414E4B;

        public uint size = 0;
        public uint globalID = 0;
        public JInstrument[] instruments = new JInstrument[0xF0];
 

        public void loadFromStream(BeBinaryReader reader)
        {
            int mountpos = (int)reader.BaseStream.Position;
            if (reader.ReadUInt32() != IBNK)
                throw new InvalidDataException("Data is not IBNK!");
            size = reader.ReadUInt32();
            globalID = reader.ReadUInt32();
            reader.BaseStream.Position = mountpos + 0x20;
            if (reader.ReadUInt32() != BANK)
                throw new InvalidDataException("Data is not BANK");
            var instPtrs = util.readInt32Array(reader, 0xF0);
            for (int i = 0; i < 0xF0; i++) {
                reader.BaseStream.Position = instPtrs[i] + mountpos;
                if (instPtrs[i] != 0)
                    instruments[i] = JInstrument.CreateFromStream(reader, mountpos);
             }
        }

        public static JInstrumentBankv1 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentBankv1();
            b.loadFromStream(reader);
            return b;
        }

        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(IBNK);
            wr.Write(size);
            wr.Write(globalID); wr.Flush();
            wr.Write(new byte[0x14]);
            wr.Write(BANK);
            for (int i = 0; i < instruments.Length; i++)
                wr.Write(instruments[i] == null ? 0 : instruments[i].mBaseAddress);                
        }
    }

    public class JInstrumentEnvelopev1
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
            while (reader.ReadUInt16() < 0xB) {
                reader.ReadUInt32();
                count++;
            }
            count++;
            reader.BaseStream.Position = origPos;
            points = new JEnvelopeVector[count];
            for (int i = 0; i < count; i++)
                points[i] = new JEnvelopeVector { Mode = reader.ReadUInt16(), Delay = reader.ReadUInt16(), Value = reader.ReadInt16() };
        }
        public static JInstrumentEnvelopev1 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentEnvelopev1();
            b.loadFromStream(reader);        
            return b;
        }
        public void WriteToStream(BeBinaryWriter wr)
        {
            var remainingLength = 32;
            for (int i=0; i < points.Length;i++)
            {
                remainingLength -= 6;
                wr.Write(points[i].Mode);
                wr.Write(points[i].Delay);
                wr.Write(points[i].Value);
            }
            if (remainingLength > 0)
                wr.Write(new byte[remainingLength]);
            else
                util.padTo(wr, 32);

        }
    }

    public class JInstrumentOscillatorv1
    {
        [JsonIgnore]
        public int mBaseAddress = 0;
        [JsonIgnore]
        public uint mHash = 0;

        public byte Target;
        public float Rate;
        public JInstrumentEnvelopev1 Attack;
        public JInstrumentEnvelopev1 Release;
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
            reader.BaseStream.Position = envA + seekbase;
            if (envA > 0)
                Attack = JInstrumentEnvelopev1.CreateFromStream(reader);
            reader.BaseStream.Position = envB + seekbase;
            if (envB > 0)
                Release = JInstrumentEnvelopev1.CreateFromStream(reader);
        }
        public static JInstrumentOscillatorv1 CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JInstrumentOscillatorv1();
            b.loadFromStream(reader,seekbase);
            return b;
        }

        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(Target);
            wr.Write(new byte[0x3]);
            wr.Write(Rate);
            wr.Write(Attack.mBaseAddress);
            wr.Write(Release.mBaseAddress);
            wr.Write(Width);
            wr.Write(Vertex);
            wr.Write(new byte[8]);
        }

    }

    public class JInstrumentSenseEffectv1
    {
        [JsonIgnore]
        public int mBaseAddress = 0;

        public byte Target;
        public byte Register;
        public byte Key;
        public float Floor;
        public float Ceiling;

        private void loadFromStream(BeBinaryReader reader)
        {
            Target = reader.ReadByte();
            reader.ReadBytes(3);
            Register = reader.ReadByte();
            Key = reader.ReadByte();
            Floor = reader.ReadSingle();
            Ceiling = reader.ReadSingle();
        }
        public static JInstrumentSenseEffectv1 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentSenseEffectv1();
            b.loadFromStream(reader);
            return b;
        }
    }

    public class JInstrumentRandEffectv1
    {
        [JsonIgnore]
        public int mBaseAddress = 0;

        public byte Target;
        public float Floor;
        public float Ceiling;

        private void loadFromStream(BeBinaryReader reader)
        {
            Target = reader.ReadByte();
            reader.ReadBytes(3);
            Floor = reader.ReadSingle();
            Ceiling = reader.ReadSingle();
        }
        public static JInstrumentRandEffectv1 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentRandEffectv1();
            b.loadFromStream(reader);
            return b;
        }
    }

    public class JInstrument
    {
        [JsonIgnore]
        public int mBaseAddress = 0;

        internal const int INST = 0x494E5354;
        internal const int PER2 = 0x50455232;
        public bool Percussion = false;

        public static JInstrument CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var magic = reader.ReadUInt32();
            if (magic == INST)
                return JStandardInstrumentv1.CreateFromStream(reader, seekbase);
            else if (magic == PER2)
                return JPercussion.CreateFromStream(reader, seekbase);
            return null;
        }

    }
    public class JStandardInstrumentv1 : JInstrument
    {

 

        public float Pitch;
        public float Volume;

        public JInstrumentOscillatorv1 oscillatorA;
        public JInstrumentOscillatorv1 oscillatorB;
        public JInstrumentSenseEffectv1 effectA;
        public JInstrumentSenseEffectv1 effectB;
        public JInstrumentRandEffectv1 randA;
        public JInstrumentRandEffectv1 randB;

        public JKeyRegionv1[] keys;

        public bool Percussion;

        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {

            reader.ReadUInt32(); // Empty 
            Pitch = reader.ReadSingle();
            Volume = reader.ReadSingle();

            var oscA = reader.ReadUInt32();
            var oscB = reader.ReadUInt32();
            var effA = reader.ReadUInt32();
            var effB = reader.ReadUInt32();
            var ranA = reader.ReadUInt32();
            var ranB = reader.ReadUInt32();

            var keyRegCount = reader.ReadUInt32();
            var keyRegPtrs = util.readInt32Array(reader, (int)keyRegCount);
            keys = new JKeyRegionv1[keyRegCount];

            reader.BaseStream.Position = oscA + seekbase;
            if (oscA > 0)
                oscillatorA = JInstrumentOscillatorv1.CreateFromStream(reader, seekbase);
            reader.BaseStream.Position = oscB + seekbase;
            if (oscB > 0)
                oscillatorB = JInstrumentOscillatorv1.CreateFromStream(reader, seekbase);


            reader.BaseStream.Position = effA + seekbase;
            if (effA > 0)
                effectA = JInstrumentSenseEffectv1.CreateFromStream(reader);

            reader.BaseStream.Position = effB + seekbase;
            if (effB > 0)
                effectB = JInstrumentSenseEffectv1.CreateFromStream(reader);


            reader.BaseStream.Position = ranA + seekbase;
            if (ranA > 0)
                randA = JInstrumentRandEffectv1.CreateFromStream(reader);


            reader.BaseStream.Position = ranB + seekbase;
            if (ranB > 0)
                randB = JInstrumentRandEffectv1.CreateFromStream(reader);

            for (int i=0; i < keyRegCount; i++)
            {
                reader.BaseStream.Position = keyRegPtrs[i] + seekbase;
                if (keyRegPtrs[i] != 0)
                    keys[i] = JKeyRegionv1.CreateFromStream(reader, seekbase);
            }
        }
        new public static JStandardInstrumentv1 CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JStandardInstrumentv1();
            b.loadFromStream(reader,seekbase);
            return b;
        }

        public void WritetoStream(BeBinaryWriter wr)
        {
            wr.Write(INST);
            wr.Write(0);
            wr.Write(Pitch);
            wr.Write(Volume);
            wr.Write(oscillatorA == null ? 0 : oscillatorA.mBaseAddress);
            wr.Write(oscillatorB == null ? 0 : oscillatorB.mBaseAddress);
            wr.Write(effectA == null ? 0 : effectA.mBaseAddress);
            wr.Write(effectB == null ? 0 : effectB.mBaseAddress);
            wr.Write(randA == null ? 0 : randA.mBaseAddress);
            wr.Write(randB == null ? 0 : randB.mBaseAddress);
            wr.Write(keys.Length);
            for (int i = 0; i < keys.Length; i++)
                wr.Write(keys[i].mBaseAddress);
           
        }
    }

    public class JPercussion : JInstrument
    {
        public JPercussionEntry[] Sounds = new JPercussionEntry[128];
        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
            Percussion = true;
            reader.ReadBytes(0x84); // Padding. 
            var keyRegPtrs = util.readInt32Array(reader, 128);
            var anchor = reader.BaseStream.Position; // Store anchor at end of pointer table at base + 0x218
            for (int i=0; i < 128; i++)
                if (keyRegPtrs[i]!=0)
                {
                    reader.BaseStream.Position = keyRegPtrs[i] + seekbase;
                    Sounds[i] = JPercussionEntry.CreateFromStream(reader, seekbase);
                }
            reader.BaseStream.Position = anchor;  // Restore anchor. 

            //reader.ReadBytes(0x70); // Padding 
            for (int i = 0; i < 128; i++)
            {
                var b = reader.ReadByte();
                if (keyRegPtrs[i] != 0)
                    Sounds[i].uflag1 = b;
            }
            //reader.ReadBytes(0x1c); // Also padding
            for (int i = 0; i < 128; i++)
            {
                var b = reader.ReadUInt16();
                if (keyRegPtrs[i] != 0)
                    Sounds[i].uflag2 = b;
            }
            // 0x50 padding.
            //reader.ReadBytes(0x50);
        }

        new public static JPercussion CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JPercussion();
            b.loadFromStream(reader, seekbase);
            return b;
        }

        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(PER2);
            wr.Write(new byte[0x84]);
            for (int i = 0; i < 128; i++)
                wr.Write(Sounds[i] != null ? Sounds[i].mBaseAddress : 0);
            //wr.Write(new byte[0x70]);
            for (int i = 0; i < 128; i++)
                wr.Write((byte)(Sounds[i] != null ? Sounds[i].uflag1 : 0));
            //wr.Write(new byte[0x1C]);
            for (int i = 0; i < 128; i++)
                wr.Write((short)(Sounds[i] != null ? Sounds[i].uflag2 : 0));
            //wr.Write(new byte[0x50]);
        }
    }


    public class JPercussionEntry 
    {
        [JsonIgnore]
        public int mBaseAddress = 0;

        public JVelocityRegionv1[] Velocities;
        public float Pitch;
        public float Volume;
        public byte uflag1;
        public ushort uflag2;


        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
   

            Pitch = reader.ReadSingle();
            Volume = reader.ReadSingle();
            reader.ReadBytes(8);
            var velregCount = reader.ReadInt32();
            Velocities = new JVelocityRegionv1[velregCount];
            var velRegPtrs = util.readInt32Array(reader, velregCount);
            for (int i = 0; i < velregCount; i++)
            {
                reader.BaseStream.Position = seekbase + velRegPtrs[i];
                Velocities[i] = JVelocityRegionv1.CreateFromStream(reader, seekbase);
            }
        }
       public static JPercussionEntry CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JPercussionEntry();
            b.loadFromStream(reader, seekbase);
            return b;
        }
        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(Pitch);
            wr.Write(Volume);
            wr.Write(0L);
            wr.Write(Velocities.Length);
            for (int i = 0; i < Velocities.Length; i++)
                wr.Write(Velocities[i].mBaseAddress);
        }
    }



    public class JKeyRegionv1
    {

        [JsonIgnore]
        public int mBaseAddress = 0;

        public byte BaseKey;
        public JVelocityRegionv1[] Velocities;
        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
            BaseKey = reader.ReadByte();
            reader.ReadBytes(3);
            var velregCount = reader.ReadInt32();
            Velocities = new JVelocityRegionv1[velregCount];
            var velRegPtrs = util.readInt32Array(reader, velregCount);
            for (int i = 0; i < velregCount; i++) {
                reader.BaseStream.Position = seekbase + velRegPtrs[i];
                Velocities[i] = JVelocityRegionv1.CreateFromStream(reader, seekbase);
            }
        }
        public static JKeyRegionv1 CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JKeyRegionv1();
            b.loadFromStream(reader, seekbase);
            return b;
        }

        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(BaseKey);
            wr.Write(new byte[0x3]);
            wr.Write(Velocities.Length);
            for (int i=0; i < Velocities.Length; i++)
                wr.Write(Velocities[i].mBaseAddress);
            util.padTo(wr,16);
        }
    }

    

    public class JVelocityRegionv1
    {
        [JsonIgnore]
        public int mBaseAddress = 0;

        public byte Velocity;
        public ushort WSYSID;
        public ushort WaveID;
        public float Volume;
        public float Pitch;


        private void loadFromStream(BeBinaryReader reader, int seekbase)
        {
            Velocity = reader.ReadByte();
            reader.ReadBytes(3); // empty 
            WSYSID = reader.ReadUInt16();
            WaveID = reader.ReadUInt16();
            Volume = reader.ReadSingle();
            Pitch = reader.ReadSingle();
        }
        public static JVelocityRegionv1 CreateFromStream(BeBinaryReader reader, int seekbase)
        {
            var b = new JVelocityRegionv1();
            b.loadFromStream(reader, seekbase);
            return b;
        }

        public void WriteToStream(BeBinaryWriter wr)
        {
            wr.Write(Velocity);
            wr.Write(new byte[0x3]);
            wr.Write(WSYSID);
            wr.Write(WaveID);
            wr.Write(Volume);
            wr.Write(Pitch);
        }

    }
}
