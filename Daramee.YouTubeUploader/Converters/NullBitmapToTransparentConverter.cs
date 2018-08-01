using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Daramee.YouTubeUploader.Converters
{
	class NullBitmapToTransparentConverter : IValueConverter
	{
		BitmapSource nullBitmap;

		public NullBitmapToTransparentConverter ()
		{
			byte [] pixels = new byte [ 96 * 54 ];
			for ( int y = 0; y < 54; ++y )
			{
				for ( int x = 0; x < 96; ++x )
				{
					if ( ( x / 8 % 2 == 0 && y / 8 % 2 == 0 ) ||
						( x / 8 % 2 == 1 && y / 8 % 2 == 1 ) )
						pixels [ y * 96 + x ] = 0xdd;
					else
						pixels [ y * 96 + x ] = 0xff;
				}
			}
			nullBitmap = BitmapSource.Create ( 96, 54, 96, 96, PixelFormats.Gray8, null, pixels, 96 );
		}

		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			if ( value == null ) return nullBitmap;
			return value;
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotImplementedException ();
		}
	}
}
