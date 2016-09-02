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
            //DEBUG
            var wangTiler = new WangTiler(3, 3, 31337);
            return;


            foreach(var src in sources)
            {
                if(BlockSize > src.Cols || BlockSize > src.Rows)
                {
                    MessageBox.Show("Block Size exceeds one of the source's size, please reduce Block Size or reload the sources.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            Quilter q = new Quilter();
            //q.MatchTolerance = 100;
            q.BlockSize = BlockSize;
            q.Overlap = BlockSize / 6;
            q.Sources = sources;

           

            int step = q.BlockSize - q.Overlap;
            int qw = (Width - q.Overlap - 1) / step + 1;
            int qh = (Height - q.Overlap - 1) / step + 1;

            q.Start(qw, qh);
            for (;;)
            {
                bool more = q.Step();
                Mat quilted8U = new Mat();
                q.Quilt.ConvertTo(quilted8U, MatType.CV_8UC3, 255.0);
                WriteableBitmap quiltedBmp = quilted8U.ToWriteableBitmap();
                Result = quiltedBmp;
                quilted8U.Dispose();
                await Task.Delay(1);
                if (!more) break;
            }
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
