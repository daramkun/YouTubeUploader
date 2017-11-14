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
