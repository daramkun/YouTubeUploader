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
	class UploadingStatusToBooleanForEditorConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			UploadingStatus status = ( UploadingStatus ) value;
			switch ( status )
			{
				case UploadingStatus.PrepareUpload:
				case UploadingStatus.UploadStart:
				case UploadingStatus.UpdateStart:
				case UploadingStatus.Uploading:
				case UploadingStatus.UpdateThumbnail:
				case UploadingStatus.UpdatePlaylist:
					return false;

				default: return true;
			}
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotImplementedException ();
		}
	}
}
