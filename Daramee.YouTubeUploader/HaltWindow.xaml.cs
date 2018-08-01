using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Daramee.YouTubeUploader
{
	/// <summary>
	/// HaltWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class HaltWindow : Window
	{
		private const int GWL_STYLE = -16;
		private const int WS_SYSMENU = 0x80000;
		[DllImport ( "user32.dll", SetLastError = true )]
		private static extern int GetWindowLong ( IntPtr hWnd, int nIndex );
		[DllImport ( "user32.dll" )]
		private static extern int SetWindowLong ( IntPtr hWnd, int nIndex, int dwNewLong );

		public HaltWindow ()
		{
			InitializeComponent ();

			var hwnd = new WindowInteropHelper ( this ).Handle;
			SetWindowLong ( hwnd, GWL_STYLE, GetWindowLong ( hwnd, GWL_STYLE ) & ~WS_SYSMENU );
		}

		private async void Window_Loaded ( object sender, RoutedEventArgs e )
		{
			await Task.Run ( () =>
			{
				for ( int i = 0; i < 30; ++i )
				{
					Dispatcher.BeginInvoke ( new Action ( () => { progressToHalt.Value += 1; } ) );
					Thread.Sleep ( 1000 );
				}
			} );
		}

		private void ProgressToHalt_ValueChanged ( object sender, RoutedPropertyChangedEventArgs<double> e )
		{
			if ( progressToHalt.Value == progressToHalt.Maximum )
			{
				var psi = new ProcessStartInfo ( "shutdown", "/s /t 0" )
				{
					CreateNoWindow = true,
					UseShellExecute = false
				};
				Process.Start ( psi );
				Close ();
			}
		}

		private void Button_Click ( object sender, RoutedEventArgs e )
		{
			progressToHalt.Maximum += 1;
			Close ();
		}
	}
}
