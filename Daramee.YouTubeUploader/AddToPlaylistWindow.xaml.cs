using Daramee.YouTubeUploader.YouTube;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Daramee.YouTubeUploader
{
	class SelectablePlaylist
	{
		public bool IsChecked { get; set; }
		public string Name { get { return Playlist.Title; } }
		public Playlist Playlist { get; set; }

		public SelectablePlaylist ( Playlist playlist )
		{
			Playlist = playlist;
		}
	}

	public partial class AddToPlaylistWindow : Window
	{
		ObservableCollection<Playlist> playlistsObject;

		public AddToPlaylistWindow ( ObservableCollection<Playlist> playlistsObject )
		{
			InitializeComponent ();

			this.playlistsObject = playlistsObject;

			ObservableCollection<SelectablePlaylist> binding = new ObservableCollection<SelectablePlaylist> ();
			foreach ( var playlist in Playlists.DetectedPlaylists )
			{
				SelectablePlaylist pl = new SelectablePlaylist ( playlist );
				var alreadySelected = ( from id in playlistsObject where pl.Playlist.Id == id.Id select id ).Count () > 0;
				if ( alreadySelected )
					pl.IsChecked = true;
				binding.Add ( pl );
			}

			listBoxPlaylists.ItemsSource = binding;
		}

		private void ButtonClose_Click ( object sender, RoutedEventArgs e )
		{
			playlistsObject.Clear ();

			foreach ( var pl in listBoxPlaylists.ItemsSource as IList<SelectablePlaylist> )
			{
				if ( pl.IsChecked )
					playlistsObject.Add ( pl.Playlist );
			}

			Close ();
		}
	}
}
