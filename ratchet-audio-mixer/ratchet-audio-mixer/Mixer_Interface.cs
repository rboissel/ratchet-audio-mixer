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
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { throw new NotSupportedException(); } }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set { throw new NotSupportedException(); }
        }

        public override void Flush() { }

        List<float[]> _TempBuffers = new List<float[]>();
        List<SourceDataChunk> _TempSources = new List<SourceDataChunk>();

        public unsafe override int Read(byte[] buffer, int offset, int count)
        {
            if (_OutputBytePerFrame == 0 || count == 0) { return 0; }

            int processingCount = count;
            if (192000 % _OutputBytePerFrame == 0) { processingCount *= (192000 / (int)_OutputSampleRate); }
            else { processingCount = (int)((float)count * (192000.0f / (float)_OutputSampleRate)); }

            int processedCount = 0;
            lock (this)
            {
                if (_OutputBytePerFrame == 0) { return 0; }
                // Process systematically 16KFrame at a time at most

                int frameCount = count / _OutputBytePerFrame;
                int processingFrameCount = processingCount / (sizeof(float) * _OutputNumChannels);
                if (processingFrameCount > 1024 * 16)
                {
                    processingFrameCount = 1024 * 16;
                    processingCount = processingFrameCount * (sizeof(float) * _OutputNumChannels);
                    if (192000 % _OutputBytePerFrame == 0) { count = processingCount / (192000 / (int)_OutputSampleRate); }
                    else { count = (int)((float)processingCount / (192000.0f / (float)_OutputSampleRate)); }
                    frameCount = count / _OutputBytePerFrame;
                }



                // Read the data for all the sources
                for (int n = 0; n < _Channels.Count; n++) 
                {
                    if (_TempSources.Count <= n)
                    {
                        SourceDataChunk chunk = new SourceDataChunk();
                        chunk.data = new float[1024 * 16];
                        _TempSources.Add(chunk);
                    }
                    _Channels[n].Read(_TempSources[n].data, processingFrameCount);
                    _TempSources[n].count = processingFrameCount;
                    _TempSources[n].x = _Channels[n].X;
                    _TempSources[n].y = _Channels[n].Y;
                    _TempSources[n].z = _Channels[n].Z;
                    _TempSources[n].quadraticAttenuation = _Channels[n]._QuadraticAttenuation;
                    _TempSources[n].linearAttenuation = _Channels[n]._LinearAttenuation;

                }

                for (int n = 0; n < _OutputNumChannels; n++)
                {
                    if (_Listeners.ContainsKey(n))
                    {
                        int minListenerReadCount = 0;
                        int listenerReadCount = 0;
                        List<Listener> listeners = _Listeners[n];
                        for (int l = 0; l < listeners.Count; l++)
                        {
                            while (l >= _TempBuffers.Count) { _TempBuffers.Add(new float[1024 * 16]); }
                            listenerReadCount = listeners[l].Compute(_TempSources, _TempBuffers[l], processingFrameCount);
                            if (listenerReadCount < minListenerReadCount) { minListenerReadCount = listenerReadCount; }
                            if (l > 0)
                            {
                                for (int x = 0; x < minListenerReadCount; x++)
                                {
                                    _TempBuffers[0][x] += _TempBuffers[l][x];
                                    if (_TempBuffers[0][x] >= 1.0f) { _TempBuffers[0][x] = 1.0f; }
                                    if (_TempBuffers[0][x] <= -1.0f) { _TempBuffers[0][x] = -1.0f; }
                                }
                            }
                        }

                        // Time to encode the result
                        _Downsampler.Downsample(_TempBuffers[0], processingFrameCount);

                        fixed (float* pSource = &_TempBuffers[0][0])
                        {
                            fixed (byte* pDestination = &buffer[0])
                            {
                                _Encoder.Encode(pDestination, pSource, frameCount, n);
                            }
                        }
                    }



                }
     

                processedCount = frameCount * _OutputBytePerFrame;

                return processedCount;
            }
        }



        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Writting directly to the mixer is not supported you must use a channel");
        }
    }
}
