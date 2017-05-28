using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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
		CannotStartUpload,
		UploadCanceled,
		ComponentError,
	}

	public sealed class UploadQueueItem : INotifyPropertyChanged, IDisposable
	{
		private static readonly IEnumerable<int> QualityLevels = new [] { 100, 80, 60, 40, 20, 1 };

		WeakReference<YouTubeSession> youTubeSession;

		Video video;
		VideosResource.InsertMediaUpload videoInsertRequest;

		Stream mediaStream;
		long totalSentBytes;
		
		BitmapSource thumbnail;

		public event PropertyChangedEventHandler PropertyChanged;
		private void PC ( [CallerMemberName] string propName = "" ) { PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propName ) ); }

		public Uri FileName { get; private set; }
		public string Title { get { return video.Snippet.Title; } set { video.Snippet.Title = value; PC (); } }
		public string Description { get { return video.Snippet.Description; } set { video.Snippet.Description = value; PC (); } }
		public PrivacyStatus PrivacyStatus
		{
			get { return GetPrivacyStatus ( video.Status.PrivacyStatus ); }
			set { video.Status.PrivacyStatus = GetPrivacyStatus ( value ); PC (); }
		}
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
			video = new Video ();
			video.Snippet = new VideoSnippet ();
			video.Status = new VideoStatus ();

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
					mediaStream = new FileStream ( HttpUtility.UrlDecode ( FileName.AbsolutePath ), FileMode.Open, FileAccess.Read, FileShare.Read );
			}
			catch { UploadingStatus = UploadingStatus.UploadFailed; return UploadResult.CannotAccesToFile; }

			bool virIsNull = videoInsertRequest == null;
			if ( virIsNull )
			{
				youTubeSession.TryGetTarget ( out YouTubeSession session );
				videoInsertRequest = session.YouTubeService.Videos.Insert ( video, "snippet,status", mediaStream, "video/*" );
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
							mediaStream.Dispose ();
							mediaStream = null;
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
			}

			try
			{
				var uploadStatus = virIsNull ? await videoInsertRequest.UploadAsync () : await videoInsertRequest.ResumeAsync ();
				if ( uploadStatus.Status == UploadStatus.NotStarted )
				{
					UploadingStatus = UploadingStatus.UploadFailed;
					return UploadResult.CannotStartUpload;
				}
				else if ( uploadStatus.Status == UploadStatus.Failed )
				{
					video = videoInsertRequest.ResponseBody ?? video;
				}
			}
			catch ( MissingMethodException ex )
			{
				UploadingStatus = UploadingStatus.UploadFailed;
				return UploadResult.ComponentError;
			}
			catch
			{
				UploadingStatus = UploadingStatus.UploadFailed;
				return UploadResult.UploadCanceled;
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

		private static PrivacyStatus GetPrivacyStatus ( string ps )
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
}
