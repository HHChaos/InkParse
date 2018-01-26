using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace InkParse
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tag = ((sender as ComboBox)?.SelectedItem as ComboBoxItem)?.Tag;
            if (tag == null || ImageStroke == null) return;
            if (tag.ToString() == "Pen")
            {
                ImageStroke.InkInputMode = InkInputProcessingMode.Inking;
            }
            else if (tag.ToString() == "Eraser")
            {
                ImageStroke.InkInputMode = InkInputProcessingMode.Erasing;
            }
        }
        private async void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                SuggestedFileName = $"SvgLab_{DateTime.Now.ToFileTime()}"
            };
            picker.FileTypeChoices.Add("Svg Picture", new List<string> { ".svg" });
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteBytesAsync(file, await ImageStroke.SaveAsSvgAsync());
            }
        }

    }
}
