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
	class UploadingStatusToBooleanForStartPauseConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			switch ( ( UploadingStatus ) value )
			{
				case UploadingStatus.PrepareUpload:
				case UploadingStatus.UploadStart:
				case UploadingStatus.UpdateStart:
				case UploadingStatus.UpdateThumbnail:
				case UploadingStatus.UpdatePlaylist:
					return false;

				case UploadingStatus.Queued:
				case UploadingStatus.UploadCompleted:
				case UploadingStatus.UploadFailed:
				case UploadingStatus.UploadCanceled:
				case UploadingStatus.UpdateFailed:
				case UploadingStatus.UpdateComplete:
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
