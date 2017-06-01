using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Daramee.YouTubeUploader
{
	public static class ClipboardImageUtility
	{
		public static BitmapSource GetClipboardImage ()
		{
			BitmapSource bitmapSource = null;

			if ( Clipboard.ContainsImage () )
			{
				bitmapSource = Clipboard.GetImage ();
			}
			else if ( Clipboard.ContainsFileDropList () )
			{
				var fileDropList = Clipboard.GetFileDropList ();

				bitmapSource = new BitmapImage ();
				( bitmapSource as BitmapImage ).BeginInit ();
				( bitmapSource as BitmapImage ).UriSource = new Uri ( fileDropList [ 0 ] );
				( bitmapSource as BitmapImage ).EndInit ();
			}
			else
				return null;

			if ( bitmapSource == null )
				return null;

			bitmapSource = new FormatConvertedBitmap ( bitmapSource, System.Windows.Media.PixelFormats.Bgr24, null, 0 );
			bitmapSource.Freeze ();

			if ( Math.Abs ( ( bitmapSource.PixelWidth / ( double ) bitmapSource.PixelHeight ) - ( 16 / 9.0 ) ) >= float.Epsilon )
			{
				MessageBox.Show ( "이미지 크기가 16:9 비율이어야 합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation );
				return null;
			}

			return bitmapSource;
		}
	}
}
