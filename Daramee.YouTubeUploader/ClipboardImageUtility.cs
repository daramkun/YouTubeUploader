using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Daramee.YouTubeUploader
{
	public static class ClipboardImageUtility
	{
		public static BitmapSource GetClipboardImage ()
		{
			BitmapSource bitmapSource = null;

			if ( Clipboard.ContainsData ( "DeviceIndependentBitmap" ) )
			{
				bitmapSource = ImageFromClipboardDib ();
			}
			else if ( Clipboard.ContainsImage () )
			{
				bitmapSource = Clipboard.GetImage ();
			}
			else if ( Clipboard.ContainsFileDropList () )
			{
				var fileDropList = Clipboard.GetFileDropList ();

				bitmapSource = new BitmapImage ();
				( bitmapSource as BitmapImage ).BeginInit ();
				( bitmapSource as BitmapImage ).UriSource = new Uri ( fileDropList [ 0 ] );
				( bitmapSource as BitmapImage ).EndInit ();
			}
			else
				return null;

			if ( bitmapSource == null )
				return null;

			bitmapSource.Freeze ();

			if ( Math.Abs ( ( bitmapSource.PixelWidth / ( double ) bitmapSource.PixelHeight ) - ( 16 / 9.0 ) ) >= float.Epsilon )
			{
				MessageBox.Show ( "이미지 크기가 16:9 비율이어야 합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation );
				return null;
			}

			return bitmapSource;
		}

		// https://www.thomaslevesque.com/2009/02/05/wpf-paste-an-image-from-the-clipboard/
		private static BitmapSource ImageFromClipboardDib ()
		{
			if ( Clipboard.GetData ( "DeviceIndependentBitmap" ) is MemoryStream ms )
			{
				byte [] dibBuffer = new byte [ ms.Length ];
				ms.Read ( dibBuffer, 0, dibBuffer.Length );

				BITMAPINFOHEADER infoHeader =
					BinaryStructConverter.FromByteArray<BITMAPINFOHEADER> ( dibBuffer );

				int fileHeaderSize = Marshal.SizeOf ( typeof ( BITMAPFILEHEADER ) );
				int infoHeaderSize = infoHeader.biSize;
				int fileSize = fileHeaderSize + infoHeader.biSize + infoHeader.biSizeImage;

				BITMAPFILEHEADER fileHeader = new BITMAPFILEHEADER ()
				{
					bfType = BITMAPFILEHEADER.BM,
					bfSize = fileSize,
					bfReserved1 = 0,
					bfReserved2 = 0,
					bfOffBits = fileHeaderSize + infoHeaderSize + infoHeader.biClrUsed * 4
				};
				byte [] fileHeaderBytes =
					BinaryStructConverter.ToByteArray<BITMAPFILEHEADER> ( fileHeader );

				MemoryStream msBitmap = new MemoryStream ();
				msBitmap.Write ( fileHeaderBytes, 0, fileHeaderSize );
				msBitmap.Write ( dibBuffer, 0, dibBuffer.Length );
				msBitmap.Seek ( 0, SeekOrigin.Begin );

				return BitmapFrame.Create ( msBitmap );
			}
			return null;
		}

		[StructLayout ( LayoutKind.Sequential, Pack = 2 )]
		private struct BITMAPFILEHEADER
		{
			public static readonly short BM = 0x4d42; // BM

			public short bfType;
			public int bfSize;
			public short bfReserved1;
			public short bfReserved2;
			public int bfOffBits;
		}

		[StructLayout ( LayoutKind.Sequential )]
		private struct BITMAPINFOHEADER
		{
			public int biSize;
			public int biWidth;
			public int biHeight;
			public short biPlanes;
			public short biBitCount;
			public int biCompression;
			public int biSizeImage;
			public int biXPelsPerMeter;
			public int biYPelsPerMeter;
			public int biClrUsed;
			public int biClrImportant;
		}

		static class BinaryStructConverter
		{
			public static T FromByteArray<T> ( byte [] bytes ) where T : struct
			{
				IntPtr ptr = IntPtr.Zero;
				try
				{
					int size = Marshal.SizeOf ( typeof ( T ) );
					ptr = Marshal.AllocHGlobal ( size );
					Marshal.Copy ( bytes, 0, ptr, size );
					object obj = Marshal.PtrToStructure ( ptr, typeof ( T ) );
					return ( T ) obj;
				}
				finally
				{
					if ( ptr != IntPtr.Zero )
						Marshal.FreeHGlobal ( ptr );
				}
			}

			public static byte [] ToByteArray<T> ( T obj ) where T : struct
			{
				IntPtr ptr = IntPtr.Zero;
				try
				{
					int size = Marshal.SizeOf ( typeof ( T ) );
					ptr = Marshal.AllocHGlobal ( size );
					Marshal.StructureToPtr ( obj, ptr, true );
					byte [] bytes = new byte [ size ];
					Marshal.Copy ( ptr, bytes, 0, size );
					return bytes;
				}
				finally
				{
					if ( ptr != IntPtr.Zero )
						Marshal.FreeHGlobal ( ptr );
				}
			}
		}
	}
}
