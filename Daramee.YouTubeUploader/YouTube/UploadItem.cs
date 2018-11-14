using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Daramee.YouTubeUploader.YouTube
{
	public enum UploadingStatus : int
	{
		Queued,
		PrepareUpload,
		UploadStart,
		Uploading,
		UploadCompleted,
		UploadFailed,
		UploadCanceled,

		UpdateStart,
		UpdateComplete,
		UpdateFailed,

		UpdateThumbnail,
		UpdatePlaylist,
	}

	public enum UploadResult : int
	{
		Succeed,
		AlreadyUploading,
		CannotAccesToFile,
		FileSizeIsTooBig,
		FailedUploadRequest,
		CannotStartUpload,
		UploadCanceled,
	}

	public sealed class UploadItem : IUploadItem, IDisposable
	{
		Video video;
		VideosResource.InsertMediaUpload videoInsertRequest;
		CancellationTokenSource cancellationTokenSource;
		TimeSpan startTime;

		Stream mediaStream;
		long lastSentBytes;

		#region Uploading Events
		public event EventHandler Started;
		public event EventHandler Uploading;
		public event EventHandler Completed;
		public event EventHandler Failed;
		#endregion

		#region INotifyPropertyChanged implements
		public event PropertyChangedEventHandler PropertyChanged;
		private void PC ( [CallerMemberName] string propName = "" ) { PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propName ) ); }
		#endregion

		public Uri FileName { get; private set; }
		public Uri URL
		{
			get
			{
				if ( string.IsNullOrEmpty ( video.Id ) ) return null;
				return new Uri ( $"https://youtube.com/watch?v={video.Id}" );
			}
		}

		public string Title { get { return video.Snippet.Title; } set { video.Snippet.Title = value; PC (); } }
		public string Description { get { return video.Snippet.Description; } set { video.Snippet.Description = value; PC (); } }
		public PrivacyStatus PrivacyStatus
		{
			get { return video.Status.PrivacyStatus.GetPrivacyStatus (); }
			set { video.Status.PrivacyStatus = value.GetPrivacyStatus (); PC (); }
		}
		public string Category { get { return video.Snippet.CategoryId; } set { video.Snippet.CategoryId = value; } }
		public IList<string> Tags { get { return video.Snippet.Tags; } }
		public IList<Playlist> Playlists { get; private set; } = new ObservableCollection<Playlist> ();

		int tempDataChunkSize;
		public int DataChunkSize
		{
			set
			{
				if ( videoInsertRequest == null ) tempDataChunkSize = value;
				else videoInsertRequest.ChunkSize = value;
			}
		}

		#region Thumbnail Image Fields and Properties
		BitmapSource thumbnail;
		bool changedThumbnail;

		public BitmapSource Thumbnail { get { return thumbnail; } set { thumbnail = value; changedThumbnail = true; PC (); } }
		#endregion

		#region Uploading Status Fields and Properties
		double progress;
		long totalUploaded;
		UploadingStatus uploadingStatus;
		TimeSpan timeRemaining;

		public double Progress { get { return progress; } private set { progress = value; PC (); } }
		public long TotalUploaded { get { return totalUploaded; } private set { totalUploaded = value; PC (); } }
		public long FileSize { get { return mediaStream != null ? mediaStream.Length : 0; } }
		public UploadingStatus UploadingStatus { get { return uploadingStatus; } private set { uploadingStatus = value; PC (); } }
		public TimeSpan TimeRemaining { get { return timeRemaining; } private set { timeRemaining = value; PC (); } }

		public bool IsManuallyPaused { get; private set; } = false;
		#endregion

		public UploadItem ( string filename )
		{
			FileName = new Uri ( filename, UriKind.Absolute );
			mediaStream = new FileStream ( filename, FileMode.Open, FileAccess.Read, FileShare.Read );
			if ( mediaStream.Length >= 68719476736 )
			{
				mediaStream.Dispose ();
				mediaStream = null;
				throw new ArgumentException ( "File size is to big. Must be filesize under 64GB.", "filename" );
			}

			video = new Video ()
			{
				Snippet = new VideoSnippet () { Tags = new ObservableCollection<string> () },
				Status = new VideoStatus ()
			};

			Title = Path.GetFileNameWithoutExtension ( filename );
			Description = "";
			PrivacyStatus = PrivacyStatus.Public;
			Category = Categories.DetectedCategories [ 0 ].Id;

			Progress = 0;
			TotalUploaded = 0;
			UploadingStatus = UploadingStatus.Queued;
		}

		public void Dispose ()
		{
			StatusReset ();

			mediaStream.Dispose ();
			mediaStream = null;
		}

		public void StatusReset ()
		{
			mediaStream.Position = 0;

			Progress = 0;
			TotalUploaded = 0;
			UploadingStatus = UploadingStatus.Queued;

			changedThumbnail = true;

			video.Id = null;
			videoInsertRequest = null;

			TimeRemaining = new TimeSpan ();

			if ( cancellationTokenSource != null )
			{
				IsManuallyPaused = true;
				cancellationTokenSource.Cancel ();
			}
			cancellationTokenSource = null;
		}

		public void Pause ()
		{
			if ( cancellationTokenSource != null )
			{
				IsManuallyPaused = true;
				cancellationTokenSource.Cancel ();
			}
		}

		public async Task<UploadResult> UploadAsync ()
		{
			if ( UploadingStatus == UploadingStatus.UploadCanceled )
				StatusReset ();

			if ( video.Id != null && (
					UploadingStatus == UploadingStatus.UploadFailed ||
					UploadingStatus == UploadingStatus.UploadCompleted ||
					UploadingStatus == UploadingStatus.UpdateFailed ||
					UploadingStatus == UploadingStatus.UpdateComplete
				) )
			{
				UploadingStatus = UploadingStatus.UpdateStart;

				await UploadThumbnail ( video.Id );

				var videoUpdateRequest = YouTubeSession.SharedYouTubeSession.YouTubeService.Videos.Update ( video, "snippet,status" );
				var result = await videoUpdateRequest.ExecuteAsync ();
				if ( result != null )
				{
					UploadingStatus = UploadingStatus.UpdateComplete;
					Completed?.Invoke ( this, EventArgs.Empty );
					return UploadResult.Succeed;
				}
				else
				{
					UploadingStatus = UploadingStatus.UpdateFailed;
					Failed?.Invoke ( this, EventArgs.Empty );
					return UploadResult.Succeed;
				}
			}

			if ( !( UploadingStatus == UploadingStatus.Queued || UploadingStatus == UploadingStatus.UploadFailed ) )
				return UploadResult.AlreadyUploading;

			UploadingStatus = UploadingStatus.PrepareUpload;

			bool virIsNull = videoInsertRequest == null;
			if ( virIsNull )
			{
				videoInsertRequest = YouTubeSession.SharedYouTubeSession.YouTubeService.Videos.Insert ( video, "snippet,status", mediaStream, "video/*" );
				if ( videoInsertRequest == null )
				{
					UploadingStatus = UploadingStatus.UploadFailed;
					return UploadResult.FailedUploadRequest;
				}
				videoInsertRequest.ChunkSize = tempDataChunkSize;

				videoInsertRequest.ProgressChanged += UploadProgressChanged;
				videoInsertRequest.ResponseReceived += UploadResponseReceived;
			}

			try
			{
				startTime = DateTime.Now.TimeOfDay;
				cancellationTokenSource = new CancellationTokenSource ();
				var uploadStatus = virIsNull ?
					await videoInsertRequest.UploadAsync ( cancellationTokenSource.Token ) :
					await videoInsertRequest.ResumeAsync ( cancellationTokenSource.Token );
				cancellationTokenSource.Dispose ();
				video = videoInsertRequest.ResponseBody ?? video;
				if ( uploadStatus.Status == UploadStatus.NotStarted )
				{
					UploadingStatus = UploadingStatus.UploadFailed;
					return UploadResult.CannotStartUpload;
				}
			}
			catch
			{
				UploadingStatus = UploadingStatus.UploadFailed;
				return UploadResult.UploadCanceled;
			}

			PC ( nameof ( URL ) );

			return UploadResult.Succeed;
		}

		private void UploadProgressChanged ( IUploadProgress uploadProgress )
		{
			TotalUploaded = uploadProgress.BytesSent;
			Progress = uploadProgress.BytesSent / ( double ) mediaStream.Length;
			double percentage = ( uploadProgress.BytesSent - lastSentBytes ) / ( double ) mediaStream.Length;
			lastSentBytes = uploadProgress.BytesSent;
			double totalSeconds = ( DateTime.Now.TimeOfDay - startTime ).TotalSeconds;
			TimeRemaining = Progress != 0 ? TimeSpan.FromSeconds ( ( totalSeconds / Progress ) * ( 1 - Progress ) ) : TimeSpan.FromDays ( 999 );

			switch ( uploadProgress.Status )
			{
				case UploadStatus.Starting:
					startTime = DateTime.Now.TimeOfDay;
					UploadingStatus = UploadingStatus.UploadStart;
					Started?.Invoke ( this, EventArgs.Empty );
					break;

				case UploadStatus.Uploading:
					UploadingStatus = UploadingStatus.Uploading;
					Uploading?.Invoke ( this, EventArgs.Empty );
					break;

				case UploadStatus.Failed:
					UploadingStatus = UploadingStatus.UploadFailed;
					Failed?.Invoke ( this, EventArgs.Empty );
					break;

				case UploadStatus.Completed:
					UploadingStatus = UploadingStatus.UploadCompleted;
					Uploading?.Invoke ( this, EventArgs.Empty );
					Completed?.Invoke ( this, EventArgs.Empty );
					mediaStream.Dispose ();
					mediaStream = null;
					break;
			}
		}

		private async void UploadResponseReceived ( Video video )
		{
			await UploadThumbnail ( video.Id );

			UploadingStatus = UploadingStatus.UpdatePlaylist;
			foreach ( var playlist in Playlists )
				playlist.AddVideo ( video.Id );
		}

		static readonly IEnumerable<int> QualityLevels = new [] { 100, 80, 60, 40, 20, 1 };
		private async Task UploadThumbnail ( string id )
		{
			if ( changedThumbnail == false || Thumbnail == null )
				return;

			using ( MemoryStream thumbnailStream = new MemoryStream () )
			{
				JpegBitmapEncoder encoder = new JpegBitmapEncoder ();
				foreach ( int quality in QualityLevels )
				{
					thumbnailStream.SetLength ( 0 );
					encoder.Frames.Clear ();

					encoder.QualityLevel = quality;
					encoder.Frames.Add ( BitmapFrame.Create ( Thumbnail ) );
					encoder.Save ( thumbnailStream );
					thumbnailStream.Position = 0;

					// File size over 2MB? -> To Lower quality
					if ( thumbnailStream.Length < 2097152 )
						break;
				}

				// Quality Level is 1 and Still File size over 2MB? -> No upload Thumbnail image.
				if ( thumbnailStream.Length < 2097152 )
				{
					UploadingStatus = UploadingStatus.UpdateThumbnail;
					await YouTubeSession.SharedYouTubeSession.YouTubeService.Thumbnails.Set ( id, thumbnailStream, "image/jpeg" ).UploadAsync ();
				}
			}
			changedThumbnail = false;
		}
	}
}
