using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace Daramee.YouTubeUploader.Uploader
{
	public class YouTubeSession : IDisposable
	{
		public YouTubeService YouTubeService { get; private set; }

		public YouTubeSession ( string sessionSaveDirectory = null )
		{
			if ( sessionSaveDirectory != null )
				GoogleWebAuthorizationBroker.Folder = sessionSaveDirectory;
		}

		public void Dispose ()
		{
			if ( YouTubeService != null )
				YouTubeService.Dispose ();
			YouTubeService = null;
		}

		public async Task<bool> Authorization ()
		{
			UserCredential credential;

			using ( var stream = new MemoryStream () )
			{
				string json = "{ installed : { \"client_id\": \"757877220144-psi8ka6kft6i41slsm96qa7mcp4re916.apps.googleusercontent.com\", \"client_secret\": \"TkXZmrTjLOu3khUEL8NOSYoj\", \"redirect_uri\" : \"urn:ietf:wg:oauth:2.0:oob\" } }";
				byte [] jsonBytes = Encoding.UTF8.GetBytes ( json );
				stream.Write ( jsonBytes, 0, jsonBytes.Length );
				stream.Position = 0;

				credential = await GoogleWebAuthorizationBroker.AuthorizeAsync (
					GoogleClientSecrets.Load ( stream ).Secrets,
					new [] { YouTubeService.Scope.YoutubeUpload },
					"user",
					CancellationToken.None
				);
			}

			if ( credential == null )
				return false;

			YouTubeService = new YouTubeService ( new BaseClientService.Initializer ()
			{
				ApplicationName = "YouTube Uploader",
				HttpClientInitializer = credential
			} );

			return true;
		}

		public void Unauthorization ()
		{
			File.Delete ( Path.Combine ( GoogleWebAuthorizationBroker.Folder, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user" ) );
			if ( YouTubeService != null )
				YouTubeService.Dispose ();
			YouTubeService = null;
		}

		public bool IsAlreadyAuthorized
		{
			get { return File.Exists ( Path.Combine ( GoogleWebAuthorizationBroker.Folder, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user" ) ); }
		}
	}
}
