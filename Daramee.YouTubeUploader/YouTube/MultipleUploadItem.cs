using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Daramee.YouTubeUploader.YouTube
{
	public class MultipleUploadItem : IUploadItem
	{
		IReadOnlyCollection<IUploadItem> collection;

		public Uri FileName
		{
			get
			{
				if ( collection.Count > 1 ) return null;
				return collection.First ().FileName;
			}
		}

		public string Title
		{
			get
			{
				string prev = collection.First ().Title;
				foreach ( var item in collection )
					if ( item.Title != prev ) return "";
					else prev = item.Title;
				return prev;
			}
			set
			{
				foreach ( var item in collection )
					item.Title = value;
				PC ( nameof ( Title ) );
			}
		}

		public string Description
		{
			get
			{
				string prev = collection.First ().Description;
				foreach ( var item in collection )
					if ( item.Description != prev ) return "";
					else prev = item.Description;
				return prev;
			}
			set
			{
				foreach ( var item in collection )
					item.Description = value;
				PC ( nameof ( Description ) );
			}
		}

		public PrivacyStatus PrivacyStatus
		{
			get
			{
				PrivacyStatus prev = collection.First ().PrivacyStatus;
				foreach ( var item in collection )
					if ( item.PrivacyStatus != prev )
						return PrivacyStatus.Unknown;
				return prev;
			}
			set
			{
				foreach ( var item in collection )
					item.PrivacyStatus = value;
				PC ( nameof ( PrivacyStatus ) );
			}
		}

		public string Category
		{
			get
			{
				string prev = collection.First ().Category;
				foreach ( var item in collection )
					if ( item.Category != prev )
						return "";
				return prev;
			}
			set
			{
				foreach ( var item in collection )
					item.Category = value;
				PC ( nameof ( Category ) );
			}
		}

		readonly MultipleList<string> tags;
		public IList<string> Tags => tags;

		readonly MultipleList<Playlist> playlists;
		public IList<Playlist> Playlists => playlists;

		public BitmapSource Thumbnail
		{
			get
			{
				BitmapSource prev = collection.First ().Thumbnail;
				foreach ( var item in collection )
					if ( item.Thumbnail != prev )
						return null;
				return prev;
			}
			set
			{
				foreach ( var item in collection )
					item.Thumbnail = value;
				PC ( nameof ( Thumbnail ) );
			}
		}

		public double Progress => throw new NotImplementedException ();
		public long TotalUploaded => throw new NotImplementedException ();
		public long FileSize => throw new NotImplementedException ();
		public UploadingStatus UploadingStatus
		{
			get
			{
				UploadingStatus prev = collection.First ().UploadingStatus;
				foreach ( var item in collection )
					if ( item.UploadingStatus != prev )
						return UploadingStatus.Uploading;
				return prev;
			}
		}
		public TimeSpan TimeRemaining => throw new NotImplementedException ();
		public bool IsManuallyPaused => throw new NotImplementedException ();

		public event PropertyChangedEventHandler PropertyChanged;

		private void PC ( string v ) { PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( v ) ); }

		public void StatusReset ()
		{
			foreach ( IUploadItem item in collection )
				item.StatusReset ();
		}

		public void Pause ()
		{
			foreach ( IUploadItem item in collection )
				item.Pause ();
		}

		public Task<UploadResult> UploadAsync ()
		{
			throw new NotImplementedException ();
		}

		public MultipleUploadItem ( IReadOnlyCollection<IUploadItem> items )
		{
			collection = items;
			List<IList<string>> tagsList = new List<IList<string>> ();
			List<IList<Playlist>> playlistsList = new List<IList<Playlist>> ();
			foreach ( var item in items )
			{
				tagsList.Add ( item.Tags );
				playlistsList.Add ( item.Playlists );
			}
			tags = new MultipleList<string> ( tagsList.ToArray () );
			playlists = new MultipleList<Playlist> ( playlistsList.ToArray () );
		}

		private class MultipleList<T> : ObservableCollection<T>
		{
			readonly IList<T> [] lists;

			public MultipleList () { }
			public MultipleList ( params IList<T> [] args )
			{
				foreach ( var list in args )
					foreach ( T item in list )
						if ( !Contains ( item ) )
							Add ( item );
				lists = args;
			}

			protected override void InsertItem ( int index, T item )
			{
				base.InsertItem ( index, item );
				foreach ( var list in lists )
					if ( !list.Contains ( item ) )
						list.Add ( item );
			}

			protected override void RemoveItem ( int index )
			{
				var item = this [ index ];
				base.RemoveItem ( index );
				foreach ( var list in lists )
				{
					int ind = list.IndexOf ( item );
					if ( ind != -1 )
						list.RemoveAt ( ind );
				}
			}

			protected override void ClearItems ()
			{
				foreach ( var list in lists )
					list.Clear ();
			}

			protected override void SetItem ( int index, T item )
			{
				var origin = this [ index ];
				base.SetItem ( index, item );
				foreach ( var list in lists )
				{
					int ind = list.IndexOf ( item );
					if ( ind != -1 )
						list [ ind ] = item;
				}
			}
		}
	}
}
