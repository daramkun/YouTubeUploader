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
				MessageBox.Show ( $"심각한 오류가 발생했습니다.\n{e2.ExceptionObject}", "오류" );
			};

			base.OnStartup ( e );
		}
	}
}
