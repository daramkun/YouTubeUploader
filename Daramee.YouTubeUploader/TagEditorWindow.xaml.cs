using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	/// TagEditorWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class TagEditorWindow : Window
	{
		WeakReference<ObservableCollection<string>> tags;

		public TagEditorWindow ( ObservableCollection<string> tags )
		{
			InitializeComponent ();
			this.tags = new WeakReference<ObservableCollection<string>> ( tags );

			listBoxTags.ItemsSource = tags;
		}

		private void ButtonAdd_Click ( object sender, RoutedEventArgs e )
		{
			if ( textBoxTag.Text.Trim ().Length == 0 )
				return;
			tags.TryGetTarget ( out ObservableCollection<string> target );
			target.Add ( textBoxTag.Text.Trim () );
			textBoxTag.Text = "";
		}

		private void ButtonRemove_Click ( object sender, RoutedEventArgs e )
		{
			if ( listBoxTags.SelectedItems.Count == 0 )
				return;
			tags.TryGetTarget ( out ObservableCollection<string> target );

			List<string> removes = new List<string> ( from string i in listBoxTags.SelectedItems select i );
			foreach ( string tag in removes )
				target.Remove ( tag );
		}

		private void ButtonClose_Click ( object sender, RoutedEventArgs e ) { Close (); }
	}
}
