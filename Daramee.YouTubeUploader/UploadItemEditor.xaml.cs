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
			OpenFileDialog ofd = new OpenFileDialog () { Filter = "가능한 모든 파일(*.jpg;*.png)|*.jpg;*.png" };
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
				var result = App.TaskDialogShow ( "알림", "이미지 크기가 16:9 비율이어야 합니다.",
					"클립보드 이미지 크기 비율이 16:9인지 확인해주세요.\nYouTube 권장 크기는 1280 * 720입니다.",
					TaskDialogIcon.Warning, TaskDialogCommonButtonFlags.OK, "레터박스 추가", "잘라내기", "늘리기" ).Button;
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
