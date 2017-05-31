using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Daramee.YouTubeUploader.Uploader;

namespace Daramee.YouTubeUploader.Converters
{
	class UploadingStatusToBooleanForRemoveConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			switch ( ( UploadingStatus ) value )
			{
				case UploadingStatus.Queued:
				case UploadingStatus.UploadFailed:
				case UploadingStatus.UploadCompleted:
				case UploadingStatus.UpdateFailed:
				case UploadingStatus.UpdateComplete:
					return true;

				case UploadingStatus.PrepareUpload:
				case UploadingStatus.UploadStart:
				case UploadingStatus.Uploading:
				case UploadingStatus.UpdateStart:
					return false;

				default: return false;
			}
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotImplementedException ();
		}
	}
}
