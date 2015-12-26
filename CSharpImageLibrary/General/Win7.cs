﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UsefulThings;

namespace CSharpImageLibrary.General
{
    /// <summary>
    /// Provides access to standard GDI+ Windows image formats.
    /// </summary>
    internal class Win7
    {
        #region Loading
        /// <summary>
        /// Attempts to load image using GDI+ codecs.
        /// Returns null on failure.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <returns>Bitmap of image, or null if failed.</returns>
        private static Bitmap AttemptWindowsCodecs(string imageFile)
        {
            Bitmap bmp = null;
            try
            {
                bmp = new Bitmap(imageFile);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            return bmp;
        }


        /// <summary>
        /// Attempts to load image using GDI+ codecs.
        /// </summary>
        /// <param name="stream">Entire file. NOT just pixels.</param>
        /// <returns>Bitmap of image, or null if failed.</returns>
        private static Bitmap AttemptWindowsCodecs(Stream stream)
        {
            Bitmap bmp = null;
            try
            {
                bmp = new Bitmap(stream);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            return bmp;
        }


        /// <summary>
        /// Loads image with Windows GDI+ codecs.
        /// </summary>
        /// <param name="imageFile">Path to image file.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <returns>BGRA Pixels as stream.</returns>
        internal static WriteableBitmap LoadImageWithCodecs(string imageFile, out int Width, out int Height)
        {
            using (FileStream fs = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadImageWithCodecs(fs, out Width, out Height, Path.GetExtension(imageFile));
        }


        /// <summary>
        /// Loads image with Windows GDI+ codecs.
        /// </summary>
        /// <param name="stream">Entire file. NOT just pixels.</param>
        /// <param name="Width">Image Width.</param>
        /// <param name="Height">Image Height.</param>
        /// <param name="extension"></param>
        /// <returns>BGRA Pixels as stream.</returns>
        internal static WriteableBitmap LoadImageWithCodecs(Stream stream, out int Width, out int Height, string extension = null)
        {
            Bitmap bmp = AttemptWindowsCodecs(stream);

            Width = 0;
            Height = 0;

            if (bmp == null)
                return null;

            byte[] imgData = UsefulThings.WinForms.Imaging.GetPixelDataFromBitmap(bmp);

            Width = bmp.Width;
            Height = bmp.Height;

            bmp.Dispose();

            WriteableBitmap wb = UsefulThings.WPF.Images.CreateWriteableBitmap(imgData, Width, Height);
            return wb;
        }
        #endregion Loading


        /// <summary>
        /// Build mipmaps for image by scaling.
        /// </summary>
        /// <param name="MipMaps">Mipmaps to build with. Often only top mipmap exists, with others populated by this function.</param>
        /// <returns>Number of mipmaps in image.</returns>
        internal static int BuildMipMaps(List<MipMap> MipMaps)
        {
            if (MipMaps?.Count == 0)
                return 0;

            MipMap currentMip = MipMaps[0];

            // KFreon: Check if mips required
            int estimatedMips = DDSGeneral.EstimateNumMipMaps(currentMip.Width, currentMip.Height);
            if ((estimatedMips + 1) == MipMaps.Count)  // +1 is cos estimatedMips is the number required to generate, not the total
                return estimatedMips;

            // KFreon: Setup dimensions
            int determiningDimension = currentMip.Height > currentMip.Width ? currentMip.Width : currentMip.Height;
            int newWidth = currentMip.Width;
            int newHeight = currentMip.Height;

            // KFreon: Half image dimensions until one dimension == 1
            MipMap[] newmips = new MipMap[estimatedMips];
            Parallel.For(0, estimatedMips, item =>
            {
                int index = item;
                newWidth /= 2;
                newHeight /= 2;

                var mip = Resize(currentMip, newWidth, newHeight);
                newmips[index] = mip;
            });

            MipMaps.AddRange(newmips);
            return estimatedMips;
        }


        /// <summary>
        /// Save using Windows 7- GDI+ Codecs to stream.
        /// Only single level images supported.
        /// </summary>
        /// <param name="pixelsWithMips">BGRA pixels.</param>
        /// <param name="destination">Image stream to save to.</param>
        /// <param name="format">Destination format.</param>
        /// <param name="Width">Width of image.</param>
        /// <param name="Height">Height of image.</param>
        /// <returns>True on success.</returns>
        internal static bool SaveWithCodecs(BitmapSource img, Stream destination, ImageEngineFormat format, int Width, int Height)
        {
            Bitmap bmp = UsefulThings.WinForms.Imaging.CreateBitmap(img, false);

            // KFreon: Get format
            System.Drawing.Imaging.ImageFormat imgformat = null;
            switch (format)
            {
                case ImageEngineFormat.BMP:
                    imgformat = System.Drawing.Imaging.ImageFormat.Bmp;
                    break;
                case ImageEngineFormat.JPG:
                    imgformat = System.Drawing.Imaging.ImageFormat.Jpeg;
                    break;
                case ImageEngineFormat.PNG:
                    imgformat = System.Drawing.Imaging.ImageFormat.Png;
                    break;
            }

            if (imgformat == null)
                throw new InvalidDataException($"Unable to parse format to Windows 7 codec format: {format}");

            bmp.Save(destination, imgformat);

            return true;
        }

        internal static MipMap Resize(MipMap mipMap, int width, int height)
        {
            Image bmp = UsefulThings.WinForms.Imaging.CreateBitmap(mipMap.BaseImage, false);
            bmp = UsefulThings.WinForms.Imaging.resizeImage(bmp, new Size(width, height));
            
            byte[] data = UsefulThings.WinForms.Imaging.GetPixelDataFromBitmap((Bitmap)bmp);
            bmp.Dispose();
            return new MipMap(UsefulThings.WPF.Images.CreateWriteableBitmap(data, width, height));
        }
    }
}
