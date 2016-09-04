using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenCvSharp.CPlusPlus;
using OpenCvSharp.Extensions;
using CvRect = OpenCvSharp.CPlusPlus.Rect;

namespace TextureTiler
{
    class Quilter
    {
        int step, qw, qh, qx, qy;
        Mat quilt;      

        Random rng = new Random();
        public int BlockSize { get; set; } = 32;
        public int Overlap { get; set; } = 5;
        public float MatchTolerance { get; set; } = 0.1f;
        public float SeamSmooth { get; set; } = 0.1f;
        public List<Mat> Sources { get; set; } = new List<Mat>();

        public Mat Quilt { get { return quilt; } }
        public float CutError { get; private set; }

        public void RngSeed(int seed)
        {
            rng = new Random(seed);
        }
        
        public void StartAndFinish(int w, int h, int rngSeed = -1)
        {
            Start(w, h, rngSeed);
            while (Step()) ;            
        }

        public void Start(int w, int h, int rngSeed = -1)
        {
            if (rngSeed >= 0) rng = new Random(rngSeed);
            CutError = 0;

            qw = w;
            qh = h;
            qx = qy = 0;           
            step = BlockSize - Overlap;
            
            //new Scalar(0) is essential cause of BlendByMask blending to top-left row
            quilt = new Mat(Overlap + step * h, Overlap + step * w, MatType.CV_32FC3, new Scalar(0)); 
        }

        public bool Step()
        {
            Mat top, left;
            if (qy == 0) top = null;
            else top = new Mat(quilt, new CvRect(qx * step, qy * step, BlockSize, Overlap));

            if (qx == 0) left = null;
            else left = new Mat(quilt, new CvRect(qx * step, qy * step, Overlap, BlockSize));

            Mat block = GetMatchingBlock(left, top, OpenCvSharp.MatchTemplateMethod.SqDiff);
            bool notEnded = Step(block);

            block.Dispose();
            if (top != null) top.Dispose();
            if (left != null) left.Dispose();

            return notEnded;
        }

        public bool Step(Mat block)
        {
            Mat top, left;
            if (qy == 0) top = null;
            else top = new Mat(quilt, new CvRect(qx * step, qy * step, BlockSize, Overlap));

            if (qx == 0) left = null;
            else left = new Mat(quilt, new CvRect(qx * step, qy * step, Overlap, BlockSize));            

            QuiltBlock(top, left, block);

            if (top != null) top.Dispose();
            if (left != null) left.Dispose();            

            qx++;
            if (qx >= qw)
            {
                qx = 0;
                qy++;
                if (qy >= qh) return false;
            }
            return true;
        }        

        public Mat GetRandomBlock()
        {
            Mat src = Sources[rng.Next(Sources.Count)];
            int x = rng.Next(src.Cols - BlockSize + 1);
            int y = rng.Next(src.Rows - BlockSize + 1);
            return new Mat(src, new CvRect(x, y, BlockSize, BlockSize));
        }

        void QuiltBlock(Mat top, Mat left, Mat block)
        {          
            Mat maskTopLeft = null;

            Mat bTop = new Mat(block, new CvRect(0, 0, BlockSize, Overlap));
            Mat bLeft = new Mat(block, new CvRect(0, 0, Overlap, BlockSize));

            //Fixing up
            Mat maskTop;
            float cutErr;
            if (top != null)
            {
                (maskTop, cutErr) = GetMinCutMask(top, bTop, false);
                CutError += cutErr;
            }
            else
                maskTop = new Mat(Overlap, BlockSize, MatType.CV_32FC1, new Scalar(1));
            Mat maskTopRight = new Mat(maskTop, new CvRect(Overlap, 0, step, Overlap));
            maskTopLeft = new Mat(maskTop, new CvRect(0, 0, Overlap, Overlap));

            Mat bTopRight = new Mat(bTop, new CvRect(Overlap, 0, step, Overlap));
            Mat topRight = new Mat(quilt, new CvRect(qx * step + Overlap, qy * step, step, Overlap));
            BlendByMask(topRight, bTopRight, maskTopRight);
            bTopRight.Dispose();
            topRight.Dispose();
            maskTopRight.Dispose();
            maskTop.Dispose();

            //Fixing left
            Mat maskLeft;
            if (left != null)
            {
                (maskLeft, cutErr) = GetMinCutMask(left, bLeft, true);
                CutError += cutErr;
            }
            else
                maskLeft = new Mat(BlockSize, Overlap, MatType.CV_32FC1, new Scalar(1));
            Mat maskLeftBottom = new Mat(maskLeft, new CvRect(0, Overlap, Overlap, step));

            Mat bLeftBottom = new Mat(bLeft, new CvRect(0, Overlap, Overlap, step));
            Mat leftBottom = new Mat(quilt, new CvRect(qx * step, qy * step + Overlap, Overlap, step));
            BlendByMask(leftBottom, bLeftBottom, maskLeftBottom);

            Mat maskLeftTop = new Mat(maskLeft, new CvRect(0, 0, Overlap, Overlap));
            Cv2.Multiply(maskTopLeft, maskLeftTop, maskTopLeft);
            maskLeftTop.Dispose();
            bLeftBottom.Dispose();
            leftBottom.Dispose();
            maskLeftBottom.Dispose();
            maskLeft.Dispose();

            //Fixing corner
            Mat bTopLeft = new Mat(block, new CvRect(0, 0, Overlap, Overlap));
            Mat topLeft = new Mat(quilt, new CvRect(qx * step, qy * step, Overlap, Overlap));
            BlendByMask(topLeft, bTopLeft, maskTopLeft);

            bTopLeft.Dispose();
            topLeft.Dispose();
            maskTopLeft.Dispose();

            //Drawing center
            Mat bBottomRight = new Mat(block, new CvRect(Overlap, Overlap, step, step));
            Mat bottomRight = new Mat(quilt, new CvRect(qx * step + Overlap, qy * step + Overlap, step, step));
            bBottomRight.CopyTo(bottomRight);
            bBottomRight.Dispose();
            bottomRight.Dispose();

            bTop.Dispose();
            bLeft.Dispose();

            //GC.Collect();          
        }

        Mat OverlapErrorSurface(Mat b1, Mat b2, bool vert, int x = 0, int y = 0)
        {
            Mat b = new Mat();

            Cv2.Subtract(b1, b2, b);
            Cv2.Multiply(b, b, b);            

            Mat bChVec = b.Reshape(1, b.Rows * b.Cols);
            Mat errVec = bChVec.Reduce(OpenCvSharp.ReduceDimension.Column, OpenCvSharp.ReduceOperation.Sum, -1);
            Mat err = errVec.Reshape(1, b.Rows).Clone();

            bChVec.Dispose();
            errVec.Dispose();
            b.Dispose();

            //GC.Collect();

            return err;
            //return b1.Clone();
        }

        void BlendByMask(Mat dst, Mat src, Mat mask)
        {
            Mat maskedSrc = new Mat();
            Mat multichannelMask = new Mat();
            Cv2.Merge(new Mat[] { mask, mask, mask }, multichannelMask);

            Cv2.Multiply(src, multichannelMask, maskedSrc);
            Cv2.Multiply(dst, new Scalar(1, 1, 1) - multichannelMask, dst);
            Cv2.Add(dst, maskedSrc, dst);
            maskedSrc.Dispose();
            multichannelMask.Dispose();
        }

        (Mat mask, float cutErr) GetMinCutMask(Mat b1, Mat b2, bool vert)
        {
            float cutErr = 0;

            Mat error = OverlapErrorSurface(b1, b2, vert);
            if (!vert) Cv2.Transpose(error, error);

            float[,] mins = new float[BlockSize + 1, Overlap + 2];
            for (int i = 0; i < BlockSize; i++)
                mins[i + 1, 0] = mins[i + 1, Overlap + 1] = float.MaxValue;

            float[,] mData = new float[BlockSize, Overlap];
            float[,] mMask = new float[BlockSize, Overlap];
            error.GetArray(0, 0, mData);

            //Fill mins
            for (int i = 0; i < BlockSize; i++)
                for (int j = 0; j < Overlap; j++)
                {
                    float e = mData[i, j];
                    mins[i + 1, j + 1] = e + Math.Min(Math.Min(mins[i, j], mins[i, j + 1]), mins[i, j + 2]);
                }

            //Backtrack and fill mask  
            int x = 0;
            float min = float.MaxValue;
            for (int j = 0; j < Overlap; j++)
                if (mins[BlockSize, j + 1] < min)
                {
                    min = mins[BlockSize, j + 1];
                    x = j;
                }

            cutErr = min;  

            for (int i = BlockSize - 1; i >= 0; i--)
            {
                if (SeamSmooth == 0)
                {
                    for (int j = x; j < Overlap; j++)
                        mMask[i, j] = 1.0f;
                }
                else
                {
                    for (int j = 0; j < Overlap; j++)
                        mMask[i, j] = 1f / (1 + (float)Math.Exp(-(j - x) / (SeamSmooth * Overlap)));
                }

                float l = mins[i, x];
                float c = mins[i, x + 1];
                float r = mins[i, x + 2];

                if (l < c && l < r) x--;
                else if (r < c && r < l) x++;
            }

            error.Dispose();

            Mat mask = new Mat(BlockSize, Overlap, MatType.CV_32FC1, mMask);
            if (!vert) Cv2.Transpose(mask, mask);

            //GC.Collect();
            return (mask, cutErr);
        }

        Mat GetMatchingBlock(Mat left, Mat top, OpenCvSharp.MatchTemplateMethod matchMethod)
        {
            if (left == null && top == null)
            {
                return GetRandomBlock();
            }          
            
            bool isMin = matchMethod == OpenCvSharp.MatchTemplateMethod.SqDiff || matchMethod == OpenCvSharp.MatchTemplateMethod.SqDiffNormed;

            List<Mat> maps = new List<Mat>();
            double best = isMin ? double.MaxValue : double.MinValue;
            foreach (Mat src in Sources)
            {
                Mat map = null;
                if (left != null)
                {
                    Mat search = new Mat(src, new CvRect(0, 0, src.Cols - BlockSize + Overlap, src.Rows));
                    map = new Mat();
                    Cv2.MatchTemplate(search, left, map, matchMethod);
                    search.Dispose();

                    if (top != null)
                    {
                        search = new Mat(src, new CvRect(Overlap, 0, src.Cols - Overlap, src.Rows - BlockSize + Overlap));
                        Mat topRight = new Mat(top, new CvRect(Overlap, 0, BlockSize - Overlap, Overlap));
                        Mat map2 = new Mat();
                        Cv2.MatchTemplate(search, topRight, map2, matchMethod);
                        search.Dispose();
                        topRight.Dispose();

                        //TODO: adding justified?
                        Cv2.Add(map, map2, map);
                        map2.Dispose();
                    }
                }
                else
                {
                    Mat search = new Mat(src, new CvRect(0, 0, src.Cols, src.Rows - BlockSize + Overlap));
                    map = new Mat();
                    Cv2.MatchTemplate(search, top, map, matchMethod);
                    search.Dispose();
                }

                double min, max;
                Cv2.MinMaxLoc(map, out min, out max);

                if (isMin && min < best) best = min;
                if (!isMin && max > best) best = max;

                maps.Add(map);
            }


            List<(int srcI, int x, int y)> candidates = new List<(int srcI, int x, int y)>();

            //TODO: non-maximum supression? (many candidates can be near maximum which harms diversity)
            unsafe
            {
                for (int k = 0; k < maps.Count; k++)
                {
                    Mat map = maps[k];
                    System.Diagnostics.Debug.Assert(map.IsContinuous());
                    float* pData = (float*)map.Data;
                    for (int i = 0; i < map.Rows; i++)
                        for (int j = 0; j < map.Cols; j++)
                        {
                            float v = *(pData + i * map.Cols + j);
                            if (isMin && v <= (1 + MatchTolerance) * best ||
                                !isMin && v >= (1 - MatchTolerance) * best)
                            {
                                candidates.Add((k, j, i));
                            }
                        }
                }
            }

            foreach (Mat map in maps)
                map.Dispose();

            (int ck, int cx, int cy) = candidates[rng.Next(candidates.Count)];

            return new Mat(Sources[ck], new CvRect(cx, cy, BlockSize, BlockSize));
        }
    }
}
