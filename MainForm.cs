using Microsoft.VisualBasic;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;

namespace ClearVision
{

    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // Метод для получения списка доступных камер
        private void GetAvailableCameras()
        {
            List<string> cameras = new List<string>(); // Инициализация пустого списка камер

            
            bool foundCamera = true;

            // Итерируемся по индексам и проверяем наличие камеры
            while (foundCamera)
            {
                using (var capture = new VideoCapture(index)) // Используется библиотека Emgu.CV для работы с камерами
                {
                    if (capture.IsOpened()) // Если камера доступна
                    {
                        cameras.Add($"Camera {index}"); // Добавляем камеру в список
                        capture.Release(); // Освобождаем ресурсы камеры
                        index++; // Переходим к следующей камере
                    }
                    else // Если камера не доступна
                    {
                        foundCamera = false; // Выходим из цикла
                    }
                }
            }

            // Очистить список перед заполнением
            listBox1.Items.Clear();

            // Добавление камер в список 
            foreach (string camera in cameras) // Итерируемся по списку камер
            {
                listBox1.Items.Add(camera); // Добавляем камеру в элемент управления ListBox
            }
        }

        // Метод, вызываемый при загрузке формы
        private void MainForm_Load(object sender, EventArgs e)
        {
            rulerCount = 5; // Устанавливаем начальное количество линеек равным 5
            rulerStep = 5; // Устанавливаем начальный шаг линеек равным 5
            pictureBox1.MouseWheel += new MouseEventHandler(pictureBox1_MouseWheel); // Подписываемся на событие скролла колесика мыши на PictureBox

            textBox1.Text = ps.ToString(); // Устанавливаем значение переменной ps в текстовое поле textBox1
            textBox2.Text = rulerCount.ToString(); // Устанавливаем количество линеек в текстовом поле textBox2
            textBox3.Text = rulerStep.ToString(); // Устанавливаем шаг линеек в текстовом поле textBox3

            comboBox1.SelectedIndex = 0; // Устанавливаем первый элемент списка комбобокса по умолчанию

            GetAvailableCameras(); // Получаем список доступных камер и заполняем им элемент управления ListBox
        }




        VideoCapture capture; // Объект для работы с камерой
        Mat frame; // Матрица для хранения изображения
        Bitmap image1, image2; // Объекты для отрисовки изображения на форме
        double k1 = 0, k2 = 0, picZoom = 0; // Коэффициенты масштабирования изображения
        int w = 0, h = 0, t = 0, l = 0, lx = 0, ly = 0, startX = 0, startY = 0, rulerCount = 0, tc2 = 0; // Параметры изображения и линеек
        bool movedraw = false, movepic = false, timerCap = false; // Флаги движения мыши и таймера
        double ps = 0.03; // Начальный размер пикселя в миллиметрах
        double tc1 = 0, tc3 = 0; // Дополнительные параметры для работы со временем
        double rulerStep = 10.0; // Шаг линеек на изображении (в пикселях)
        int index = 0; // Индекс первой камеры
        private void DrawCircleButton_Click(object sender, EventArgs e)
        {
            
        }



        // Метод для изменения размера изображения
        void ResizeP(int rx, int ry, double rzoom)
        {
            if (image1 == null) return; // Если изображение не загружено, то выходим
            double rs = rzoom; // Задаем коэффициент масштабирования изображения

            k1 = (double)1 * panel2.Width / image1.Width; // Вычисляем коэффициенты масштабирования изображения по ширине и высоте
            k2 = (double)1 * panel2.Height / image1.Height;

            if (k1 > k2) k1 = k2; // Берем минимальный коэффициент масштабирования

            if (rs < k1) rs = k1; // Если новый коэффициент масштабирования меньше минимального, то берем минимальный

            pictureBox1.Width = (int)(rs * image1.Width); // Изменяем ширину и высоту PictureBox в соответствии с новым коэффициентом масштабирования
            pictureBox1.Height = (int)(rs * image1.Height);

            // Перемещаем PictureBox в зависимости от изменения масштаба
            pictureBox1.Left = (int)(pictureBox1.Left - (rs - picZoom) * rx / picZoom);
            pictureBox1.Top = (int)(pictureBox1.Top - (rs - picZoom) * ry / picZoom);

            // Проверяем, чтобы PictureBox не выходил за границы панели
            if (pictureBox1.Height < panel2.Height) // Если высота PictureBox меньше высоты панели
            {
                pictureBox1.Top = (int)((panel2.Height - pictureBox1.Height) / 2); // Центрируем PictureBox по высоте панели
            }
            else // Если высота PictureBox больше высоты панели
            {
                if (pictureBox1.Top > 0) // Если верхняя граница PictureBox выходит за верхнюю границу панели
                {
                    pictureBox1.Top = 0; // Сдвигаем PictureBox вверх до верхней границы панели
                }
                if ((pictureBox1.Top + pictureBox1.Height) < panel2.Height) // Если нижняя граница PictureBox выходит за нижнюю границу панели
                {
                    pictureBox1.Top = panel2.Height - pictureBox1.Height; // Сдвигаем PictureBox вниз до нижней границы панели
                }
            }

            if (pictureBox1.Width < panel2.Width) // Если ширина PictureBox меньше ширины панели
            {
                pictureBox1.Left = (int)((panel2.Width - pictureBox1.Width) / 2); // Центрируем PictureBox по ширине панели
            }
            else // Если ширина PictureBox больше ширины панели
            {
                if (pictureBox1.Left > 0) // Если левая граница PictureBox выходит за левую границу панели
                {
                    pictureBox1.Left = 0; // Сдвигаем PictureBox влево до левой границы панели
                }
                if (pictureBox1.Left + pictureBox1.Width < panel2.Width) // Если правая граница PictureBox выходит за правую границу панели
                {
                    pictureBox1.Left = panel2.Width - pictureBox1.Width; // Сдвигаем PictureBox вправо до правой границы панели
                }
            }

            picZoom = rs; // Сохраняем новый коэффициент масштабирования

        }
        // Метод для перерисовки изображения с линейками
        private void repaint()
        {
            if (image1 == null) return; // Если изображение не загружено, то выходим

            image2 = new Bitmap(image1); // Создаем новый объект Bitmap на основе исходного изображения
            Graphics g = Graphics.FromImage(image2); // Получаем объект Graphics для отрисовки на изображении

            Pen pen = new Pen(Color.Red); // Создаем перо красного цвета для отрисовки линеек на изображении
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid; // Задаем стиль линии - сплошную

            int tx = lx, ty = ly; // Задаем координаты точки, в которой размещены линейки

            System.Drawing.Point pw1 = new System.Drawing.Point(0, ty); // Координаты первой точки для вертикальной линейки
            System.Drawing.Point pw2 = new System.Drawing.Point(image2.Width, ty); // Координаты второй точки для вертикальной линейки
            g.DrawLine(pen, pw1, pw2); // Рисуем вертикальную линейку

            System.Drawing.Point ph1 = new System.Drawing.Point(tx, 0); // Координаты первой точки для горизонтальной линейки
            System.Drawing.Point ph2 = new System.Drawing.Point(tx, image2.Height); // Координаты второй точки для горизонтальной линейки
            g.DrawLine(pen, ph1, ph2); // Рисуем горизонтальную линейку

            int i = 1; // Инициализируем счетчик для отрисовки шкалы на линейках
            int delta = 0; // Задаем дельту - расстояние между шкалами

            bool draw = true; // Флаг для отрисовки шкалы

            string MS = "0"; // Значение миллиметровой шкалы
            Font stringFont = new Font("Arial", 14); // Шрифт для отображения значений миллиметровой шкалы

            SizeF stringSize = new SizeF(); // Размер строки для выравнивания по центру

            int SMMM = 1; // Определяем множитель миллиметровой шкалы

            if (comboBox1.SelectedIndex != 0) // Если выбран режим сантиметровой шкалы
            {
                SMMM = 10; // Устанавливаем множитель равным 10
            }

            // Цикл для отрисовки шкалы на линейках
            while (draw)
            {
                draw = false; // Устанавливаем флаг отрисовки в ложное значение
                delta = (int)((i * rulerStep / rulerCount) / ps); // Вычисляем расстояние между шкалами

                if (i % rulerCount == 0) // Если номер шкалы кратен числу делений на шкале
                {
                    MS = (i * rulerStep / rulerCount / SMMM).ToString(); // Записываем значение шкалы в соответствии с выбранным режимом шкалы
                    stringSize = g.MeasureString(MS, stringFont); // Вычисляем размер строки для выравнивания по центру
                }

                //ГОРИЗОНТАЛЬНАЯ ЛИНИЯ

                if (tx + delta < image2.Width) // Если координата x новой шкалы не превышает ширину изображения
                {
                    if (i % rulerCount == 0) // Если номер шкалы кратен числу делений на шкале
                    {
                        g.DrawString(MS, stringFont, Brushes.Red, new PointF((tx - stringSize.Width / 2) + delta, ty - stringSize.Height - 10 - stringSize.Height / 5)); // Отрисовываем значение шкалы и центрируем его под шкалой
                        g.DrawLine(pen, tx + delta, ty - 10, tx + delta, ty + 10); // Отрисовываем вертикальную линейку для шкалы
                    }
                    else g.DrawLine(pen, tx + delta, ty - 2, tx + delta, ty + 2); // Отрисовываем короткую вертикальную линию для шкалы
                    draw = true; // Устанавливаем флаг отрисовки в истинное значение
                }
                if (tx - delta > 0) // Если координата x новой шкалы не меньше нуля
                {
                    if (i % rulerCount == 0) // Если номер шкалы кратен числу делений на шкале
                    {
                        g.DrawString(MS, stringFont, Brushes.Red, new PointF((tx - stringSize.Width / 2) - delta, ty + 10 + stringSize.Height / 5)); // Отрисовываем значение шкалы и центрируем его над шкалой
                        g.DrawLine(pen, tx - delta, ty - 10, tx - delta, ty + 10); // Отрисовываем вертикальную линейку для шкалы
                    }
                    else g.DrawLine(pen, tx - delta, ty - 2, tx - delta, ty + 2); // Отрисовываем короткую вертикальную линию для шкалы
                    draw = true; // Устанавливаем флаг отрисовки в истинное значение
                }

                //ВЕРТИКАЛЬНАЯ ЛИНИЯ



                if (ty + delta < image2.Height) // Если координата y новой шкалы не превышает высоту изображения
                {
                    if (i % rulerCount == 0) // Если номер шкалы кратен числу делений на шкале
                    {
                        g.DrawString(MS, stringFont, Brushes.Red, new PointF(tx + 35 - stringSize.Width, (ty - stringSize.Height / 2) + delta)); // Отрисовываем значение шкалы и центрируем его справа от шкалы
                        g.DrawLine(pen, tx - 10, ty + delta, tx + 10, ty + delta); // Отрисовываем горизонтальную линейку для шкалы
                    }
                    else g.DrawLine(pen, tx - 2, ty + delta, tx + 2, ty + delta); // Отрисовываем короткую горизонтальную линию для шкалы
                    draw = true; // Устанавливаем флаг отрисовки в истинное значение
                }
                if (ty - delta > 0) // Если координата y новой шкалы не меньше нуля
                {
                    if (i % rulerCount == 0) // Если номер шкалы кратен числу делений на шкале
                    {
                        g.DrawString(MS, stringFont, Brushes.Red, new PointF(tx - 15 - stringSize.Width, (ty - stringSize.Height / 2) - delta)); // Отрисовываем значение шкалы и центрируем его слева от шкалы
                        g.DrawLine(pen, tx - 10, ty - delta, tx + 10, ty - delta); // Отрисовываем горизонтальную линейку для шкалы
                    }
                    else g.DrawLine(pen, tx - 2, ty - delta, tx + 2, ty - delta); // Отрисовываем короткую горизонтальную линию для шкалы
                    draw = true; // Устанавливаем флаг отрисовки в истинное значение
                }
                i++; // Увеличиваем счетчик номера шкалы
            }

            pen.Dispose(); // Освобождаем ресурсы, занятые пером
            g.Dispose(); // Освобождаем ресурсы, занятые объектом Graphics
            var oldImage = pictureBox1.Image; // Сохраняем старое изображение
            pictureBox1.Image = image2; // Устанавливаем новое изображение на PictureBox
            if (oldImage != null)
            {
                oldImage.Dispose(); // Освобождаем ресурсы, занятые старым изображением
            }

        }

        // Метод для открытия изображения в программе
        private void picture()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog(); // Создаем объект диалогового окна выбора файла
            openFileDialog1.Title = "Выберите файл"; // Устанавливаем заголовок диалогового окна
            openFileDialog1.Filter = "Image files (.jpg;.png;.bmp)|*.jpg;*.png;*.bmp"; // Устанавливаем фильтр для выбора только файлов изображений

            if (openFileDialog1.ShowDialog() != DialogResult.OK) return; // Если пользователь не выбрал файл или нажал кнопку "Отмена", то выходим из метода

            var oldImage = pictureBox1.Image; // Сохраняем старое изображение, если оно есть
            if (oldImage != null)
            {
                oldImage.Dispose(); // Освобождаем ресурсы, занятые старым изображением
            }

            pictureBox1.Load(openFileDialog1.FileName); // Загружаем выбранное изображение на PictureBox
            image1 = new Bitmap(pictureBox1.Image); // Создаем копию изображения

            if (image1 == null) return; // Если не удалось создать копию изображения, то выходим из метода

            k1 = (double)1 * panel2.Width / image1.Width; // Вычисляем коэффициент масштабирования по ширине
            k2 = (double)1 * panel2.Height / image1.Height; // Вычисляем коэффициент масштабирования по высоте
            if (k1 > k2)
            {
                k1 = k2; // Устанавливаем минимальный коэффициент масштабирования
            }
            picZoom = k1; // Сохраняем коэффициент масштабирования в переменную
            w = (int)(image1.Width * k1); // Вычисляем новую ширину изображения после масштабирования
            h = (int)(image1.Height * k1); // Вычисляем новую высоту изображения после масштабирования
            l = (panel2.Width - w) / 2; // Вычисляем новую координату x левого верхнего угла PictureBox для центрирования
            t = (panel2.Height - h) / 2; // Вычисляем новую координату y левого верхнего угла PictureBox для центрирования
            pictureBox1.Left = l; // Устанавливаем новую координату x левого верхнего угла PictureBox
            pictureBox1.Top = t; // Устанавливаем новую координату y левого верхнего угла PictureBox
            pictureBox1.Height = h; // Устанавливаем новую высоту PictureBox
            pictureBox1.Width = w; // Устанавливаем новую ширину PictureBox

            lx = image1.Width / 2; ly = image1.Height / 2; // Устанавливаем центральную координату изображения
            repaint(); // Вызываем метод перерисовки изображения
        }

        // Метод для применения новых значений параметров шкалы из текстовых полей
        private void tbox()
        {
            tc1 = double.Parse(textBox1.Text.Replace('.', ',')); // Получаем новое значение параметра ps и приводим его к типу double
            tc2 = int.Parse(textBox2.Text); // Получаем новое значение параметра rulerCount и приводим его к типу int
            tc3 = double.Parse(textBox3.Text.Replace('.', ',')); // Получаем новое значение параметра rulerStep и приводим его к типу double
            ps = tc1; // Устанавливаем новое значение параметра ps в переменную
            rulerCount = tc2; // Устанавливаем новое значение параметра rulerCount в переменную
            rulerStep = tc3; // Устанавливаем новое значение параметра rulerStep в переменную
            repaint(); // Вызываем метод перерисовки изображения с новыми параметрами шкалы
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            tbox();
        }

        private void panel2_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            tbox();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            repaint();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            index = int.Parse(listBox1.SelectedItem.ToString());
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Остановка таймера timer1 для избежания параллельного выполнения блока кода
            timer1.Stop();
            // Проверяем, открыт ли объект захвата видео
            if (capture.IsOpened())
            {
                // Считываем кадр и конвертируем его в Bitmap-изображение
                capture.Read(frame);
                image1 = BitmapConverter.ToBitmap(frame);
            }

            // Обновляем отображение
            repaint();

            // Запускаем таймер timer1 снова для продолжения выполнения блока кода
            timer1.Start();
        }

        // Обработчик события, вызываемый при прокрутке колеса мыши на pictureBox1
        void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            // Если изображение image1 не загружено, выходим из метода
            if (image1 == null) return;
            // Изменяем масштаб изображения в зависимости от направления прокрутки колеса мыши
            if (e.Delta > 0)
            {
                if ((picZoom * 1.5) > 12)
                    ResizeP(e.X, e.Y, 12);
                else
                    ResizeP(e.X, e.Y, picZoom * 1.5);
            }
            if (e.Delta < 0)
            {
                ResizeP(e.X, e.Y, picZoom / 1.5);
            }
        }
        // Обработчик события, вызываемый при перемещении указателя мыши по pictureBox1
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            // Если изображение image1 не загружено, выходим из метода
            if (image1 == null) return;
            // Если установлен режим рисования moveDraw, то сохраняем координаты линий lx и ly и обновляем отображение
            if (movedraw)
            {
                lx = (e.X * pictureBox1.Image.Width) / pictureBox1.Width;
                ly = (e.Y * pictureBox1.Image.Height) / pictureBox1.Height;
                repaint();
            }

            // Если установлен режим перемещения movepic, то меняем позицию pictureBox1 в зависимости от координат указателя мыши
            if (movepic)
            {
                if (pictureBox1.Width > panel2.Width)
                {
                    int NewX = e.X - startX + pictureBox1.Left;
                    if (NewX > 0) NewX = 0;
                    if ((NewX + pictureBox1.Width) < panel2.Width) NewX = panel2.Width - pictureBox1.Width;
                    pictureBox1.Left = NewX;
                }
                if (pictureBox1.Height > panel2.Height)
                {
                    int NewY = e.Y - startY + pictureBox1.Top;
                    if (NewY > 0) NewY = 0;
                    if ((NewY + pictureBox1.Height) < panel2.Height) NewY = panel2.Height - pictureBox1.Height;
                    pictureBox1.Top = NewY;
                }
            }
        }
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {

            if (image1 == null) return;
            pictureBox1.Focus();
            if (e.Button == MouseButtons.Right)
            {
                movedraw = true;
                lx = (e.X * pictureBox1.Image.Width) / pictureBox1.Width;
                ly = (e.Y * pictureBox1.Image.Height) / pictureBox1.Height;
                repaint();
            }


            if (e.Button == MouseButtons.Left)
            {
                movepic = true;
                startX = e.X;
                startY = e.Y;
            }
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Right) movedraw = false;
            if (e.Button == MouseButtons.Left) movepic = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            tbox();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            picture();
        }
        // Обработчик нажатия на кнопку "button3"
        private void button3_Click(object sender, EventArgs e)
        {
            // Если таймер уже запущен
            if (timerCap)
            {
                // Очищаем старый объект захвата изображения
                var oldCap = capture;
                oldCap.Dispose();
            }
            // Создаем новый объект Mat для хранения изображения
            frame = new Mat();
            // Создаем объект VideoCapture для захвата видео с камеры
            capture = new VideoCapture();
            // Открываем камеру с индексом 0 (то есть первую доступную)
            capture.Open(0);
            // Если удалось открыть камеру
            if (capture.IsOpened())
            {
                // Захватываем текущий кадр
                capture.Read(frame);
                // Преобразуем его в Bitmap
                image1 = BitmapConverter.ToBitmap(frame);
            }
            // Если не удалось получить изображение, выходим из метода
            if (image1 == null) return;

            // Вычисляем коэффициенты масштабирования по ширине и высоте
            k1 = (double)1 * panel2.Width / image1.Width;
            k2 = (double)1 * panel2.Height / image1.Height;
            // Выбираем меньший коэффициент для сохранения пропорций
            if (k1 > k2)
            {
                k1 = k2;
            }
            // Устанавливаем значение нового масштаба
            picZoom = k1;
            // Вычисляем новую ширину и высоту изображения
            w = (int)(image1.Width * k1);
            h = (int)(image1.Height * k1);
            // Вычисляем отступы для центрирования изображения внутри панели
            l = (panel2.Width - w) / 2;
            t = (panel2.Height - h) / 2;
            // Устанавливаем значения свойств PictureBox для отображения изображения
            pictureBox1.Left = l;
            pictureBox1.Top = t;
            pictureBox1.Height = h;
            pictureBox1.Width = w;

            // Вычисляем центр изображения
            lx = image1.Width / 2;
            ly = image1.Height / 2;



            // Перерисовываем изображение на форме
            repaint();

            // Устанавливаем интервал таймера и запускаем его
            timer1.Interval = 1000;

            timer1.Start();

        }
        private void button4_Click(object sender, EventArgs e)
        {
            timer1.Stop(); //Остановка таймера, тем самым останавливая трансляцию
        }

        private void panel2_Resize(object sender, EventArgs e)
        {
            ResizeP(0, 0, picZoom);
        }
        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }
        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private void panel1_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

    }
}
