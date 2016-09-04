using OpenCvSharp.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CvRect = OpenCvSharp.CPlusPlus.Rect;
using CvPoint = OpenCvSharp.CPlusPlus.Point;
using CvSize = OpenCvSharp.CPlusPlus.Size;

namespace TextureTiler
{
    class WangTile : IEquatable<WangTile>
    {
        public int L, T, R, B;

        public WangTile(int l, int t, int r, int b)
        {
            L = l;
            T = t;
            R = r;
            B = b;
        }

        public bool Equals(WangTile other)
        {
            return L == other.L && T == other.T && R == other.R && B == other.B;
        }

        public override int GetHashCode()
        {
            return (L << 18) + (T << 12) + (R << 6) + B;
        }

        public override string ToString()
        {
            return $"{L}{T}{R}{B}";
        }             
    }

    class WangTiler
    {
        int horizColors, vertColors;
        List<WangTile> tiles;

        static uint rngSeed;
        static void SeedRng(uint seed)
        {
            rngSeed = seed;
        }

        static int NextRandom(int max)
        {
            rngSeed = (uint)((ulong)rngSeed * 1664525 + 1013904223);
            return (int)((rngSeed >> 16) % max);
        }

        void GenerateSet(uint rngSeed)
        {
            SeedRng(rngSeed);
            tiles = new List<WangTile>();

            for (int i = 0; i < horizColors; i++)
                for (int j = 0; j < vertColors; j++)
                {
                    WangTile v1 = new WangTile(i, j, NextRandom(horizColors), NextRandom(vertColors));
                    WangTile v2;
                    do
                    {
                        v2 = new WangTile(i, j, NextRandom(horizColors), NextRandom(vertColors));
                    } while (v1.Equals(v2));
                    tiles.Add(v1);
                    tiles.Add(v2);
                }            
        }

        public WangTiler(int horizColors, int vertColors, uint rngSeed)
        {
            //DEBUG
            //SeedRng(rngSeed);
            //List<int> a = new List<int>();
            //for (int i = 0; i < 100; i++)
            //{
            //    a.Add(NextRandom(2));
            //}

            this.horizColors = horizColors;
            this.vertColors = vertColors;
            GenerateSet(rngSeed);

            //DEBUG
            //SeedRng(rngSeed);

            //int w = 64;
            //int h = 64;
            //int tileSize = 8;
            //int[,] map = new int[h, w];

            //for (int i = 0; i < h; i++)            
            //    for (int j = 0; j < w; j++)
            //    {
            //        int l = -1, t = -1;
            //        if (i > 0) t = tiles[map[i - 1, j]].B;
            //        if (j > 0) l = tiles[map[i, j - 1]].R;

            //        var cand = tiles.Select((x,k) => (tile: x, idx: k))
            //            .Where(x => (l == -1 || x.tile.L == l) && (t == -1 || x.tile.T == t))
            //            .Select(x => x.idx).ToList();
            //        map[i, j] = cand[NextRandom(cand.Count)];
            //    }

            //Mat img = new Mat(h * tileSize, w * tileSize, MatType.CV_8UC3);
            //Scalar[] colors = new[]
            //{
            //    new Scalar(0,0,255),
            //    new Scalar(0,255,0),
            //    new Scalar(255,0,0),
            //    new Scalar(128,128,0),
            //    new Scalar(128,0,128),
            //    new Scalar(0,128,128)
            //};

            //for (int i = 0; i < h; i++)
            //{
            //    for (int j = 0; j < w; j++)
            //    {
            //        WangTile tile = tiles[map[i, j]];
            //        Mat tilePatch = new Mat(img, new CvRect(j * tileSize, i * tileSize, tileSize, tileSize));

            //        Cv2.FillPoly(tilePatch, new[] { new[] { new CvPoint(0, 0), new CvPoint(0.5 * tileSize, 0.5 * tileSize), new CvPoint(0, tileSize) } }, colors[tile.L]);
            //        Cv2.FillPoly(tilePatch, new[] { new[] { new CvPoint(tileSize, 0), new CvPoint(tileSize, tileSize), new CvPoint(0.5 * tileSize, 0.5 * tileSize) } }, colors[tile.R]);
            //        Cv2.FillPoly(tilePatch, new[] { new[] { new CvPoint(0, 0), new CvPoint(tileSize, 0), new CvPoint(0.5 * tileSize, 0.5 * tileSize) } }, colors[horizColors + tile.T]);
            //        Cv2.FillPoly(tilePatch, new[] { new[] { new CvPoint(0, tileSize), new CvPoint(0.5 * tileSize, 0.5 * tileSize), new CvPoint(tileSize, tileSize) } }, colors[horizColors + tile.B]);

            //        tilePatch.Dispose();
            //    }
            //}

            //Cv2.ImShow("Test", img);
        }

        public List<Mat> GenerateTiles(Quilter quilter, int size, float overlapQuotient, int nIter)
        {
            int qSize = (int)Math.Round(Math.Sqrt(2) * size);
            int overlap =  (int)Math.Round(qSize / (2 - overlapQuotient) * overlapQuotient);
            if ((qSize + overlap % 2) == 1) overlap++;
            int bs = (qSize + overlap) / 2;

            quilter.BlockSize = bs;
            quilter.Overlap = overlap; 

            float minCutErr = float.MaxValue;
            //List<Mat> bestTiles = null;

            for (int k = 0; k < nIter; k++)
            {
                float cutErr = 0;

                //List<Mat> currTiles = new List<Mat>();
                List<Mat> vert = new List<Mat>(), horiz = new List<Mat>();
                for (int i = 0; i < vertColors; i++)
                    vert.Add(quilter.GetRandomBlock());
                for (int i = 0; i < horizColors; i++)
                    horiz.Add(quilter.GetRandomBlock());

                foreach (var tile in tiles)
                {
                    quilter.Start(2, 2);
                    quilter.Step(horiz[tile.L]);
                    quilter.Step(vert[tile.T]);
                    quilter.Step(vert[tile.B]);
                    quilter.Step(horiz[tile.R]);

                    cutErr += quilter.CutError;
                    quilter.Quilt.Dispose();
                    //currTiles.Add(quilter.Quilt);
                }

                //if(cutErr < minCutErr)
                //{
                //    minCutErr = cutErr;
                //    if (bestTiles != null)
                //        foreach (var m in bestTiles)
                //            m.Dispose();
                //    bestTiles = currTiles;
                //}
                //else
                //{
                //    foreach (var m in currTiles)
                //        m.Dispose();                   
                //}

                if(k % 10 == 0)
                    System.Diagnostics.Debug.WriteLine($"{minCutErr} {cutErr}");

                foreach (var m in vert) m.Dispose();
                foreach (var m in horiz) m.Dispose();

                //GC.Collect(3, GCCollectionMode.Forced, true);
                //await Task.Delay(1);
            }

            //List<Mat> turned = new List<Mat>();

            Console.WriteLine("123123");

            //foreach (var m in bestTiles)
            //{
            //    Mat trans = Cv2.GetRotationMatrix2D(new Point2f(0.5f * qSize, 0.5f * qSize), -45, 1);
            //    trans.Set<double>(0, 2, trans.At<double>(0, 2) - 0.5 * (qSize - size));
            //    trans.Set<double>(1, 2, trans.At<double>(1, 2) - 0.5 * (qSize - size));
            //    Mat tm = new Mat();
            //    Cv2.WarpAffine(m, tm, trans, new CvSize(size, size), OpenCvSharp.Interpolation.Linear);

            //    turned.Add(tm);
            //    m.Dispose();
            //    trans.Dispose();
            //}

            //return turned;
            return null;
        }
    }
}
