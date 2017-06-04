using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Daramee.YouTubeUploader.Properties;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Daramee.YouTubeUploader.Notify
{
	public enum NotifyType
	{
		Message,
		Information,
		Warning,
		Error,
		Succeed,
	}

	public interface INotifier : IDisposable
	{
		bool IsEnabledNotification { get; set; }
		void Notify ( string title, string text, NotifyType type );
	}

	public sealed class LegacyNotifier : INotifier
	{
		NotifyIcon notifyIcon;

		public bool IsEnabledNotification { get; set; } = true;

		public LegacyNotifier ()
		{
			notifyIcon = new NotifyIcon ()
			{
				Icon = Resources.MainIcon,
				Text = "다람 유튜브 업로더",
				Visible = true,
			};
			notifyIcon.BalloonTipClicked += ( sender, e ) =>
			{
				System.Windows.Application.Current.Dispatcher.BeginInvoke ( new Action ( () =>
				{
					System.Windows.Application.Current.MainWindow.Activate ();
				} ) );
			};
		}

		public void Dispose ()
		{
			notifyIcon.Dispose ();
		}

		public void Notify ( string title, string text, NotifyType type )
		{
			if ( !IsEnabledNotification ) return;
			notifyIcon.ShowBalloonTip ( 10, title, text, ConvertIcon ( type ) );
		}

		private ToolTipIcon ConvertIcon ( NotifyType type )
		{
			switch ( type )
			{
				case NotifyType.Information: return ToolTipIcon.Info;
				case NotifyType.Warning: return ToolTipIcon.Warning;
				case NotifyType.Error: return ToolTipIcon.Error;
				default: return ToolTipIcon.None;
			}
		}
	}

	public sealed class Win8Notifier : INotifier
	{
		static readonly string tempPath = Path.Combine ( Path.GetTempPath (), "DaramYouTubeUploaderCache" );

		public bool IsEnabledNotification { get; set; } = true;

		public Win8Notifier ()
		{
			string [] temps = new [] { "WarningIcon", "InformationIcon", "ErrorIcon", "SucceedIcon" };
			Directory.CreateDirectory ( tempPath );
			foreach ( var tempName in temps )
			{
				string filename = Path.Combine ( tempPath, $"{tempName}.png" );
				if ( !File.Exists ( filename ) )
					File.WriteAllBytes ( filename, Resources.ResourceManager.GetObject ( tempName ) as byte [] );
			}
		}

		public void Dispose ()
		{

		}

		public void Notify ( string title, string text, NotifyType type )
		{
			if ( !IsEnabledNotification ) return;

			XmlDocument toastXml = ToastNotificationManager.GetTemplateContent ( type != NotifyType.Message ? ToastTemplateType.ToastImageAndText04 : ToastTemplateType.ToastText04 );

			XmlNodeList stringElements = toastXml.GetElementsByTagName ( "text" );
			stringElements [ 0 ].AppendChild ( toastXml.CreateTextNode ( title ) );
			stringElements [ 1 ].AppendChild ( toastXml.CreateTextNode ( text ) );

			if ( type != NotifyType.Message )
			{
				XmlNodeList imageElements = toastXml.GetElementsByTagName ( "image" );
				imageElements [ 0 ].Attributes.GetNamedItem ( "src" ).NodeValue = GetIconPath ( type );
			}

			ToastNotification toast = new ToastNotification ( toastXml );
			toast.Activated += ( sender, e ) =>
			{
				System.Windows.Application.Current.Dispatcher.BeginInvoke ( new Action ( () =>
				{
					System.Windows.Application.Current.MainWindow.Activate ();
				} ) );
			};

			ToastNotificationManager.CreateToastNotifier ( "Daram YouTube Uploader" ).Show ( toast );
		}

		private string GetIconPath ( NotifyType type )
		{
			switch ( type )
			{
				case NotifyType.Warning: return new Uri ( Path.GetFullPath ( Path.Combine ( tempPath, "WarningIcon.png" ) ) ).AbsoluteUri;
				case NotifyType.Information: return new Uri ( Path.GetFullPath ( Path.Combine ( tempPath, "InformationIcon.png" ) ) ).AbsoluteUri;
				case NotifyType.Error: return new Uri ( Path.GetFullPath ( Path.Combine ( tempPath, "ErrorIcon.png" ) ) ).AbsoluteUri;
				case NotifyType.Succeed: return new Uri ( Path.GetFullPath ( Path.Combine ( tempPath, "SucceedIcon.png" ) ) ).AbsoluteUri;
				default: return null;
			}
		}
	}

	public static class NotifyManager
	{
		public static INotifier Notifier { get; private set; }

		public static void Initialize ()
		{
			if ( Environment.OSVersion.Version.Major >= 8 )
				Notifier = new Win8Notifier ();
			else
				Notifier = new LegacyNotifier ();
		}

		public static void Uninitialize ()
		{
			Notifier.Dispose ();
		}

		public static void Notify ( string title, string text, NotifyType type )
		{
			Notifier.Notify ( title, text, type );
		}
	}
}
