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
		UploadStart,
		Uploading,
		UploadCompleted,
		UploadFailed,
	}

	public sealed class UploadQueueItem : INotifyPropertyChanged, IDisposable
	{
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

		public async Task<bool> UploadStart ()
		{
			if ( UploadingStatus != UploadingStatus.Queued )
				return false;

			try
			{
				if ( mediaStream == null )
					mediaStream = new FileStream ( FileName.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read );
			}
			catch { return false; }
			
			youTubeSession.TryGetTarget ( out YouTubeSession session );
			var videoInsertRequest = session.YouTubeService.Videos.Insert ( new Video ()
			{
				Snippet = new VideoSnippet ()
				{
					Title = Title,
					Description = Description
				},
				Status = new VideoStatus ()
				{
					PrivacyStatus = GetPrivacyStatus ( PrivacyStatus )
				}
			}, "snippet,status", mediaStream, "video/*" );
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
					BitmapEncoder enc = new JpegBitmapEncoder ();
					enc.Frames.Add ( BitmapFrame.Create ( Thumbnail ) );
					enc.Save ( thumbnailStream );

					thumbnailStream.Position = 0;
					youTubeSession.TryGetTarget ( out YouTubeSession ySession );
					ySession.YouTubeService.Thumbnails.Set ( video.Id, thumbnailStream, "image/jpeg" ).Upload ();
				}
			};
			var uploadStatus = await videoInsertRequest.UploadAsync ();
			if ( uploadStatus.Status == UploadStatus.NotStarted )
				return false;

			return true;
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
