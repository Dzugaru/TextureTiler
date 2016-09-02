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
        int overlap, step, qw, qh, qx, qy;
        Mat quilt;

        Random rng = new Random();
        public int BlockSize { get; set; } = 32;
        public float OverlapQuotient { get; set; } = 1f / 6;
        public float MatchTolerance { get; set; } = 0.1f;
        public float SeamSmooth { get; set; } = 0.1f;
        public List<Mat> Sources { get; set; } = new List<Mat>();

        public Mat Result { get { return quilt; } }

        public Mat Quilt(int w, int h, int rngSeed = -1)
        {
            Start(w, h, rngSeed);
            while (Step()) ;
            return quilt;
        }

        public void Start(int w, int h, int rngSeed = -1)
        {
            if (rngSeed >= 0) rng = new Random(rngSeed);

            qw = w;
            qh = h;
            qx = qy = 0;
            overlap = (int)(BlockSize * OverlapQuotient);
            step = BlockSize - overlap;
            quilt = new Mat(overlap + step * h, overlap + step * w, MatType.CV_32FC3);
        }

        public bool Step()
        {
            Mat top, left;
            if (qy == 0) top = null;
            else top = new Mat(quilt, new CvRect(qx * step, qy * step, BlockSize, overlap));

            if (qx == 0) left = null;
            else left = new Mat(quilt, new CvRect(qx * step, qy * step, overlap, BlockSize));


            Mat block = GetMatchingBlock(left, top, OpenCvSharp.MatchTemplateMethod.SqDiff);
            Mat maskTopLeft = null;

            Mat bTop = new Mat(block, new CvRect(0, 0, BlockSize, overlap));
            Mat bLeft = new Mat(block, new CvRect(0, 0, overlap, BlockSize));

            //Fixing up
            Mat maskTop;
            if (top != null)
                maskTop = GetMinCutMask(top, bTop, false);
            else
                maskTop = new Mat(overlap, BlockSize, MatType.CV_32FC1, new Scalar(1));
            Mat maskTopRight = new Mat(maskTop, new CvRect(overlap, 0, step, overlap));
            maskTopLeft = new Mat(maskTop, new CvRect(0, 0, overlap, overlap));           

            Mat bTopRight = new Mat(bTop, new CvRect(overlap, 0, step, overlap));
            Mat topRight = new Mat(quilt, new CvRect(qx * step + overlap, qy * step, step, overlap));            
            BlendByMask(topRight, bTopRight, maskTopRight);
            bTopRight.Dispose();
            topRight.Dispose();
            maskTopRight.Dispose();
            maskTop.Dispose();

            //Fixing left
            Mat maskLeft;
            if (left != null)
                maskLeft = GetMinCutMask(left, bLeft, true);
            else
                maskLeft = new Mat(BlockSize, overlap, MatType.CV_32FC1, new Scalar(1));
            Mat maskLeftBottom = new Mat(maskLeft, new CvRect(0, overlap, overlap, step));           

            Mat bLeftBottom = new Mat(bLeft, new CvRect(0, overlap, overlap, step));
            Mat leftBottom = new Mat(quilt, new CvRect(qx * step, qy * step + overlap, overlap, step));            
            BlendByMask(leftBottom, bLeftBottom, maskLeftBottom);

            Mat maskLeftTop = new Mat(maskLeft, new CvRect(0, 0, overlap, overlap));
            Cv2.Multiply(maskTopLeft, maskLeftTop, maskTopLeft);
            maskLeftTop.Dispose();
            bLeftBottom.Dispose();
            leftBottom.Dispose();
            maskLeftBottom.Dispose();
            maskLeft.Dispose();

            //Fixing corner
            Mat bTopLeft = new Mat(block, new CvRect(0, 0, overlap, overlap));
            Mat topLeft = new Mat(quilt, new CvRect(qx * step, qy * step, overlap, overlap));            
            BlendByMask(topLeft, bTopLeft, maskTopLeft);

            bTopLeft.Dispose();
            topLeft.Dispose();
            maskTopLeft.Dispose();

            //Drawing center
            Mat bBottomRight = new Mat(block, new CvRect(overlap, overlap, step, step));
            Mat bottomRight = new Mat(quilt, new CvRect(qx * step + overlap, qy * step + overlap, step, step));
            bBottomRight.CopyTo(bottomRight);
            bBottomRight.Dispose();
            bottomRight.Dispose();

            if (top != null) top.Dispose();
            if (left != null) left.Dispose();

            block.Dispose();
            bTop.Dispose();
            bLeft.Dispose();            

            qx++;
            if(qx >= qw)
            {
                qx = 0;
                qy++;
                if (qy >= qh) return false;
            }
            return true;
        }

        Mat OverlapErrorSurface(Mat b1, Mat b2, bool vert, int x = 0, int y = 0)
        {
            Mat b = new Mat();

            Cv2.Subtract(b1, b2, b);
            Cv2.Multiply(b, b, b);            

            Mat bChVec = b.Reshape(1, b.Rows * b.Cols);
            Mat errVec = bChVec.Reduce(OpenCvSharp.ReduceDimension.Column, OpenCvSharp.ReduceOperation.Sum, -1);
            Mat err = errVec.Reshape(1, b.Rows);

            bChVec.Dispose();
            errVec.Dispose();
            b.Dispose();

            return err;
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

        Mat GetMinCutMask(Mat b1, Mat b2, bool vert)
        {            
            Mat error = OverlapErrorSurface(b1, b2, vert);
            if (!vert) Cv2.Transpose(error, error);
            Mat mask = new Mat(BlockSize, overlap, MatType.CV_32FC1, new Scalar(0));

            float[,] mins = new float[BlockSize + 1, overlap + 2];
            for (int i = 0; i < BlockSize; i++)
                mins[i + 1, 0] = mins[i + 1, overlap + 1] = float.MaxValue;

            unsafe
            {
                System.Diagnostics.Debug.Assert(error.IsContinuous());
                System.Diagnostics.Debug.Assert(mask.IsContinuous());

                float* pData = (float*)error.Data;
                float* pMask = (float*)mask.Data;

                //Fill mins
                for (int i = 0; i < BlockSize; i++)
                    for (int j = 0; j < overlap; j++)
                    {
                        float e = *(pData + i * overlap + j);
                        mins[i + 1, j + 1] = e + Math.Min(Math.Min(mins[i, j], mins[i, j + 1]), mins[i, j + 2]);
                    }

                //Backtrack and fill mask  
                int x = 0;
                float min = float.MaxValue;
                for (int j = 0; j < overlap; j++)
                    if (mins[BlockSize, j + 1] < min)
                    {
                        min = mins[BlockSize, j + 1];
                        x = j;
                    }

                for (int i = BlockSize - 1; i >= 0; i--)
                {                    
                    if (SeamSmooth == 0)
                    {
                        for (int j = x; j < overlap; j++)
                            *(pMask + i * overlap + j) = 1.0f;
                    }
                    else
                    {
                        for (int j = 0; j < overlap; j++)
                            *(pMask + i * overlap + j) = 1f / (1 + (float)Math.Exp(-(j - x) / (SeamSmooth * overlap)));
                    }

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

        Mat GetMatchingBlock(Mat left, Mat top, OpenCvSharp.MatchTemplateMethod matchMethod)
        {
            if (left == null && top == null)
            {
                Mat src = Sources[rng.Next(Sources.Count)];
                int x = rng.Next(src.Cols - BlockSize + 1);
                int y = rng.Next(src.Rows - BlockSize + 1);
                return new Mat(src, new CvRect(x, y, BlockSize, BlockSize));
            }          
            
            bool isMin = matchMethod == OpenCvSharp.MatchTemplateMethod.SqDiff || matchMethod == OpenCvSharp.MatchTemplateMethod.SqDiffNormed;

            List<Mat> maps = new List<Mat>();
            double best = isMin ? double.MaxValue : double.MinValue;
            foreach (Mat src in Sources)
            {
                Mat map = null;
                if (left != null)
                {
                    Mat search = new Mat(src, new CvRect(0, 0, src.Cols - BlockSize + overlap, src.Rows));
                    map = new Mat();
                    Cv2.MatchTemplate(search, left, map, matchMethod);
                    search.Dispose();

                    if (top != null)
                    {
                        search = new Mat(src, new CvRect(overlap, 0, src.Cols - overlap, src.Rows - BlockSize + overlap));
                        Mat topRight = new Mat(top, new CvRect(overlap, 0, BlockSize - overlap, overlap));
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
                    Mat search = new Mat(src, new CvRect(0, 0, src.Cols, src.Rows - BlockSize + overlap));
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
