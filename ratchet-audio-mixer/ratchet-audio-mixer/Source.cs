using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratchet.Audio
{
    public partial class Mixer : System.IO.Stream
    {
        public abstract class Source<T>
        {
            internal float _X = 0.0f; public virtual float X { get { return _X; } set { _X = value; } }
            internal float _Y = 0.0f; public virtual float Y { get { return _Y; } set { _Y = value; } }
            internal float _Z = 0.0f; public virtual float Z { get { return _Z; } set { _Z = value; } }
            internal float _LinearAttenuation = 0.1f; public float LinearAttenuation { get { return _LinearAttenuation; } set { _LinearAttenuation = value; } }
            internal float _QuadraticAttenuation = 0.01f; public float QuadraticAttenuation { get { return _QuadraticAttenuation; } set { _QuadraticAttenuation = value; } }

            public abstract int Read(T[] Buffer, int FrameCount);
        }

        List<Source<float>> _Channels = new List<Source<float>>();

        class Source_Byte_Adapter : Source<float>
        {
            public override float X { get => _Channel.X; set => _Channel.X = value; }
            public override float Y { get => _Channel.Y; set => _Channel.Y = value; }
            public override float Z { get => _Channel.Z; set => _Channel.Z = value; }

            Source<byte> _Channel = null;
            byte[] _TempBuffer = new byte[4096];

            public Source_Byte_Adapter(Source<byte> SourceChannel)
            {
                _Channel = SourceChannel;
            }

            public override int Read(float[] Buffer, int FrameCount)
            {
                int remaining = FrameCount;
                while (remaining > 0)
                {
                    int count = _Channel.Read(_TempBuffer, remaining > _TempBuffer.Length ? _TempBuffer.Length : remaining);
                }
                return 0;
            }
        }

        class Source_Int16_Adapter : Source<float>
        {
            public override float X { get => _Channel.X; set => _Channel.X = value; }
            public override float Y { get => _Channel.Y; set => _Channel.Y = value; }
            public override float Z { get => _Channel.Z; set => _Channel.Z = value; }

            Source<Int16> _Channel = null;
            Int16[] _TempBuffer = new Int16[4096];

            public Source_Int16_Adapter(Source<Int16> SourceChannel)
            {
                _Channel = SourceChannel;
            }

            public override int Read(float[] Buffer, int FrameCount)
            {
                int remaining = FrameCount;
                int offset = 0;
                while (remaining > 0)
                {
                    int count = _Channel.Read(_TempBuffer, remaining > _TempBuffer.Length ? _TempBuffer.Length : remaining);
                    for (int n = 0; n < count; offset++, n++) { Buffer[offset] = (((float)_TempBuffer[n]) / (float)(Int16.MaxValue)); }
                    remaining -= count;
                }
                return offset;
            }
        }

        class Source_Int32_Adapter : Source<float>
        {
            public override float X { get => _Channel.X; set => _Channel.X = value; }
            public override float Y { get => _Channel.Y; set => _Channel.Y = value; }
            public override float Z { get => _Channel.Z; set => _Channel.Z = value; }

            Source<Int32> _Channel = null;
            Int32[] _TempBuffer = new Int32[4096];

            public Source_Int32_Adapter(Source<Int32> SourceChannel)
            {
                _Channel = SourceChannel;
            }

            public override int Read(float[] Buffer, int FrameCount)
            {
                int remaining = FrameCount;
                while (remaining > 0)
                {
                    int count = _Channel.Read(_TempBuffer, remaining > _TempBuffer.Length ? _TempBuffer.Length : remaining);
                }
                return 0;
            }
        }

        public void AddSource<T>(Source<T> Channel)
        {

            if (typeof(T) == typeof(float)) { _Channels.Add(Channel as Source<float>); }
            else if (typeof(T) == typeof(byte)) { _Channels.Add(new Source_Byte_Adapter(Channel as Source<byte>)); }
            else if (typeof(T) == typeof(Int16)) { _Channels.Add(new Source_Int16_Adapter(Channel as Source<Int16>)); }
            else if (typeof(T) == typeof(Int32)) { _Channels.Add(new Source_Int32_Adapter(Channel as Source<Int32>)); }
        }

        internal class SourceDataChunk
        {
            public float x;
            public float y;
            public float z;
            public float[] data;
            public int count;
            public float linearAttenuation;
            public float quadraticAttenuation;

        }
    }
}
