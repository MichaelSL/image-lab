using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HistogramComparer.Desktop
{
    public class HistogramService
    {
        public const int DEFAULT_HISTOGRAM_WIDTH = 256;

        public HistogramService()
        {

        }

        public double[] GetNormalizedGraysacale(Bitmap source)
        {
            var histogram = new double[DEFAULT_HISTOGRAM_WIDTH];
            histogram.Initialize();

            var imagePixels = source.Width * source.Height;
            BitmapData data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;

                int remain = data.Stride - data.Width * 4;

                for (int i = 0; i < data.Height; i++)
                {
                    for (int j = 0; j < data.Width; j++)
                    {
                        if (ptr[3] > 127)
                        {
                            int mean = ptr[0] + ptr[1] + ptr[2];
                            mean /= 3;

                            histogram[mean]++;
                        }
                        ptr += 4;
                    }

                    ptr += remain;
                }

                for (int i = 0; i < histogram.Length; i++)
                {
                    histogram[i] /= imagePixels;
                }
            }

            return histogram;
        }

        public int[] GetGraysacale(Bitmap source)
        {
            var histogram = new int[DEFAULT_HISTOGRAM_WIDTH];
            histogram.Initialize();
            return histogram;
        }
    }

    public struct Pixel
    {
        readonly int R;
        readonly int G;
        readonly int B;
    }
}
