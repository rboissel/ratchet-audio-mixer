using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ratchet.Audio
{
    public partial class Mixer : System.IO.Stream
    {
        public class Listener
        {
            float _X = 0.0f;
            float _Y = 0.0f;
            float _Z = 0.0f;
            Mixer _Parent;

            internal Listener(float X, float Y, float Z, int OutputChannel, Mixer Parent)
            {
                _X = X;
                _Y = Y;
                _Z = Z;
                _Parent = Parent;
            }

            internal int Compute(List<SourceDataChunk> Sources, float[] Output, int Count)
            {
                for (int n = 0; n < Sources.Count; n++)
                {
                    float dx = Sources[n].x - _X;
                    float dy = Sources[n].y - _Y;
                    float dz = Sources[n].z - _Z;
                    float dis = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    float linearAttenuation = (dis * Sources[n].linearAttenuation);
                    if (linearAttenuation >= 1.0f) { continue; }
                    float quadraticAttenuation = (dis * dis * Sources[n].quadraticAttenuation);
                    if (quadraticAttenuation >= 1.0f) { continue; }
                    float attenuation = (linearAttenuation + quadraticAttenuation);
                    if (attenuation >= 1.0f) { continue; }

                    float volume = 1.0f - attenuation;
                    if (n == 0)
                    {
                        for (int x = 0; x < Count; x++)
                        {
                            Output[x] = Sources[n].data[x] * volume;
                        }
                    }
                    else
                    {
                        for (int x = 0; x < Count; x++)
                        {
                            Output[x] += Sources[n].data[x] * volume;
                        }
                    }
                }
                return Count;
            }
        }
    }
}
