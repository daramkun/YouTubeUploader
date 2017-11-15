using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Daramee.YouTubeUploader.Converters
{
	class ByteUnitConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			long original = ( long ) value;
			if ( original < 1024 )
				return $"{original}B";
			float divided = original / 1024.0f;
			if ( divided < 1024 )
				return $"{Math.Round ( divided, 3, MidpointRounding.AwayFromZero )}KB";
			divided /= 1024;
			if ( divided < 1024 )
				return $"{Math.Round ( divided, 3, MidpointRounding.AwayFromZero )}MB";
			return $"{Math.Round ( divided / 1024, 3, MidpointRounding.AwayFromZero )}GB";
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotImplementedException ();
		}
	}
}
