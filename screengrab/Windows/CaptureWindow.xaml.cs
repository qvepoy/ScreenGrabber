﻿using screengrab.Classes;
using screengrab.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace screengrab {
    public partial class CaptureWindow : Window {
        // Screen characteristics
        double screenWidth, screenHeight, screenLeft, screenTop;

        // Desktop screenshot
        Image img = new Image();

        int instaScreen = 0;

        // Close window on Escape click
        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape)
                CloseWindow();
        }

        public void CloseWindow() {
            Properties.Settings.Default.CaptureWindowOpened = false;
            Close();
        }

        public CaptureWindow(int inf) {
            InitializeComponent();

            instaScreen = inf;

            screenLeft = SystemParameters.VirtualScreenLeft;
            screenTop = SystemParameters.VirtualScreenTop;
            screenWidth = SystemParameters.VirtualScreenWidth;
            screenHeight = SystemParameters.VirtualScreenHeight;

            Height = screenHeight;
            Width = screenWidth;

            Top = screenTop;
            Left = screenLeft;

            img.Source = CopyScreen();
            canvas.Children.Add(img);
        }

        // library for remove memory leak
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private BitmapSource CopyScreen() {
            var left = (int)screenLeft;
            var top = (int)screenTop;
            var width = (int)screenWidth;
            var height = (int)screenHeight;
            
            using (var screenBmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                using (var bmpGraphics = System.Drawing.Graphics.FromImage(screenBmp)) {
                    bmpGraphics.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height));
                    IntPtr hBitmap = screenBmp.GetHbitmap();
                    try {
                        return Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    }
                    finally {
                        // Remove memory leak
                        DeleteObject(hBitmap);
                    }
                }
            }
        }

        Point currentPoint = new Point();
        Point firstClick = new Point();
        bool first = false;
        Rectangle _rect;
        double x, y, w, h;

        private void MouseUp(object sender, MouseButtonEventArgs e) {
            canvas.Children.Remove(_rect);
            canvas.Children.Remove(img);
            canvas.Children.Clear();

            if (_rect.Height < 20 || _rect.Width < 20) {
                CloseWindow();
                Tray.ShotNotification("Screenshot cannot loaded, very small");
                return;
            }
            first = false;

            CloseWindow();

            CroppedBitmap cb = new CroppedBitmap(
                (BitmapSource)img.Source,
                new Int32Rect((int)x, (int)y, (int)w, (int)h));

            Image croppedImage = new Image();
            croppedImage.Source = cb;

            if (instaScreen == 1) {
                ImageConverter.CopyToClipboard(croppedImage, Properties.Settings.Default.ImageFormat);
            } else {
                EditWindow editWindow = new EditWindow(croppedImage);
                editWindow.Show();
            }

            string loadtodisk = "";

            if (Properties.Settings.Default.LoadToDisk) {
                ImageConverter.SaveImageTo(Properties.Settings.Default.ImageFormat,
                                       Properties.Settings.Default.LoadImagePath +
                                            "Image-" +
                                            DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") +
                                            ImageConverter.ImageFormat(Properties.Settings.Default.ImageFormat),
                                       croppedImage);
                loadtodisk = " and saved to disk";
            }

            Tray.ShotNotification("Screenshot loaded" + loadtodisk);
        }

        private void MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ButtonState == MouseButtonState.Pressed)
                currentPoint = e.GetPosition(this);
            if (first == false && e.ButtonState == MouseButtonState.Pressed) {
                first = true;
                firstClick = e.GetPosition(this);

                _rect = new Rectangle {
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                    Fill = new SolidColorBrush(Color.FromArgb(75, 255, 255, 255))
                };
                Canvas.SetLeft(_rect, firstClick.X);
                Canvas.SetTop(_rect, firstClick.Y);

                canvas.Children.Add(_rect);
            }
        }

        private void MouseMove(object sender, MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                currentPoint = e.GetPosition(this);

                if (currentPoint.Y < 0 || currentPoint.X < 0 || currentPoint.Y > Height || currentPoint.X > Width) {
                    return;
                }

                x = Math.Min(currentPoint.X, firstClick.X);
                y = Math.Min(currentPoint.Y, firstClick.Y);

                w = Math.Max(currentPoint.X, firstClick.X) - x;
                h = Math.Max(currentPoint.Y, firstClick.Y) - y;

                _rect.Width = w;
                _rect.Height = h;

                if (w > 40 && h > 40) {
                    WidthTB.Text = w.ToString();
                    Canvas.SetTop(WidthPanel, y + 5);
                    Canvas.SetLeft(WidthPanel, x + w / 2 - 10);

                    HeightTB.Text = h.ToString();
                    Canvas.SetTop(HeightPanel, y + h / 2 - 10);
                    Canvas.SetLeft(HeightPanel, x + 5);
                } else {
                    WidthTB.Text = "";
                    HeightTB.Text = "";
                }

                Canvas.SetLeft(_rect, x);
                Canvas.SetTop(_rect, y);
            }
        }
    }
}
