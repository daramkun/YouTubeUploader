using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daramee.YouTubeUploader.YouTube
{
	public enum PrivacyStatus : int
	{
		Unknown = -1,
		Public = 0,
		Unlisted = 1,
		Private = 2,
	}

	public static class PrivacyStatusExtension
	{
		public static string GetPrivacyStatus ( this PrivacyStatus ps )
		{
			switch ( ps )
			{
				case PrivacyStatus.Public: return "public";
				case PrivacyStatus.Private: return "private";
				case PrivacyStatus.Unlisted: return "unlisted";
				default: return "";
			}
		}

		public static PrivacyStatus GetPrivacyStatus ( this string ps )
		{
			switch ( ps )
			{
				case "public": return PrivacyStatus.Public;
				case "private": return PrivacyStatus.Private;
				case "unlisted": return PrivacyStatus.Unlisted;
				default: return PrivacyStatus.Unknown;
			}
		}
	}
}
