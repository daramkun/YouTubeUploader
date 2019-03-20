using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Daramee.YouTubeUploader
{
	/// <summary>
	/// GetSnapshotFromMediaWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class GetSnapshotFromMediaWindow : Window
	{
		public BitmapSource CapturedSource { get; private set; }

		public GetSnapshotFromMediaWindow ( Uri filename )
		{
			InitializeComponent ();
			mediaElement.Source = filename;
		}

		private void Slider_ValueChanged ( object sender, RoutedPropertyChangedEventArgs<double> e )
		{
			mediaElement.MediaPosition = TimeSpan.FromSeconds ( TimeSpan.FromTicks ( mediaElement.MediaDuration ).TotalSeconds * ( sender as Slider ).Value ).Ticks;
			mediaElement.Play ();
			mediaElement.Pause ();
		}

		private void MediaElement_MediaOpened ( object sender, RoutedEventArgs e )
		{
			mediaElement.Play ();
			mediaElement.Pause ();
			timelineSlider.IsEnabled = true;
			buttonImport.IsEnabled = true;
		}

		private void ButtonImport_Click ( object sender, RoutedEventArgs e )
		{
			RenderTargetBitmap rtb = new RenderTargetBitmap ( mediaElement.NaturalVideoWidth, mediaElement.NaturalVideoHeight, 96, 96, PixelFormats.Default );
			mediaElement.Width = mediaElement.NaturalVideoWidth;
			mediaElement.Height = mediaElement.NaturalVideoHeight;
			mediaElement.Measure ( new Size ( mediaElement.NaturalVideoWidth, mediaElement.NaturalVideoHeight ) );
			mediaElement.Arrange ( new Rect ( 0, 0, mediaElement.NaturalVideoWidth, mediaElement.NaturalVideoHeight ) );
			rtb.Render ( mediaElement );
			rtb.Freeze ();
			CapturedSource = rtb;

			DialogResult = true;
		}

		private void Button_Click ( object sender, RoutedEventArgs e )
		{
			DialogResult = false;
		}
	}
}
