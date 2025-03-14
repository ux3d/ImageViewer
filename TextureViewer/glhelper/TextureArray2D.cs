﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using TextureViewer.Models;
using TextureViewer.Models.Shader;

namespace TextureViewer.glhelper
{
    public class TextureArray2D
    {
        /// <summary>
        /// id for the main texture array
        /// </summary>
        private int id = 0;
        /// <summary>
        /// id for the cubemap texture view
        /// </summary>
        private int cubeId = 0;
        /// <summary>
        /// ids for single 2d textures
        /// </summary>
        private int[] tex2DId;

        private readonly SizedInternalFormat internalFormat;
        private readonly int nMipmaps;
        private readonly int nLayer;
        // width of upper level
        private readonly int width;
        // height of upper level
        private readonly int height;

        public bool HasMipmaps => nMipmaps > 1;
        public int NumMipmaps => nMipmaps;
        public int NumLayer => nLayer;

        /// <summary>
        /// indicates if the texture needs to be converted from srb space to physical space
        /// </summary>
        public bool IsSrgb { get; } = false;

        /// <summary>
        /// creates an empty Texture 2D Array
        /// </summary>
        /// <param name="numLayers"></param>
        /// <param name="numMipmaps"></param>
        /// <param name="internalFormat"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public TextureArray2D(int numLayers, int numMipmaps, SizedInternalFormat internalFormat, 
            int width, int height, bool isSrgb = false)
        {
            id = GL.GenTexture();
            this.internalFormat = internalFormat;
            this.nMipmaps = numMipmaps;
            this.nLayer = numLayers;
            this.width = width;
            this.height = height;
            this.IsSrgb = isSrgb;
            GL.BindTexture(TextureTarget.Texture2DArray, id);

            GL.TexStorage3D(TextureTarget3d.Texture2DArray, numMipmaps,
                internalFormat, width,
                height, numLayers);

            CreateTexture2DViews();
        }

        /// <summary>
        /// create texture form an existing image
        /// </summary>
        /// <param name="image"></param>
        public TextureArray2D(ImageLoader.Image image)
        {
            id = GL.GenTexture();
            this.nMipmaps = image.NumMipmaps;
            this.nLayer = image.Layers.Count;
            this.internalFormat = image.Format.InternalFormat;
            this.IsSrgb = image.Format.IsSrgb;
            this.width = image.GetWidth(0);
            this.height = image.GetHeight(0);

            GL.BindTexture(TextureTarget.Texture2DArray, id);

            // create storage

            GL.TexStorage3D(TextureTarget3d.Texture2DArray, image.NumMipmaps,
                internalFormat, image.GetWidth(0),
                image.GetHeight(0), image.Layers.Count);

            if (image.Format.IsCompressed)
            {
                for (int face = 0; face < image.Layers.Count; ++face)
                {
                    for (int level = 0; level < image.NumMipmaps; ++level)
                    {
                        GL.CompressedTexSubImage3D(TextureTarget.Texture2DArray, level,
                            0, 0, face, image.GetWidth(level), image.GetHeight(level),
                            1, image.Format.Format,
                            (int)image.Layers[face].Mipmaps[level].Size,
                            image.Layers[face].Mipmaps[level].Bytes);
                    }
                }
            }
            else
            {
                for (int face = 0; face < image.Layers.Count; ++face)
                {
                    for (int level = 0; level < image.NumMipmaps; ++level)
                    {
                        GL.TexSubImage3D(TextureTarget.Texture2DArray, level,
                            0, 0, face, image.GetWidth(level), image.GetHeight(level),
                            1, image.Format.Format, image.Format.Type, image.Layers[face].Mipmaps[level].Bytes);
                    }
                }
            }

            CreateTexture2DViews();
        }

        /// <summary>
        /// creates texture 2d views if they were not already created
        /// </summary>
        private void CreateTexture2DViews()
        {
            if (tex2DId != null)
                return;

            tex2DId = new int[nLayer * nMipmaps];
            GL.GenTextures(nLayer * nMipmaps, tex2DId);
            for (int curLayer = 0; curLayer < nLayer; ++curLayer)
            {
                for (int curMipmap = 0; curMipmap < nMipmaps; ++curMipmap)
                {
                    GL.TextureView(tex2DId[GetTextureIndex(curLayer, curMipmap)], TextureTarget.Texture2D, id,
                        (PixelInternalFormat)internalFormat, curMipmap, 1, curLayer, 1);

                    GL.BindTexture(TextureTarget.Texture2D, tex2DId[GetTextureIndex(curLayer, curMipmap)]);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);
                }
            }
        }

        private int GetTextureIndex(int layer, int mipmap)
        {
            return layer * nMipmaps + mipmap;
        }

        /// <summary>
        /// creates cube map view if it was not already created
        /// </summary>
        private void CreateCubeMapView()
        {
            if (cubeId != 0)
                return;

            Debug.Assert(nLayer == 6);
            cubeId = GL.GenTexture();
            GL.TextureView(cubeId, TextureTarget.TextureCubeMap, id,
                (PixelInternalFormat)internalFormat, 0, nMipmaps, 0, 6);
            GL.BindTexture(TextureTarget.TextureCubeMap, cubeId);

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapR, (int)TextureParameterName.ClampToEdge);
        }

        /// <summary>
        /// generates new mipmaps
        /// </summary>
        public TextureArray2D GenerateMipmapLevels(int levels)
        {
            Debug.Assert(!HasMipmaps);
            // create a texture with the same format and more mipmaps
            var newTex = new TextureArray2D(nLayer, levels, internalFormat, width, height, IsSrgb);

            // copy image data of first level
            GL.CopyImageSubData(id, ImageTarget.Texture2DArray, 0, 0, 0, 0,
                         newTex.id, ImageTarget.Texture2DArray, 0, 0, 0, 0, width, height, nLayer);

            newTex.GenerateMipmaps();

            return newTex;
        }

        /// <summary>
        /// creates a new texture that has only one mipmap level
        /// </summary>
        /// <param name="level">mipmap level to clone</param>
        /// <returns></returns>
        public TextureArray2D CloneMipmapLevel(int level)
        {
            Debug.Assert(level < nMipmaps);
            Debug.Assert(level >= 0);

            GL.GetTexLevelParameter(TextureTarget.Texture2DArray, level, GetTextureParameter.TextureWidth, out int lvlWidth);
            GL.GetTexLevelParameter(TextureTarget.Texture2DArray, level, GetTextureParameter.TextureHeight, out int lvlHeight);
            var newTex = new TextureArray2D(nLayer, 1, internalFormat, lvlWidth, lvlHeight, IsSrgb);

            // copy data of first mipmap level
            GL.CopyImageSubData(id, ImageTarget.Texture2DArray, level, 0, 0, 0,
                         newTex.id, ImageTarget.Texture2DArray, 0, 0, 0, 0, lvlWidth, lvlHeight, nLayer);

            return newTex;
        }

        /// <summary>
        /// performs a deep gpu copy of the textures
        /// </summary>
        /// <returns></returns>
        public TextureArray2D Clone()
        {
            var newTex = new TextureArray2D(nLayer, nMipmaps, internalFormat, width, height, IsSrgb);

            for (var level = 0; level < nMipmaps; ++level)
            {
                GL.GetTexLevelParameter(TextureTarget.Texture2DArray, level, GetTextureParameter.TextureWidth, out int lwidth);
                GL.GetTexLevelParameter(TextureTarget.Texture2DArray, level, GetTextureParameter.TextureHeight, out int lheight);

                GL.CopyImageSubData(id, ImageTarget.Texture2DArray, level, 0, 0, 0,
                    newTex.id, ImageTarget.Texture2DArray, level, 0, 0, 0, 
                    lwidth, lheight, nLayer);
            }

            return newTex;
        }

        /// <summary>
        /// generates mipmaps for the existing mipmap levels
        /// </summary>
        public void GenerateMipmaps()
        {
            Debug.Assert(HasMipmaps);
            GL.BindTexture(TextureTarget.Texture2DArray, id);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
        }

        /// <summary>
        /// binds specified texture
        /// </summary>
        /// <param name="target">texture target</param>
        /// <param name="texId">texture id</param>
        private void BindAs(TextureTarget target, int texId)
        {
            GL.BindTexture(target, texId);
        }

        /// <summary>
        /// binds texture as texture array 2d
        /// </summary>
        /// <param name="slot">binding slot</param>
        public void Bind(int slot)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + slot);
            BindAs(TextureTarget.Texture2DArray, id);
        }

        /// <summary>
        /// binds texture as cube map
        /// </summary>
        /// <param name="slot">binding slot</param>
        public void BindAsCubemap(int slot)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + slot);
            CreateCubeMapView();
            BindAs(TextureTarget.TextureCubeMap, cubeId);
        }

        /// <summary>
        /// binds texture as texture2D
        /// </summary>
        /// <param name="slot">binding slot</param>
        /// <param name="layer">which layer of the texture</param>
        /// <param name="mipmap">mipmap mipmap</param>
        public void BindAsTexture2D(int slot, int layer, int mipmap)
        {
            Debug.Assert(tex2DId != null);
            GL.ActiveTexture(TextureUnit.Texture0 + slot);
            BindAs(TextureTarget.Texture2D, tex2DId[GetTextureIndex(layer, mipmap)]);
        }

        /// <summary>
        /// binds the texture as image
        /// </summary>
        /// <param name="slot">binding slot</param>
        /// <param name="mipmap">which mipmap</param>
        /// <param name="layer">which layer</param>
        /// <param name="access">texture access</param>
        public void BindAsImage(int slot, int layer, int mipmap, TextureAccess access)
        {
            GL.BindImageTexture(slot, id, mipmap, false, layer, access, internalFormat);
        }

        /// <summary>
        /// reads (sub) data from a single image layer and a single image mipmap with the specified format and type
        /// </summary>
        /// <param name="layer">image layer</param>
        /// <param name="mipmap">image mipmap</param>
        /// <param name="format">destination format</param>
        /// <param name="type">destination type</param>
        /// <param name="toSrgb">indicates if data should be converted into srgb space</param>
        /// <param name="useCropping">indicates if the source image should be cropped</param>
        /// <param name="xOffset">if useCropping: x pixel offset</param>
        /// <param name="yOffset">if useCropping: y pixel offset</param>
        /// <param name="width">if useCropping: width of destination image. Will be set to the exported image width</param>
        /// <param name="height">if useCropping: height of destination image. Will be set to the exported image height</param>
        /// <param name="exportShader">shader required for srgb conversion and cropping</param>
        /// <param name="bufferSize"> Expected size of buffer in bytes. If bufferSize = 0 the buffer size will be calculated based the pixel type size times the number of pixel components (this only works for simple formats)</param>
        /// <returns></returns>
        public byte[] GetData(int layer, int mipmap, ImageLoader.ImageFormat format, bool useCropping,
            int xOffset, int yOffset, ref int width, ref int height, PixelExportShader exportShader, int bufferSize = 0)
        {
            Debug.Assert(!format.IsCompressed);

            // retrieve width and height of the mipmap
            GL.BindTexture(TextureTarget.Texture2DArray, id);
            GL.GetTexLevelParameter(TextureTarget.Texture2DArray, mipmap, GetTextureParameter.TextureWidth, out int maxWidth);
            GL.GetTexLevelParameter(TextureTarget.Texture2DArray, mipmap, GetTextureParameter.TextureHeight, out int maxHeight);
            
            Debug.Assert(layer < nLayer);
            Debug.Assert(mipmap < nMipmaps);
            if (useCropping)
            {
                Debug.Assert(xOffset + width <= maxWidth);
                Debug.Assert(yOffset + height <= maxHeight);
            }
            else
            {
                xOffset = 0;
                yOffset = 0;
                width = maxWidth;
                height = maxHeight;
            }


            int bs = bufferSize;
            // try to calculate the buffer size (only for simple formats)
            if(bs == 0) bs = width * height * GetPixelTypeSize(format.Type) * GetPixelFormatCount(format.Format);
            byte[] buffer = new byte[bs];

            //if (format.IsSrgb || useCropping)
            // This needs to be used due to some export bug with certain formats (e.g. RGBA4_UNORM_PACK16)
            {
                // create temporary texture and convert data
                var tmpTex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, tmpTex);
                GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, width, height);

                // use crop/srgb shader
                GL.BindImageTexture(exportShader.GetDestinationTextureLocation(), tmpTex, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba32f);
                BindAsTexture2D(exportShader.GetSourceTextureLocation(), layer, mipmap);

                exportShader.Use(xOffset, yOffset, width, height, format.IsSrgb);

                // obtain data
                GL.BindTexture(TextureTarget.Texture2D, tmpTex);
                GL.GetTexImage(TextureTarget.Texture2D, 0, format.Format, format.Type, buffer);

                // cleanup
                GL.DeleteTexture(tmpTex);
            }
            //else
            //{
            //    // read directly (NOT POSSIBLE DUE TO EXPORT BUG e.g. RGBA4_UNORM_PACK16)
            //    BindAsTexture2D(0, layer, mipmap);
            //    GL.GetTexImage(TextureTarget.Texture2D, 0, format.Format, format.Type, buffer);
            //}

            return buffer;
        }

        public static int GetPixelFormatCount(PixelFormat f)
        {
            switch (f)
            {
                case PixelFormat.RedInteger:
                case PixelFormat.GreenInteger:
                case PixelFormat.BlueInteger:
                case PixelFormat.AlphaInteger:
                case PixelFormat.Green:
                case PixelFormat.Blue:
                case PixelFormat.Alpha:
                case PixelFormat.Red:
                case PixelFormat.Luminance:
                    return 1;

                case PixelFormat.Rgb:
                case PixelFormat.Bgr:
                case PixelFormat.RgbInteger:
                case PixelFormat.BgrInteger:
                    return 3;
                case PixelFormat.Rgba:
                case PixelFormat.AbgrExt:
                case PixelFormat.Bgra:
                case PixelFormat.RgbaInteger:
                case PixelFormat.BgraInteger:
                    return 4;
                case PixelFormat.RgInteger:
                case PixelFormat.Rg:
                case PixelFormat.LuminanceAlpha:
                    return 2;
                default:
                    throw new Exception("invalid pixel format used: " + f);
            }
        }

        private static int GetPixelTypeSize(PixelType t)
        {
            switch (t)
            {
                case PixelType.UnsignedByte:
                case PixelType.Byte:
                case PixelType.UnsignedInt8888Reversed:
                    return 1;
                case PixelType.HalfFloat:
                case PixelType.Short:
                case PixelType.UnsignedShort:
                    return 2;
                case PixelType.Int:
                case PixelType.UnsignedInt:
                case PixelType.Float:
                    return 4;
                default:
                    throw new Exception("invalid pixel type used: " + t);
            }
        }

        public void Dispose()
        {
            if (tex2DId != null)
            {
                GL.DeleteTextures(nLayer * nMipmaps, tex2DId);
                tex2DId = null;
            }
            if (cubeId != 0)
            {
                GL.DeleteTexture(cubeId);
                cubeId = 0;
            }
            if (id != 0)
            {
                GL.DeleteTexture(id);
                id = 0;
            }
        }

    }
}
