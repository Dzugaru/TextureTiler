using Microsoft.Win32;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

using CvRect = OpenCvSharp.CPlusPlus.Rect;
using CvPoint = OpenCvSharp.CPlusPlus.Point;
using CvSize = OpenCvSharp.CPlusPlus.Size;

namespace TextureTiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        Random rng;

        public MainWindow()
        {
            InitializeComponent();
            rng = new Random();

            //DEBUG
            //Mat m1 = new Mat(4, 4, MatType.CV_32FC3);
            //m1.SetTo(new Scalar(1, 0, 0));

            //Mat m2 = new Mat(4, 4, MatType.CV_32FC3);
            //m2.SetTo(new Scalar(0.5, 1, 0));

            //double err = GetOverlapError(m1, m2, 2, true);
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            double blockQuotient = 0.5;
            double overlapQuotient = 1.0 / 6;
            int nSearchBlocks = 1000;

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Images|*.jpg;*.png;*.jpeg;*.bmp";
            dialog.RestoreDirectory = true;
            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Mat img8U = Cv2.ImRead(dialog.FileName);
                Mat img = new Mat();
                img8U.ConvertTo(img, MatType.CV_32FC3, 1.0 / 255);

                //WriteableBitmap bmp = img8U.ToWriteableBitmap();
                //image.Source = bmp;

                int w = img.Cols;
                int h = img.Rows;               
                
                int blockSize = (int)(Math.Min(w, h) * blockQuotient);
                int overlap = (int)(overlapQuotient * blockSize);

                Mat quilted = await Quilt(img, 8, 8, blockSize, overlap, nSearchBlocks);
                Mat quilted8U = new Mat();
                quilted.ConvertTo(quilted8U, MatType.CV_8UC3, 255.0);

              

                //List<(int, int, double)> errors = new List<(int, int, double)>();

                //var min = (err: double.MaxValue, x: 0, y: 0);
                //(Mat b0, int x0, int y0) = GetRandomBlock(img, blockSize);
                
                //for (int i = 0; i < nSearchBlocks; i++)
                //{
                //    (Mat b1, int x1, int y1) = GetRandomBlock(img, blockSize);
                //    double err = GetOverlapError(b0, b1, overlap, true);
                //    b1.Dispose();
                //    if (err < min.err)
                //    {
                //        min.err = err;
                //        min.x = x1;
                //        min.y = y1;
                //    }

                //    //System.Diagnostics.Debug.WriteLine(err);
                //}

                //GC.Collect();

                //unsafe
                //{
                //    byte* pImg = (byte*)img.Data;

                //    for (int i = 0; i < img.Rows; i++)
                //    {
                //        for (int j = 0; j < img.Cols; j++)
                //        {
                //            byte* pPix = pImg + 3 * (i * img.Cols + j);

                //            int b = *pPix;
                //            int g = *(pPix + 1);
                //            int r = *(pPix + 2);
                //        }
                //    }
                //}
                //img.GetArray()
            }
        }  
        
        (Mat, int, int) GetRandomBlock(Mat img, int sz)
        {
            int x = rng.Next(img.Cols - sz + 1);
            int y = rng.Next(img.Rows - sz + 1);
            return (new Mat(img, new CvRect(x, y, sz, sz)), x, y);
        }

        Mat GetBlock(Mat img, int x, int y, int sz)
        {
            return new Mat(img, new CvRect(x, y, sz, sz));
        }
        
        double GetOverlapError(Mat b1, Mat b2, int overlap, bool vert)
        {
            int blockSz = b1.Cols;

            Mat o1, o2;
            if(vert)
            {
                o1 = b1.Clone(new CvRect(blockSz - overlap, 0, overlap, blockSz));
                o2 = new Mat(b2, new CvRect(0, 0, overlap, blockSz));
            }
            else
            {
                o1 = b1.Clone(new CvRect(0, blockSz - overlap, blockSz, overlap));
                o2 = new Mat(b2, new CvRect(0, 0, blockSz, overlap));
            }

            Cv2.Subtract(o1, o2, o1);
            Cv2.Multiply(o1, o1, o1);           
                      
            Scalar s = Cv2.Sum(o1);
            double err = s.Val0 + s.Val1 + s.Val2;

            o1.Dispose();
            o2.Dispose();          
               
            return err;            
        }

        Mat GetGreedyVerticalOverlapMask(Mat b1, Mat b2, int overlap)
        {
            int blockSz = b1.Cols;

            Mat o1, o2;           
            o1 = new Mat(b1, new CvRect(blockSz - overlap, 0, overlap, blockSz));
            o2 = new Mat(b2, new CvRect(0, 0, overlap, blockSz));

            var min = (err: double.MaxValue, x0: 0);
            for(int x0 = 0; x0 <= overlap; x0++)
            {
                double err = 0;
            }

            o1.Dispose();
            o2.Dispose();
        }

        async Task<Mat> Quilt(Mat src, int h, int w, int blockSize, int overlap, int nSearchBlocks)
        {
            int step = blockSize - overlap;
            Mat result = new Mat(overlap + step * h, overlap + step * w, MatType.CV_32FC3);
            //DEBUG
            result.SetTo(new Scalar(0, 0, 0));
            Mat up, left;

            for (int i = 0; i < h; i++) //DEBUG
            {
                for (int j = 0; j < w; j++) //DEBUG
                {
                    if (i == 0) up = null;
                    else up = new Mat(result, new CvRect(j * step, (i - 1) * step, blockSize, blockSize));

                    if (j == 0) left = null;
                    else left = new Mat(result, new CvRect((j - 1) * step, i * step, blockSize, blockSize));

                    var min = (err: double.MaxValue, x: 0, y: 0);
                    for (int k = 0; k < nSearchBlocks; k++)
                    {
                        (Mat cb, int cx, int cy) = GetRandomBlock(src, blockSize);
                        double err = 0;
                        if (up != null) err += GetOverlapError(up, cb, overlap, false);
                        if (left != null) err += GetOverlapError(left, cb, overlap, true);
                        cb.Dispose();

                        if(err < min.err)
                        {
                            min.err = err;
                            min.x = cx;
                            min.y = cy;
                        }
                    }

                    Mat minBlock = GetBlock(src, min.x, min.y, blockSize);
                    Mat resultDst = new Mat(result, new CvRect(j * step, i * step, blockSize, blockSize));
                    minBlock.CopyTo(resultDst);
                    minBlock.Dispose();
                    resultDst.Dispose();

                    if (up != null) up.Dispose();
                    if (left != null) left.Dispose();

                    GC.Collect();

                    //DEBUG
                    Mat quilted8U = new Mat();
                    result.ConvertTo(quilted8U, MatType.CV_8UC3, 255.0);

                    WriteableBitmap quiltedBmp = quilted8U.ToWriteableBitmap();
                    image.Source = quiltedBmp;

                    await Task.Delay(100);
                }
            }

            return result;
        }
    }     
}
