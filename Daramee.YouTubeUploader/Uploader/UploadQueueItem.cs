using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3.Data;

namespace Daramee.YouTubeUploader.Uploader
{
	public enum PrivacyStatus : int
	{
		Public,
		Unlisted,
		Private,
	}

	public enum UploadingStatus : int
	{
		Queued,
		PrepareUpload,
		UploadStart,
		Uploading,
		UploadCompleted,
		UploadFailed,
	}

	public enum UploadResult : int
	{
		Succeed,
		AlreadyUploading,
		CannotAccesToFile,
		FailedUploadRequest,
		CannotStartUpload
	}

	public sealed class UploadQueueItem : INotifyPropertyChanged, IDisposable
	{
		private static readonly IEnumerable<int> QualityLevels = new [] { 100, 80, 60, 40, 20, 1 };

		WeakReference<YouTubeSession> youTubeSession;

		Stream mediaStream;
		long totalSentBytes;
		
		string title;
		string description;
		PrivacyStatus privacyStatus;
		BitmapSource thumbnail;

		public event PropertyChangedEventHandler PropertyChanged;
		private void PC ( [CallerMemberName] string propName = "" ) { PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propName ) ); }

		public Uri FileName { get; private set; }
		public string Title { get { return title; } set { title = value; PC (); } }
		public string Description { get { return description; } set { description = value; PC (); } }
		public PrivacyStatus PrivacyStatus { get { return privacyStatus; } set { privacyStatus = value; PC (); } }
		public BitmapSource Thumbnail { get { return thumbnail; } set { thumbnail = value; PC (); } }

		public double Progress { get; private set; }
		public UploadingStatus UploadingStatus { get; private set; }

		public event EventHandler Started;
		public event EventHandler Uploading;
		public event EventHandler Completed;
		public event EventHandler Failed;

		public UploadQueueItem ( YouTubeSession session, string filename )
		{
			youTubeSession = new WeakReference<YouTubeSession> ( session );

			FileName = new Uri ( filename, UriKind.Absolute );
			Title = Path.GetFileNameWithoutExtension ( filename );
			Description = "";
			PrivacyStatus = PrivacyStatus.Public;

			Progress = 0;
			UploadingStatus = UploadingStatus.Queued;
		}

		public void Dispose ()
		{
			if ( mediaStream != null )
				mediaStream.Dispose ();
			mediaStream = null;
		}

		public async Task<UploadResult> UploadStart ()
		{
			if ( !( UploadingStatus == UploadingStatus.Queued || UploadingStatus == UploadingStatus.UploadFailed ) )
				return UploadResult.AlreadyUploading;

			UploadingStatus = UploadingStatus.PrepareUpload;

			try
			{
				if ( mediaStream == null )
					mediaStream = new FileStream ( FileName.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read );
			}
			catch { UploadingStatus = UploadingStatus.UploadFailed; return UploadResult.CannotAccesToFile; }
			
			youTubeSession.TryGetTarget ( out YouTubeSession session );
			var videoInsertRequest = session.YouTubeService.Videos.Insert ( new Video ()
			{
				Snippet = new VideoSnippet ()
				{
					Title = Title.Trim (),
					Description = Description
				},
				Status = new VideoStatus ()
				{
					PrivacyStatus = GetPrivacyStatus ( PrivacyStatus )
				}
			}, "snippet,status", mediaStream, "video/*" );
			if ( videoInsertRequest == null )
			{
				UploadingStatus = UploadingStatus.UploadFailed;
				return UploadResult.FailedUploadRequest;
			}
			
			videoInsertRequest.ChunkSize = ResumableUpload.MinimumChunkSize;
			videoInsertRequest.ProgressChanged += ( uploadProgress ) =>
			{
				totalSentBytes = uploadProgress.BytesSent;
				Progress = totalSentBytes / ( double ) mediaStream.Length;
				PC ( nameof ( Progress ) );

				switch ( uploadProgress.Status )
				{
					case UploadStatus.Starting:
						UploadingStatus = UploadingStatus.UploadStart;
						PC ( nameof ( UploadingStatus ) );
						Started?.Invoke ( this, EventArgs.Empty );
						break;

					case UploadStatus.Uploading:
						UploadingStatus = UploadingStatus.Uploading;
						PC ( nameof ( UploadingStatus ) );
						Uploading?.Invoke ( this, EventArgs.Empty );
						break;

					case UploadStatus.Failed:
						UploadingStatus = UploadingStatus.UploadFailed;
						PC ( nameof ( UploadingStatus ) );
						Failed?.Invoke ( this, EventArgs.Empty );
						break;

					case UploadStatus.Completed:
						UploadingStatus = UploadingStatus.UploadCompleted;
						PC ( nameof ( UploadingStatus ) );
						Completed?.Invoke ( this, EventArgs.Empty );
						break;
				}
			};
			videoInsertRequest.ResponseReceived += ( video ) =>
			{
				if ( thumbnail == null )
					return;

				using ( MemoryStream thumbnailStream = new MemoryStream () )
				{
					JpegBitmapEncoder encoder = new JpegBitmapEncoder ();
					foreach ( int quality in QualityLevels )
					{
						encoder.QualityLevel = quality;
						encoder.Frames.Add ( BitmapFrame.Create ( Thumbnail ) );
						thumbnailStream.SetLength ( 0 );
						encoder.Save ( thumbnailStream );
						thumbnailStream.Position = 0;

						if ( thumbnailStream.Length < 2097152 )
							break;
					}

					if ( thumbnailStream.Length >= 2097152 )
						return;

					youTubeSession.TryGetTarget ( out YouTubeSession ySession );
					ySession.YouTubeService.Thumbnails.Set ( video.Id, thumbnailStream, "image/jpeg" ).Upload ();
				}
			};

			var uploadStatus = await videoInsertRequest.UploadAsync ();
			if ( uploadStatus.Status == UploadStatus.NotStarted )
			{
				UploadingStatus = UploadingStatus.UploadFailed;
				return UploadResult.CannotStartUpload;
			}

			return UploadResult.Succeed;
		}

		private static string GetPrivacyStatus ( PrivacyStatus ps )
		{
			switch ( ps )
			{
				case PrivacyStatus.Public: return "public";
				case PrivacyStatus.Private: return "private";
				case PrivacyStatus.Unlisted: return "unlisted";
				default: return "";
			}
		}
	}
}
