using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using Daramee.DaramCommonLib;
using Daramee.TaskDialogSharp;

namespace Daramee.YouTubeUploader
{
	/// <summary>
	/// App.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class App : Application
	{
		public static TaskDialogResult TaskDialogShow ( string title, string message, string content, TaskDialogIcon icon,
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
				Title = Localizer.SharedStrings [ "daram_renamer" ],
				MainInstruction = message,
				Content = content,
				MainIcon = icon,
				CommonButtons = commonButtons,
				Buttons = tdButtons.Count > 0 ? tdButtons.ToArray () : null,
			};
			return taskDialog.Show ();
		}

		protected override void OnStartup ( StartupEventArgs e )
		{
			if ( Environment.OSVersion.Version <= new Version ( 5, 0 ) )
			{
				MessageBox.Show ( "이 프로그램은 Windows XP 이하의\n운영체제에서는 동작하지 않습니다.", "안내",
					MessageBoxButton.OK, MessageBoxImage.Error );
				Shutdown ( -1 );
			}

			if ( !NetworkHelper.IsNetworkAvailable ( 0 ) )
			{
				TaskDialogShow ( "오류", "인터넷 연결을 확인 후 다시 실행해주세요.", "이 프로그램은 네트워크 연결을 필요로 합니다.", 
					TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK );
				Shutdown ();
			}

			ProgramHelper.Initialize ( Assembly.GetExecutingAssembly (), "daramkun", "YouTubeUploader" );

			AppDomain.CurrentDomain.UnhandledException += ( sender, e2 ) =>
			{
				if ( e2.ExceptionObject is MissingMethodException )
				{
					TaskDialogShow ( "오류", "심각한 오류가 발생했습니다.", ( e2.ExceptionObject as MissingMethodException ).Message,
						TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK );
				}
				else if ( e2.ExceptionObject is FileNotFoundException )
				{
					TaskDialogShow ( "오류", "심각한 오류가 발생했습니다.", $"{( e2.ExceptionObject as FileNotFoundException ).Message} 파일이 존재하지 않습니다.",
						TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK );
				}
				else
				{
					TaskDialogShow ( "오류", "오류가 발생했습니다.", ( e2.ExceptionObject as Exception ).Message,
						TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK );
				}
			};
			this.DispatcherUnhandledException += ( sender, e2 ) =>
			{

			};

			base.OnStartup ( e );
		}
	}
}
