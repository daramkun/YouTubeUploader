using Daramee.DaramCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media;

namespace Daramee.YouTubeUploader
{
	[DataContract]
	class SaveData
	{
		[DataMember ( IsRequired = false )]
		public bool RetryWhenCanceled { get; set; } = true;
		[DataMember ( IsRequired = false )]
		public bool HaltWhenAllCompleted { get; set; } = false;
		[DataMember ( IsRequired = false )]
		public bool DeleteWhenComplete { get; set; } = false;
		[DataMember ( IsRequired = false )]
		public bool HardwareAcceleration
		{
			get { return RenderOptions.ProcessRenderMode == RenderMode.Default; }
			set { RenderOptions.ProcessRenderMode = value ? RenderMode.Default : RenderMode.SoftwareOnly; }
		}
		[DataMember ( IsRequired = false )]
		public int RetryDelayIndex { get; set; } = 3;
		[DataMember ( IsRequired = false )]
		public int PrivacyStatusIndex { get; set; } = 0;
		[DataMember ( IsRequired = false )]
		public int DataChunkSizeIndex { get; set; } = 3;
		[DataMember ( IsRequired = false )]
		public bool Notification
		{
			get { return NotificatorManager.Notificator.IsEnabledNotification; }
			set { NotificatorManager.Notificator.IsEnabledNotification = value; }
		}
	}
}
