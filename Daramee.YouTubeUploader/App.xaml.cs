using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Windows;

namespace Daramee.YouTubeUploader
{
	/// <summary>
	/// App.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup ( StartupEventArgs e )
		{
			if ( !IsNetworkAvailable ( 0 ) )
			{
				MessageBox.Show ( "이 프로그램은 네트워크 연결을 필요로 합니다.\n인터넷 연결을 확인 후 다시 실행해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error );
				Shutdown ();
			}

			/*AppDomain.CurrentDomain.AssemblyResolve += ( sender, e2 ) =>
			{
				foreach ( string path in new [] { ".\\" } )
				{
					string dir = System.IO.Path.Combine ( Environment.CurrentDirectory, path );
					if ( !Directory.Exists ( dir ) )
						continue;
					foreach ( string extensions in Directory.GetFiles ( dir, "*.dll", SearchOption.AllDirectories ) )
					{
						string filename = System.IO.Path.GetFileName ( extensions );
						try
						{
							Assembly asm = Assembly.LoadFile ( extensions );
							if ( asm.FullName == e2.Name )
								return asm;
						}
						catch { }
					}
				}
				return null;
			};*/
			AppDomain.CurrentDomain.UnhandledException += ( sender, e2 ) =>
			{
				if ( e2.ExceptionObject is MissingMethodException )
				{
					MessageBox.Show ( $"심각한 오류가 발생했습니다.\n{( e2.ExceptionObject as MissingMethodException ).Message}",
						"오류", MessageBoxButton.OK, MessageBoxImage.Error );
				}
				else if ( e2.ExceptionObject is FileNotFoundException )
				{
					MessageBox.Show ( $"심각한 오류가 발생했습니다.\n{( e2.ExceptionObject as FileNotFoundException ).FileName} 파일이 존재하지 않습니다.",
						"오류", MessageBoxButton.OK, MessageBoxImage.Error );
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
