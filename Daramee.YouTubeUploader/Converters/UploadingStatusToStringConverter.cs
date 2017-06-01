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
	public class UploadingStatusToStringConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			switch ( ( UploadingStatus ) value )
			{
				case UploadingStatus.Queued: return "업로드를 준비 중입니다.";
				case UploadingStatus.PrepareUpload: return "업로드 시작을 준비합니다.";
				case UploadingStatus.UploadStart: return "업로드가 시작됐습니다.";
				case UploadingStatus.Uploading: return "업로드 중...";
				case UploadingStatus.UploadCompleted: return "업로드에 성공했습니다.";
				case UploadingStatus.UploadFailed: return "업로드에 실패했습니다.";

				case UploadingStatus.UpdateStart: return "업데이트가 시작됐습니다.";
				case UploadingStatus.UpdateComplete: return "업데이트에 성공했습니다.";
				case UploadingStatus.UpdateFailed: return "업데이트에 실패했습니다.";

				default: return "";
			}
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotImplementedException ();
		}
	}
}
