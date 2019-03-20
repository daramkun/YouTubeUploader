using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daramee.YouTubeUploader.YouTube
{
	public class PlaylistVideo
	{
		public Google.Apis.YouTube.v3.Data.Video Original { get; private set; }

		public string Id { get { return Original.Id; } }
		public string Title { get { return Original.Snippet.Title; } set { Original.Snippet.Title = value; } }
		public string Description { get { return Original.Snippet.Description; } set { Original.Snippet.Description = value; } }
		public PrivacyStatus PrivacyStatus
		{
			get { return Original.Status.PrivacyStatus.GetPrivacyStatus (); }
			set { Original.Status.PrivacyStatus = value.GetPrivacyStatus (); }
		}

		public PlaylistVideo ( YouTubeSession session, Google.Apis.YouTube.v3.Data.Video original = null )
		{
			Original = original ?? new Google.Apis.YouTube.v3.Data.Video ()
			{
				Snippet = new Google.Apis.YouTube.v3.Data.VideoSnippet (),
				Status = new Google.Apis.YouTube.v3.Data.VideoStatus ()
			};
		}
	}

	public class Playlist
	{
		public IReadOnlyList<PlaylistVideo> DetectedVideos { get; private set; } = new ObservableCollection<PlaylistVideo> ();
		public Google.Apis.YouTube.v3.Data.Playlist Original { get; private set; }

		public string Id { get { return Original.Id; } }
		public string Title { get { return Original.Snippet.Title; } set { Original.Snippet.Title = value; } }
		public string Description { get { return Original.Snippet.Description; } set { Original.Snippet.Description = value; } }
		public PrivacyStatus PrivacyStatus
		{
			get { return Original.Status.PrivacyStatus.GetPrivacyStatus (); }
			set { Original.Status.PrivacyStatus = value.GetPrivacyStatus (); }
		}

		public Playlist ( Google.Apis.YouTube.v3.Data.Playlist original = null )
		{
			Original = original ?? new Google.Apis.YouTube.v3.Data.Playlist ()
			{
				Snippet = new Google.Apis.YouTube.v3.Data.PlaylistSnippet (),
				Status = new Google.Apis.YouTube.v3.Data.PlaylistStatus ()
			};
			//Refresh ();
		}

		/*private void Refresh ()
		{
			if ( Original.Id == null ) return;

			( DetectedVideos as IList<Playlist> ).Clear ();

			youTubeSession.TryGetTarget ( out YouTubeSession session );
			var request = session.YouTubeService.PlaylistItems.List ( "snippet,status" );
			var result = request.Execute ();
			foreach ( var item in result.Items )
			{
				( DetectedVideos as IList<Video> ).Add ( new Video ( session, item ) );
			}
		}*/

		public bool AddVideo ( string videoId )
		{
			if ( videoId == null )
				return false;

			Google.Apis.YouTube.v3.Data.PlaylistItem item = new Google.Apis.YouTube.v3.Data.PlaylistItem ()
			{
				Snippet = new Google.Apis.YouTube.v3.Data.PlaylistItemSnippet ()
				{
					PlaylistId = Original.Id,
					ResourceId = new Google.Apis.YouTube.v3.Data.ResourceId ()
					{
						Kind = "youtube#video",
						VideoId = videoId
					}
				},
			};
			
			var insert = YouTubeSession.SharedYouTubeSession.YouTubeService.PlaylistItems.Insert ( item, "snippet" );
			var result = insert.Execute ();

			return result != null;
		}
	}

	public static class Playlists
	{
		public static IReadOnlyList<Playlist> DetectedPlaylists { get; private set; } = new ObservableCollection<Playlist> ();

		public static async Task Refresh ()
		{
			( DetectedPlaylists as IList<Playlist> ).Clear ();

			string nextToken = null;
			do
			{
				var request = YouTubeSession.SharedYouTubeSession.YouTubeService.Playlists.List ( "snippet,status" );
				request.Mine = true;
				request.MaxResults = 50;
				request.PageToken = nextToken;
				request.Hl = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
				var result = await request.ExecuteAsync ();
				foreach ( var item in result.Items )
				{
					( DetectedPlaylists as IList<Playlist> ).Add ( new Playlist ( item ) );
				}
				nextToken = result.NextPageToken;
			}
			while ( nextToken != null );
		}

		public static bool AddPlaylist ( YouTubeSession session, string title, string description, PrivacyStatus privacyStatus )
		{
			if ( privacyStatus == PrivacyStatus.Unlisted ) return false;

			Google.Apis.YouTube.v3.Data.Playlist pl = new Google.Apis.YouTube.v3.Data.Playlist
			{
				Snippet = new Google.Apis.YouTube.v3.Data.PlaylistSnippet ()
				{
					Title = title,
					Description = description
				},
				Status = new Google.Apis.YouTube.v3.Data.PlaylistStatus ()
				{
					PrivacyStatus = privacyStatus.GetPrivacyStatus ()
				}
			};

			var insert = session.YouTubeService.Playlists.Insert ( pl, "snippet,status" );

			return ( insert.Execute () != null );
		}

		public static bool RemovePlaylist ( YouTubeSession session, Playlist playlist )
		{
			if ( playlist.Id == null ) return false;

			var delete = session.YouTubeService.Playlists.Delete ( playlist.Id );
			var result = delete.Execute ();
			return ( result == null || result == "" );
		}

		public static bool UpdatePlaylist ( YouTubeSession session, Playlist playlist )
		{
			var update = session.YouTubeService.Playlists.Update ( playlist.Original, "snippet,status" );
			var result = update.Execute ();
			return ( result != null );
		}
	}
}
