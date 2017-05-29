using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daramee.YouTubeUploader.Uploader
{
	public class VideoCategory
	{
		public string Name { get; set; }
		public string Id { get; set; }
	}

	public class Categories
	{
		public static IReadOnlyList<VideoCategory> DetectedCategories { get; private set; }
			= new List<VideoCategory> () { new VideoCategory () { Name = "No Category", Id = null } };

		public Categories ( YouTubeSession session )
		{
			var request = session.YouTubeService.VideoCategories.List ( "snippet" );
			request.RegionCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;
			var result = request.Execute ();
			foreach ( var i in result.Items)
			{
				VideoCategory item = new VideoCategory ();
				item.Name = i.Snippet.Title;
				item.Id = i.Id;
				( DetectedCategories as List<VideoCategory> ).Add ( item );
			}
		}
	}
}
