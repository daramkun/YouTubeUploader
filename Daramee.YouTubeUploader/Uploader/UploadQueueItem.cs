using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Imaging;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Daramee.YouTubeUploader.Uploader
{
	public enum PrivacyStatus : int
	{
		Unknown = -1,
		Public = 0,
		Unlisted = 1,
		Private = 2,
	}

	public static class PrivacyStatusExtension
	{
		public static string GetPrivacyStatus ( this PrivacyStatus ps )
		{
			switch ( ps )
			{
				case PrivacyStatus.Public: return "public";
				case PrivacyStatus.Private: return "private";
				case PrivacyStatus.Unlisted: return "unlisted";
				default: return "";
			}
		}

		public static PrivacyStatus GetPrivacyStatus ( this string ps )
		{
			switch ( ps )
			{
				case "public": return PrivacyStatus.Public;
				case "private": return PrivacyStatus.Private;
				case "unlisted": return PrivacyStatus.Unlisted;
				default: return PrivacyStatus.Unknown;
			}
		}
	}

	public enum UploadingStatus : int
	{
		Queued,
		PrepareUpload,
		UploadStart,
		Uploading,
		UploadCompleted,
		UploadFailed,
		UpdateStart,
		UpdateComplete,
		UpdateFailed,
	}

	public enum UploadResult : int
	{
		Succeed,
		AlreadyUploading,
		CannotAccesToFile,
		FailedUploadRequest,
		CannotStartUpload,
		UploadCanceled,
		UpdateFailed,
	}

	public sealed class UploadQueueItem : INotifyPropertyChanged, IDisposable
	{
		private static readonly IEnumerable<int> QualityLevels = new [] { 100, 80, 60, 40, 20, 1 };

		WeakReference<YouTubeSession> youtubeSession;

		Google.Apis.YouTube.v3.Data.Video video;
		VideosResource.InsertMediaUpload videoInsertRequest;
		TimeSpan startTime;

		Stream mediaStream;
		long lastSentBytes;

		BitmapSource thumbnail;
		bool changedThumbnail;

		public event PropertyChangedEventHandler PropertyChanged;
		private void PC ( [CallerMemberName] string propName = "" ) { PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propName ) ); }

		public Uri FileName { get; private set; }
		public string Title { get { return video.Snippet.Title; } set { video.Snippet.Title = value; PC (); } }
		public string Description { get { return video.Snippet.Description; } set { video.Snippet.Description = value; PC (); } }
		public PrivacyStatus PrivacyStatus
		{
			get { return video.Status.PrivacyStatus.GetPrivacyStatus (); }
			set { video.Status.PrivacyStatus = value.GetPrivacyStatus (); PC (); }
		}
		public string Category { get { return video.Snippet.CategoryId; } set { video.Snippet.CategoryId = value; } }
		public IList<string> Tags { get { return video.Snippet.Tags; } }
		public BitmapSource Thumbnail { get { return thumbnail; } set { thumbnail = value; changedThumbnail = true; PC (); } }

		public IList<Playlist> Playlists { get; private set; } = new ObservableCollection<Playlist> ();

		public double Progress { get; private set; }
		public UploadingStatus UploadingStatus { get; private set; }
		public TimeSpan TimeRemaining { get; private set; }

		public event EventHandler Started;
		public event EventHandler Uploading;
		public event EventHandler Completed;
		public event EventHandler Failed;

		public UploadQueueItem ( YouTubeSession session, string filename )
		{
			youtubeSession = new WeakReference<YouTubeSession> ( session );

			FileName = new Uri ( filename, UriKind.Absolute );
			var fileInfo = new FileInfo ( filename );

			video = new Google.Apis.YouTube.v3.Data.Video ()
			{
				Snippet = new VideoSnippet ()
				{
					Tags = new ObservableCollection<string> ()
				},
				Status = new VideoStatus ()
			};

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
			youtubeSession.TryGetTarget ( out YouTubeSession session );

			if ( video.Id != null && (
					UploadingStatus == UploadingStatus.UploadFailed ||
					UploadingStatus == UploadingStatus.UploadCompleted ||
					UploadingStatus == UploadingStatus.UpdateFailed ||
					UploadingStatus == UploadingStatus.UpdateComplete
				) )
			{
				UploadingStatus = UploadingStatus.UpdateStart;

				await UploadThumbnail ( video.Id );

				var videoUpdateRequest = session.YouTubeService.Videos.Update ( video, "snippet,status" );
				var result = await videoUpdateRequest.ExecuteAsync ();
				if ( result != null )
				{
					UploadingStatus = UploadingStatus.UpdateComplete;
					PC ( nameof ( UploadingStatus ) );
					Completed?.Invoke ( this, EventArgs.Empty );
					return UploadResult.Succeed;
				}
				else
				{
					UploadingStatus = UploadingStatus.UpdateFailed;
					PC ( nameof ( UploadingStatus ) );
					Failed?.Invoke ( this, EventArgs.Empty );
					return UploadResult.UpdateFailed;
				}
			}

			if ( !( UploadingStatus == UploadingStatus.Queued || UploadingStatus == UploadingStatus.UploadFailed ) )
				return UploadResult.AlreadyUploading;

			UploadingStatus = UploadingStatus.PrepareUpload;

			try
			{
				if ( mediaStream == null )
					mediaStream = new FileStream ( HttpUtility.UrlDecode ( FileName.AbsolutePath ), FileMode.Open, FileAccess.Read, FileShare.Read );
			}
			catch { UploadingStatus = UploadingStatus.UploadFailed; return UploadResult.CannotAccesToFile; }

			bool virIsNull = videoInsertRequest == null;
			if ( virIsNull )
			{
				videoInsertRequest = session.YouTubeService.Videos.Insert ( video, "snippet,status", mediaStream, "video/*" );
				if ( videoInsertRequest == null )
				{
					UploadingStatus = UploadingStatus.UploadFailed;
					return UploadResult.FailedUploadRequest;
				}

				videoInsertRequest.ChunkSize = ResumableUpload.MinimumChunkSize;
				videoInsertRequest.ProgressChanged += ( uploadProgress ) =>
				{
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

					PC ( nameof ( Progress ) );
					PC ( nameof ( UploadingStatus ) );
					PC ( nameof ( TimeRemaining ) );
				};
				videoInsertRequest.ResponseReceived += async ( video ) =>
				{
					await UploadThumbnail ( video.Id );

					foreach ( var playlist in Playlists )
						playlist.AddVideo ( video.Id );
				};
			}

			try
			{
				startTime = DateTime.Now.TimeOfDay;
				var uploadStatus = virIsNull ? await videoInsertRequest.UploadAsync () : await videoInsertRequest.ResumeAsync ();
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

			return UploadResult.Succeed;
		}

		private async Task UploadThumbnail ( string id )
		{
			if ( changedThumbnail == false || Thumbnail == null )
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

				if ( thumbnailStream.Length < 2097152 )
				{
					youtubeSession.TryGetTarget ( out YouTubeSession session );
					var result = await session.YouTubeService.Thumbnails.Set ( id, thumbnailStream, "image/jpeg" ).UploadAsync ();
				}
			}
			changedThumbnail = false;
		}
	}
}
