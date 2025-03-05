using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace EventManager.Utilities
{
    public static class ImageHelper
    {
        public static ImageSource ConvertBytesToImage(byte[] imageSource, int width = 130, int height = 130)
        {
            if (imageSource == null || imageSource.Length == 0)
            {
                return "blank_id.png";
            }

            byte[] resizedBytes = ResizeImage(imageSource, width, height);

            return ImageSource.FromStream(() => new MemoryStream(resizedBytes));
        }

        private static byte[] ResizeImage(byte[] imageBytes, int width, int height)
        {
            using var inputStream = new MemoryStream(imageBytes);
            using var original = SKBitmap.Decode(inputStream);

            if (original == null)
            {
                return imageBytes; // Return original if decoding fails
            }

            // Create a new empty bitmap with the target size
            using var resized = new SKBitmap(width, height);

            // Define high-quality sampling options
            var samplingOptions = new SKSamplingOptions(SKCubicResampler.CatmullRom); // High quality

            // Scale pixels manually with better quality
            original.ScalePixels(resized, samplingOptions);

            using var image = SKImage.FromBitmap(resized);
            using var outputStream = new MemoryStream();

            // Encode as PNG at max quality
            image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(outputStream);
            return outputStream.ToArray();
        }

    }
}
