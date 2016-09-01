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
using System.IO;


//TODO: using using for Mats
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
            rng = new Random(31337);

            //DEBUG
            //float[] data =
            //{
            //    2,1,2,3,
            //    2,2,0,2,
            //    2,0,2,2,
            //    3,2,1,2
            //};

            //Mat testCut = new Mat(4, 4, MatType.CV_32FC1, data);
            //GetVertMinCutMask(testCut, null, 4);

            //Mat m1 = new Mat(4, 4, MatType.CV_32FC3);
            //m1.SetTo(new Scalar(1, 0, 0));

            //Mat m2 = new Mat(4, 4, MatType.CV_32FC3);
            //m2.SetTo(new Scalar(0.5, 1, 0));

            //double err = ErrorSum(OverlapError(m1, m2, 2, true));

            //Mat up8U = Cv2.ImRead("up.png");
            //Mat src8U = Cv2.ImRead(@"F:\WORK\MilitarySim\Refs\5.png");
            //Mat up = new Mat(), src = new Mat();
            //double blockQuotient = 0.5;
            //double overlapQuotient = 1.0 / 6;


            //up8U.ConvertTo(up, MatType.CV_32FC3, 1.0 / 255);
            //src8U.ConvertTo(src, MatType.CV_32FC3, 1.0 / 255);
            

            ////WriteableBitmap bmp = img8U.ToWriteableBitmap();
            ////image.Source = bmp;

            //int w = src.Cols;
            //int h = src.Rows;

            //int blockSize = (int)(Math.Min(w, h) * blockQuotient);
            //int overlap = (int)(overlapQuotient * blockSize);

            //Mat myMap = new Mat(src.Rows - blockSize + 1, src.Cols - blockSize + 1, MatType.CV_32FC1);
            //var min = (err: double.MaxValue, x: 0, y: 0);
            //for (int cy = 0; cy < src.Rows - blockSize + 1; cy++)
            //{
            //    for (int cx = 0; cx < src.Cols - blockSize + 1; cx++)
            //    {
            //        Mat cb = GetBlock(src, cx, cy, blockSize);
            //        double err = ErrorSum(OverlapError(up, cb, overlap, false, cx, cy));
            //        //File.AppendAllText("log.txt", $"{cx} {cy} {err}\r\n");
            //        cb.Dispose();
            //        myMap.Set(cy, cx, (float)err);

            //        if (err < min.err)
            //        {
            //            min.err = err;
            //            min.x = cx;
            //            min.y = cy;
            //        }
            //    }
            //}
            //myMap = myMap.Normalize(0, 1, OpenCvSharp.NormType.MinMax);
            //SaveMat(myMap, "myMap.png");
            //double tmin, tmax;
            //CvPoint minLoc, maxLoc;
            //Cv2.MinMaxLoc(myMap, out tmin, out tmax, out minLoc, out maxLoc);
            //System.Diagnostics.Debug.WriteLine($"{maxLoc}");

            //Mat templ = up.Clone(new CvRect(0, blockSize - overlap, blockSize, overlap));
            //Mat search = new Mat(src, new CvRect(0, 0, src.Cols, src.Rows - blockSize + overlap));
            //Mat map = new Mat();
            //Cv2.MatchTemplate(search, templ, map, OpenCvSharp.MatchTemplateMethod.SqDiff);

            //map = map.Normalize(0, 1, OpenCvSharp.NormType.MinMax);
            //map = 1 - map;
            //SaveMat(templ, "templ.png");
            //SaveMat(map, "map.png");

            //double min, max;
            //CvPoint minLoc, maxLoc;
            //Cv2.MinMaxLoc(map, out min, out max, out minLoc, out maxLoc);

            //System.Diagnostics.Debug.WriteLine($"{maxLoc}");
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            double blockQuotient = 0.5;
            double overlapQuotient = 1.0 / 6;            
            int quiltW = 8;
            int quiltH = 8;

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

                Mat quilted = await Quilt(img, quiltH, quiltW, blockSize, overlap);
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
        
        //(Mat, int, int) GetRandomBlock(Mat img, int sz)
        //{
        //    int x = rng.Next(img.Cols - sz + 1);
        //    int y = rng.Next(img.Rows - sz + 1);
        //    return (new Mat(img, new CvRect(x, y, sz, sz)), x, y);
        //}

        Mat GetBlock(Mat img, int x, int y, int sz)
        {
            return new Mat(img, new CvRect(x, y, sz, sz));
        }

        (int, int) GetMatchingBlock(Mat src, Mat left, Mat top, int blockSize, int overlap, OpenCvSharp.MatchTemplateMethod matchMethod)
        {
            Mat map = null;
            if (left != null)
            {
                Mat search = new Mat(src, new CvRect(0, 0, src.Cols - blockSize + overlap, src.Rows));
                map = new Mat();
                Cv2.MatchTemplate(search, left, map, matchMethod);
                search.Dispose();

                if(top != null)
                {
                    search = new Mat(src, new CvRect(overlap, 0, src.Cols - overlap, src.Rows - blockSize + overlap));
                    Mat topRight = new Mat(top, new CvRect(overlap, 0, blockSize - overlap, overlap));
                    Mat map2 = new Mat();
                    Cv2.MatchTemplate(search, topRight, map2, matchMethod);
                    search.Dispose();
                    topRight.Dispose();

                    //TODO: adding justified?
                    Cv2.Add(map, map2, map);
                    map2.Dispose();
                }
            }
            else if(top != null)
            {
                Mat search = new Mat(src, new CvRect(0, 0, src.Cols, src.Rows - blockSize + overlap));
                map = new Mat();
                Cv2.MatchTemplate(search, top, map, matchMethod);
                search.Dispose();
            }
            else
            {
                //TODO: return random block
                return (0, 0);
            }

            //TODO: return random block within error tolerance

            double min, max;
            CvPoint minLoc, maxLoc;
            Cv2.MinMaxLoc(map, out min, out max, out minLoc, out maxLoc);
            map.Dispose();

            bool isMin = matchMethod == OpenCvSharp.MatchTemplateMethod.SqDiff || matchMethod == OpenCvSharp.MatchTemplateMethod.SqDiffNormed;           

            return (isMin ? minLoc.X : maxLoc.X, isMin ? minLoc.Y : maxLoc.Y);
        }
        
        double ErrorSum(Mat err)
        {
            return Cv2.Sum(err).Val0;
        }
        
        Mat OverlapError(Mat b1, Mat b2, int overlap, bool vert, int x = 0, int y = 0)
        {
            //SaveMat(o1, $"test/u{x}-{y}.png");
            //if(x % 10 == 0) SaveMat(o2, $"test/c{y}-{x}.png");

            Mat b = new Mat();

            Cv2.Subtract(b1, b2, b);
            Cv2.Multiply(b, b, b);
            //Cv2.Absdiff(o1, o2, o1);
            
            Mat bChVec = b.Reshape(1, b.Rows * b.Cols);
            Mat errVec = bChVec.Reduce(OpenCvSharp.ReduceDimension.Column, OpenCvSharp.ReduceOperation.Sum, -1);
            Mat err = errVec.Reshape(1, b.Rows);

            bChVec.Dispose();
            errVec.Dispose();
            b.Dispose();            
               
            return err;            
        }

        Mat GetMinCutMask(Mat b1, Mat b2, int blockSize, int overlap, bool vert)
        {           
            //DEBUG
            //Mat error = b1;
            Mat error = OverlapError(b1, b2, overlap, vert);
            if (!vert) Cv2.Transpose(error, error);
            Mat mask = new Mat(blockSize, overlap, MatType.CV_8UC1, new Scalar(0));

            float[,] mins = new float[blockSize + 1, overlap + 2];
            for (int i = 0; i < blockSize; i++)
                mins[i + 1, 0] = mins[i + 1, overlap + 1] = float.MaxValue;            

            unsafe
            {
                float* pData = (float*)error.Data;
                byte* pMask = (byte*)mask.Data;

                //Fill mins
                for (int i = 0; i < blockSize; i++)
                    for (int j = 0; j < overlap; j++)
                    {
                        float e = *(pData + i * overlap + j);
                        mins[i + 1, j + 1] = e + Math.Min(Math.Min(mins[i, j], mins[i, j + 1]), mins[i, j + 2]);
                    }

                //Backtrack and fill mask  
                int x = 0;
                float min = float.MaxValue;
                for (int j = 0; j < overlap; j++)
                    if(mins[blockSize, j + 1] < min)
                    {
                        min = mins[blockSize, j + 1];
                        x = j;
                    }                

                for (int i = blockSize - 1; i >= 0; i--)
                {
                    //TODO: cut by x or x + 1?
                    //DEBUG
                    for (int j = x; j < overlap; j++)
                        *(pMask + i * overlap + j) = 1;

                    float l = mins[i, x];
                    float c = mins[i, x + 1];
                    float r = mins[i, x + 2];

                    if (l < c && l < r) x--;
                    else if (r < c && r < l) x++;
                }
            }

            error.Dispose();

            if (!vert) Cv2.Transpose(mask, mask);
            return mask;        
        }        

        void SaveMat(Mat mat, string filename)
        {
            Mat sm = new Mat();
            mat.ConvertTo(sm, MatType.CV_8UC3, 255);
            sm.SaveImage(filename);
            sm.Dispose();
        }

        async Task<Mat> Quilt(Mat src, int h, int w, int blockSize, int overlap)
        {
            int step = blockSize - overlap;
            Mat result = new Mat(overlap + step * h, overlap + step * w, MatType.CV_32FC3);
            //DEBUG
            result.SetTo(new Scalar(0, 0, 0));
            Mat top, left;

            for (int i = 0; i < h; i++) //DEBUG
            {
                for (int j = 0; j < w; j++) //DEBUG
                {                    
                    if (i == 0) top = null;
                    else top = new Mat(result, new CvRect(j * step, i * step, blockSize, overlap));

                    if (j == 0) left = null;
                    else left = new Mat(result, new CvRect(j * step, i * step, overlap, blockSize));


                    (int bx, int by) = GetMatchingBlock(src, left, top, blockSize, overlap, OpenCvSharp.MatchTemplateMethod.SqDiffNormed);

                    //DEBUG
                    //System.Diagnostics.Debug.WriteLine($"{i} {j} {min.x} {min.y} {min.err}");
                    //TODO: choose random block among a number of best within tolerance 

                    Mat block = GetBlock(src, bx, by, blockSize);
                    Mat maskTopLeft = null;

                    Mat bTop = new Mat(block, new CvRect(0, 0, blockSize, overlap));
                    Mat bLeft = new Mat(block, new CvRect(0, 0, overlap, blockSize));

                    //Fixing up
                    Mat maskTop;
                    if (top != null)
                        maskTop = GetMinCutMask(top, bTop, blockSize, overlap, false);
                    else
                        maskTop = new Mat(overlap, blockSize, MatType.CV_8UC1, new Scalar(1));                
                    Mat maskTopRight = new Mat(maskTop, new CvRect(overlap, 0, step, overlap));
                    maskTopLeft = new Mat(maskTop, new CvRect(0, 0, overlap, overlap));

                    //DEBUG    
                    //if (i == 1 && j == 1)
                    //{
                    //    image.Source = ((Mat)(maskUp * 255)).ToWriteableBitmap();
                    //    await Task.Delay(2000);
                    //}

                    Mat bTopRight = new Mat(bTop, new CvRect(overlap, 0, step, overlap));
                    Mat topRight = new Mat(result, new CvRect(j * step + overlap, i * step, step, overlap));
                    bTopRight.CopyTo(topRight, maskTopRight);
                    bTopRight.Dispose();
                    topRight.Dispose();
                    maskTopRight.Dispose();
                    maskTop.Dispose();

                    //Fixing left
                    Mat maskLeft;
                    if (left != null)
                        maskLeft = GetMinCutMask(left, bLeft, blockSize, overlap, true);
                    else
                        maskLeft = new Mat(blockSize, overlap, MatType.CV_8UC1, new Scalar(1));
                    Mat maskLeftBottom = new Mat(maskLeft, new CvRect(0, overlap, overlap, step));

                    //DEBUG      
                    //if (i == 1 && j == 1)
                    //{
                    //    image.Source = ((Mat)(maskLeft * 255)).ToWriteableBitmap();
                    //    await Task.Delay(2000);
                    //}

                    Mat bLeftBottom = new Mat(bLeft, new CvRect(0, overlap, overlap, step));
                    Mat leftBottom = new Mat(result, new CvRect(j * step, i * step + overlap, overlap, step));
                    bLeftBottom.CopyTo(leftBottom, maskLeftBottom);
                    
                    Mat maskLeftTop = new Mat(maskLeft, new CvRect(0, 0, overlap, overlap));
                    Cv2.BitwiseAnd(maskTopLeft, maskLeftTop, maskTopLeft);
                    maskLeftTop.Dispose();
                    bLeftBottom.Dispose();
                    leftBottom.Dispose();
                    maskLeftBottom.Dispose();
                    maskLeft.Dispose();

                    //Fixing corner
                    Mat bTopLeft = new Mat(block, new CvRect(0, 0, overlap, overlap));
                    Mat topLeft = new Mat(result, new CvRect(j * step, i * step, overlap, overlap));
                    bTopLeft.CopyTo(topLeft, maskTopLeft);

                    bTopLeft.Dispose();
                    topLeft.Dispose();                   
                    maskTopLeft.Dispose();

                    //DEBUG 
                    //if (i == 1 && j == 1)
                    //{
                    //    image.Source = ((Mat)(maskUpLeft * 255)).ToWriteableBitmap();
                    //    await Task.Delay(5000);
                    //}


                    //Drawing center
                    Mat bBottomRight = new Mat(block, new CvRect(overlap, overlap, step, step)); 
                    Mat bottomRight = new Mat(result, new CvRect(j * step + overlap, i * step + overlap, step, step));
                    bBottomRight.CopyTo(bottomRight);
                    bBottomRight.Dispose();
                    bottomRight.Dispose();

                    if (top != null) top.Dispose();
                    if (left != null) left.Dispose();

                    block.Dispose();
                    bTop.Dispose();
                    bLeft.Dispose();

                    GC.Collect();

                    //DEBUG
                    using (Mat quilted8U = new Mat())
                    {
                        result.ConvertTo(quilted8U, MatType.CV_8UC3, 255.0);                       
                        WriteableBitmap quiltedBmp = quilted8U.ToWriteableBitmap();
                        image.Source = quiltedBmp;
                        await Task.Delay(100);                       
                    }
                }
            }

            return result;
        }
    }     
}
