using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Daramee.DaramCommonLib;
using Daramee.YouTubeUploader.Uploader;
using Microsoft.Win32;
using TaskDialogInterop;

namespace Daramee.YouTubeUploader
{
	[DataContract]
	class SaveData
	{
		[DataMember ( IsRequired = false )]
		public bool RetryWhenCanceled { get; set; } = true;
		[DataMember ( IsRequired = false )]
		public bool HaltWhenAllCompleted { get; set; } = false;
		[DataMember ( IsRequired = false )]
		public bool DeleteWhenComplete { get; set; } = false;
		[DataMember (IsRequired = false )]
		public bool HardwareAcceleration
		{
			get { return RenderOptions.ProcessRenderMode == RenderMode.Default; }
			set { RenderOptions.ProcessRenderMode = value ? RenderMode.Default : RenderMode.SoftwareOnly; }
		}
		[DataMember ( IsRequired = false )]
		public int RetryDelayIndex { get; set; } = 3;
		[DataMember ( IsRequired = false )]
		public int PrivacyStatusIndex { get; set; } = 0;
	}

	public sealed partial class MainWindow : Window
	{
		public static MainWindow SharedWindow { get; private set; }

		UpdateChecker updateChecker;
		Optionizer<SaveData> option = new Optionizer<SaveData> ( "DARAM WORLD", "DaramYouTubeUploader" )
		{
			IsSaveToRegistry = false
		};
		YouTubeSession youtubeSession = new YouTubeSession ( Environment.CurrentDirectory );
		Categories categories = new Categories ();

		public YouTubeSession YouTubeSession { get { return youtubeSession; } }

		public bool RetryWhenCanceled { get { return option.Options.RetryWhenCanceled; } set { option.Options.RetryWhenCanceled = value; } }
		public bool HaltWhenAllCompleted { get { return option.Options.HaltWhenAllCompleted; } set { option.Options.HaltWhenAllCompleted = value; } }
		public bool DeleteWhenComplete { get { return option.Options.DeleteWhenComplete; } set { option.Options.DeleteWhenComplete = value; } }
		public int RetryDelayIndex { get { return option.Options.RetryDelayIndex; } set { option.Options.RetryDelayIndex = value; } }
		public int PrivacyStatusIndex { get { return option.Options.PrivacyStatusIndex; } set { option.Options.PrivacyStatusIndex = value; } }

		public bool HardwareAcceleration { get { return option.Options.HardwareAcceleration; } set { option.Options.HardwareAcceleration = value; } }

		public MainWindow ()
		{
			SharedWindow = this;
			
			updateChecker = new UpdateChecker ( "v{0}.{1}{2}" );

			InitializeComponent ();
			TaskbarItemInfo = new TaskbarItemInfo ();

			uploadQueueListBox.ItemsSource = new ObservableCollection<UploadQueueItem> ();
		}

		private async void Window_Loaded ( object sender, RoutedEventArgs e )
		{
			InitializeNotificatorImages ();
			
			Stream iconStream = Application.GetResourceStream ( new Uri ( "pack://application:,,,/DaramYouTubeUploader;component/Resources/MainIcon.ico" ) ).Stream;
			NotificatorManager.Initialize ( new NotificatorInitializer ()
			{
				AppId = "Daramee.YouTubeUploader",

				Title = "다람 유튜브 업로더",
				Icon = new System.Drawing.Icon ( iconStream, 16, 16 ),

				WarningTypeImagePath = Path.Combine ( tempPath, "WarningIcon.png" ),
				InformationTypeImagePath = Path.Combine ( tempPath, "InformationIcon.png" ),
				ErrorTypeImagePath = Path.Combine ( tempPath, "ErrorIcon.png" ),
				CustomTypeImagePath1 = Path.Combine ( tempPath, "SucceedIcon.png" ),
			} );
			iconStream.Dispose ();
			NotificatorManager.Notificator.Clicked += ( sender2, e2 ) =>
			{
				Dispatcher.BeginInvoke ( new Action ( () =>
				{
					if ( WindowState == WindowState.Minimized )
						WindowState = WindowState.Normal;
					//Focus ();
					Activate ();
				} ) );
			};

			notificationToggleCheckBox.DataContext = NotificatorManager.Notificator;
			notificationToggleCheckBox.SetBinding ( CheckBox.IsCheckedProperty, new Binding ( nameof ( NotificatorManager.Notificator.IsEnabledNotification ) )
			{
				Mode = BindingMode.TwoWay,
			} );

			if ( youtubeSession.IsAlreadyAuthorized )
				ButtonConnect_Click ( sender, e );

			if ( await updateChecker.CheckUpdate () == true )
				NotificatorManager.Notify ( "업데이트 확인", "Daram YouTube Uploader의 최신 버전이 있습니다.", NotifyType.Information );
		}

		static readonly string tempPath = Path.Combine ( Path.GetTempPath (), "DARAM WORLD", "DaramYouTubeUploaderCache" );
		private void InitializeNotificatorImages ()
		{
			var resDict = Resources [ "notifyIcons" ] as ResourceDictionary;
			var transparentBrush = new SolidColorBrush ( Colors.Transparent );
			transparentBrush.Freeze ();

			Directory.CreateDirectory ( tempPath );
			foreach ( var tempName in new [] { "WarningIcon", "InformationIcon", "ErrorIcon", "SucceedIcon" } )
			{
				string filename = Path.Combine ( tempPath, $"{tempName}.png" );
				if ( !File.Exists ( filename ) )
				{
					var path = resDict [ tempName ] as System.Windows.Shapes.Path;
					RenderTargetBitmap rtb = new RenderTargetBitmap ( 128, 128, 96, 96, PixelFormats.Pbgra32 );
					Grid grid = new Grid
					{
						Background = transparentBrush
					};
					grid.Children.Add ( path );
					grid.Measure ( new Size ( 128, 128 ) );
					grid.Arrange ( new Rect ( 0, 0, 128, 128 ) );
					rtb.Render ( grid );
					rtb.Freeze ();

					PngBitmapEncoder encoder = new PngBitmapEncoder ();
					BitmapFrame frame = BitmapFrame.Create ( rtb );
					encoder.Frames.Add ( frame );

					using ( var stream = File.Open ( filename, FileMode.Create ) )
						encoder.Save ( stream );
				}
			}
		}

		private void Window_Closing ( object sender, System.ComponentModel.CancelEventArgs e )
		{
			var list = uploadQueueListBox.ItemsSource as IList<UploadQueueItem>;
			bool incompleted = false;
			foreach ( var i in list )
			{
				if ( i.UploadingStatus == UploadingStatus.UploadCompleted || i.UploadingStatus == UploadingStatus.UpdateComplete )
					continue;
				incompleted = true;
				break;
			}

			if ( incompleted )
			{
				var result = App.TaskDialogShow ( "종료하시겠습니까?", "아직 업로드가 완전히 끝나지 않았습니다. 종료하실 경우 이어서 업로드 하기가 불가능합니다.", "안내",
					VistaTaskDialogIcon.Warning, "예", "아니오" );
				if ( result.CustomButtonResult == 1 )
				{
					e.Cancel = true;
					return;
				}
			}
		}

		private void Window_Closed ( object sender, EventArgs e )
		{
			NotificatorManager.Uninitialize ();
			option.Save ();
		}

		private void ButtonOpen_Click ( object sender, RoutedEventArgs e )
		{
			OpenFileDialog ofd = new OpenFileDialog ()
			{
				Filter = "가능한 모든 파일(*.mp4;*.mkv;*.webm;*.avi;*.mov;*.flv;*.wmv;*.3gp)|*.mp4;*.mkv;*.webm;*.avi;*.mov;*.flv;*.wmv;*.3gp",
				Multiselect = true
			};
			if ( ofd.ShowDialog () == false )
				return;

			foreach ( var filename in ofd.FileNames )
			{
				AddItem ( filename );
			}
		}

		private async void ButtonConnect_Click ( object sender, RoutedEventArgs e )
		{
			if ( await youtubeSession.Authorization () )
			{
				try
				{
					await categories.Refresh ( youtubeSession );
					await Playlists.Refresh ( youtubeSession );
				}
				catch ( Exception ex )
				{
					if ( ex is Google.GoogleApiException )
					{
						if ( ( ex as Google.GoogleApiException ).Error.Message.IndexOf ( "Daily Limit Exceeded." ) >= 0 )
						{
							if ( App.TaskDialogShow ( "Google API 호출 제한",
								"금일 또는 100초 내 Google API 최대 호출량을 넘어서서 현재 이용이 불가능합니다. 100초 이후 또는 금일이 지나면 다시 이용이 가능하나, 지금 바로 이용하시려면 자세한 내용은 해결법 버튼을 눌러 확인해주세요.",
								"안내", VistaTaskDialogIcon.Error, "확인", "해결법" ).CustomButtonResult == 1 )
								Process.Start ( "https://github.com/daramkun/YouTubeUploader/wiki/사용자-지정-키-사용하기" );
						}
						else if ( ( ex as Google.GoogleApiException ).Error.Message.IndexOf ( "Invalid Credentials" ) >= 0 )
						{
							App.TaskDialogShow ( "Google API 인증 오류",
								"api_key.txt 파일 또는 client_secrets.json 파일 중 하나가 적용이 되지 않거나 YouTube Data API 사용 설정이 제대로 되지 않아 문제가 발생했습니다. 다시 한번 확인해주세요.",
								"안내", VistaTaskDialogIcon.Error, "확인" );
						}
					}
					ButtonDisconnect_Click ( this, e );
				}
			}
		}

		private void ButtonDisconnect_Click ( object sender, RoutedEventArgs e )
		{
			youtubeSession.Unauthorization ();
		}

		private async void ButtonAllUpload_Click ( object sender, RoutedEventArgs e )
		{
			await Task.Run ( new Action ( () =>
			{
				foreach ( var item in uploadQueueListBox.ItemsSource as ObservableCollection<UploadQueueItem> )
				{
					if ( item.UploadingStatus == UploadingStatus.Queued || item.UploadingStatus == UploadingStatus.UploadFailed )
					{
						ThreadPool.QueueUserWorkItem ( async ( i ) => { await UploadItem ( i as UploadQueueItem ); }, item );
						while ( item.UploadingStatus < UploadingStatus.Uploading )
							Thread.Sleep ( 1 );
					}
				}
			} ) );
		}

		private async void ButtonCheckUpdate_Click ( object sender, RoutedEventArgs e )
		{
			if ( await updateChecker.CheckUpdate () == true )
			{
				if ( App.TaskDialogShow ( $"업데이트가 확인되었습니다.",
					$"현재 버전: {updateChecker.ThisVersion}\n최신 버전: {await updateChecker.GetNewestVersion ()}", "안내",
					VistaTaskDialogIcon.Information, "확인", "업데이트" ).CustomButtonResult == 1 )
					updateChecker.ShowDownloadPage ();
			}
			else
			{
				App.TaskDialogShow ( "현재 버전이 최신 버전입니다.",
					$"현재 버전: {updateChecker.ThisVersion}\n최신 버전: {await updateChecker.GetNewestVersion ()}", "안내",
					VistaTaskDialogIcon.Information, "확인" );
			}
		}

		private void ButtonPlayItem_Click ( object sender, RoutedEventArgs e )
		{
			( ( ( ( sender as Button ).Parent as StackPanel ).Parent as Grid ).Children [ 1 ] as MediaElement ).Play ();
		}

		private void ButtonStopItem_Click ( object sender, RoutedEventArgs e )
		{
			var mediaElement = ( ( ( ( sender as Button ).Parent as StackPanel ).Parent as Grid ).Children [ 1 ] as MediaElement );
			mediaElement.Stop ();
			mediaElement.Close ();
		}

		private void ItemCategoryComboBox_SelectionChanged ( object sender, SelectionChangedEventArgs e )
		{
			if ( e.AddedItems.Count == 0 ) return;
			var item = ( sender as ComboBox ).DataContext as UploadQueueItem;
			item.Category = ( e.AddedItems [ 0 ] as VideoCategory ).Id;
		}

		private async void ButtonUpload_Click ( object sender, RoutedEventArgs e )
		{
			var uploadQueueItem = ( ( sender as Button ).DataContext as UploadQueueItem );
			await UploadItem ( uploadQueueItem );
		}

		private void ButtonReInitialize_Click ( object sender, RoutedEventArgs e )
		{
			if ( App.TaskDialogShow ( "이 항목의 업로드 상태를 초기화하시겠습니까?", "업로드 상태를 초기화하면 업로드를 이어하지 않고 새로 업로드를 시작합니다.",
				"안내", VistaTaskDialogIcon.Warning, "예", "아니오" ).CustomButtonResult == 1 )
				return;

			var item = ( sender as Button ).DataContext as UploadQueueItem;
			item.StatusReset ();
		}

		private void ButtonRemoveItem_Click ( object sender, RoutedEventArgs e )
		{
			var item = ( sender as Button ).DataContext as UploadQueueItem;
			if ( item.UploadingStatus == UploadingStatus.UploadFailed )
			{
				if ( App.TaskDialogShow ( "이 항목을 목록에서 제거하시겠습니까?", "이 항목을 지우면 업로드를 이어서 하지 못하게 됩니다.",
					"안내", VistaTaskDialogIcon.Warning, "예", "아니오" ).CustomButtonResult == 1 )
					return;
			}

			( uploadQueueListBox.ItemsSource as IList<UploadQueueItem> ).Remove ( item );
		}

		private void HyperlinkBrowse_Click ( object sender, RoutedEventArgs e )
		{
			OpenFileDialog ofd = new OpenFileDialog ()
			{
				Filter = "가능한 모든 파일(*.jpg;*.png)|*.jpg;*.png"
			};
			if ( ofd.ShowDialog () == false )
				return;

			BitmapSource bitmapSource = ImageSourceHelper.GetImageFromFile ( ofd.FileName );
			SetThumbnailImage ( ( sender as Hyperlink ).DataContext as UploadQueueItem, bitmapSource );
		}

		private void HyperlinkClipboard_Click ( object sender, RoutedEventArgs e )
		{
			BitmapSource bitmapSource = ImageSourceHelper.GetClipboardImage ();
			SetThumbnailImage ( ( sender as Hyperlink ).DataContext as UploadQueueItem, bitmapSource );
		}

		private void SetThumbnailImage ( UploadQueueItem uploadQueueItem, BitmapSource bitmapSource )
		{
			if ( bitmapSource == null || uploadQueueItem == null )
				return;

			if ( Math.Abs ( ( bitmapSource.PixelWidth / ( double ) bitmapSource.PixelHeight ) - ( 16 / 9.0 ) ) >= float.Epsilon )
			{
				var result = App.TaskDialogShow ( "이미지 크기가 16:9 비율이어야 합니다.",
					"클립보드 이미지 크기 비율이 16:9인지 확인해주세요.\nYouTube 권장 크기는 1280 * 720입니다.",
					"알림", VistaTaskDialogIcon.Warning, "확인", "레터박스 추가", "잘라내기", "늘리기" ).CustomButtonResult;
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

			uploadQueueItem.Thumbnail = bitmapSource;
		}

		private void HyperlinkEditTags_Click ( object sender, RoutedEventArgs e )
		{
			var item = ( ( sender as Hyperlink ).DataContext as UploadQueueItem );
			TagEditorWindow window = new TagEditorWindow ( item.Tags as ObservableCollection<string> );
			window.ShowDialog ();
		}

		private void HyperlinkAddPlaylists_Click ( object sender, RoutedEventArgs e )
		{
			var item = ( ( sender as Hyperlink ).DataContext as UploadQueueItem );
			PlaylistWindow window = new PlaylistWindow ( item.Playlists as ObservableCollection<Playlist> );
			window.ShowDialog ();
		}

		private void UploadQueueListBox_Drop ( object sender, DragEventArgs e )
		{
			if ( !e.Data.GetDataPresent ( DataFormats.FileDrop ) )
				return;

			foreach ( var filename in e.Data.GetData ( DataFormats.FileDrop ) as string [] )
			{
				string extension = System.IO.Path.GetExtension ( filename ).ToLower ();
				if ( !( extension == ".mp4" || extension == ".mkv" || extension == ".webm" || extension == ".avi" || extension == ".mov" ||
					extension == ".flv" || extension == ".wmv" || extension == ".3gp" ) )
					continue;

				AddItem ( filename );
			}
		}

		private void UploadQueueListBox_DragEnter ( object sender, DragEventArgs e )
		{
			if ( e.Data.GetDataPresent ( DataFormats.FileDrop ) )
				e.Effects = DragDropEffects.None;
		}

		private void DeleteWhenCompleteCheckBox_Checked ( object sender, RoutedEventArgs e )
		{
			App.TaskDialogShow ( "이 기능은 업로드 완료 후 해당 파일을 삭제합니다.",
				"삭제할 영상이 휴지통으로 가지 않고 곧바로 완전히 삭제되므로 이 기능을 사용할 때는 주의해주세요.", "안내", VistaTaskDialogIcon.Information, "확인" );
		}

		private void AddItem ( string filename )
		{
			if ( !youtubeSession.IsAlreadyAuthorized )
				return;

			bool alreadyAdded = false;
			Uri filenameUri = new Uri ( filename );
			foreach ( var item in uploadQueueListBox.ItemsSource as IList<UploadQueueItem> )
			{
				if ( item.FileName.AbsolutePath == filenameUri.AbsolutePath )
				{
					alreadyAdded = true;
					break;
				}
			}

			if ( alreadyAdded )
				return;

			var queueItem = new UploadQueueItem ( youtubeSession, filename ) { PrivacyStatus = ( PrivacyStatus ) comboBoxDefaultPrivacyStatus.SelectedIndex };
			// 업로드 성공
			queueItem.Completed += ( sender, e ) =>
			{
				if ( DeleteWhenComplete )
				{
					File.Delete ( System.Web.HttpUtility.UrlDecode ( ( sender as UploadQueueItem ).FileName.AbsolutePath ) );
				}

				if ( HaltWhenAllCompleted )
				{
					var list = uploadQueueListBox.ItemsSource as IList<UploadQueueItem>;
					bool incompleted = false;
					foreach ( var i in list )
					{
						if ( i.UploadingStatus == UploadingStatus.UploadCompleted || i.UploadingStatus == UploadingStatus.UpdateComplete )
							continue;
						incompleted = true;
						break;
					}

					if ( !incompleted )
					{
						Dispatcher.BeginInvoke ( new Action ( () => { new HaltWindow ().ShowDialog (); } ) );
					}
				}
			};
			// 업로드 실패
			queueItem.Failed += async ( sender, e ) =>
			{
				if ( ( sender as UploadQueueItem ).UploadingStatus == UploadingStatus.UploadCompleted ||
				( sender as UploadQueueItem ).UploadingStatus == UploadingStatus.UpdateComplete )
					return;
				if ( !HaltWhenAllCompleted && !RetryWhenCanceled )
					return;

				int sec = 0;
				switch ( option.Options.RetryDelayIndex )
				{
					case 0: sec = 0; break;
					case 1: sec = 5; break;
					case 2: sec = 10; break;
					case 3: sec = 15; break;
					case 4: sec = 30; break;
					case 5: sec = 60; break;
				}
				if ( sec != 0 )
					await Task.Delay ( 1000 * sec );

				if ( !HaltWhenAllCompleted && !RetryWhenCanceled )
					return;
				var uploadState = ( sender as UploadQueueItem ).UploadingStatus;
				if ( uploadState == UploadingStatus.Uploading || uploadState == UploadingStatus.UploadCompleted || uploadState == UploadingStatus.UpdateComplete )
					return;

				await ( sender as UploadQueueItem ).UploadStart ();
			};
			// 업로드 중
			queueItem.Uploading += ( sender, e ) =>
			{
				Dispatcher.BeginInvoke ( new Action ( () =>
				{
					double totalProgress = 0;
					bool thereIsFailedItem = false;
					int totalCount = 0;

					foreach ( var item in uploadQueueListBox.ItemsSource as ObservableCollection<UploadQueueItem> )
					{
						if ( item.UploadingStatus == UploadingStatus.UploadFailed || item.UploadingStatus == UploadingStatus.UpdateFailed )
							thereIsFailedItem = true;
						else if ( item.UploadingStatus == UploadingStatus.Queued )
							continue;
						totalProgress += item.Progress;
						++totalCount;
					}

					TaskbarItemInfo.ProgressState = thereIsFailedItem ? TaskbarItemProgressState.Paused : TaskbarItemProgressState.Normal;
					TaskbarItemInfo.ProgressValue = totalProgress / totalCount;
				} ) );
			};
			( uploadQueueListBox.ItemsSource as IList<UploadQueueItem> ).Add ( queueItem );
		}

		private async Task UploadItem ( UploadQueueItem uploadQueueItem )
		{
			if ( uploadQueueItem.Title.Trim ().Length == 0 )
			{
				App.TaskDialogShow ( "입력에 오류가 있습니다.", "영상 제목은 반드시 채워져야 합니다.", "오류", VistaTaskDialogIcon.Error, "확인" );
				return;
			}

			string itemName = $"{uploadQueueItem.Title}({Path.GetFileName ( HttpUtility.UrlDecode ( uploadQueueItem.FileName.AbsolutePath ) )})";
			switch ( await uploadQueueItem.UploadStart () )
			{
				case UploadResult.Succeed:
					{
						switch ( uploadQueueItem.UploadingStatus )
						{
							case UploadingStatus.UploadCompleted:
								NotificatorManager.Notify ( "안내",
									$"{itemName}에 대한 업로드를 성공했습니다.",
									NotifyType.CustomType1 );
								break;
							case UploadingStatus.UpdateComplete:
								NotificatorManager.Notify ( "안내",
									$"{itemName}에 대한 업데이트를 성공했습니다.",
									NotifyType.CustomType1 );
								break;
							case UploadingStatus.UploadFailed:
								NotificatorManager.Notify ( "안내",
									$"{itemName}에 대한 업로드를 실패했습니다.",
									NotifyType.Warning );
								break;
							case UploadingStatus.UpdateFailed:
								NotificatorManager.Notify ( "안내",
									$"{itemName}에 대한 업데이트를 실패했습니다.",
									NotifyType.Warning );
								break;
						}
					}
					break;
				case UploadResult.UploadCanceled:
					NotificatorManager.Notify ( "안내",
						$"{itemName}에 대한 업로드가 중단됐습니다.\n이어서 업로드가 가능합니다.",
						NotifyType.Warning );
					break;
				case UploadResult.AlreadyUploading:
					NotificatorManager.Notify ( "오류", $"{itemName}은 이미 업로드가 시작되었습니다.", NotifyType.Error );
					break;
				case UploadResult.CannotAccesToFile:
					NotificatorManager.Notify ( "오류", $"{itemName}의 영상 파일에 접근할 수 없었습니다.", NotifyType.Error );
					break;
				case UploadResult.FailedUploadRequest:
					NotificatorManager.Notify ( "오류", $"{itemName}의 업로드 요청을 시작할 수 없었습니다.", NotifyType.Error );
					break;
				case UploadResult.CannotStartUpload:
					NotificatorManager.Notify ( "오류", $"{itemName}의 업로드 작업을 시작할 수 없었습니다.", NotifyType.Error );
					break;
				case UploadResult.FileSizeIsTooBig:
					NotificatorManager.Notify ( "오류", $"{itemName}의 업로드할 파일의 크기는 64GB를 넘길 수 없습니다.", NotifyType.Error );
					break;
			}
		}
	}
}
