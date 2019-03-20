﻿using Daramee.DaramCommonLib;
using Daramee.Winston.Dialogs;
using Daramee.YouTubeUploader.YouTube;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public static MainWindow SharedWindow { get; private set; }

		UpdateChecker updateChecker;
		Optionizer<SaveData> option;
		Categories categories = new Categories ();

		public event PropertyChangedEventHandler PropertyChanged;
		private void PC ( string name ) { PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( name ) ); }

		public YouTubeSession YouTubeSession { get; private set; } = new YouTubeSession ( AppDomain.CurrentDomain.BaseDirectory );

		public bool RetryWhenCanceled { get { return option.Options.RetryWhenCanceled; } set { option.Options.RetryWhenCanceled = value; PC ( nameof ( RetryWhenCanceled ) ); } }
		public bool HaltWhenAllCompleted { get { return option.Options.HaltWhenAllCompleted; } set { option.Options.HaltWhenAllCompleted = value; PC ( nameof ( HaltWhenAllCompleted ) ); } }
		public bool DeleteWhenComplete { get { return option.Options.DeleteWhenComplete; } set { option.Options.DeleteWhenComplete = value; PC ( nameof ( DeleteWhenComplete ) ); } }
		public int RetryDelayIndex { get { return option.Options.RetryDelayIndex; } set { option.Options.RetryDelayIndex = value; PC ( nameof ( RetryDelayIndex ) ); } }
		public int PrivacyStatusIndex { get { return option.Options.PrivacyStatusIndex; } set { option.Options.PrivacyStatusIndex = value; PC ( nameof ( PrivacyStatusIndex ) ); } }
		public int DataChunkSizeIndex { get { return option.Options.DataChunkSizeIndex; } set { option.Options.DataChunkSizeIndex = value; PC ( nameof ( DataChunkSizeIndex ) ); } }
		public bool Notification { get { return option.Options.Notification; } set { option.Options.Notification = value; PC ( nameof ( Notification ) ); } }

		public bool HardwareAcceleration { get { return option.Options.HardwareAcceleration; } set { option.Options.HardwareAcceleration = value; PC ( nameof ( HardwareAcceleration ) ); } }

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
				NotificatorManager.Notify ( StringTable.SharedStrings [ "title_error" ],
					string.Format ( StringTable.SharedStrings [ "message_filesize_limit" ], itemName),
					NotifyType.Error );
				return;
			}
			catch ( IOException )
			{
				NotificatorManager.Notify ( StringTable.SharedStrings [ "title_error" ],
					string.Format ( StringTable.SharedStrings [ "message_cannot_access_file" ], itemName ),
					NotifyType.Error );
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
				App.TaskDialogShow ( StringTable.SharedStrings [ "message_input_has_error" ], 
					StringTable.SharedStrings [ "content_input_has_error" ],
					TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK );
				return;
			}

			string itemName = $"{uploadItem.Title}({System.IO.Path.GetFileName ( HttpUtility.UrlDecode ( uploadItem.FileName.AbsolutePath ) )})";
			switch ( await uploadItem.UploadAsync () )
			{
				case UploadResult.Succeed:
					switch ( uploadItem.UploadingStatus )
					{
						case UploadingStatus.UploadCompleted:
							NotificatorManager.Notify ( StringTable.SharedStrings [ "title_notice" ],
								string.Format ( StringTable.SharedStrings [ "uploadingstatus_upload_succeed" ], itemName ),
								NotifyType.CustomType1 );
							break;
						case UploadingStatus.UpdateComplete:
							NotificatorManager.Notify ( StringTable.SharedStrings [ "title_notice" ],
								string.Format ( StringTable.SharedStrings [ "uploadingstatus_update_succeed" ], itemName ),
								NotifyType.CustomType1 );
							break;
						case UploadingStatus.UploadFailed:
							NotificatorManager.Notify ( StringTable.SharedStrings [ "title_notice" ],
								string.Format ( StringTable.SharedStrings [ "uploadingstatus_upload_failed" ], itemName ),
								NotifyType.Warning );
							break;
						case UploadingStatus.UpdateFailed:
							NotificatorManager.Notify ( StringTable.SharedStrings [ "title_notice" ],
								string.Format ( StringTable.SharedStrings [ "uploadingstatus_update_failed" ], itemName ),
								NotifyType.Warning );
							break;
					}
					break;
				case UploadResult.UploadCanceled:
					NotificatorManager.Notify ( StringTable.SharedStrings [ "title_notice" ],
						string.Format ( StringTable.SharedStrings [ "uploadingstatus_upload_paused" ], itemName ),
						NotifyType.Warning );
					break;
				case UploadResult.AlreadyUploading:
					NotificatorManager.Notify ( StringTable.SharedStrings [ "title_error" ],
						string.Format ( StringTable.SharedStrings [ "uploadingstatus_upload_is_already_started" ], itemName ),
						NotifyType.Error );
					break;
				case UploadResult.FailedUploadRequest:
					NotificatorManager.Notify ( StringTable.SharedStrings [ "title_error" ],
						string.Format ( StringTable.SharedStrings [ "uploadingstatus_upload_cannot_request" ], itemName ),
						NotifyType.Error );
					break;
				case UploadResult.CannotStartUpload:
					NotificatorManager.Notify ( StringTable.SharedStrings [ "title_error" ],
						string.Format ( StringTable.SharedStrings [ "uploadingstatus_upload_cannot_start" ], itemName ),
						NotifyType.Error );
					break;
			}
		}

		public MainWindow ()
		{
			SharedWindow = this;

			updateChecker = new UpdateChecker ( "Version {0}.{1}{2}" );

			Stream iconStream = Application.GetResourceStream ( new Uri ( "pack://application:,,,/DaramYouTubeUploader;component/Resources/MainIcon.ico" ) ).Stream;
			NotificatorManager.Initialize ( new NotificatorInitializer ()
			{
				AppId = "Daramee.YouTubeUploader",

				Title = StringTable.SharedStrings [ "youtube_uploader" ],
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
			
			Left = option.Options.Left;
			Top = option.Options.Top;
			Width = option.Options.Width;
			Height = option.Options.Height;
			WindowState = option.Options.WindowState;

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
				NotificatorManager.Notify ( StringTable.SharedStrings [ "title_hasupdate" ],
					string.Format ( StringTable.SharedStrings [ "message_update_available" ], StringTable.SharedStrings [ "youtube_uploader" ] ),
					NotifyType.Information );
		}

		private void Window_Closing ( object sender, System.ComponentModel.CancelEventArgs e )
		{
			if ( IsIncompleted () )
			{
				var result = App.TaskDialogShow ( StringTable.SharedStrings [ "message_ask_quit" ],
					StringTable.SharedStrings [ "content_ask_quit" ],
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
			option.Options.Left = Left;
			option.Options.Top = Top;
			option.Options.Width = Width;
			option.Options.Height = Height;
			option.Options.WindowState = WindowState;
			option.Save ();

			NotificatorManager.Uninitialize ();
		}

		private void ButtonOpen_Click ( object sender, RoutedEventArgs e )
		{
			var ofd = new OpenFileDialog
			{
				AllowMultiSelection = true,
				Filter = StringTable.SharedStrings [ "ofd_availableallvideos" ]
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
							if ( App.TaskDialogShow ( StringTable.SharedStrings [ "message_google_api_limit" ],
								StringTable.SharedStrings [ "content_google_api_limit" ],
								TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK, StringTable.SharedStrings [ "button_solution" ] ).Button == 101 )
								Process.Start ( "https://github.com/daramkun/YouTubeUploader/wiki/사용자-지정-키-사용하기" );
						}
						else if ( ( ex as Google.GoogleApiException ).Error.Message.IndexOf ( "Invalid Credentials" ) >= 0 )
						{
							App.TaskDialogShow ( StringTable.SharedStrings [ "message_google_api_auth_error" ],
								StringTable.SharedStrings [ "content_google_api_auth_error" ],
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
				if ( App.TaskDialogShow ( StringTable.SharedStrings [ "message_ask_disconnect" ],
					StringTable.SharedStrings [ "content_ask_disconnect" ], 
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
				if ( App.TaskDialogShow ( StringTable.SharedStrings [ "message_check_update_available" ],
					string.Format ( StringTable.SharedStrings [ "content_check_update" ], updateChecker.ThisVersion, await updateChecker.GetNewestVersion () ),
					TaskDialogIcon.Information, TaskDialogCommonButtonFlags.OK, StringTable.SharedStrings [ "button_update" ] ).Button == 101 )
					updateChecker.ShowDownloadPage ();
			}
			else
			{
				App.TaskDialogShow ( StringTable.SharedStrings [ "message_check_update_latest" ],
					string.Format ( StringTable.SharedStrings [ "content_check_update" ], updateChecker.ThisVersion, await updateChecker.GetNewestVersion () ),
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
			if ( App.TaskDialogShow ( StringTable.SharedStrings [ "message_ask_uploading_status_clear" ],
				StringTable.SharedStrings [ "content_ask_uploading_status_clear" ],
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
				if ( App.TaskDialogShow ( StringTable.SharedStrings [ "message_ask_remove_item" ],
					StringTable.SharedStrings [ "content_ask_remove_item" ],
					TaskDialogIcon.Warning, TaskDialogCommonButtonFlags.Yes | TaskDialogCommonButtonFlags.No ).Button == TaskDialogResult.No )
					return;
			}

			( listBoxItems.ItemsSource as IList<IUploadItem> ).Remove ( item );
		}
	}
}
