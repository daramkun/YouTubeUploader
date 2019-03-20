using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Daramee.YouTubeUploader.YouTube;

namespace Daramee.YouTubeUploader.Converters
{
	public class PrivacyStatusToIntegerConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture ) { return ( int ) value; }
		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture ) { return ( PrivacyStatus ) value; }
	}
}
