using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using TaskDialogInterop;

namespace Daramee.YouTubeUploader
{
	/// <summary>
	/// App.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class App : Application
	{

		public static TaskDialogResult TaskDialogShow ( string message, string content, string title, VistaTaskDialogIcon icon, params string [] buttons )
		{
			TaskDialogOptions config = new TaskDialogOptions ();
			config.Owner = null;
			config.Title = title;
			config.MainInstruction = message;
			config.Content = content;
			config.MainIcon = icon;
			config.CustomButtons = buttons;
			return TaskDialog.Show ( config );
		}

		protected override void OnStartup ( StartupEventArgs e )
		{
			if ( Environment.OSVersion.Version <= new Version ( 5, 0 ) )
			{
				MessageBox.Show ( "이 프로그램은 Windows XP 이하의\n운영체제에서는 동작하지 않습니다.", "안내",
					MessageBoxButton.OK, MessageBoxImage.Error );
				Shutdown ( -1 );
			}

			if ( !IsNetworkAvailable ( 0 ) )
			{
				TaskDialogShow ( "인터넷 연결을 확인 후 다시 실행해주세요.", "이 프로그램은 네트워크 연결을 필요로 합니다.", "오류", VistaTaskDialogIcon.Error, "확인" );
				Shutdown ();
			}

			AppDomain.CurrentDomain.UnhandledException += ( sender, e2 ) =>
			{
				if ( e2.ExceptionObject is MissingMethodException )
				{
					TaskDialogShow ( "심각한 오류가 발생했습니다.", ( e2.ExceptionObject as MissingMethodException ).Message,
						"오류", VistaTaskDialogIcon.Error, "확인" );
				}
				else if ( e2.ExceptionObject is FileNotFoundException )
				{
					TaskDialogShow ( "심각한 오류가 발생했습니다.", $"{( e2.ExceptionObject as FileNotFoundException ).Message} 파일이 존재하지 않습니다.",
						"오류", VistaTaskDialogIcon.Error, "확인" );
				}
			};

			base.OnStartup ( e );
		}

		// https://stackoverflow.com/questions/520347/how-do-i-check-for-a-network-connection
		private static bool IsNetworkAvailable ( long minimumSpeed )
		{
			if ( !NetworkInterface.GetIsNetworkAvailable () )
				return false;

			foreach ( NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces () )
			{
				// discard because of standard reasons
				if ( ( ni.OperationalStatus != OperationalStatus.Up ) ||
					( ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ) ||
					( ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel ) )
					continue;

				// this allow to filter modems, serial, etc.
				// I use 10000000 as a minimum speed for most cases
				if ( ni.Speed < minimumSpeed )
					continue;

				// discard virtual cards (virtual box, virtual pc, etc.)
				if ( ( ni.Description.IndexOf ( "virtual", StringComparison.OrdinalIgnoreCase ) >= 0 ) ||
					( ni.Name.IndexOf ( "virtual", StringComparison.OrdinalIgnoreCase ) >= 0 ) )
					continue;

				// discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
				if ( ni.Description.Equals ( "Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase ) )
					continue;

				return true;
			}
			return false;
		}
	}
}
