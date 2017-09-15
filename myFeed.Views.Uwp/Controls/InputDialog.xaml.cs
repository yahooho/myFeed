﻿using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;

namespace myFeed.Views.Uwp.Controls
{
    public sealed partial class InputDialog : ContentDialog
    {
        public InputDialog(string message, string title)
        {
            InitializeComponent();
            var resourceLoader = new ResourceLoader();
            PrimaryButtonText = resourceLoader.GetString("Ok");
            SecondaryButtonText = resourceLoader.GetString("Cancel");
            InputBox.PlaceholderText = message;
            Title = title;
        }

        public string Value => InputBox.Text;
    }
}