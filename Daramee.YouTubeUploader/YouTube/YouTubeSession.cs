using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Daramee.YouTubeUploader.YouTube
{
	public sealed class YouTubeSession : INotifyPropertyChanged, IDisposable
	{
		public static YouTubeSession SharedYouTubeSession { get; private set; }

		public YouTubeService YouTubeService { get; private set; }

		public YouTubeSession ( string sessionSaveDirectory = null )
		{
			if ( sessionSaveDirectory != null )
				GoogleWebAuthorizationBroker.Folder = sessionSaveDirectory;

			ServicePointManager.DefaultConnectionLimit = int.MaxValue;

			YouTubeService = null;

			SharedYouTubeSession = this;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void PC ( [CallerMemberName] string propName = "" ) { PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( propName ) ); }

		public void Dispose ()
		{
			if ( YouTubeService != null )
				YouTubeService.Dispose ();
			YouTubeService = null;
		}

		struct ClientSecret
		{
			public struct ClientSecretInstalled
			{
				public string client_id;
				public string client_secret;
				public string redirect_uri;
			}
			public ClientSecretInstalled installed;

			public ClientSecret ( string client_id, string client_secret, string redirect_uri )
			{
				installed = new ClientSecretInstalled
				{
					client_id = client_id,
					client_secret = client_secret,
					redirect_uri = redirect_uri
				};
			}
		}

		private void GetAPIKeyAndClientSecret ( out string apiKey, out Stream clientSecret )
		{
			if ( File.Exists ( "user_custom_settings.txt" ) )
			{
				var lines = File.ReadAllLines ( "user_custom_settings.txt" );
				apiKey = lines [ 0 ];
				clientSecret = new MemoryStream ( Encoding.UTF8.GetBytes ( $"{{ installed : {{ \"client_id\": \"{ lines [ 1 ] }\", \"client_secret\": \"{ lines [ 2 ] }\", \"redirect_uri\" : \"urn:ietf:wg:oauth:2.0:oob\" }} }}" ) );
			}
			else
			{
				apiKey = "AIzaSyCvMp_S7s1vNhrfCZuOEUtSk7tcEuuKcpE";
				clientSecret = new MemoryStream ( Encoding.UTF8.GetBytes ( "{ installed : { \"client_id\": \"265154369970-nvej6rlmsigg57b0956clc36j7of2anu.apps.googleusercontent.com\", \"client_secret\": \"T0v3vOo6WndzgH3WQ9UvkLlU\", \"redirect_uri\" : \"urn:ietf:wg:oauth:2.0:oob\" } }" ) );
			}
		}

		public async Task<bool> Authorization ()
		{
			GetAPIKeyAndClientSecret ( out string apiKey, out Stream stream );

			UserCredential credential;
			using ( stream )
			{
				credential = await GoogleWebAuthorizationBroker.AuthorizeAsync (
					GoogleClientSecrets.Load ( stream ).Secrets,
					new [] { YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeUpload },
					"user",
					CancellationToken.None
				);
			}
			if ( credential == null )
				return false;

			YouTubeService = new YouTubeService ( new BaseClientService.Initializer ()
			{
				ApiKey = apiKey,
				ApplicationName = "DaramYouTubeUploader",
				GZipEnabled = true,
				HttpClientInitializer = credential,
				HttpClientFactory = new MyHttpClientFactory (),
			} );
			YouTubeService.HttpClient.Timeout = TimeSpan.FromMinutes ( 5 );

			PC ( nameof ( IsAuthorized ) );
			PC ( nameof ( IsAlreadyAuthorized ) );

			return true;
		}

		public void Unauthorization ()
		{
			File.Delete ( Path.Combine ( GoogleWebAuthorizationBroker.Folder, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user" ) );
			if ( YouTubeService != null )
				YouTubeService.Dispose ();
			YouTubeService = null;

			PC ( nameof ( IsAuthorized ) );
			PC ( nameof ( IsAlreadyAuthorized ) );
		}

		public bool IsAuthorized { get { return YouTubeService != null; } }

		public bool IsAlreadyAuthorized
		{
			get
			{
				if ( IsAuthorized ) return true;
				return File.Exists ( Path.Combine ( GoogleWebAuthorizationBroker.Folder, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user" ) );
			}
		}

		class MyHttpClientFactory : IHttpClientFactory
		{
			public ConfigurableHttpClient CreateHttpClient ( CreateHttpClientArgs args )
			{
				var handler = CreateHandler ( args );
				var configurableHandler = new ConfigurableMessageHandler ( handler )
				{
					ApplicationName = args.ApplicationName
				};

				var client = new ConfigurableHttpClient ( configurableHandler );
				client.DefaultRequestHeaders.Referrer = new Uri ( "https://youtubeuploader.daram.me" );
				foreach ( var initializer in args.Initializers )
				{
					initializer.Initialize ( client );
				}

				return client;
			}

			protected virtual HttpMessageHandler CreateHandler ( CreateHttpClientArgs args )
			{
				var handler = new HttpClientHandler ();

				if ( handler.SupportsRedirectConfiguration )
					handler.AllowAutoRedirect = false;

				if ( handler.SupportsAutomaticDecompression && args.GZipEnabled )
					handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

				handler.UseProxy = false;
				handler.Proxy = null;

				return handler;
			}
		}
	}
}
