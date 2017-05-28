using System;
using System.IO;
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
			AppDomain.CurrentDomain.AssemblyResolve += ( sender, e2 ) =>
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
			};
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
	}
}
