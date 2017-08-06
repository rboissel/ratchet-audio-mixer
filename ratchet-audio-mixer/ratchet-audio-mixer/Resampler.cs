using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Ratchet.Audio
{
    public partial class Mixer
    {

        class Upsampler_To_192K : Source<float>
        {
            Source<float> _Source;
            float[] _TempBuffer = new float[4096];
            public override uint SampleRate { get { return 192000; } set { base.SampleRate = value; } }
            public Upsampler_To_192K(Source<float> Source)
            {
                _Source = Source;
            }

            public override int Read(float[] Buffer, int FrameCount)
            {
                if (_Source.SampleRate == 48000)
                {
                    FrameCount = _Source.Read(_TempBuffer, FrameCount / 4) * 4;
                    Buffer[FrameCount - 1] = _TempBuffer[FrameCount / 4 - 1];
                    Buffer[FrameCount - 2] = _TempBuffer[FrameCount / 4 - 1];
                    Buffer[FrameCount - 3] = _TempBuffer[FrameCount / 4 - 1];
                    Buffer[FrameCount - 4] = _TempBuffer[FrameCount / 4 - 1];

                    for (int n = 0, x = 0; n < FrameCount - 4; n += 4, x++)
                    {
                        float diff = (_TempBuffer[x + 1] - _TempBuffer[x]) / 4.0f;
                        Buffer[n] = _TempBuffer[x];
                        Buffer[n + 1] = Buffer[n] + diff;
                        Buffer[n + 2] = Buffer[n + 1] + diff;
                        Buffer[n + 3] = Buffer[n + 2] + diff;
                    }

                    return FrameCount;
                }
                else if (_Source.SampleRate == 96000)
                {
                    FrameCount = _Source.Read(_TempBuffer, FrameCount / 2) * 2;
                    Buffer[FrameCount - 1] = _TempBuffer[FrameCount / 2 - 1];

                    for (int n = 0, x = 0; n < FrameCount - 1; n += 2, x++)
                    {
                        Buffer[n] = _TempBuffer[x];
                        Buffer[n + 1] = (_TempBuffer[x + 1] + _TempBuffer[x]) / 2.0f;
                    }

                    return FrameCount;
                }
                else if (_Source.SampleRate == 192000)
                {
                    return FrameCount = _Source.Read(_TempBuffer, FrameCount);
                }
                else
                {
                    // Ok go with the generic path (it is expensive)
                    float ratio = (float)((_Source.SampleRate) / (192000.0f));
                    float iratio = (float)((192000.0f) / (_Source.SampleRate));

                    if (ratio > 1.0f) { throw new Exception("Sampling rate can't be higher than 192KHz"); }
                    int SourceFrameCount = (int)((float)FrameCount * ratio);
                    int SourceReadFrameCount = _Source.Read(_TempBuffer, SourceFrameCount);
                    SourceFrameCount = (int)((float)SourceReadFrameCount * (float)iratio);
                    if (SourceFrameCount < FrameCount - 4)
                    {
                        FrameCount = SourceFrameCount;
                    }

                    float error = 0.0f;
                    int n = 0;
                    for (int x = 0; n < FrameCount - 1 && x < _TempBuffer.Length - 1; x++)
                    {
                        float vx = error;
                        float tempBuffer = _TempBuffer[x];
                        float nextTempBuffer = _TempBuffer[x + 1];

      
                        while (vx < 1.0f)
                        {
                            Buffer[n++] = tempBuffer * (1.0f - vx) + tempBuffer * vx;
                            vx += ratio;
                        }

                        error = vx - 1.0f;
                    }
                    for (; n < FrameCount; n++) { Buffer[n] = _TempBuffer[SourceReadFrameCount - 1]; }
                    return SourceFrameCount;
                }
            }
        }

        abstract class Downsampler
        {
            public abstract void Downsample(float[] Buffer, int FrameCount);
        }

        class Downsampler_From_192K : Downsampler
        {
            int _TargetSampleRate = 0;
            public Downsampler_From_192K(int TargetSampleRate)
            {
                _TargetSampleRate = TargetSampleRate;
            }
            
            public override void Downsample(float[] Buffer, int FrameCount)
            {
                float ratio = 192000.0f / (float)_TargetSampleRate;
                float iratio = (float)_TargetSampleRate / 192000.0f;

                FrameCount = (int)((float)FrameCount / ratio);
                float error = 0.0f;
                for (int n = 0, x = 0; x < FrameCount; x++)
                {
                    float sum = error;
                    float sumValue = 0.0f;
                    int localCount = 0;
                    while (sum < 1.0f && n < Buffer.Length)
                    {
                        sumValue += Buffer[n++];
                        sum += iratio;
                        localCount++;
                    }
                    error = sum - 1.0f;
                    Buffer[x] = sumValue / (float)localCount;
                }
            }
        }

        class Downsampler_From_192K_To_48K : Downsampler
        {
            public Downsampler_From_192K_To_48K()
            {
            }

            public override void Downsample(float[] Buffer, int FrameCount)
            {
                for (int n = 0, x = 0; n < FrameCount / 4; n++, x += 4)
                {
                    Buffer[n] = (Buffer[x] + Buffer[x + 1] + Buffer[x + 2] + Buffer[x + 3]) / 4.0f;
                }
            }
        }

        class Downsampler_From_192K_To_92K : Downsampler
        {
            public Downsampler_From_192K_To_92K()
            {
            }

            public override void Downsample(float[] Buffer, int FrameCount)
            {
                for (int n = 0, x = 0; n < FrameCount / 2; n++, x += 2)
                {
                    Buffer[n] = (Buffer[x] + Buffer[x + 1]) / 2.0f;
                }
            }
        }


        class Downsampler_None : Downsampler
        {
            public Downsampler_None() { }
            public override void Downsample(float[] Buffer, int FrameCount) { }
        }
    }
}
