using Daramee.TaskDialogSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Daramee.YouTubeUploader
{
	/// <summary>
	/// APIKeyWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class APIKeyWindow : Window
	{
		public APIKeyWindow ()
		{
			InitializeComponent ();
		}

		private void Button_Click ( object sender, RoutedEventArgs e )
		{
			if ( string.IsNullOrEmpty(textBoxAPIKey.Text.Trim ()) ||
				string.IsNullOrEmpty(textBoxClientID.Text.Trim()) ||
				string.IsNullOrEmpty(textBoxClientSecret.Text.Trim()))
			{
				App.TaskDialogShow ( "오류", "빈 입력란이 있습니다.",
					"세 개의 입력란이 모두 정상적으로 채워져야 합니다.\n기본 값을 사용하고 싶으신 경우 \"기본 값 사용\" 버튼을 눌러주세요.",
					TaskDialogIcon.Error, TaskDialogCommonButtonFlags.OK );
				return;
			}

			File.WriteAllLines ( "user_custom_settings.txt", new []
			{
				textBoxAPIKey.Text,
				textBoxClientID.Text,
				textBoxClientSecret.Text
			} );

			Close ();
		}

		private void Button_Click_1 ( object sender, RoutedEventArgs e )
		{
			File.Delete ( "user_custom_settings.txt" );
			File.Delete ( "client_secrets.json" );
			File.Delete ( "api_key.txt" );

			Close ();
		}
	}
}
