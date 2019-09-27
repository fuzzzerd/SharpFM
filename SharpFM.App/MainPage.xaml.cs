using SharpFM.Core;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SharpFM.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<FileMakerClip> Keys { get; }
        public ObservableCollection<FileMakerClip> Layouts
        {
            get
            {
                return new ObservableCollection<FileMakerClip>(Keys.Where(k => FileMakerClip.ClipTypes[k.ClipboardFormat] == "Layout"));
            }
        }

        public MainPage()
        {
            InitializeComponent();

            Keys = new ObservableCollection<FileMakerClip>();

            Clipboard.ContentChanged += Clipboard_ContentChanged;
        }

        private async void Clipboard_ContentChanged(object sender, object e)
        {
            var clip = Clipboard.GetContent();

            var formats = clip.AvailableFormats.Where(f => f.StartsWith("Mac-", StringComparison.CurrentCultureIgnoreCase)).Distinct();

            Debug.WriteLine($"Formats: {formats.Count()}");

            foreach (var format in formats)
            {
                object clipData = null;

                try
                {
                    if (format.Equals("bitmap", StringComparison.CurrentCultureIgnoreCase))
                    {
                        clipData = await clip.GetBitmapAsync();
                    }
                    clipData = await clip.GetDataAsync(format);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Debug.WriteLine(ex.Message);
                }

                if (!(clipData is IRandomAccessStream dataObj))
                {
                    // this is some type of clipboard data this program can't handle
                    continue;
                }

                var stream = dataObj.GetInputStreamAt(0);
                IBuffer buff = new Windows.Storage.Streams.Buffer((uint)dataObj.Size);
                await stream.ReadAsync(buff, (uint)dataObj.Size, InputStreamOptions.None);
                var buffArray = buff.ToArray();

                var fmclip = new FileMakerClip("new-clip", format, buffArray);

                // don't bother adding a duplicate. For some reason entries were getting entered twice per clip
                // this is not the most efficient method to detect it, but it works well enough for now
                if (Keys.Any(k => k.XmlData == fmclip.XmlData))
                {
                    continue;
                }

                Keys.Add(fmclip);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var dp = new DataPackage();

            if (!(mdv.SelectedItem is FileMakerClip data))
            {
                return; // no data
            }

            // recalculate the length of the original text and make sure that is the first four bytes in the stream
            //var code = data.RawData;// XamlCodeRenderer.Text;
            //byte[] byteList = Encoding.UTF8.GetBytes(code);
            //int bl = byteList.Length;
            //byte[] intBytes = BitConverter.GetBytes(bl);

            //dp.SetData("Mac-XMSS", intBytes.Concat(byteList).ToArray().AsBuffer().AsStream().AsRandomAccessStream());
            dp.SetData(data.ClipboardFormat, data.RawData.AsBuffer().AsStream().AsRandomAccessStream());

            Clipboard.SetContent(dp);
            Clipboard.Flush();
        }

        private void masterNewScript_Click(object sender, RoutedEventArgs e)
        {
            Keys.Add(new FileMakerClip("", "Mac-XMSS", Array.Empty<byte>()));
        }

        private async void asModelAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: improve the UX of this whole thing. This works as a hack for proving the concept, but it could be so much better.

            var data = mdv.SelectedItem as FileMakerClip;

            var md = new MessageDialog("Do you want to use a layout to limit the number of fields in the generated model?", "Use Layout Projection?");
            // setup the command that will show the Layout picker and generate content that way
            md.Commands.Add(new UICommand("Pick a Layout", new UICommandInvokedHandler(async uic =>
            {
                var picker = new LayoutClipPicker
                {
                    DataContext = this.DataContext
                };
                var pickerResult = await picker.ShowAsync(ContentDialogPlacement.InPlace);
                if (pickerResult == ContentDialogResult.Primary)
                {
                    // regenerate using the layout picker
                    var classString = data.CreateClass(picker.DialogResult);
                    var dp = new DataPackage();
                    dp.SetText(classString);
                    Clipboard.SetContent(dp);
                    Clipboard.Flush();
                }
            })));

            // setup the command that will generate the full model
            md.Commands.Add(new UICommand("No Projection", new UICommandInvokedHandler(uic =>
            {
                var classString = data.CreateClass();
                var dp = new DataPackage();
                dp.SetText(classString);
                Clipboard.SetContent(dp);
                Clipboard.Flush();
            })));

            var result = await md.ShowAsync();
        }
    }
}