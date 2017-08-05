using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiniWavePlayer
{
    class WaveChannel : Ratchet.Audio.Mixer.Source<Int16>
    {
        Ratchet.IO.Format.Waveform.Channel<Int16> _Channel;
        int _Offset = 0;

        public WaveChannel(Ratchet.IO.Format.Waveform.Channel<Int16> Channel, uint SampleRate)
        {
            _Channel = Channel;
            this.SampleRate = SampleRate;
        }

        public override int Read(Int16[] Buffer, int FrameCount)
        {
            if (_Offset + FrameCount >= _Channel.Length) { FrameCount = _Channel.Length - _Offset; }
            for (int n = 0; n < FrameCount; n++)
            {
                Buffer[n] = _Channel.Samples[n + _Offset];
            }
            _Offset += FrameCount;
            return FrameCount;
        }
    }

    public partial class MiniWavePlayer : Form
    {
        public MiniWavePlayer()
        {
            InitializeComponent();
        }

        Ratchet.Audio.Mixer _Mixer;
        Ratchet.Audio.PlaybackDevice _Device;
        Ratchet.Audio.PlaybackClient _Client;
        Ratchet.IO.Format.Waveform.Sound<Int16> _Sound;

        private void openButton_Click(object sender, EventArgs e) { openFileDialog.ShowDialog(); }

        private void MiniWavePlayer_Load(object sender, EventArgs e)
        {
            List<Ratchet.Audio.PlaybackDevice> devices = Ratchet.Audio.PlaybackDevice.GetDevices();

            for (int n = 0; n < devices.Count; n++) { if (devices[n].Enabled) { output.Items.Add(devices[n]); } }

        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void output_Click(object sender, EventArgs e)
        {

        }

        private void output_SelectedIndexChanged(object sender, EventArgs e)
        {
            Ratchet.Audio.PlaybackDevice oldDevice = _Device;
            _Mixer = new Ratchet.Audio.Mixer();
            _Device = (Ratchet.Audio.PlaybackDevice)output.SelectedItem;
            if (oldDevice != _Device)
            {
                if (_Client != null) { _Client.Stop(); }
                _Mixer = new Ratchet.Audio.Mixer();
                _Client = _Device.CreateClient(_Mixer);
                _Mixer.OutputFormat = _Client.Format;
                _Mixer.OutputSampleRate = _Client.SampleRate;
                _Mixer.OutputChannelCount = _Client.ChannelCount;
                _Client.Start();

                // Just use one or two listeners for the demo
                if (_Client.ChannelCount == 1)
                {
                    Ratchet.Audio.Mixer.Listener Center = _Mixer.CreateListener(0.0f, 0.0f, 0.0f, 0);
                }
                else if (_Client.ChannelCount >= 2)
                {
                    Ratchet.Audio.Mixer.Listener Left = _Mixer.CreateListener(-1.0f, 0.0f, 0.0f, 0);
                    Ratchet.Audio.Mixer.Listener Right = _Mixer.CreateListener(1.0f, 0.0f, 0.0f, 1);
                }
            }
        }

        private void openFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                _Sound = Ratchet.IO.Format.Waveform.Read<Int16>(System.IO.File.ReadAllBytes(openFileDialog.FileName));
                if (_Sound.Channels.Count == 1)
                {
                    WaveChannel channel = new WaveChannel(_Sound.Channels[0], _Sound.SampleRate);
                    _Mixer.AddSource(channel);
                }
                else if (_Sound.Channels.Count >= 2)
                {
                    WaveChannel left = new WaveChannel(_Sound.Channels[0], _Sound.SampleRate);
                    WaveChannel right = new WaveChannel(_Sound.Channels[1], _Sound.SampleRate);
                    left.X = -0.8f;
                    right.X = 0.8f;
                    _Mixer.AddSource(left);
                    _Mixer.AddSource(right);


                }
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Can't open the specified file: " + openFileDialog.FileName);
            }
        }
    }
}
