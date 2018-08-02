using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Daramee.DaramCommonLib;
using Daramee.YouTubeUploader.YouTube;

namespace Daramee.YouTubeUploader.Converters
{
	public class UploadingStatusToStringConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			switch ( ( UploadingStatus ) value )
			{
				case UploadingStatus.Queued:
					return StringTable.SharedStrings [ "upload_status_queued" ];
				case UploadingStatus.PrepareUpload:
					return StringTable.SharedStrings [ "upload_status_prepareupload" ];
				case UploadingStatus.UploadStart:
					return StringTable.SharedStrings [ "upload_status_uploadstart" ];
				case UploadingStatus.Uploading:
					return StringTable.SharedStrings [ "upload_status_uploading" ];
				case UploadingStatus.UploadCompleted:
					return StringTable.SharedStrings [ "upload_status_uploadcomplete" ];
				case UploadingStatus.UploadFailed:
					return StringTable.SharedStrings [ "upload_status_uploadfail" ];

				case UploadingStatus.UpdateStart:
					return StringTable.SharedStrings [ "upload_status_updatestart" ];
				case UploadingStatus.UpdateComplete:
					return StringTable.SharedStrings [ "upload_status_updatecomplete" ];
				case UploadingStatus.UpdateFailed:
					return StringTable.SharedStrings [ "upload_status_updatefail" ];

				case UploadingStatus.UpdateThumbnail:
					return StringTable.SharedStrings [ "upload_status_updatethumbnail" ];
				case UploadingStatus.UpdatePlaylist:
					return StringTable.SharedStrings [ "upload_status_updateplaylist" ];

				default: return "";
			}
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotImplementedException ();
		}
	}
}
