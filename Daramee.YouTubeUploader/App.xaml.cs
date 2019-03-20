using Daramee.DaramCommonLib;
using Daramee.Winston.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Daramee.YouTubeUploader
{
	public partial class App : Application
	{
		public static TaskDialogResult TaskDialogShow ( string message, string content, TaskDialogIcon icon,
			   TaskDialogCommonButtonFlags commonButtons, params string [] buttons )
		{
			List<TaskDialogButton> tdButtons = new List<TaskDialogButton> ( buttons != null ? buttons.Length : 0 );
			if ( tdButtons != null )
			{
				int id = 101;
				foreach ( var button in buttons )
				{
					TaskDialogButton b = new TaskDialogButton ();
					b.ButtonID = id++;
					b.ButtonText = button;
					tdButtons.Add ( b );
				}
			}

			TaskDialog taskDialog = new TaskDialog
			{
				Title = StringTable.SharedStrings [ "youtube_uploader" ],
				MainInstruction = message,
				Content = content,
				MainIcon = icon,
				CommonButtons = commonButtons,
				Buttons = tdButtons.Count > 0 ? tdButtons.ToArray () : null,
			};
			if ( YouTubeUploader.MainWindow.SharedWindow != null )
				return taskDialog.Show ( YouTubeUploader.MainWindow.SharedWindow );
			return taskDialog.Show ();
		}

		protected override void OnStartup ( StartupEventArgs e )
		{
			if ( Environment.OSVersion.Version <= new Version ( 5, 0 ) )
			{
				MessageBox.Show ( "This application cannot use in Windows XP or lesser.", "Notice",
					MessageBoxButton.OK, MessageBoxImage.Error );
				Shutdown ( -1 );
			}

			ProgramHelper.Initialize ( Assembly.GetExecutingAssembly (), "daramkun", "YouTubeUploader" );
			StringTable stringTable = new StringTable ();

			if ( !NetworkHelper.IsNetworkAvailable ( 0 ) )
			{
				TaskDialogShow ( StringTable.SharedStrings [ "message_check_network" ], StringTable.SharedStrings [ "content_check_network" ],
					TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK );
				Shutdown ();
			}
			

			AppDomain.CurrentDomain.UnhandledException += ( sender, e2 ) =>
			{
				TaskDialogShow ( StringTable.SharedStrings [ "message_error_raised" ], ( e2.ExceptionObject as Exception ).Message,
					TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK );
			};
			this.DispatcherUnhandledException += ( sender, e2 ) =>
			{
				TaskDialogShow ( StringTable.SharedStrings [ "message_error_raised" ], e2.Exception.Message,
					TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK );
			};

			base.OnStartup ( e );
		}
	}
}
