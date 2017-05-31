using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Daramee.YouTubeUploader.Uploader;

namespace Daramee.YouTubeUploader.Converters
{
	class UploadingStatusToColorConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			switch ( ( UploadingStatus ) value )
			{
				case UploadingStatus.Queued: return Color.FromRgb ( 0, 0, 0 );
				case UploadingStatus.UploadFailed:
				case UploadingStatus.UpdateFailed: return Color.FromRgb ( 255, 0, 0 );

				case UploadingStatus.UploadStart:
				case UploadingStatus.Uploading:
				case UploadingStatus.UpdateStart:
					return Color.FromRgb ( 0, 0, 0 );

				case UploadingStatus.UploadCompleted:
				case UploadingStatus.UpdateComplete:
					return Color.FromRgb ( 0, 0, 255 );

				default: return false;
			}
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotImplementedException ();
		}
	}
}
