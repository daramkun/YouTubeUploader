using Daramee.YouTubeUploader.Uploader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Daramee.YouTubeUploader.Converters
{
	class UploadingStatusToBooleanForPauseConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			switch ( ( UploadingStatus ) value )
			{
			case UploadingStatus.Uploading:
				return true;
				
			default: return false;
			}
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotImplementedException ();
		}
	}
}
