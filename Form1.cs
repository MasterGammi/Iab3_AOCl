using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV.CvEnum;
using System.Linq.Expressions;
using Emgu.CV.Util;
using System.Threading;
using Emgu.CV.OCR; //модуль оптического распознавания символов

namespace lab3
{
    public partial class Form1 : Form
    {
        private Image<Bgr, byte> sourceImage;
        List<Rectangle> rois = new List<Rectangle>();
        Tesseract ocr;
        private VideoCapture capture;
        List<Rectangle> faces = new List<Rectangle>();
        Image<Bgr, byte> imagewithface;
        Image<Bgra, byte> small;


        public Form1()
        {
            InitializeComponent();
        }

        private void button_load_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            var result = openFileDialog.ShowDialog(); // открытие диалога выбора файла
            if (result == DialogResult.OK) // открытие выбранного файла
            {
                string fileName = openFileDialog.FileName;
                sourceImage = new Image<Bgr, byte>(fileName);
            }
            imageBox1.Image = sourceImage;
            ocr = new Tesseract("D:\\tessdata-main", "eng", OcrEngineMode.TesseractLstmCombined);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var gray = sourceImage.Convert<Gray,byte>();
            gray._ThresholdBinaryInv(new Gray(100), new Gray(255));

            var delatedImage = gray.Dilate(5);

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(delatedImage, contours, null,
            RetrType.List, ChainApproxMethod.ChainApproxSimple);
            var copy = sourceImage.Copy();
            for (int i = 0; i < contours.Size; i++)
            {
                if (CvInvoke.ContourArea(contours[i], false) > 100) //игнорирование маленьких контуров
                {
                    Rectangle rect = CvInvoke.BoundingRectangle(contours[i]);
                    rois.Add(rect);
                    copy.Draw(rect, new Bgr(Color.Blue), 1);

                    sourceImage.ROI = rect;

                    var roiCopy = sourceImage.Copy();

                    sourceImage.ROI = Rectangle.Empty;
                    imageBox2.Image = copy;
                   

                    ocr.SetImage(roiCopy);
                    ocr.Recognize();
                    string text = ocr.GetUTF8Text();
                    listBox1.Items.Add(text);

                   
                }

            }
           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // инициализация веб-камеры
            capture = new VideoCapture();
            capture.ImageGrabbed += ProcessFrame;
            capture.Start(); // начало обработки видеопотока
        }
        private void ProcessFrame(object sender, EventArgs e)
        {
            var frame = new Mat();
            capture.Retrieve(frame); // получение текущего кадра
            imageBox2.Image = frame;
            imagewithface = frame.ToImage<Bgr, byte>();
        }

        public void button3_Click(object sender, EventArgs e)
        {
           // Image<Bgr, byte> image = frame.ToImage<Bgr, byte>();
            capture.Stop();
            faces.Clear();
            

            using (CascadeClassifier face = new
            CascadeClassifier("D:\\haarcascade_frontalface_default.xml"))
            {
                using (Mat ugray = new Mat())
                {
                    CvInvoke.CvtColor(imagewithface, ugray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                    Rectangle[] facesDetected = face.DetectMultiScale(ugray, 1.1, 10, new Size(20, 20));
                    faces.AddRange(facesDetected);
                }
            }
            foreach (Rectangle rect in faces)
            imagewithface.Draw(rect, new Bgr(Color.Yellow), 2);
           
            imageBox2.Image = imagewithface;
           



        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
             openFileDialog.ShowDialog();
            Mat frame = CvInvoke.Imread(openFileDialog.FileName, ImreadModes.Unchanged);
            Image<Bgra, byte> res = imagewithface.Convert<Bgra,byte>();

            foreach (Rectangle rect in faces) //для каждого лица
            {
                res.ROI = rect; //для области содержащей лицо
                small = frame.ToImage<Bgra, byte>().Resize(rect.Width, rect.Height,Inter.Nearest); //создание
                               //копирование изображения small на изображение res с использованием маски копирования mask
                CvInvoke.cvCopy(small, res, small.Split()[3]);
                res.ROI = System.Drawing.Rectangle.Empty;
            }
            imageBox2.Image = res;
        }
    }
}
