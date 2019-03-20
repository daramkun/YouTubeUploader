using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Daramee.YouTubeUploader.YouTube
{
	public interface IUploadItem : INotifyPropertyChanged
	{
		Uri FileName { get; }
		Uri URL { get; }

		string Title { get; set; }
		string Description { get; set; }
		PrivacyStatus PrivacyStatus { get; set; }
		string Category { get; set; }

		IList<string> Tags { get; }
		IList<Playlist> Playlists { get; }

		BitmapSource Thumbnail { get; set; }

		double Progress { get; }
		long TotalUploaded { get; }
		long FileSize { get; }
		UploadingStatus UploadingStatus { get; }
		TimeSpan TimeRemaining { get; }

		bool IsManuallyPaused { get; }

		void StatusReset ();
		void Pause ();
		Task<UploadResult> UploadAsync ();
	}
}
