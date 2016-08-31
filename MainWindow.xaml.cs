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
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            double blockQuotient = 0.33;
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
        
        double ErrorSum(Mat err)
        {
            return Cv2.Sum(err).Val0;
        }
        
        Mat OverlapError(Mat b1, Mat b2, int overlap, bool vert)
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
            
            Mat o1ChVec = o1.Reshape(1, o1.Rows * o1.Cols);
            Mat errVec = o1ChVec.Reduce(OpenCvSharp.ReduceDimension.Column, OpenCvSharp.ReduceOperation.Sum, -1);
            Mat err = errVec.Reshape(1, o1.Rows);

            o1ChVec.Dispose();
            errVec.Dispose();
            o1.Dispose();
            o2.Dispose();          
               
            return err;            
        }

        Mat GetMinCutMask(Mat b1, Mat b2, int overlap, bool vert)
        {
            int blockSz = b1.Cols;

            //DEBUG
            //Mat error = b1;
            Mat error = OverlapError(b1, b2, overlap, vert);
            if (!vert) Cv2.Transpose(error, error);
            Mat mask = new Mat(blockSz, overlap, MatType.CV_8UC1, new Scalar(0));

            float[,] mins = new float[blockSz + 1, overlap + 2];
            for (int i = 0; i < blockSz; i++)
                mins[i + 1, 0] = mins[i + 1, overlap + 1] = float.MaxValue;            

            unsafe
            {
                float* pData = (float*)error.Data;
                byte* pMask = (byte*)mask.Data;

                //Fill mins
                for (int i = 0; i < blockSz; i++)
                    for (int j = 0; j < overlap; j++)
                    {
                        float e = *(pData + i * overlap + j);
                        mins[i + 1, j + 1] = e + Math.Min(Math.Min(mins[i, j], mins[i, j + 1]), mins[i, j + 2]);
                    }

                //Backtrack and fill mask  
                int x = 0;
                float min = float.MaxValue;
                for (int j = 0; j < overlap; j++)
                    if(mins[blockSz, j + 1] < min)
                    {
                        min = mins[blockSz, j + 1];
                        x = j;
                    }                

                for (int i = blockSz - 1; i >= 0; i--)
                {
                    //TODO: cut by x or x + 1?
                    //for (int j = x; j < overlap; j++)
                    //    *(pMask + i * overlap + j) = 1;

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

        async Task<Mat> Quilt(Mat src, int h, int w, int blockSize, int overlap)
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
                    for (int cy = 0; cy < step; cy++)
                        for (int cx = 0; cx < step; cx++)
                        {
                            Mat cb = GetBlock(src, cx, cy, blockSize);
                            double err = 0;
                            if (up != null) err += ErrorSum(OverlapError(up, cb, overlap, false));
                            if (left != null) err += ErrorSum(OverlapError(left, cb, overlap, true));
                            cb.Dispose();

                            if (err < min.err)
                            {
                                min.err = err;
                                min.x = cx;
                                min.y = cy;
                            }
                        }

                    //TODO: choose random block among a number of best within tolerance 

                    Mat minBlock = GetBlock(src, min.x, min.y, blockSize);
                    Mat maskUpLeft = null;
                    if (up != null)
                    {
                        Mat maskUp = GetMinCutMask(up, minBlock, overlap, false);
                        Mat maskUpRight = new Mat(maskUp, new CvRect(overlap, 0, step, overlap));
                        maskUpLeft = new Mat(maskUp, new CvRect(0, 0, overlap, overlap));

                        //DEBUG    
                        //if (i == 1 && j == 1)
                        //{
                        //    image.Source = ((Mat)(maskUp * 255)).ToWriteableBitmap();
                        //    await Task.Delay(2000);
                        //}

                        Mat blockUp = new Mat(src, new CvRect(min.x + overlap, min.y, step, overlap));
                        Mat resultUpDst = new Mat(result, new CvRect(j * step + overlap, i * step, step, overlap));
                        blockUp.CopyTo(resultUpDst, maskUpRight);
                        blockUp.Dispose();
                        resultUpDst.Dispose();
                        maskUpRight.Dispose();
                        maskUp.Dispose();
                    }

                    if (left != null)
                    {
                        Mat maskLeft = GetMinCutMask(left, minBlock, overlap, true);
                        Mat maskLeftDown = new Mat(maskLeft, new CvRect(0, overlap, overlap, step));

                        //DEBUG      
                        //if (i == 1 && j == 1)
                        //{
                        //    image.Source = ((Mat)(maskLeft * 255)).ToWriteableBitmap();
                        //    await Task.Delay(2000);
                        //}

                        Mat blockLeft = new Mat(src, new CvRect(min.x, min.y + overlap, overlap, step));
                        Mat resultLeftDst = new Mat(result, new CvRect(j * step, i * step + overlap, overlap, step));
                        blockLeft.CopyTo(resultLeftDst, maskLeftDown);

                        if (maskUpLeft != null)
                        {
                            Mat maskLeftUp = new Mat(maskLeft, new CvRect(0, 0, overlap, overlap));
                            Cv2.BitwiseAnd(maskUpLeft, maskLeftUp, maskUpLeft);

                            //DEBUG 
                            //if (i == 1 && j == 1)
                            //{
                            //    image.Source = ((Mat)(maskUpLeft * 255)).ToWriteableBitmap();
                            //    await Task.Delay(5000);
                            //}

                            Mat blockCorner = new Mat(src, new CvRect(min.x, min.y, overlap, overlap));
                            Mat resultCornerDst = new Mat(result, new CvRect(j * step, i * step, overlap, overlap));
                            blockCorner.CopyTo(resultLeftDst, maskUpLeft);

                            blockCorner.Dispose();
                            resultCornerDst.Dispose();
                            maskLeftUp.Dispose();
                        }

                        blockLeft.Dispose();
                        resultLeftDst.Dispose();
                        maskLeftDown.Dispose();
                        maskLeft.Dispose();
                    }

                    if (maskUpLeft != null) maskUpLeft.Dispose();


                    Mat blockCenter = new Mat(src, new CvRect(min.x + overlap, min.y + overlap, step, step)); //GetBlock(src, min.x, min.y, blockSize);
                    Mat resultCenterDst = new Mat(result, new CvRect(j * step + overlap, i * step + overlap, step, step));
                    blockCenter.CopyTo(resultCenterDst);
                    blockCenter.Dispose();
                    resultCenterDst.Dispose();


                    if (up != null) up.Dispose();
                    if (left != null) left.Dispose();

                    GC.Collect();

                    //DEBUG
                    using (Mat quilted8U = new Mat())
                    {
                        result.ConvertTo(quilted8U, MatType.CV_8UC3, 255.0);
                        //NOTE: new Mat doesn't work, only Clone does!
                        using (Mat quilted8UCrop = quilted8U.Clone(new CvRect(overlap, overlap, result.Cols - overlap, result.Rows - overlap)))
                        {
                            WriteableBitmap quiltedBmp = quilted8UCrop.ToWriteableBitmap();
                            image.Source = quiltedBmp;
                            await Task.Delay(100);
                        }
                    }
                }
            }

            return result;
        }
    }     
}
