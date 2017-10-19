﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using OpenTKImageViewer.ImageContext;

namespace OpenTKImageViewer.Dialogs
{
    /// <summary>
    /// Interaction logic for ImageWindow.xaml
    /// </summary>
    public partial class ImageWindow : Window
    {
        /// <summary>
        /// container around the two equation boxes
        /// </summary>
        private class EquationBox
        {
            public readonly CheckBox BoxVisible;
            public readonly CheckBox BoxTonemapper;
            public readonly TextBox BoxRgbEquation;
            public readonly TextBox BoxAlphaEquation;
            private ImageConfiguration configuration;
            private ImageContext.ImageContext context;

            public EquationBox(CheckBox boxVisible, CheckBox boxTonemapper, TextBox boxRgbEquation,
                TextBox boxAlphaEquation, ImageConfiguration configuration, ImageContext.ImageContext context)
            {
                BoxVisible = boxVisible;
                BoxTonemapper = boxTonemapper;
                BoxRgbEquation = boxRgbEquation;
                BoxAlphaEquation = boxAlphaEquation;
                this.configuration = configuration;
                this.context = context;

                // little setup
                BoxRgbEquation.FontFamily = new FontFamily("Consolas");
                BoxAlphaEquation.FontFamily = new FontFamily("Consolas");

                BoxVisible.IsChecked = true;
                BoxVisible.Checked += BoxVisibleOnCheckedChange;
                BoxVisible.Unchecked += BoxVisibleOnCheckedChange;

                LoadConfigurationValues();
            }

            /// <summary>
            /// tries to apply content of the boxes to the context configuration.
            /// Throws an exception on failure.
            /// </summary>
            public void Apply()
            {
                Debug.Assert(BoxVisible.IsChecked != null);
                configuration.Active = (bool)BoxVisible.IsChecked;

                if (!configuration.Active) return;

                Debug.Assert(BoxTonemapper.IsChecked != null);
                configuration.UseTonemapper = (bool)BoxTonemapper.IsChecked;
                configuration.CombineFormula.ApplyFormula(BoxRgbEquation.Text, context.GetNumImages());
                configuration.AlphaFormula.ApplyFormula(BoxAlphaEquation.Text, context.GetNumImages());
            }

            /// <summary>
            /// compares the box content with the currently applied setting
            /// </summary>
            /// <returns>true if changes were made</returns>
            public bool HasChanges()
            {
                // visibility changed
                if (BoxVisible.IsChecked != configuration.Active)
                    return true;

                // nothing has changed if it is not active
                if (!configuration.Active)
                    return false;

                if (BoxTonemapper.IsChecked != configuration.UseTonemapper)
                    return true;

                if (BoxRgbEquation.Text != configuration.CombineFormula.Original)
                    return true;

                if (BoxAlphaEquation.Text != configuration.AlphaFormula.Original)
                    return true;

                return false;
            }

            private void LoadConfigurationValues()
            {
                // enable editing
                BoxVisible.IsChecked = true;

                BoxTonemapper.IsChecked = configuration.UseTonemapper;
                BoxRgbEquation.Text = configuration.CombineFormula.Original;
                BoxAlphaEquation.Text = configuration.AlphaFormula.Original;

                BoxVisible.IsChecked = configuration.Active;
            }

            private void BoxVisibleOnCheckedChange(object sender, RoutedEventArgs routedEventArgs)
            {
                if (BoxVisible.IsChecked == null)
                    return;
                bool enabled = (bool) BoxVisible.IsChecked;
                BoxTonemapper.IsEnabled = enabled;
                BoxRgbEquation.IsEnabled = enabled;
                BoxAlphaEquation.IsEnabled = enabled;
            }
        }

        private readonly MainWindow parent;
        private readonly EquationBox[] equationBoxes = new EquationBox[2];
        private readonly Brush buttonDefaultColor;
        private readonly Brush buttonHighlightColor = new SolidColorBrush(Color.FromRgb(128,190,255));

        public ImageWindow(MainWindow parent)
        {
            this.parent = parent;
            InitializeComponent();

            this.buttonDefaultColor = ButtonApply.Background;

            equationBoxes[0] = new EquationBox(BoxVisible1, BoxTonemapper1, EquationBox1, EquationBoxAlpha1,
                parent.Context.GetImageConfiguration(0), parent.Context);
            equationBoxes[1] = new EquationBox(BoxVisible2, BoxTonemapper2, EquationBox2, EquationBoxAlpha2,
                parent.Context.GetImageConfiguration(1), parent.Context);

            parent.Context.ChangedImages += OnChangedImages;
            BoxSplitView.SelectedIndex = (int) parent.Context.SplitView;

            if (parent.Context.GetNumActiveImages() != 2)
                BoxSplitView.IsEnabled = false;

            // proper button highlighting
            foreach (var equationBox in equationBoxes)
            {
                equationBox.BoxVisible.Checked += (sender, args) => OnChangedFormulas();
                equationBox.BoxTonemapper.Checked += (sender, args) => OnChangedFormulas();
                equationBox.BoxVisible.Unchecked += (sender, args) => OnChangedFormulas();
                equationBox.BoxTonemapper.Unchecked += (sender, args) => OnChangedFormulas();
                equationBox.BoxRgbEquation.TextChanged += (sender, args) => OnChangedFormulas();
                equationBox.BoxAlphaEquation.TextChanged += (sender, args) => OnChangedFormulas();
            }

            RefreshImageList();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            parent.ImageDialog = null;
        }

        private void OnChangedImages(object sender, EventArgs e)
        {
            RefreshImageList();
        }

        private void RefreshImageList()
        {
            ImageList.Items.Clear();

            // refresh image list
            foreach (var item in parent.GenerateImageItems())
                ImageList.Items.Add(item);
        }

        private void OnChangedFormulas()
        {
            if(equationBoxes[0].HasChanges() || equationBoxes[1].HasChanges())
                ButtonApply.Background = buttonHighlightColor;
            else
                ButtonApply.Background = buttonDefaultColor;
        }

        private void ButtonApply_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.Assert(BoxVisible1.IsChecked != null);
                Debug.Assert(BoxVisible2.IsChecked != null);
                if((bool)!BoxVisible1.IsChecked && (bool)!BoxVisible2.IsChecked)
                    throw new Exception("At least one image has to be visible");

                BoxSplitView.IsEnabled = (bool)BoxVisible1.IsChecked && (bool)BoxVisible2.IsChecked;

                foreach (var equationBox in equationBoxes)
                {
                    equationBox.Apply();
                }

                ButtonApply.Background = buttonDefaultColor;
                parent.RedrawFrame();
            }
            catch (Exception exception)
            {
                App.ShowErrorDialog(this, exception.Message);
            }
        }

        private void BoxSplitView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(BoxSplitView.SelectedIndex == -1)
                return;
            var mode = (ImageContext.ImageContext.SplitViewMode) BoxSplitView.SelectedIndex;
            parent.Context.SplitView = mode;

            if (parent.Context.GetNumActiveImages() == 2)
                parent.RedrawFrame();
        }
    }
}