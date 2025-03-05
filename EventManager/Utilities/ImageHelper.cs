using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Utilities
{
    public static class ImageHelper
    {
        public static ImageSource ConvertBytesToImage(byte[] imageSource)
        {
            if(imageSource == null || imageSource.Length == 0)
            {
                return "blank_id.pmg";
            }

            return ImageSource.FromStream(() => new MemoryStream(imageSource));

        }
    }
}
