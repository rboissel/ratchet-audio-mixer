using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratchet.Audio
{
    public partial class Mixer : System.IO.Stream
    {
        unsafe abstract class Encoder
        {
            protected int _ChannelCount;
            public Encoder(int ChannelCount)
            {
                _ChannelCount = ChannelCount;
            }
            public abstract void Encode(void* Output, void* Input, int Count, int ChannelId);
        }

        unsafe class Encoder_Float_To_Byte : Encoder
        {
            public Encoder_Float_To_Byte(int ChannelCount) : base(ChannelCount) { }
            public override unsafe void Encode(void* Output, void* Input, int Count, int ChannelId)
            {
            }
        }

        unsafe class Encoder_Float_To_Int16 : Encoder
        {
            public Encoder_Float_To_Int16(int ChannelCount) : base(ChannelCount) { }
            public override unsafe void Encode(void* Output, void* Input, int Count, int ChannelId)
            {
            }
        }

        unsafe class Encoder_Float_To_Int32 : Encoder
        {
            public Encoder_Float_To_Int32(int ChannelCount) : base(ChannelCount) { }
            public override unsafe void Encode(void* Output, void* Input, int Count, int ChannelId)
            {
            }
        }

        unsafe class Encoder_Float_To_Float : Encoder
        {
            public Encoder_Float_To_Float(int ChannelCount) : base(ChannelCount) { }
            public override unsafe void Encode(void* Output, void* Input, int Count, int ChannelId)
            {
                float* pOutput = (float*)Output + ChannelId;
                float* pInput = (float*)Input;

                for (int n = 0; n< Count; n++)
                {
                    *pOutput = *pInput++;
                    pOutput += _ChannelCount;
                }
            }
        }

        Encoder _Encoder = new Encoder_Float_To_Float(1);

        int _OutputNumChannels = 0;
        public int OutputChannelCount
        {
            get { return _OutputNumChannels; }
            set
            {
                lock (this)
                {
                    _OutputNumChannels = value;
                    _OutputBytePerFrame = _OutputFormatSizeInBytes * _OutputNumChannels;
                    if (_OutputFormat == typeof(float)) { _Encoder = new Encoder_Float_To_Float(_OutputNumChannels); }
                    else if (_OutputFormat == typeof(byte)) { _Encoder = new Encoder_Float_To_Byte(_OutputNumChannels); }
                    else if (_OutputFormat == typeof(Int16)) { _Encoder = new Encoder_Float_To_Int16(_OutputNumChannels); }
                    else if (_OutputFormat == typeof(Int32)) { _Encoder = new Encoder_Float_To_Int32(_OutputNumChannels); }
                }
            }
        }

        uint _OutputSampleRate = 0;
        public uint OutputSampleRate
        {
            get { return _OutputSampleRate; }
            set
            {
                lock (this)
                {
                    _OutputSampleRate = value;
                }
            }
        }

        Type _OutputFormat;
        int _OutputFormatSizeInBytes;

        int _OutputBytePerFrame;
        public Type OutputFormat
        {
            get { return _OutputFormat; }
            set
            {
                if (value == null)
                { throw new Exception("Invalid output format"); }
                if (value != typeof(float) && value != typeof(byte) && value != typeof(Int16) && value != typeof(Int32))
                { throw new Exception("Invalid output format. Accepted values are float, byte, int16, int32"); }
                lock (this)
                {
                    _OutputFormat = value;
                    if (value == typeof(float)) { _OutputFormatSizeInBytes = 4; _Encoder = new Encoder_Float_To_Float(_OutputNumChannels); }
                    else if (value == typeof(byte)) { _OutputFormatSizeInBytes = 1; _Encoder = new Encoder_Float_To_Byte(_OutputNumChannels); }
                    else if (value == typeof(Int16)) { _OutputFormatSizeInBytes = 2; _Encoder = new Encoder_Float_To_Int16(_OutputNumChannels); }
                    else if (value == typeof(Int32)) { _OutputFormatSizeInBytes = 4; _Encoder = new Encoder_Float_To_Int32(_OutputNumChannels); }
                    _OutputBytePerFrame = _OutputFormatSizeInBytes * _OutputNumChannels;
                }
            }
        }

        Dictionary<int, List<Listener>> _Listeners = new Dictionary<int, List<Listener>>();

        public Listener CreateListener(float X, float Y, float Z, int OutputChannel)
        {
            Listener listener = new Listener(X, Y, Z, OutputChannel, this);
            lock (this)
            {
                if (!_Listeners.ContainsKey(OutputChannel)) { _Listeners.Add(OutputChannel, new List<Listener>()); }
                _Listeners[OutputChannel].Add(listener);
            }
            return listener;
        }
    }
}
