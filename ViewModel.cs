using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp.Extensions;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Prism.Commands;
using System.Windows.Media;
using System.Windows;

using CvPoint = OpenCvSharp.CPlusPlus.Point;
using CvSize = OpenCvSharp.CPlusPlus.Size;

namespace TextureTiler
{
    class ViewModel : BindableBase
    {
        List<Mat> sources = new List<Mat>();
        ImageSource result;

        public ObservableCollection<SourceImageViewModel> BitmapSources { get; private set; } = new ObservableCollection<SourceImageViewModel>();
        public DelegateCommand LoadCommand { get; private set; }
        public DelegateCommand QuiltCommand { get; private set; }

        public ImageSource Result
        {
            get { return result; }
            set { SetProperty(ref result, value); }
        }

        public int Width { get; set; } = 512;
        public int Height { get; set; } = 512;
        public int BlockSize { get; set; } = 32;

        public ViewModel()
        {
            LoadCommand = new DelegateCommand(Load);
            QuiltCommand = new DelegateCommand(Quilt);
        }

        void Load()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "Images|*.jpg;*.png;*.jpeg;*.bmp";
            dialog.RestoreDirectory = true;
            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                foreach (var src in sources)
                    src.Dispose();
                sources = new List<Mat>();
                BitmapSources.Clear();
                foreach (string filename in dialog.FileNames)
                {
                    Mat img8U = Cv2.ImRead(filename);
                    Mat img = new Mat();
                    img8U.ConvertTo(img, MatType.CV_32FC3, 1.0 / 255);                    
                    sources.Add(img);
                    BitmapSources.Add(new SourceImageViewModel(img8U.ToWriteableBitmap()));
                    img8U.Dispose();
                }                
            }
        }

        async void Quilt()
        {
            //for (;;)
            //{
            //    Mat b1 = new Mat(16, 16, MatType.CV_32FC3, new Scalar(1, 1, 1));
            //    Mat b2 = new Mat(16, 16, MatType.CV_32FC3, new Scalar(1, 1, 1));
            //    Mat b = new Mat(16, 16, MatType.CV_32FC3, new Scalar(1, 1, 1));
            //    Cv2.Subtract(b1, b2, b);

            //    b1.Dispose();
            //    b2.Dispose();
            //    b.Dispose();

            //    //GC.Collect();
            //}

            //int size = 64;
            //int qSize = (int)Math.Round(Math.Sqrt(2) * size);
            //Mat m = new Mat(qSize, qSize, MatType.CV_32FC3, new Scalar(255,0,0));
            //Cv2.FillPoly(m, new[] { new[] { new CvPoint(0.5f * qSize, 0), new CvPoint(qSize, 0.5f * qSize), new CvPoint(0.5f * qSize, qSize), new CvPoint(0, 0.5f * qSize) } }, new Scalar(0, 0, 255));

            //Mat trans = Cv2.GetRotationMatrix2D(new Point2f(0.5f * qSize, 0.5f * qSize), -45, 1);           
            //trans.Set<double>(0, 2, trans.At<double>(0, 2) - 0.5 * (qSize - size));
            //trans.Set<double>(1, 2, trans.At<double>(1, 2) - 0.5 * (qSize - size));
            //Mat tm = new Mat();
            //Cv2.WarpAffine(m, tm, trans, new CvSize(size, size), OpenCvSharp.Interpolation.Linear);

            //Cv2.ImShow("Test", tm);

            //DEBUG
            //int size = 64;
            //int qSize = (int)Math.Round(Math.Sqrt(2) * size);
            
            //Mat m = new Mat(qSize, qSize, MatType.CV_32FC3, new Scalar(255,0,0));
            //Cv2.FillPoly(m, new[] { new[] { new CvPoint(0.5f * qSize, 10), new CvPoint(qSize - 10, 0.5f * qSize), new CvPoint(0.5f * qSize, qSize - 10), new CvPoint(10, 0.5f * qSize) } }, new Scalar(0, 0, 255));

            //float[,] mapx = new float[size, size];
            //float[,] mapy = new float[size, size];

            //float dxy = (0.5f * qSize - 0.5f) / (size + 1);
            //float x0 = 0.5f * (qSize - 1);
            //float y0 = 0;

            //for (int i = 0; i < size; i++)
            //{
            //    for (int j = 0; j < size; j++)
            //    {
            //        mapx[i, j] = x0 + j * dxy;
            //        mapy[i, j] = y0 + j * dxy;
            //    }

            //    x0 -= dxy;
            //    y0 += dxy;
            //}

            //Mat mMapx = new Mat(size, size, MatType.CV_32FC1, mapx);
            //Mat mMapy = new Mat(size, size, MatType.CV_32FC1, mapy);

            //Mat tm = new Mat(size, size, MatType.CV_32FC3);
            //Cv2.Remap(m, tm, mMapx, mMapy, OpenCvSharp.Interpolation.Cubic, OpenCvSharp.BorderType.Wrap);

            //Cv2.ImShow("Src", m);
            //Cv2.ImShow("Dst", tm);

            //return;



            //DEBUG
            var wangTiler = new WangTiler(3, 3, 31337);
            var q = new Quilter();
            q.Sources = sources;

            //System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            //{
                List<Mat> tiles = await wangTiler.GenerateTiles(q, BlockSize, 1f / 6, 1000);
            //}
            //));
            //th.Start();
            Mat result = wangTiler.FitTiles(8, 8, BlockSize, tiles);

            //Cv2.ImShow("Test", result);
            Mat quilted8U = new Mat();
            result.ConvertTo(quilted8U, MatType.CV_8UC3, 255.0);
            WriteableBitmap quiltedBmp = quilted8U.ToWriteableBitmap();
            Result = quiltedBmp;

            return;


            //foreach (var src in sources)
            //{
            //    if (BlockSize > src.Cols || BlockSize > src.Rows)
            //    {
            //        MessageBox.Show("Block Size exceeds one of the source's size, please reduce Block Size or reload the sources.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //        return;
            //    }
            //}

            //Quilter q = new Quilter();
            ////q.MatchTolerance = 100;
            //q.BlockSize = BlockSize;
            //q.Overlap = BlockSize / 6;
            //q.Sources = sources;



            //int step = q.BlockSize - q.Overlap;
            //int qw = (Width - q.Overlap - 1) / step + 1;
            //int qh = (Height - q.Overlap - 1) / step + 1;

            //q.Start(qw, qh);
            //for (;;)
            //{                
            //    bool more = q.Step();
            //    Mat quilted8U = new Mat();
            //    q.Quilt.ConvertTo(quilted8U, MatType.CV_8UC3, 255.0);
            //    WriteableBitmap quiltedBmp = quilted8U.ToWriteableBitmap();
            //    Result = quiltedBmp;
            //    quilted8U.Dispose();
            //    await Task.Delay(1);
            //    if (!more) break;
            //}

            //System.Diagnostics.Debug.WriteLine(q.CutError);
        }
    }

    class SourceImageViewModel : BindableBase
    {
        public string Dimensions { get; }
        public ImageSource Source { get; }

        public SourceImageViewModel(WriteableBitmap bmp)
        {
            Source = bmp;
            Dimensions = $"{bmp.Width}x{bmp.Height}";
        }
    }
}
