using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		void Notify ( string title, string text, NotifyType type );
	}

	public sealed class LegacyNotifier : INotifier
	{
		NotifyIcon notifyIcon;

		public LegacyNotifier ()
		{
			notifyIcon = new NotifyIcon ()
			{
				Icon = Resources.MainIcon,
				Text = "다람 유튜브 업로더",
				Visible = true,
			};
		}

		public void Dispose ()
		{
			notifyIcon.Dispose ();
		}

		public void Notify ( string title, string text, NotifyType type )
		{
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
		public void Dispose ()
		{

		}

		public void Notify ( string title, string text, NotifyType type )
		{
			XmlDocument toastXml = ToastNotificationManager.GetTemplateContent ( type != NotifyType.Message ? ToastTemplateType.ToastImageAndText04 : ToastTemplateType.ToastText04 );
			
			XmlNodeList stringElements = toastXml.GetElementsByTagName ( "text" );
			stringElements [ 0 ].AppendChild ( toastXml.CreateTextNode ( title ) );
			stringElements [ 1 ].AppendChild ( toastXml.CreateTextNode ( text ) );

			if ( type != NotifyType.Message )
			{
				string imagePath = "file:///" + Path.GetFullPath ( "toastImageAndText.png" );
				XmlNodeList imageElements = toastXml.GetElementsByTagName ( "image" );
				imageElements [ 0 ].Attributes.GetNamedItem ( "src" ).NodeValue = GetIconPath ( type );
			}

			ToastNotification toast = new ToastNotification ( toastXml );
			
			ToastNotificationManager.CreateToastNotifier ( "Daram YouTube Uploader" ).Show ( toast );
		}

		private object GetIconPath ( NotifyType type )
		{
			switch ( type )
			{
				case NotifyType.Warning: return "https://github.com/daramkun/YouTubeUploader/blob/master/DocumentResources/WarningIcon.png";
				case NotifyType.Information: return "https://github.com/daramkun/YouTubeUploader/blob/master/DocumentResources/InformationIcon.png";
				case NotifyType.Error: return "https://github.com/daramkun/YouTubeUploader/blob/master/DocumentResources/ErrorIcon.png";
				case NotifyType.Succeed: return "https://github.com/daramkun/YouTubeUploader/blob/master/DocumentResources/SucceedIcon.png";
				default: return null;
			}
		}
	}

	public static class NotifyManager
	{
		static INotifier notifier;

		public static void Initialize ()
		{
			if ( Environment.OSVersion.Version.Major <= 8 )
				notifier = new Win8Notifier ();
			else
				notifier = new LegacyNotifier ();
		}

		public static void Uninitialize ()
		{
			notifier.Dispose ();
		}

		public static void Notify ( string title, string text, NotifyType type )
		{
			notifier.Notify ( title, text, type );
		}
	}
}
