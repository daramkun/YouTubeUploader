using Daramee.DaramCommonLib;
using Daramee.Winston.Dialogs;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Daramee.YouTubeUploader
{
	/// <summary>
	/// UploadItemEditor.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class UploadItemEditor : UserControl
	{
		public static DependencyProperty UploadItemProperty = DependencyProperty.Register ( "UploadItem", typeof ( IUploadItem ), typeof ( UploadItemEditor ) );

		public IUploadItem UploadItem
		{
			get { return GetValue ( UploadItemProperty ) as IUploadItem; }
			set
			{
				SetValue ( UploadItemProperty, value );
				DataContext = value;

				if ( value != null )
				{
					VideoCategory item = null;
					foreach ( var cat in Categories.DetectedCategories )
						if ( cat.Id == value.Category )
							item = cat;

					categoryComboBox.SelectedItem = item;
				}
				else
				{
					categoryComboBox.SelectedItem = null;
				}
			}
		}

		public UploadItemEditor ()
		{
			InitializeComponent ();
		}

		private void CategorySelection_Changed ( object sender, SelectionChangedEventArgs e )
		{
			if ( DataContext != null )
				( DataContext as IUploadItem ).Category = ( categoryComboBox.SelectedItem as VideoCategory ).Id;
		}

		private void EditTagsHyperlink_Click ( object sender, RoutedEventArgs e )
		{
			new TagEditorWindow ( UploadItem.Tags as ObservableCollection<string> )
			{
				Owner = MainWindow.SharedWindow
			}.ShowDialog ();
		}

		private void AddToPlaylistHyperlink_Click ( object sender, RoutedEventArgs e )
		{
			new AddToPlaylistWindow ( UploadItem.Playlists as ObservableCollection<Playlist> )
			{
				Owner = MainWindow.SharedWindow
			}.ShowDialog ();
		}

		private void ThumbnailFromFileHyperlink_Click ( object sender, RoutedEventArgs e )
		{
			OpenFileDialog ofd = new OpenFileDialog () { Filter = StringTable.SharedStrings [ "ofd_availableallimages" ] };
			if ( ofd.ShowDialog () == false )
				return;

			BitmapSource bitmapSource = ImageSourceHelper.GetImageFromFile ( ofd.FileName );
			SetThumbnailImage ( bitmapSource );
		}

		private void ThumbnailFromClipboardHyperlink_Click ( object sender, RoutedEventArgs e )
		{
			BitmapSource bitmapSource = ImageSourceHelper.GetClipboardImage ();
			SetThumbnailImage ( bitmapSource );
		}

		private void ThumbnailFromVideoClipHyperlink_Click ( object sender, RoutedEventArgs e )
		{
			// TODO
			var window = new GetSnapshotFromMediaWindow ( UploadItem.FileName ) { Owner = MainWindow.SharedWindow };
			if ( window.ShowDialog () == false )
				return;
			SetThumbnailImage ( window.CapturedSource );
		}

		private void SetThumbnailImage ( BitmapSource bitmapSource )
		{
			if ( bitmapSource == null )
				return;

			if ( Math.Abs ( ( bitmapSource.PixelWidth / ( double ) bitmapSource.PixelHeight ) - ( 16 / 9.0 ) ) >= float.Epsilon )
			{
				var result = App.TaskDialogShow ( StringTable.SharedStrings [ "message_image_size_must_16_9" ],
					StringTable.SharedStrings [ "content_image_size_must_16_9" ],
					TaskDialogIcon.Warning, TaskDialogCommonButtonFlags.OK, StringTable.SharedStrings [ "button_add_letterbox" ], StringTable.SharedStrings [ "button_crop" ], StringTable.SharedStrings [ "button_stretch" ] ).Button;
				if ( result != 0 )
				{
					var blackBrush = new SolidColorBrush ( Colors.Black );
					blackBrush.Freeze ();

					RenderTargetBitmap bmp = new RenderTargetBitmap ( 1280, 720, 96, 96, PixelFormats.Default );

					Grid container = new Grid ()
					{
						Width = 1280,
						Height = 720,
						Background = blackBrush
					};
					Image image = new Image ()
					{
						Source = bitmapSource,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
					};

					switch ( result )
					{
						case 1: image.Stretch = Stretch.Uniform; break;
						case 2: image.Stretch = Stretch.UniformToFill; break;
						case 3: image.Stretch = Stretch.Fill; break;
					}

					container.Children.Add ( image );
					container.Measure ( new Size ( 1280, 720 ) );
					container.Arrange ( new Rect ( 0, 0, 1280, 720 ) );

					bmp.Render ( container );
					bmp.Freeze ();

					bitmapSource = bmp;
				}
				else return;
			}

			UploadItem.Thumbnail = bitmapSource;

			GC.Collect ();
		}
	}
}
