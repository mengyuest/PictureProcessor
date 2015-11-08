using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
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
using Microsoft.Win32;

namespace PictureProcessor
{
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window
	{
		#region 变量区

		//public static Image OriginImage = new Image();

		public static Image EditedImage = new Image();

		//public static BitmapSource OriginBitmapSource;

		public String OriginPath = null;

		public static int OriginX;

		public static int OriginY;

		public static BitmapSource EditedBitmapSource;

		public static byte[] OriginData;

		public static byte[] OriginRGBMatrix;

		public static byte[] EditedData;


		public struct States
		{
			public int MethodIndex;
			public float Theta;
			public float X;
			public float Y;
			public bool IsINF;
			public bool IsFirstRotate;
			public bool IsCentered;


			public void Init()
			{
				MethodIndex = 0;
				Theta = 0;
				X = 1;
				Y = 1;
				IsINF = false;
				IsFirstRotate = false;
				IsCentered = false;
			}
		}

		public struct Point
		{
			public float X;
			public float Y;
		}

		public static States PhotoStates;

		public static Point[] Center = new Point[3];
		public static Point[] Margin = new Point[3];
		public static int simuNum = 1000000;
		public static float[] functionS = new float[simuNum];
		#endregion



		public MainWindow()
		{
			InitializeComponent();
			GenerateS();
			Initialize();
			ThetaSlider.ValueChanged += (ThetaSlider_ValueChanged);//注册事件  
			XSlider.ValueChanged += (XSlider_ValueChanged);//注册事件  
			YSlider.ValueChanged += (YSlider_ValueChanged);//注册事件  
			DisableAllButton();

		}

		private void GenerateS()
		{
			for (var i = 0; i < simuNum/4; i++)
			{
				float x = (i - simuNum / 2) * 4.0f / simuNum;
				functionS[i] = 4 + 8 * x + 5 * x * x + x * x * x;
			}
			for (var i = simuNum/4; i < simuNum/2; i++)
			{
				float x = (i - simuNum / 2) * 4.0f / simuNum;
				functionS[i] = 1 - 2*x*x - x*x*x;
			}
			for (var i = simuNum / 2; i < 3*simuNum / 4; i++)
			{
				float x = (i - simuNum / 2) * 4.0f / simuNum;
				functionS[i] = 1 - 2 * x * x + x * x * x;
			}
			for (var i = 3*simuNum / 4; i < simuNum; i++)
			{
				float x = (i - simuNum / 2) * 4.0f / simuNum;
				functionS[i] = 4 - 8 * x + 5 * x * x - x * x * x;
			}
		}

		private void Initialize()
		{
			
			EditedCanvas.Children.Add(EditedImage);
			
			EditedImage.Stretch = Stretch.Fill;
		}

		private void ImportButton_OnClick(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.DefaultExt = ".jpg";
			ofd.Filter = "jpg file|*.jpg";
			if (ofd.ShowDialog() == true)
			{
			    OriginPath = ofd.FileName;
				BitmapSource OriginBitmapSource = new BitmapImage(new Uri(OriginPath, UriKind.Absolute));
				OriginBitmapSource = new BitmapImage(new Uri(ofd.FileName, UriKind.Absolute));
				Image OriginImage = new Image();
				OriginImage.Source = OriginBitmapSource;
				OriginCanvas.Width = OriginBitmapSource.PixelWidth;
				OriginCanvas.Height = OriginBitmapSource.PixelHeight;

				int width = OriginBitmapSource.PixelWidth;
				int height = OriginBitmapSource.PixelHeight;
				var cut = new Int32Rect(0, 0, width, height);
				var stride = OriginBitmapSource.Format.BitsPerPixel * cut.Width / 8;
				OriginData = new byte[cut.Height * stride];
				OriginBitmapSource.CopyPixels(cut, OriginData, stride, 0);
				EditedData = new byte[cut.Height * stride];

				OriginRGBMatrix = new byte[(cut.Height+3)*(cut.Width+3)*3];
				GenerateMarginMatrix(OriginBitmapSource.PixelWidth,OriginBitmapSource.PixelHeight);
				OriginCanvas.Children.Clear();
				OriginCanvas.Children.Add(OriginImage);
				OriginImage.Stretch = Stretch.Fill;
				

				for (var i = 0; i < OriginData.Count(); i++)
				{
					EditedData[i] = OriginData[i];
				}


				EditedBitmapSource = BitmapSource.Create(width, height, 0, 0, PixelFormats.Bgr32, null, EditedData, stride);
				EditedImage.Source = EditedBitmapSource;
				EditedCanvas.Width = EditedBitmapSource.PixelWidth;
				EditedCanvas.Height = EditedBitmapSource.PixelHeight;

				EnableAllButton();
				PhotoStates.Init();
				PanelUpdate();
			}

		}

		private void GenerateMarginMatrix(int x, int y)
		{
			Margin[0].X = x;
			Margin[0].Y = y;
			for (var i = 0; i < y+3; i++)
			{
				for (var j = 0; j < x+3; j++)
				{
					if (i >= 1 && i <= y && j >= 1 && j <= x)
					{
						OriginRGBMatrix[(i *( x+3) + j) * 3 + 0] = OriginData[((i - 1)*x + j - 1)*4 + 0];
						OriginRGBMatrix[(i *( x+3) + j) * 3 + 1] = OriginData[((i - 1) * x + j - 1) * 4 + 1];
						OriginRGBMatrix[(i *( x+3) + j) * 3 + 2] = OriginData[((i - 1) * x + j - 1) * 4 + 2];
					}
					else
					{
						if (i == 0 && j == 0)
						{
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 0] = OriginData[0];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 1] = OriginData[1];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 2] = OriginData[2];
						}
						else if (i == 0 && j <= x)
						{
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 0] = OriginData[(j - 1) * 4 + 0];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 1] = OriginData[(j - 1) * 4 + 1];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 2] = OriginData[(j - 1) * 4 + 2];
						}
						else if (i == 0)
						{
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 0] = OriginData[(y - 1) * 4 + 0];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 1] = OriginData[(y - 1) * 4 + 1];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 2] = OriginData[(y - 1) * 4 + 2];
						}

						if (j == 0 && i <= y && i!=0)
						{
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 0] = OriginData[((i - 1) * x) * 4 + 0];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 1] = OriginData[((i - 1) * x) * 4 + 1];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 2] = OriginData[((i - 1) * x) * 4 + 2];
						}

						else if (j == 0 && i != 0)
						{
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 0] = OriginData[((y - 1) * x) * 4 + 0];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 1] = OriginData[((y - 1) * x) * 4 + 1];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 2] = OriginData[((y - 1) * x) * 4 + 2];
						}
						
						if (i != 0 && i <= y && j > x)
						{
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 0] = OriginData[((i - 1) * x + x - 1) * 4 + 0];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 1] = OriginData[((i - 1) * x + x - 1) * 4 + 1];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 2] = OriginData[((i - 1) * x + x - 1) * 4 + 2];
						}

						if (j != 0 && j <= x && i > y)
						{
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 0] = OriginData[((y - 1) * x + j - 1) * 4 + 0];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 1] = OriginData[((y - 1) * x + j - 1) * 4 + 1];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 2] = OriginData[((y - 1) * x + j - 1) * 4 + 2];
						}

						if (i > y && j > x)
						{
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 0] = OriginData[((y - 1) * x + x - 1) * 4 + 0];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 1] = OriginData[((y - 1) * x + x - 1) * 4 + 1];
							OriginRGBMatrix[(i *( x+3) + j) * 3 + 2] = OriginData[((y - 1) * x + x - 1) * 4 + 2];
						}

					}
				}
			}
		}

		private void DisableAllButton()
		{
			MethodARadioButton.IsEnabled = false;
			MethodBRadioButton.IsEnabled = false;
			MethodCRadioButton.IsEnabled = false;
			CropButton.IsEnabled = false;
			LooseButton.IsEnabled = false;
			ThetaTextBox.IsEnabled = false;
			XTextBox.IsEnabled = false;
			YTextBox.IsEnabled = false;
			ThetaSlider.IsEnabled = false;
			XSlider.IsEnabled = false;
			YSlider.IsEnabled = false;
			AdjustModeCheckBox.IsEnabled = false;
		}

		private void EnableAllButton()
		{
			MethodARadioButton.IsEnabled = true;
			MethodBRadioButton.IsEnabled = true;
			MethodCRadioButton.IsEnabled = true;
			CropButton.IsEnabled = true;
			LooseButton.IsEnabled = true;
			CropButton.IsChecked = true;
			LooseButton.IsChecked = false;
			ThetaTextBox.IsEnabled = true;
			XTextBox.IsEnabled = true;
			YTextBox.IsEnabled = true;
			ThetaSlider.IsEnabled = true;
			XSlider.IsEnabled = true;
			YSlider.IsEnabled = true;
			AdjustModeCheckBox.IsEnabled = true;
		}

		private void PanelUpdate()
		{
			switch (PhotoStates.MethodIndex)
			{
				case 0:
					MethodARadioButton.IsChecked = true;
					MethodBRadioButton.IsChecked = false;
					MethodCRadioButton.IsChecked = false;
					break;
				case 1:
					MethodARadioButton.IsChecked = false;
					MethodBRadioButton.IsChecked = true;
					MethodCRadioButton.IsChecked = false;
					break;
				case 2:
					MethodARadioButton.IsChecked = false;
					MethodBRadioButton.IsChecked = false;
					MethodCRadioButton.IsChecked = true;
					break;
			}


			if (PhotoStates.IsINF == false)
			{
				AdjustModeCheckBox.IsChecked = false;
				ThetaSlider.IsEnabled = false;
				XSlider.IsEnabled = false;
				YSlider.IsEnabled = false;
				ThetaTextBox.IsEnabled = true;
				ThetaTextBox.Text = "0";
				XTextBox.IsEnabled = true;
				XTextBox.Text = "1";
				YTextBox.IsEnabled = true;
				YTextBox.Text = "1";
				CropButton.IsChecked = true;
				RunButton.IsEnabled = true;
			}

			else
			{
				AdjustModeCheckBox.IsChecked = true;
				ThetaSlider.IsEnabled = true;
				XSlider.IsEnabled = true;
				YSlider.IsEnabled = true;
				ThetaTextBox.IsEnabled = false;
				XTextBox.IsEnabled = false;
				YTextBox.IsEnabled = false;
				RunButton.IsEnabled = false;
			}

		}

		private void ExportButton_OnClick(object sender, RoutedEventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.DefaultExt = ".jpg";
			sfd.Filter = "jpeg file|*.jpg";
			if (sfd.ShowDialog() == true)
			{
			    if (sfd.FileName == OriginPath)
			    {
			        MessageBox.Show("您的保存路径与打开文件路径冲突，请更换路径！");
			        return;
			    }
			    using (FileStream JPGStream = new FileStream(sfd.FileName, FileMode.Create))
				{
					BitmapEncoder encoder = new JpegBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(EditedBitmapSource));
					encoder.Save(JPGStream);
				}
			}
		}

		private void ConfirmLengthAndCenterPoint(bool isCentered = false)
		{
			

			if (CropButton.IsChecked == true)
			{
				if (isCentered == false)
				{
					Margin[1].X = Margin[0].X*PhotoStates.X;
					Margin[1].Y = Margin[0].Y*PhotoStates.Y;
					Margin[2].X =
						(float)
							(Math.Abs(Margin[1].X*Math.Cos(PhotoStates.Theta)) +
							 Math.Abs(Margin[1].Y*Math.Sin(PhotoStates.Theta)));
					Margin[2].Y =
						(float)
							(Math.Abs(Margin[1].Y*Math.Cos(PhotoStates.Theta)) +
							 Math.Abs(Margin[1].X*Math.Sin(PhotoStates.Theta)));
				}
				else
				{
					Margin[1].X =
						(float)
							(Math.Abs(Margin[0].X*Math.Cos(PhotoStates.Theta)) +
							 Math.Abs(Margin[0].Y*Math.Sin(PhotoStates.Theta)));
					Margin[1].Y =
						(float)
							(Math.Abs(Margin[0].Y*Math.Cos(PhotoStates.Theta)) +
							 Math.Abs(Margin[0].X*Math.Sin(PhotoStates.Theta)));
					Margin[2].X = Margin[1].X*PhotoStates.X;
					Margin[2].Y = Margin[1].Y*PhotoStates.Y;
				}
			}
			else
			{

				Margin[1].X = Margin[0].X*PhotoStates.X;
				Margin[1].Y = Margin[0].Y*PhotoStates.Y;
				Margin[2].X =
					(float)
						Margin[1].X;
				Margin[2].Y =
					(float)
						Margin[1].Y;


			}
			for (int i = 0; i < 3; i++)
			{
				Center[i].X = Margin[i].X / 2;
				Center[i].Y = Margin[i].Y / 2;
			}
		}

		private void TransformImplement()
		{
			
			Stopwatch sw = new Stopwatch();
			sw.Start();
			ConfirmLengthAndCenterPoint(PhotoStates.IsCentered);
			AssignMemoryForEditedData();
			switch (PhotoStates.MethodIndex)
			{
				case 0:
					TransformMethodA(false);
					break;
				case 1:
					TransformMethodB(false);
					break;
				case 2:
					TransformMethodC(false);
					break;
			}

			GenerateEditedData();
			sw.Stop();
			TimeCountLabel.Content = sw.ElapsedMilliseconds.ToString()+" ms";
		}

		private void AssignMemoryForEditedData()
		{
			EditedData = new byte[(int)Margin[2].X * (int)Margin[2].Y * 4];
		}

		/// <summary>
		/// 最近邻插值(并行计算)
		/// </summary>
		private void TransformMethodA(bool isMultiThread)
		{
			var AX = PhotoStates.X;
			var AY = PhotoStates.Y;
			var theta = PhotoStates.Theta;

			int margin2X = (int)Margin[2].X;
			int margin2Y = (int)Margin[2].Y;

			int margin0X = (int) Margin[0].X;
			int margin0Y = (int)Margin[0].Y;

			float center2X = Center[2].X;
			float center2Y = Center[2].Y;

			float center0X = Center[0].X;
			float center0Y = Center[0].Y;
			bool isFirstRotate = PhotoStates.IsFirstRotate;


			for (var j = 0; j < margin2X; j++)
			{
				Parallel.For(0, margin2Y, i =>
				{
					float x0;
					float y0;

					if (isFirstRotate == false)
					{
						x0 = (float)(center0X +
									  1 / AX *
									  (Math.Cos(theta) * (j - center2X) +
									   Math.Sin(theta) * (i - center2Y)));
						y0 = (float)(center0Y +
									  1 / AY *
									  (Math.Cos(theta) * (i - center2Y) -
									   Math.Sin(theta) * (j - center2X)));
					}

					else
					{
						x0 = (float)(center0X +
									  1 / AX *
									  (Math.Cos(theta) * (j - center2X)) +
									   1 / AY * (Math.Sin(theta) * (i - center2Y)));
						y0 = (float)(center0Y +
									  1 / AY *
									  (Math.Cos(theta) * (i - center2Y)) -
									   1 / AX * (Math.Sin(theta) * (j - center2X)));
					}


					int NearestX0;

					if (x0 - Math.Floor(x0) > 0.5)
					{
						NearestX0 = (int)Math.Ceiling(x0);
					}

					else
					{
						NearestX0 = (int)Math.Floor(x0);
					}

					int NearestY0;

					if (y0 - Math.Floor(y0) > 0.5)
					{
						NearestY0= (int)Math.Ceiling(y0);
					}

					else
					{
						NearestY0= (int)Math.Floor(y0);
					}

					var pos = (i* margin2X + j)*4;
					if (NearestX0 > 0 && NearestX0 < margin0X - 1 && NearestY0 > 0 && NearestY0 < margin0Y - 1)
					{
						var OriPos = (NearestY0 * margin0X + NearestX0) * 4;
						for (var k = 0; k < 4; k++)
						{
							EditedData[pos + k] = OriginData[OriPos + k];
						}
					}
					else
					{
						for (var k = 0; k < 4; k++)
						{
							EditedData[pos + k] = 0;
						}
					}
				});
			}
		}

		/// <summary>
		/// 双线性插值（并行计算）
		/// </summary>
		private void TransformMethodB(bool isMultiThread)
		{
			var AX = PhotoStates.X;
			var AY = PhotoStates.Y;
			var theta = PhotoStates.Theta;

			int margin2X = (int)Margin[2].X;
			int margin2Y = (int)Margin[2].Y;

			int margin0X = (int)Margin[0].X;
			int margin0Y = (int)Margin[0].Y;

			float center2X = Center[2].X;
			float center2Y = Center[2].Y;

			float center0X = Center[0].X;
			float center0Y = Center[0].Y;
			bool isFirstRotate = PhotoStates.IsFirstRotate;

			Parallel.For(0, margin2X, j =>
			{
				for (var i = 0; i < margin2Y; i++)
				{
					float x0;
					float y0;

					if (isFirstRotate == false)
					{
						x0 = (float) (center0X +
									  1/AX*
									  (Math.Cos(theta)*(j - center2X) +
									   Math.Sin(theta)*(i - center2Y)));
						y0 = (float) (center0Y +
									  1/AY*
									  (Math.Cos(theta)*(i - center2Y) -
									   Math.Sin(theta)*(j - center2X)));
					}

					else
					{
						x0 = (float) (center0X +
									  1/AX*
									  (Math.Cos(theta)*(j - center2X)) +
									  1/AY*(Math.Sin(theta)*(i - center2Y)));
						y0 = (float) (center0Y +
									  1/AY*
									  (Math.Cos(theta)*(i - center2Y)) -
									  1/AX*(Math.Sin(theta)*(j - center2X)));
					}

					int NearestX0 = (int) Math.Floor(x0);
					int NearestY0 = (int) Math.Floor(y0);

					var pos = (i*margin2X + j)*4;
					if (NearestX0 >= 0 && NearestX0 < margin0X && NearestY0 >= 0 && NearestY0 < margin0Y - 1)
					{
						var u = x0 - NearestX0;
						var v = y0 - NearestY0;
						var OriPos1 = (NearestY0*margin0X + NearestX0)*4;
						var OriPos2 = ((NearestY0 + 1)*margin0X + NearestX0)*4;
						var OriPos3 = (NearestY0*margin0X + NearestX0 + 1)*4;
						var OriPos4 = ((NearestY0 + 1)*margin0X + NearestX0 + 1)*4;

						for (var k = 0; k < 3; k++)
						{
							float tempByte = ((OriginRGBMatrix[((NearestY0 + 1)*(margin0X + 3) + NearestX0 + 1)*3 + k]*(1 - u)*
											   (1 - v)) +
											  (OriginRGBMatrix[((NearestY0 + 2)*(margin0X + 3) + NearestX0 + 1)*3 + k]*u*
											   (1 - v)) +
											  (OriginRGBMatrix[((NearestY0 + 1)*(margin0X + 3) + NearestX0 + 2)*3 + k]*(1 - u)*
											   v) +
											  (OriginRGBMatrix[((NearestY0 + 2)*(margin0X + 3) + NearestX0 + 2)*3 + k]*u*v));
							EditedData[pos + k] = (byte) tempByte;
						}
						EditedData[pos + 3] = 0;
					}
					else
					{

						for (var k = 0; k < 4; k++)
						{
							EditedData[pos + k] = 0;
						}

					}
				}
			});

		}

		/// <summary>
		/// 双三次插值（并行计算）
		/// 代码优化！ unsafe指针访问
		/// </summary>
		private void TransformMethodC(bool isMultiThread)
		{
			var AX = PhotoStates.X;
			var AY = PhotoStates.Y;
			
			
			var theta = PhotoStates.Theta;

			int margin2X = (int)Margin[2].X;
			int margin2Y = (int)Margin[2].Y;

			int margin0X = (int)Margin[0].X;
			int margin0Y = (int)Margin[0].Y;

			float center2X = Center[2].X;
			float center2Y = Center[2].Y;

			float center0X = Center[0].X;
			float center0Y = Center[0].Y;
			bool isFirstRotate = PhotoStates.IsFirstRotate;

			float delta = 100/margin2X;

			float sinTheta = (float)Math.Sin(theta);
			float cosTheta = (float)Math.Cos(theta);
			float _AX = 1 / AX;
			float _AY = 1 / AY;
			float _AX_sinTheta = _AX*sinTheta;
			float _AY_sinTheta = _AY * sinTheta;
			float _AX_cosTheta = _AX * cosTheta;
			float _AY_cosTheta = _AY * cosTheta;

			Parallel.For(0,margin2X,j =>
			{
				for(var i=0;i<margin2Y;i++)
				{
					float x0;
					float y0;
					float[] VectorS = new float[8];
					float tempByte = 0;
					float tempByteR = 0;
					float tempByteG = 0;
					float tempByteB = 0;

					if (isFirstRotate == false)
					{
						x0 = (center0X +
									  
									  (_AX_cosTheta * (j - center2X) +
									   _AX_sinTheta * (i - center2Y)));
						y0 = (center0Y +
									  
									  (_AY_cosTheta * (i - center2Y) -
									   _AY_sinTheta * (j - center2X)));
					}

					else
					{
						x0 = (center0X +
									  
									  (_AX_cosTheta * (j - center2X)) +
									   (_AY_sinTheta * (i - center2Y)));
						y0 = (center0Y +
									  
									  (_AY_cosTheta * (i - center2Y)) -
									   _AX_sinTheta * (j - center2X));
					}


					int NearestX0 = (int) Math.Floor(x0);
					int NearestY0 = (int) Math.Floor(y0);

					var pos = (i*margin2X + j)*4;
					if (NearestX0 >= 0 && NearestX0 < margin0X && NearestY0 >= 0 && NearestY0 < margin0Y)
					{
						var u = x0 - NearestX0;
						var v = y0 - NearestY0;
						unsafe
						{
							fixed (float* floatPtr = &(functionS[0]))
							{
								for (var enumI = 0; enumI < 4; enumI++)
								{
									//非指针，函数方法
									//VectorS[enumI] = FunctionS(u + 1 - enumI);
									//VectorS[4 + enumI] = FunctionS(v + 1 - enumI);
									
									
									//非指针 插值近似方法
									//VectorS[enumI] = functionS[(int)((u + 1 - enumI + 2) * simuNum / 4)];
									//VectorS[4 + enumI] = functionS[(int)((v + 1 - enumI + 2) * simuNum / 4)];

									//指针 插值近似方法
									VectorS[enumI] = floatPtr[(int)((u + 1 - enumI + 2) * simuNum / 4)];
									VectorS[4 + enumI] = floatPtr[(int)((v + 1 - enumI + 2) * simuNum / 4)];
								}
							}
						}
							tempByteR = 0;
							tempByteG = 0;
							tempByteB = 0;
							unsafe
							{
								fixed (byte* bytePtr = &(OriginRGBMatrix[0]))
								{
									var NY0_M0X_3_3 = NearestY0*(margin0X + 3)*3;
									var NX3 = NearestX0*3;
									for (var enumI = 0; enumI < 4; enumI++)
									{
										var NY0ENUMI_MARX_3_3 = NY0_M0X_3_3 + enumI*(margin0X + 3)*3;
										var k = VectorS[4 + enumI];
										var kk0 = k * VectorS[0];
										var kk1 = k * VectorS[1];
										var kk2 = k * VectorS[2];
										var kk3 = k * VectorS[3];
										var offset0 = NY0ENUMI_MARX_3_3 + NX3;
										var offset1 = NY0ENUMI_MARX_3_3 + NX3 + 3;
										var offset2 = NY0ENUMI_MARX_3_3 + NX3 + 6;
										var offset3 = NY0ENUMI_MARX_3_3 + NX3 + 9;
										tempByteR += kk0 * bytePtr[offset0] + kk1 * bytePtr[offset1] + kk2 * bytePtr[offset2] + kk3 * bytePtr[offset3];
										tempByteG += kk0 * bytePtr[offset0 + 1] + kk1 * bytePtr[offset1 + 1] + kk2 * bytePtr[offset2 + 1] + kk3 * bytePtr[offset3 + 1];
										tempByteB += kk0 * bytePtr[offset0 + 2] + kk1 * bytePtr[offset1 + 2] + kk2 * bytePtr[offset2 + 2] + kk3 * bytePtr[offset3 + 2];
									}
								}
							}
							if (tempByteR < 0)
							{
								EditedData[pos + 0] = 0;
							}
							else if (tempByteR > 255)
							{
								EditedData[pos + 0] = 255;
							}
							else
							{
								EditedData[pos + 0] = (Byte) tempByteR;
							}

					if (tempByteG < 0)
							{
								EditedData[pos + 1] = 0;
							}
							else if (tempByteG > 255)
							{
								EditedData[pos + 1] = 255;
							}
							else
							{
								EditedData[pos + 1] = (Byte) tempByteG;
							}

					if (tempByteB < 0)
							{
								EditedData[pos + 2] = 0;
							}
							else if (tempByteB > 255)
							{
								EditedData[pos + 2] = 255;
							}
							else
							{
								EditedData[pos + 2] = (Byte) tempByteB;
							}

						//}

						EditedData[pos + 3] = 0;

					}
					else
					{
						for (var k = 0; k < 4; k++)
						{
							EditedData[pos + k] = 0;
						}
					}

				}
			});

		}

		private void GenerateEditedData()
		{

			var width = (int)Margin[2].X;
			var height = (int)Margin[2].Y;
			var stride = 4 * width;
			EditedBitmapSource = BitmapSource.Create(width, height, 0, 0, PixelFormats.Bgr32, null, EditedData, stride);
			EditedImage.Source = EditedBitmapSource;
			EditedCanvas.Width = EditedBitmapSource.PixelWidth;
			EditedCanvas.Height = EditedBitmapSource.PixelHeight;
		}


		private bool IsNumberic(String str, out float result)
		{
			result = -1;   //result 定义为out 用来输出值
			try
			{
				result = (float)Convert.ToDouble(str);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private void AdjustModeCheckBox_Click(object sender, RoutedEventArgs e)
		{
			PhotoStates.IsINF = !PhotoStates.IsINF;
			PanelUpdate();
		}

		private void MethodARadioButton_Checked(object sender, RoutedEventArgs e)
		{
			PhotoStates.MethodIndex = 0;
		}

		private void MethodBRadioButton_Checked(object sender, RoutedEventArgs e)
		{
			PhotoStates.MethodIndex = 1;
		}

		private void MethodCRadioButton_Checked(object sender, RoutedEventArgs e)
		{
			PhotoStates.MethodIndex = 2;
		}

		private void ThetaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			PhotoStates.Theta = (float)ThetaSlider.Value;
			TransformImplement();
		}

		private void XSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			PhotoStates.X = (float)XSlider.Value;
			TransformImplement();
		}

		private void YSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			PhotoStates.Y = (float)YSlider.Value;
			TransformImplement();
		}

		private void PhotoCenterCheckBox_OnClick(object sender, RoutedEventArgs e)
		{
			PhotoStates.IsCentered = !PhotoStates.IsCentered;
			if (PhotoStates.IsCentered == true)
			{
				Canvas.SetLeft(EditedImage, 350/2-Center[2].X);
				Canvas.SetTop(EditedImage, 200 - Center[2].Y);
			}

			else
			{
				Canvas.SetLeft(EditedImage, 0);
				Canvas.SetTop(EditedImage, 0);
			}
			
		}

		private void FirstRotateCheckBox_OnClick(object sender, RoutedEventArgs e)
		{
			PhotoStates.IsFirstRotate = !PhotoStates.IsFirstRotate;
		}

		private void RunButton_OnClick(object sender, RoutedEventArgs e)
		{
			float bufferValue = 0;

			if (IsNumberic(XTextBox.Text, out bufferValue))
			{
				if (bufferValue > 5 || bufferValue < 0.1)
				{
					return;
				}
				else
				{
					PhotoStates.X = bufferValue;
					XSlider.Value = bufferValue;
					
				}
			}

			else
			{
				MessageBox.Show("呵呵哒，你给的数据有问题！请输入数字！");
				return;
			}


			if (IsNumberic(YTextBox.Text, out bufferValue))
			{
				if (bufferValue > 5 || bufferValue < 0.1)
				{
					return;
				}
				else
				{
					PhotoStates.Y = bufferValue;
					YSlider.Value = bufferValue;
				}
			}

			else
			{
				MessageBox.Show("呵呵哒，你给的数据有问题！请输入数字！");
				return;
			}

			if (IsNumberic(ThetaTextBox.Text, out bufferValue))
			{
				PhotoStates.Theta = bufferValue;
				ThetaSlider.Value = bufferValue % (2 * Math.PI);
			}

			else
			{
				MessageBox.Show("呵呵哒，你给的数据有问题！请输入数字！");
				return;
			}
			TransformImplement();
		}
	}
}
