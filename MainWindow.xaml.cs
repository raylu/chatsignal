using System;
using System.Windows;
using NAudio.Wave;

namespace chatsignal {
	public partial class MainWindow : Window {
		BufferedWaveProvider waveProvider;
		WaveIn waveIn;
		WaveOut waveOut;
		KeyboardListener keyboard;

		public MainWindow() {
			InitializeComponent();

			keyboard = new KeyboardListener();
			keyboard.KeyDown += keyDown;
			keyboard.KeyUp += keyUp;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			if (waveIn != null) {
				waveIn.Dispose();
				waveOut.Dispose();
			}
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
			if (waveIn != null) {
				waveIn.Dispose();
				waveOut.Stop();
				waveOut.Dispose();
			}

			waveIn = new WaveIn();
			waveIn.BufferMilliseconds = 25;
			waveIn.DataAvailable += waveIn_DataAvailable;
			waveProvider = new BufferedWaveProvider(waveIn.WaveFormat);
			waveOut = new WaveOut();
			waveOut.DesiredLatency = 100;
			waveOut.Init(waveProvider);

			recording.Visibility = System.Windows.Visibility.Visible;
			waveIn.StartRecording();
		}

		private void stopRecording() {
			waveIn.StopRecording();
			recording.Visibility = System.Windows.Visibility.Hidden;
			waveOut.Play();
		}

		void waveIn_DataAvailable(object sender, WaveInEventArgs e) {
			waveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
		}
	}
}