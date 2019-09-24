﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using OpenTK.Graphics.OpenGL4;
using TextureViewer.Annotations;
using TextureViewer.Models.Dialog;
using TextureViewer.Views;
using static TextureViewer.ImageLoader;
using static TextureViewer.Models.Dialog.ExportModel;

namespace TextureViewer.ViewModels.Dialog
{
    public class ExportViewModel : INotifyPropertyChanged
    {
        private readonly Models.Models models;

        public ExportViewModel(Models.Models models)
        {
            this.models = models;

            // init layers
            for (var i = 0; i < models.Images.NumLayers; ++i)
            {
                AvailableLayers.Add(new ComboBoxItem<int>("Layer " + i, i));
            }
            selectedLayer = AvailableLayers[models.Export.Layer];
            Debug.Assert(selectedLayer.Cargo == models.Export.Layer);

            // init mipmaps
            for (var i = 0; i < models.Images.NumMipmaps; ++i)
            {
                AvailableMipmaps.Add(new ComboBoxItem<int>("Mipmap " + i, i));
            }
            selectedMipmap = AvailableMipmaps[models.Export.Mipmap];
            Debug.Assert(selectedMipmap.Cargo == models.Export.Mipmap);

            // all layer option for ktx and dds
            if (models.Images.NumLayers > 1 && (models.Export.FileType == FileFormat.Ktx || models.Export.FileType == FileFormat.Ktx2 || models.Export.FileType == FileFormat.Dds))
            {
                AvailableLayers.Add(new ComboBoxItem<int>("All Layer", -1));
                selectedLayer = AvailableLayers.Last();
                models.Export.Layer = selectedLayer.Cargo;
            }

            // all mipmaps option for ktx and dds
            if (models.Images.NumMipmaps > 1 && (models.Export.FileType == FileFormat.Ktx || models.Export.FileType == FileFormat.Ktx2 || models.Export.FileType == FileFormat.Dds))
            {
                AvailableMipmaps.Add(new ComboBoxItem<int>("All Mipmaps", -1));
                selectedMipmap = AvailableMipmaps.Last();
                models.Export.Mipmap = selectedMipmap.Cargo;
            }

            // init formats
            foreach (var format in models.Export.SupportedFormats)
            {
                AvailableFormat.Add(new ComboBoxItem<DisplayedFormat>(format.DisplayedName, format));
                if (format == models.Export.TexFormat)
                    SelectedFormat = AvailableFormat.Last();
            }

            models.Export.PropertyChanged += ExportOnPropertyChanged;
        }

        public void Dispose()
        {
            models.Export.PropertyChanged -= ExportOnPropertyChanged;
        }

        private void ExportOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case nameof(ExportModel.UseCropping):
                    OnPropertyChanged(nameof(UseCropping));
                    OnPropertyChanged(nameof(IsValid));
                    break;
                case nameof(ExportModel.CropMinX):
                    OnPropertyChanged(nameof(CropMinX));
                    break;
                case nameof(ExportModel.CropMinY):
                    OnPropertyChanged(nameof(CropMinY));
                    break;
                case nameof(ExportModel.CropMaxX):
                    OnPropertyChanged(nameof(CropMaxX));
                    break;
                case nameof(ExportModel.CropMaxY):
                    OnPropertyChanged(nameof(CropMaxY));
                    break;
                case nameof(ExportModel.CropStartX):
                    OnPropertyChanged(nameof(CropStartX));
                    OnPropertyChanged(nameof(IsValid));
                    break;
                case nameof(ExportModel.CropStartY):
                    OnPropertyChanged(nameof(CropStartY));
                    OnPropertyChanged(nameof(IsValid));
                    break;
                case nameof(ExportModel.CropEndX):
                    OnPropertyChanged(nameof(CropEndX));
                    OnPropertyChanged(nameof(IsValid));
                    break;
                case nameof(ExportModel.CropEndY):
                    OnPropertyChanged(nameof(CropEndY));
                    OnPropertyChanged(nameof(IsValid));
                    break;
                case nameof(ExportModel.Mipmap):
                    if(models.Export.Mipmap < 0)
                        selectedMipmap = AvailableMipmaps.Last();
                    else
                        selectedMipmap = AvailableMipmaps[models.Export.Mipmap];
                    OnPropertyChanged(nameof(SelectedMipmap));
                    break;
                case nameof(ExportModel.Layer):
                    if (models.Export.Layer < 0)
                        selectedLayer = AvailableLayers.Last();
                    else
                        selectedLayer = AvailableLayers[models.Export.Layer];
                    OnPropertyChanged(nameof(SelectedLayer));
                    break;
                case nameof(ExportModel.Quality):
                    OnPropertyChanged(nameof(Quality));
                    break;
                case nameof(ExportModel.AllowCropping):
                    OnPropertyChanged(nameof(AllowCropping));
                    break;
            }
        }

        public bool IsValid => !UseCropping || (CropStartX <= CropEndX && CropStartY <= CropEndY);

        public string Filename
        {
            get => models.Export.Filename;
            set
            {
                // do nothing. the text box needs a read/write property but wont be changed anyways
            }
        }

        public ObservableCollection<ComboBoxItem<int>> AvailableLayers { get; } = new ObservableCollection<ComboBoxItem<int>>();
        public ObservableCollection<ComboBoxItem<int>> AvailableMipmaps { get; } = new ObservableCollection<ComboBoxItem<int>>();
        public ObservableCollection<ComboBoxItem<DisplayedFormat>> AvailableFormat { get; } = new ObservableCollection<ComboBoxItem<DisplayedFormat>>();

        public bool EnableLayers => AvailableLayers.Count > 1;
        public bool EnableMipmaps => AvailableMipmaps.Count > 1;
        public bool EnableFormat => AvailableFormat.Count > 1;

        private ComboBoxItem<int> selectedLayer;
        public ComboBoxItem<int> SelectedLayer
        {
            get => selectedLayer;
            set
            {
                if (value == null || value == selectedLayer) return;
                //selectedLayer = value;
                models.Export.Layer = value.Cargo;
                //OnPropertyChanged(nameof(SelectedLayer));
            }
        }

        private ComboBoxItem<int> selectedMipmap;
        public ComboBoxItem<int> SelectedMipmap
        {
            get => selectedMipmap;
            set
            {
                if (value == null || value == selectedMipmap) return;
                //selectedMipmap = value;
                models.Export.Mipmap = value.Cargo;
                //OnPropertyChanged(nameof(SelectedMipmap));
            }
        }

        private ComboBoxItem<DisplayedFormat> selectedFormat;
        public ComboBoxItem<DisplayedFormat> SelectedFormat
        {
            get => selectedFormat;
            set
            {
                if (value == null || value == selectedFormat) return;
                selectedFormat = value;
                models.Export.TexFormat = selectedFormat.Cargo;
                OnPropertyChanged(nameof(SelectedFormat));
            }
        }

        public bool UseCropping
        {
            get => models.Export.UseCropping;
            set => models.Export.UseCropping = value;
        }

        public bool AllowCropping => models.Export.AllowCropping;

        public int CropMinX => models.Export.CropMinX;
        public int CropMaxX => models.Export.CropMaxX;
        public int CropMinY => models.Export.CropMinY;
        public int CropMaxY => models.Export.CropMaxY;

        public int CropStartX
        {
            get => models.Export.CropStartX;
            set => models.Export.CropStartX = value;
        }

        public int CropStartY
        {
            get => models.Export.CropStartY;
            set => models.Export.CropStartY = value;
        }

        public int CropEndX
        {
            get => models.Export.CropEndX;
            set => models.Export.CropEndX = value;
        }

        public int CropEndY
        {
            get => models.Export.CropEndY;
            set => models.Export.CropEndY = value;
        }

        public Visibility HasQuality => models.Export.HasQuality ? Visibility.Visible : Visibility.Collapsed;
        public int MinQuality => models.Export.MinQuality;
        public int MaxQuality => models.Export.MaxQuality;
        public int Quality
        {
            get => models.Export.Quality;
            set => models.Export.Quality = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
