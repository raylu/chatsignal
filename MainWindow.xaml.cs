using FragLabs.Audio.Codecs;
using NAudio.Wave;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace chatsignal {
	public partial class MainWindow : Window {
		BufferedWaveProvider waveProvider;
		WaveIn waveIn;
		WaveOut waveOut;
		KeyboardListener keyboard;
		TcpClient client;
		NetworkStream stream;
		OpusEncoder encoder;
		OpusDecoder decoder;
		Thread recvThread;

		public MainWindow() {
			InitializeComponent();

			waveIn = new WaveIn(); // create dummy WaveIn to get format for encoder/provider
			WaveFormat wf = waveIn.WaveFormat;
			Console.WriteLine(wf.SampleRate);
			encoder = OpusEncoder.Create(wf.SampleRate, wf.Channels, FragLabs.Audio.Codecs.Opus.Application.Voip);
			decoder = OpusDecoder.Create(wf.SampleRate, wf.Channels);
			//encoder.Bitrate = 128000;

			waveProvider = new BufferedWaveProvider(waveIn.WaveFormat);
			waveOut = new WaveOut();
			waveOut.DesiredLatency = 100;
			waveOut.Init(waveProvider);
			waveOut.Play();

			keyboard = new KeyboardListener();
			keyboard.KeyDown += keyDown;
			keyboard.KeyUp += keyUp;

			client = new TcpClient();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			waveIn.Dispose();
			waveOut.Dispose();
			encoder.Dispose();
			if (recvThread != null)
				recvThread.Abort();
		}

		private void keyDown(object sender, RawKeyEventArgs args) {
			if (args.Key == System.Windows.Input.Key.LeftCtrl)
				this.Dispatcher.BeginInvoke(new Action(startRecording), null);
		}

		private void keyUp(object sender, RawKeyEventArgs args) {
			if (args.Key == System.Windows.Input.Key.LeftCtrl)
				this.Dispatcher.BeginInvoke(new Action(stopRecording), null);
		}

		private void ptt_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			startRecording();
		}

		private void ptt_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			stopRecording();
		}

		private void startRecording() {
			waveIn.Dispose();

			waveIn = new WaveIn();
			waveIn.BufferMilliseconds = 20; // be very careful when changing this; OPUS is very picky about the frame_size
			waveIn.DataAvailable += waveIn_DataAvailable;

			recording.Visibility = System.Windows.Visibility.Visible;
			waveIn.StartRecording();
		}

		private void stopRecording() {
			waveIn.StopRecording();
			recording.Visibility = System.Windows.Visibility.Hidden;
		}

		private void connect_Click(object sender, RoutedEventArgs e) {
			client.Connect(host.Text, 63636);
			serverStatus.Content = "Connected";
			stream = client.GetStream();
			recvThread = new Thread(new ThreadStart(recv));
		}

		private void recv() {
			byte[] buffer = new byte[400];
			int offset = 0;
			while (true) {
				int read = stream.Read(buffer, offset, buffer.Length - offset);
				int length;
				byte[] pcm = decoder.Decode(buffer, offset + read, out length);
				offset -= length;
				waveProvider.AddSamples(pcm, 0, length);
			}
		}

		private void waveIn_DataAvailable(object sender, WaveInEventArgs e) {
			if (client.Connected) {
				int length;
				byte[] buffer = encoder.Encode(e.Buffer, e.BytesRecorded, out length);
				stream.Write(buffer, 0, length);
			}
		}
	}
}