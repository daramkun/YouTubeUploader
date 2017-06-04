using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace Daramee.YouTubeUploader.Uploader
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

		private Stream GetDefaultClientSecretsStream ()
		{
			var stream = new MemoryStream ();

			string json = "{ installed : { \"client_id\": \"265154369970-nvej6rlmsigg57b0956clc36j7of2anu.apps.googleusercontent.com\", \"client_secret\": \"T0v3vOo6WndzgH3WQ9UvkLlU\", \"redirect_uri\" : \"urn:ietf:wg:oauth:2.0:oob\" } }";
			byte [] jsonBytes = Encoding.UTF8.GetBytes ( json );
			stream.Write ( jsonBytes, 0, jsonBytes.Length );
			stream.Position = 0;

			return stream;
		}

		public async Task<bool> Authorization ()
		{
			UserCredential credential;

			Stream stream;
			using ( stream = File.Exists ( "client_secrets.json" ) ? new FileStream ( "client_secrets.json", FileMode.Open, FileAccess.Read ) : GetDefaultClientSecretsStream () )
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
				ApiKey = File.Exists ( "api_key.txt" ) ? File.ReadAllText ( "api_key.txt" ) : "AIzaSyCvMp_S7s1vNhrfCZuOEUtSk7tcEuuKcpE",
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
