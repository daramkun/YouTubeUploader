using Daramee.YouTubeUploader.YouTube;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Daramee.YouTubeUploader.Converters
{
	class UploadingStatusToPathConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			switch ( ( UploadingStatus ) value )
			{
				case UploadingStatus.Uploading:
					return Application.Current.FindResource ( "pathUploadPause" );

				default: return Application.Current.FindResource ( "pathUploadStart" );
			}
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotImplementedException ();
		}
	}
}
