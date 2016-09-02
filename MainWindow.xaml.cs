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


//TODO: using using for Mats?
namespace TextureTiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        List<Mat> sources;

        public MainWindow()
        {
            InitializeComponent();
        }
      
        void SaveMat(Mat mat, string filename)
        {
            Mat sm = new Mat();
            mat.ConvertTo(sm, MatType.CV_8UC3, 255);
            sm.SaveImage(filename);
            sm.Dispose();
        }

        private void loadSrcButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "Images|*.jpg;*.png;*.jpeg;*.bmp";
            dialog.RestoreDirectory = true;
            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                sources = new List<Mat>();
                foreach(string filename in dialog.FileNames)
                {
                    Mat img8U = Cv2.ImRead(filename);
                    Mat img = new Mat();
                    img8U.ConvertTo(img, MatType.CV_32FC3, 1.0 / 255);
                    img8U.Dispose();
                    sources.Add(img);
                }

                sourceDisplayTextBox.Content = "Sources: " + sources.Count;
            }
        }

        private async void quiltButton_Click(object sender, RoutedEventArgs e)
        {
            int quiltW = 8;
            int quiltH = 8;

            Quilter q = new Quilter();
            //q.MatchTolerance = 100;
            q.BlockSize = int.Parse(blockSizeTextBox.Text);
            q.Sources = sources;
            q.Start(quiltW, quiltH);
            for(;;)
            {
                bool more = q.Step();
                Mat quilted8U = new Mat();
                q.Result.ConvertTo(quilted8U, MatType.CV_8UC3, 255.0);
                WriteableBitmap quiltedBmp = quilted8U.ToWriteableBitmap();
                image.Source = quiltedBmp;
                quilted8U.Dispose();
                await Task.Delay(1);
                if (!more) break;
            }

            //Mat quilted = q.Quilt(quiltW, quiltH);
            
        }
    }     
}
