using Daramee.DaramCommonLib;
using Daramee.Winston.Dialogs;
using Daramee.YouTubeUploader.YouTube;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace Daramee.YouTubeUploader
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window
	{
		public static MainWindow SharedWindow { get; private set; }

		UpdateChecker updateChecker;
		Optionizer<SaveData> option;
		Categories categories = new Categories ();

		public YouTubeSession YouTubeSession { get; private set; } = new YouTubeSession ( AppDomain.CurrentDomain.BaseDirectory );

		public bool RetryWhenCanceled { get { return option.Options.RetryWhenCanceled; } set { option.Options.RetryWhenCanceled = value; } }
		public bool HaltWhenAllCompleted { get { return option.Options.HaltWhenAllCompleted; } set { option.Options.HaltWhenAllCompleted = value; } }
		public bool DeleteWhenComplete { get { return option.Options.DeleteWhenComplete; } set { option.Options.DeleteWhenComplete = value; } }
		public int RetryDelayIndex { get { return option.Options.RetryDelayIndex; } set { option.Options.RetryDelayIndex = value; } }
		public int PrivacyStatusIndex { get { return option.Options.PrivacyStatusIndex; } set { option.Options.PrivacyStatusIndex = value; } }
		public int DataChunkSizeIndex { get { return option.Options.DataChunkSizeIndex; } set { option.Options.DataChunkSizeIndex = value; } }
		public bool Notification { get { return option.Options.Notification; } set { option.Options.Notification = value; } }

		public bool HardwareAcceleration { get { return option.Options.HardwareAcceleration; } set { option.Options.HardwareAcceleration = value; } }

		private void AddItem ( string filename )
		{
			if ( !YouTubeSession.IsAlreadyAuthorized )
				return;

			bool alreadyAdded = false;
			Uri filenameUri = new Uri ( filename );
			foreach ( var item in listBoxItems.ItemsSource as IList<IUploadItem> )
			{
				if ( item.FileName.AbsolutePath == filenameUri.AbsolutePath )
				{
					alreadyAdded = true;
					break;
				}
			}

			if ( alreadyAdded )
				return;

			string itemName = System.IO.Path.GetFileName ( filename );
			UploadItem queueItem;
			try
			{
				queueItem = new UploadItem ( filename )
				{
					DataChunkSize = int.Parse ( ( dataChunkSize.SelectedItem as ComboBoxItem ).Tag as string ) * 1024
				};
			}
			catch ( ArgumentException )
			{
				NotificatorManager.Notify ( "오류", $"{itemName}의 업로드할 파일의 크기는 64GB를 넘길 수 없습니다.", NotifyType.Error );
				return;
			}
			catch ( IOException )
			{
				NotificatorManager.Notify ( "오류", $"{itemName}의 영상 파일에 접근할 수 없었습니다.", NotifyType.Error );
				return;
			}

			// 업로드 성공
			queueItem.Completed += ( sender, e ) =>
			{
				if ( DeleteWhenComplete )
					File.Delete ( System.Web.HttpUtility.UrlDecode ( ( sender as UploadItem ).FileName.AbsolutePath ) );

				if ( HaltWhenAllCompleted )
				{
					bool incompleted = IsIncompleted ();

					if ( !incompleted )
					{
						Dispatcher.BeginInvoke ( new Action ( () =>
						{
							new HaltWindow () { Owner = this }.ShowDialog ();
						} ) );
					}
				}
			};
			// 업로드 실패
			queueItem.Failed += async ( sender, e ) =>
			{
				if ( ( sender as UploadItem ).UploadingStatus == UploadingStatus.UploadCompleted ||
					 ( sender as UploadItem ).UploadingStatus == UploadingStatus.UpdateComplete ||
					 ( sender as UploadItem ).UploadingStatus == UploadingStatus.UploadCanceled )
					return;
				if ( ( sender as UploadItem ).IsManuallyPaused )
					return;
				if ( !HaltWhenAllCompleted && !RetryWhenCanceled )
					return;

				int sec = 0;
				switch ( RetryDelayIndex )
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
				var uploadState = ( sender as UploadItem ).UploadingStatus;
				if ( uploadState == UploadingStatus.Uploading || uploadState == UploadingStatus.UploadCompleted || uploadState == UploadingStatus.UpdateComplete )
					return;

				await ( sender as UploadItem ).UploadAsync ();
			};
			// 업로드 중
			queueItem.Uploading += ( sender, e ) =>
			{
				Dispatcher.BeginInvoke ( new Action ( () =>
				{
					double totalProgress = 0;
					bool thereIsFailedItem = false;
					int totalCount = 0;

					foreach ( var item in listBoxItems.ItemsSource as ObservableCollection<IUploadItem> )
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

			( listBoxItems.ItemsSource as IList<IUploadItem> ).Add ( queueItem );
		}

		private bool IsIncompleted ()
		{
			bool incompleted = false;
			foreach ( var i in listBoxItems.ItemsSource as IList<IUploadItem> )
			{
				if ( i.UploadingStatus == UploadingStatus.UploadCompleted || i.UploadingStatus == UploadingStatus.UpdateComplete )
					continue;
				incompleted = true;
				break;
			}
			return incompleted;
		}

		private async Task UploadItemAsync ( UploadItem uploadItem )
		{
			if ( string.IsNullOrEmpty ( uploadItem.Title.Trim () ) )
			{
				App.TaskDialogShow ( "오류", "입력에 오류가 있습니다.", "영상 제목은 반드시 채워져야 합니다.",
					TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK );
				return;
			}

			string itemName = $"{uploadItem.Title}({System.IO.Path.GetFileName ( HttpUtility.UrlDecode ( uploadItem.FileName.AbsolutePath ) )})";
			switch ( await uploadItem.UploadAsync () )
			{
				case UploadResult.Succeed:
					switch ( uploadItem.UploadingStatus )
					{
						case UploadingStatus.UploadCompleted: NotificatorManager.Notify ( "안내", $"{itemName}에 대한 업로드를 성공했습니다.", NotifyType.CustomType1 ); break;
						case UploadingStatus.UpdateComplete: NotificatorManager.Notify ( "안내", $"{itemName}에 대한 업데이트를 성공했습니다.", NotifyType.CustomType1 ); break;
						case UploadingStatus.UploadFailed: NotificatorManager.Notify ( "안내", $"{itemName}에 대한 업로드를 실패했습니다.", NotifyType.Warning ); break;
						case UploadingStatus.UpdateFailed: NotificatorManager.Notify ( "안내", $"{itemName}에 대한 업데이트를 실패했습니다.", NotifyType.Warning ); break;
					}
					break;
				case UploadResult.UploadCanceled: NotificatorManager.Notify ( "안내", $"{itemName}에 대한 업로드가 중단됐습니다.\n업로드 재개가 가능합니다.", NotifyType.Warning ); break;
				case UploadResult.AlreadyUploading: NotificatorManager.Notify ( "오류", $"{itemName}은 이미 업로드가 시작되었습니다.", NotifyType.Error ); break;
				case UploadResult.FailedUploadRequest: NotificatorManager.Notify ( "오류", $"{itemName}의 업로드 요청을 시작할 수 없었습니다.", NotifyType.Error ); break;
				case UploadResult.CannotStartUpload: NotificatorManager.Notify ( "오류", $"{itemName}의 업로드 작업을 시작할 수 없었습니다.", NotifyType.Error ); break;
			}
		}

		public MainWindow ()
		{
			SharedWindow = this;

			updateChecker = new UpdateChecker ( "v{0}.{1}{2}" );

			Stream iconStream = Application.GetResourceStream ( new Uri ( "pack://application:,,,/DaramYouTubeUploader;component/Resources/MainIcon.ico" ) ).Stream;
			NotificatorManager.Initialize ( new NotificatorInitializer ()
			{
				AppId = "Daramee.YouTubeUploader",

				Title = "다람 유튜브 업로더",
				Icon = new System.Drawing.Icon ( iconStream, 16, 16 ),

				ForceLegacy = true
			} );
			iconStream.Dispose ();
			NotificatorManager.Notificator.Clicked += ( sender2, e2 ) =>
			{
				Dispatcher.BeginInvoke ( new Action ( () =>
				{
					if ( WindowState == WindowState.Minimized )
						WindowState = WindowState.Normal;
					Activate ();
				} ) );
			};

			option = new Optionizer<SaveData> ( "DARAM WORLD", "DaramYouTubeUploader" );

			InitializeComponent ();
			TaskbarItemInfo = new TaskbarItemInfo ();

			if ( dataChunkSize.SelectedItem as ComboBoxItem == null ||
				string.IsNullOrEmpty ( ( dataChunkSize.SelectedItem as ComboBoxItem ).Tag as string ) )
				DataChunkSizeIndex = 3;

			listBoxItems.ItemsSource = new ObservableCollection<IUploadItem> ();
		}

		private async void Window_Loaded ( object sender, RoutedEventArgs e )
		{
			if ( YouTubeSession.IsAlreadyAuthorized )
				ButtonConnect_Click ( sender, e );
			if ( await updateChecker.CheckUpdate () == true )
				NotificatorManager.Notify ( "업데이트 확인", "Daram YouTube Uploader의 최신 버전이 있습니다.", NotifyType.Information );
		}

		private void Window_Closing ( object sender, System.ComponentModel.CancelEventArgs e )
		{
			if ( IsIncompleted () )
			{
				var result = App.TaskDialogShow ( "주의", "종료하시겠습니까?",
					"아직 업로드가 완전히 끝나지 않았습니다. 지금 종료하실 경우 이어서 업로드 하기가 불가능합니다.",
					TaskDialogIcon.Warning, TaskDialogCommonButtonFlags.Yes | TaskDialogCommonButtonFlags.No );
				if ( result.Button == TaskDialogResult.No )
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
			var ofd = new OpenFileDialog
			{
				AllowMultiSelection = true,
				Filter = "가능한 모든 비디오 파일(*.mp4;*.mkv;*.webm;*.avi;*.mov;*.flv;*.wmv;*.3gp)|*.mp4;*.mkv;*.webm;*.avi;*.mov;*.flv;*.wmv;*.3gp|모든 파일(*.*)|*.*"
			};
			if ( ofd.ShowDialog ( this ) == false )
				return;

			foreach ( var filename in ofd.FileNames )
			{
				AddItem ( filename );
			}
		}

		private async void ButtonConnect_Click ( object sender, RoutedEventArgs e )
		{
			if ( await YouTubeSession.Authorization () )
			{
				try
				{
					await categories.Refresh ();
					await Playlists.Refresh ();
				}
				catch ( Exception ex )
				{
					if ( ex is Google.GoogleApiException )
					{
						if ( ( ex as Google.GoogleApiException ).Error.Message.IndexOf ( "Daily Limit Exceeded." ) >= 0 )
						{
							if ( App.TaskDialogShow ( "안내", "Google API 호출 제한",
								"금일 또는 100초 내 Google API 최대 호출량을 넘어서서 현재 이용이 불가능합니다. 100초 이후 또는 금일이 지나면 다시 이용이 가능하나, 지금 바로 이용하시려면 자세한 내용은 해결법 버튼을 눌러 확인해주세요.",
								TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK, "해결법" ).Button == 101 )
								Process.Start ( "https://github.com/daramkun/YouTubeUploader/wiki/사용자-지정-키-사용하기" );
						}
						else if ( ( ex as Google.GoogleApiException ).Error.Message.IndexOf ( "Invalid Credentials" ) >= 0 )
						{
							App.TaskDialogShow ( "안내", "Google API 인증 오류",
								"API 키 및 클라이언트 비밀 보안 중 하나 이상이 문제가 있거나 YouTube Data API 사용 설정이 제대로 되지 않아 문제가 발생했습니다. 다시 한번 확인해주세요.",
								TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK );
						}
					}
					ButtonDisconnect_Click ( this, e );
				}
			}
		}

		private void ButtonDisconnect_Click ( object sender, RoutedEventArgs e )
		{
			if ( IsIncompleted () )
			{
				if ( App.TaskDialogShow ( "주의", "아직 업로드 중인 파일이 존재합니다.",
					"지금 YouTube로부터 연결을 해제하면 업로드가 중단되고 업로드 리스트도 비워집니다.\n중단된 영상은 이어서 업로드가 불가능하니 주의하시기 바랍니다.", 
					TaskDialogIcon.Warning, TaskDialogCommonButtonFlags.OK | TaskDialogCommonButtonFlags.Cancel ).Button
					== TaskDialogResult.Cancel )
					return;
			}

			YouTubeSession.Unauthorization ();
			foreach ( IUploadItem item in listBoxItems.ItemsSource as IList<IUploadItem> )
				item.Pause ();
			( listBoxItems.ItemsSource as IList<IUploadItem> ).Clear ();
		}

		private void APIKeyButton_Click ( object sender, RoutedEventArgs e )
		{
			new APIKeyWindow () { Owner = this }.ShowDialog ();
		}

		private async void ButtonAllUpload_Click ( object sender, RoutedEventArgs e )
		{
			List<IUploadItem> copied = new List<IUploadItem> ( listBoxItems.ItemsSource as IEnumerable<IUploadItem> );
			await Task.Run ( new Action ( () =>
			{
				foreach ( var item in copied )
				{
					if ( item.UploadingStatus == UploadingStatus.Queued || item.UploadingStatus == UploadingStatus.UploadFailed )
					{
						ThreadPool.QueueUserWorkItem ( async ( i ) => { await UploadItemAsync ( i as UploadItem ); }, item );
						while ( item.UploadingStatus < UploadingStatus.Uploading )
							Thread.Sleep ( 1 );
					}
				}
			} ) );
		}

		private void DataChunkSize_SelectionChanged ( object sender, SelectionChangedEventArgs e )
		{
			if ( listBoxItems == null || listBoxItems.ItemsSource == null ) return;
			foreach ( UploadItem item in listBoxItems.ItemsSource as IList<IUploadItem> )
				item.DataChunkSize = int.Parse ( ( dataChunkSize.SelectedItem as ComboBoxItem ).Tag as string ) * 1024;
		}

		private async void ButtonCheckUpdate_Click ( object sender, RoutedEventArgs e )
		{
			if ( await updateChecker.CheckUpdate () == true )
			{
				if ( App.TaskDialogShow ( "안내", $"업데이트가 확인되었습니다.",
					$"현재 버전: {updateChecker.ThisVersion}\n최신 버전: {await updateChecker.GetNewestVersion ()}",
					TaskDialogIcon.Information, TaskDialogCommonButtonFlags.OK, "업데이트" ).Button == 101 )
					updateChecker.ShowDownloadPage ();
			}
			else
			{
				App.TaskDialogShow ( "안내", "현재 버전이 최신 버전입니다.",
					$"현재 버전: {updateChecker.ThisVersion}\n최신 버전: {await updateChecker.GetNewestVersion ()}",
					TaskDialogIcon.Information, TaskDialogCommonButtonFlags.OK );
			}
		}

		private void ListBoxItems_SelectionChanged ( object sender, SelectionChangedEventArgs e )
		{
			if ( listBoxItems.SelectedItems.Count == 0 )
				uploadItemEditor.UploadItem = null;
			else if ( listBoxItems.SelectedItems.Count == 1 )
				uploadItemEditor.UploadItem = listBoxItems.SelectedItem as IUploadItem;
			else
			{
				List<IUploadItem> items = new List<IUploadItem> ();
				foreach ( IUploadItem item in listBoxItems.SelectedItems )
					items.Add ( item );
				uploadItemEditor.UploadItem = new MultipleUploadItem ( items );
			}
		}

		private void ListBoxItems_DragEnter ( object sender, DragEventArgs e )
		{
			if ( !YouTubeSession.IsAuthorized )
				return;

			if ( e.Data.GetDataPresent ( DataFormats.FileDrop ) )
				e.Effects = DragDropEffects.None;
		}

		private void ListBoxItems_Drop ( object sender, DragEventArgs e )
		{
			if ( !YouTubeSession.IsAuthorized )
				return;

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

		private void ListBoxItems_MouseDown ( object sender, MouseButtonEventArgs e )
		{
			HitTestResult r = VisualTreeHelper.HitTest ( this, e.GetPosition ( this ) );
			if ( r.VisualHit.GetType () != typeof ( ListBoxItem ) )
				listBoxItems.UnselectAll ();
		}

		private async void ButtonUpload_Click ( object sender, RoutedEventArgs e )
		{
			var uploadItem = ( ( sender as Button ).DataContext as UploadItem );
			if ( uploadItem.UploadingStatus == UploadingStatus.Uploading )
				uploadItem.Pause ();
			else
				await UploadItemAsync ( uploadItem );
		}

		private void ButtonReInitialize_Click ( object sender, RoutedEventArgs e )
		{
			if ( App.TaskDialogShow ( "안내", "이 항목의 업로드 상태를 초기화하시겠습니까?", "업로드 상태를 초기화하면 업로드를 이어하지 않고 새로 업로드를 시작합니다.",
				TaskDialogIcon.Warning, TaskDialogCommonButtonFlags.Yes | TaskDialogCommonButtonFlags.No ).Button == TaskDialogResult.No )
				return;

			var item = ( sender as Button ).DataContext as UploadItem;
			item.StatusReset ();
		}

		private void ButtonRemoveItem_Click ( object sender, RoutedEventArgs e )
		{
			var item = ( sender as Button ).DataContext as UploadItem;
			if ( item.UploadingStatus == UploadingStatus.UploadFailed )
			{
				if ( App.TaskDialogShow ( "안내", "이 항목을 목록에서 제거하시겠습니까?", "이 항목을 지우면 업로드를 이어서 하지 못하게 됩니다.",
					TaskDialogIcon.Warning, TaskDialogCommonButtonFlags.Yes | TaskDialogCommonButtonFlags.No ).Button == TaskDialogResult.No )
					return;
			}

			( listBoxItems.ItemsSource as IList<IUploadItem> ).Remove ( item );
		}
	}
}
