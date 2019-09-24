﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using OpenTK.Graphics.OpenGL4;
using TextureViewer.Annotations;
using TextureViewer.Controller;
using TextureViewer.glhelper;

namespace TextureViewer.Models
{
    public class ImagesModel: INotifyPropertyChanged
    {
        private struct Dimension
        {
            public int Width;
            public int Height;
        }

        private OpenGlContext context;
        private AppModel app;
        private Dimension[] dimensions = null;

        public class TextureArray2DInformation
        {
            public TextureArray2D Image { get; set; }
            public string Name { get; set; }
            public bool IsGrayscale { get; set; }
            public bool IsAlpha { get; set; }
        }

        private class ImageData
        {
            public TextureArray2D TextureArray { get; private set; }
            public int NumMipmaps { get; private set; }
            public int NumLayers { get; }
            public bool IsGrayscale { get; }
            public bool HasAlpha { get; }
            public bool IsHdr { get; }
            public string Filename { get; }
            public string FormatName { get; }

            public ImageData(ImageLoader.Image image)
            {
                this.TextureArray = new TextureArray2D(image);
                NumMipmaps = image.NumMipmaps;
                NumLayers = image.NumLayers;
                IsGrayscale = image.IsGrayscale();
                HasAlpha = image.HasAlpha();
                IsHdr = image.IsHdr();
                Filename = image.Filename;
                FormatName = image.Format.ToString();
            }

            public ImageData(TextureArray2DInformation info)
            {
                this.TextureArray = info.Image;
                NumMipmaps = TextureArray.NumMipmaps;
                NumLayers = TextureArray.NumLayer;
                IsGrayscale = info.IsGrayscale;
                HasAlpha = info.IsAlpha;
                IsHdr = true;
                Filename = info.Name;
                FormatName = new ImageLoader.ImageFormat(PixelFormat.Rgba, PixelType.Float, false).ToString();
            }

            public void GenerateMipmaps(int levels)
            {
                var oldTex = TextureArray;
                TextureArray = TextureArray.GenerateMipmapLevels(levels);
                oldTex.Dispose();
                NumMipmaps = levels;
            }

            public void DeleteMipmaps()
            {
                var oldTex = TextureArray;
                TextureArray = TextureArray.CloneMipmapLevel(0);
                oldTex.Dispose();
                NumMipmaps = 1;
            }

            /// <summary>
            /// disposes all opengl related data.
            /// Component should not be used after this
            /// </summary>
            public void Dispose()
            {
                TextureArray.Dispose();
            }
        }

        private readonly List<ImageData> images;

        public ImagesModel(OpenGlContext context, AppModel app)
        {
            this.app = app;
            this.context = context;
            images = new List<ImageData>();
        }

        #region Public Members

        // this property change will be triggered if the image order changes (and not if the number of images changes)
        public static string ImageOrder = nameof(ImageOrder);

        public int NumImages => images.Count;
        public int NumMipmaps => images.Count == 0 ? 0 : images[0].NumMipmaps;
        public int NumLayers => images.Count == 0 ? 0 : images[0].NumLayers;
        /// <summary>
        /// true if all images are grayscale
        /// </summary>
        public bool IsGrayscale => images.All(imageData => imageData.IsGrayscale);
        /// <summary>
        /// true if any image has an alpha channel
        /// </summary>
        public bool IsAlpha => images.Any(imageData => imageData.HasAlpha);
        /// <summary>
        /// true if any image is hdr
        /// </summary>
        public bool IsHdr => images.Any(imageData => imageData.IsHdr);

        /// <summary>
        /// width for the biggest mipmap (or 0 if no images are present)
        /// </summary>
        public int Width => images.Count != 0 ? GetWidth(0) : 0;
        /// <summary>
        /// height for the biggest mipmap (or 0 if no images are present)
        /// </summary>
        public int Height => images.Count != 0 ? GetHeight(0) : 0;

        // helper for many other models
        /// <summary>
        /// previous number of images
        /// </summary>
        public int PrevNumImages { get; private set; } = 0;

        /// <summary>
        /// width of the mipmap
        /// </summary>
        /// <param name="mipmap"></param>
        /// <returns></returns>
        public int GetWidth(int mipmap)
        {
            Debug.Assert(images.Count != 0);
            Debug.Assert(mipmap < NumMipmaps && mipmap >= 0);
            return dimensions[mipmap].Width;
        }

        /// <summary>
        /// height of the mipmap
        /// </summary>
        /// <param name="mipmap"></param>
        /// <returns></returns>
        public int GetHeight(int mipmap)
        {
            Debug.Assert(images.Count != 0);
            Debug.Assert(mipmap < NumMipmaps && mipmap >= 0);
            return dimensions[mipmap].Height;
        }

        public string GetFilename(int image)
        {
            Debug.Assert((uint)(image) < images.Count);
            return images[image].Filename;
        }

        /// <summary>
        /// name of the file format
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public string GetFileFormat(int image)
        {
            Debug.Assert((uint)(image) < images.Count);
            return images[image].FormatName;
        }

        public TextureArray2D GetTexture(int image)
        {
            Debug.Assert((uint)(image) < images.Count);
            return images[image].TextureArray;
        }

        /// <summary>
        /// tries to add the image to the current collection.
        /// Throws an exception if the image cannot be added
        /// </summary>
        /// <param name="imgs">images that should be added</param>
        public void AddImages(List<ImageLoader.Image> imgs)
        {
            Debug.Assert(!context.GlEnabled);
            context.Enable();
            foreach (var image in imgs)
            {
                if (images.Count == 0)
                {
                    images.Add(new ImageData(image));

                    // intialize dimensions
                    dimensions = new Dimension[image.NumMipmaps];
                    for (var i = 0; i < image.NumMipmaps; ++i)
                    {
                        dimensions[i] = new Dimension()
                        {
                            Height = image.GetHeight(i),
                            Width = image.GetWidth(i)
                        };
                    }

                    // a lot has changed
                    PrevNumImages = NumImages - 1;
                    OnPropertyChanged(nameof(NumImages));
                    OnPropertyChanged(nameof(NumLayers));
                    OnPropertyChanged(nameof(NumMipmaps));
                    OnPropertyChanged(nameof(IsAlpha));
                    OnPropertyChanged(nameof(IsGrayscale));
                    OnPropertyChanged(nameof(IsHdr));
                    OnPropertyChanged(nameof(Width));
                    OnPropertyChanged(nameof(Height));
                }
                else // test if image compatible with previous images
                {
                    if(image.Layers.Count != NumLayers)
                        throw new Exception($"{image.Filename}: Inconsistent amount of layers. Expected {NumLayers} got {image.Layers.Count}");

                    ImageData imgData = null;
                    if (image.NumMipmaps != NumMipmaps)
                    {
                        // try to generate/discard mipmaps if the dimensions match
                        // first layer MUST be of the same size
                        if(image.GetWidth(0) != GetWidth(0) || image.GetHeight(0) != GetHeight(0))
                            throw new Exception($"{image.Filename}: Mismatching image resolution. Expected {GetWidth(0)}x{GetHeight(0)}" +
                                                $" got {image.GetWidth(0)}x{image.GetHeight(0)}");

                        // either generate or remove mipmaps
                        if (NumMipmaps == 1)
                        {
                            // TODO inform the user? offer to generate mipmaps for the other images instead?
                            // remove mipmaps from the new image
                            imgData = new ImageData(image);
                            imgData.DeleteMipmaps();
                        }
                        else if(image.NumMipmaps == 1)
                        {
                            // generate mipmaps for the new image (silent)
                            imgData = new ImageData(image);
                            imgData.GenerateMipmaps(NumMipmaps);
                        }
                        else throw new Exception($"{image.Filename}: Inconsistent amount of mipmaps. Expected {NumMipmaps} got {image.NumMipmaps}");                       
                    }
                    else // mipmaps are present in both images
                    {
                        // test mipmaps
                        for (var level = 0; level < NumMipmaps; ++level)
                        {
                            if (image.GetWidth(level) != GetWidth(level) || image.GetHeight(level) != GetHeight(level))
                                throw new Exception(
                                    $"{image.Filename}: Inconsistent mipmaps dimension. Expected {GetWidth(level)}x{GetHeight(level)}" +
                                    $" got {image.GetWidth(level)}x{image.GetHeight(level)}");
                        }
                        imgData = new ImageData(image);
                    }    

                    Add(imgData);
                }
            }
            context.Disable();
        }

        /// <summary>
        /// Adds image from an existing TextureArray2D
        /// </summary>
        /// <param name="texture"></param>
        public void AddImage(TextureArray2DInformation texture)
        {
            Debug.Assert(!context.GlEnabled);
            context.Enable();
            Add(new ImageData(texture));
            context.Disable();
        }

        /// <summary>
        /// adds an image and triggers some properties that might have changed
        /// Only possible if there are already images in the list
        /// </summary>
        /// <param name="imgData"></param>
        private void Add(ImageData imgData)
        {
            Debug.Assert(context.GlEnabled);
            Debug.Assert(imgData != null);
            Debug.Assert(images.Count != 0);

            // remember old properties
            var isAlpha = IsAlpha;
            var isGrayscale = IsGrayscale;
            var isHdr = IsHdr;

            images.Add(imgData);

            PrevNumImages = NumImages - 1;
            OnPropertyChanged(nameof(NumImages));
            if (isAlpha != IsAlpha)
                OnPropertyChanged(nameof(isAlpha));
            if (isGrayscale != IsGrayscale)
                OnPropertyChanged(nameof(IsGrayscale));
            if (isHdr != IsHdr)
                OnPropertyChanged(nameof(IsHdr));
        }

        /// <summary>
        /// deletes an image including all opengl data
        /// </summary>
        /// <param name="imageId"></param>
        public void DeleteImage(int imageId)
        {
            Debug.Assert(imageId >= 0 && imageId < NumImages);

            // remember old properties
            var isAlpha = IsAlpha;
            var isGrayscale = IsGrayscale;
            var isHdr = IsHdr;

            Debug.Assert(!context.GlEnabled);
            context.Enable();
            // delete old data
            images[imageId].Dispose();

            images.RemoveAt(imageId);

            PrevNumImages = NumImages + 1;
            OnPropertyChanged(nameof(NumImages));

            if (isAlpha != IsAlpha)
                OnPropertyChanged(nameof(isAlpha));
            if (isGrayscale != IsGrayscale)
                OnPropertyChanged(nameof(IsGrayscale));
            if (isHdr != IsHdr)
                OnPropertyChanged(nameof(IsHdr));

            if (NumImages == 0)
            {
                // everything was resettet
                OnPropertyChanged(nameof(NumLayers));
                OnPropertyChanged(nameof(NumMipmaps));
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Height));
            }

            context.Disable();
        }

        /// <summary>
        /// moves the image from index 1 to index 2
        /// </summary>
        /// <param name="idx1">current image index</param>
        /// <param name="idx2">index after moving the image</param>
        public void MoveImage(int idx1, int idx2)
        {
            Debug.Assert(idx1 >= 0);
            Debug.Assert(idx2 >= 0);
            Debug.Assert(idx1 < NumImages);
            Debug.Assert(idx2 < NumImages);
            if (idx1 == idx2) return;

            var i = images[idx1];
            images.RemoveAt(idx1);
            images.Insert(idx2, i);

            OnPropertyChanged(nameof(ImageOrder));
        }

        /// <summary>
        /// deletes all opengl related data.
        /// Component should not be used after this
        /// </summary>
        public void Dispose()
        {
            foreach (var imageData in images)
            {
                imageData.Dispose();
            }
            images.Clear();
        }

        /// <summary>
        /// generates mipmaps for all images
        /// </summary>
        public void GenerateMipmaps()
        {
            Debug.Assert(!context.GlEnabled);
            context.Enable();

            Debug.Assert(NumMipmaps == 1);

            // compute new mipmap levels
            var levels = ComputeMaxMipLevels();
            if (levels == NumMipmaps) return;

            foreach (var image in images)
            {
                image.GenerateMipmaps(levels);
            }

            // recalc dimensions array
            var w = Width;
            var h = Height;
            dimensions = new Dimension[levels];
            for (int i = 0; i < levels; ++i)
            {
                dimensions[i].Width = w;
                dimensions[i].Height = h;
                w = Math.Max(w / 2, 1);
                h = Math.Max(h / 2, 1);
            }

            OnPropertyChanged(nameof(NumMipmaps));

            context.Disable();
        }

        public void DeleteMipmaps()
        {
            Debug.Assert(!context.GlEnabled);
            context.Enable();
            
            Debug.Assert(NumMipmaps > 1);
            foreach (var image in images)
            {
                image.DeleteMipmaps();
            }
            // refresh dimensions array
            var w = Width;
            var h = Height;
            dimensions = new Dimension[1];
            dimensions[0].Width = w;
            dimensions[0].Height = h;

            OnPropertyChanged(nameof(NumMipmaps));

            context.Disable();
        }

        /// <summary>
        /// computes the maximum amount of mipmap levels for the current width and height
        /// </summary>
        /// <returns></returns>
        private int ComputeMaxMipLevels()
        {
            var resolution = Math.Max(Width, Height);
            var maxMip = 1;
            while ((resolution /= 2) > 0) ++maxMip;
            return maxMip;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
