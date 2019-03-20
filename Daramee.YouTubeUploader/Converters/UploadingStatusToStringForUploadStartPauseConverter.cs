using Daramee.DaramCommonLib;
using Daramee.YouTubeUploader.YouTube;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Daramee.YouTubeUploader.Converters
{
	class UploadingStatusToStringForUploadStartPauseConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			switch ( ( UploadingStatus ) value )
			{
				case UploadingStatus.Uploading:
					return StringTable.SharedStrings [ "item_status_uploadpause" ];

				default: return StringTable.SharedStrings [ "item_status_uploadstart" ];
			}
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotImplementedException ();
		}
	}
}
