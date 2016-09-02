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
        public MainWindow()
        {
            DataContext = new ViewModel();
            InitializeComponent();            
        }
      
        void SaveMat(Mat mat, string filename)
        {
            Mat sm = new Mat();
            mat.ConvertTo(sm, MatType.CV_8UC3, 255);
            sm.SaveImage(filename);
            sm.Dispose();
        }       
    }     
}
