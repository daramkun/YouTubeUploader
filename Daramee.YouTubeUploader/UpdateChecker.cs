using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TaskDialogInterop;

namespace Daramee.YouTubeUploader
{
	public static class UpdateChecker
	{
		public static async Task<bool?> CheckUpdate ( bool messageShow = false )
		{
			Stream stream = null;
			string version = null;
			bool checkUpdate = false;
			try
			{
				HttpWebRequest req = WebRequest.CreateHttp ( "https://github.com/daramkun/YouTubeUploader/releases" );
				req.Proxy = null;

				HttpWebResponse res = await req.GetResponseAsync () as HttpWebResponse;

				stream = res.GetResponseStream ();
				using ( StreamReader reader = new StreamReader ( stream ) )
				{
					stream = null;
					string text = reader.ReadToEnd ();
					int begin = text.IndexOf ( "<span class=\"css-truncate-target\">" );
					if ( begin == -1 ) { version = null; return false; };
					int end = text.IndexOf ( "</span>", begin );
					if ( end == -1 ) { version = null; return false; };
					version = text.Substring ( end - 5, 5 );
					Version currentVersion = Assembly.GetEntryAssembly ().GetName ().Version;
					string current = $"v{currentVersion.Major}.{currentVersion.Minor}{currentVersion.Build}";
					checkUpdate = version != current;

					if ( messageShow )
					{
						if ( checkUpdate == true )
						{
							if ( App.TaskDialogShow ( $"업데이트가 확인되었습니다.", $"현재 버전: {current}\n최신 버전: {version}", "안내",
								VistaTaskDialogIcon.Information, "확인", "업데이트" ).CustomButtonResult == 1 )
								Process.Start ( "https://github.com/daramkun/YouTubeUploader/releases" );
						}
						else
						{
							App.TaskDialogShow ( "현재 버전이 최신 버전입니다.", $"현재 버전: {current}\n최신 버전: {version}", "안내",
								VistaTaskDialogIcon.Information, "확인" );
						}
					}
				}
			}
			catch { version = null; return null; }
			finally { if ( stream != null ) stream.Dispose (); }

			return checkUpdate;
		}
	}
}
