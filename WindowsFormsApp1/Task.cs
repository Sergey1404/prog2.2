using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApp1
{
    public partial class Task : Form
    {
        public Task()
        {
            InitializeComponent();
        }

        //static double TruncateDecimal(double Value, int Accuracy)
        //{
        //   double Step = (double)Math.Pow(10, Accuracy);
        //   double Temp = Math.Truncate(Step * Value);
        //   return Temp / Step;
        //}

        void Draw(List<List<double>> Points, List<List<double>> MinimumPoints)
        {
            foreach (List<double> Point in Points)
            {
                if (Point[0] > 99999999 || Point[0] < -99999999 || Point[1] > 99999999 || Point[1] < -99999999 || Point[1] is double.NaN)
                {
                    continue;
                }
                else
                {
                    Chart.Series[0].Points.AddXY(Point[0], Point[1]);
                }
            }

            foreach (List<double> MinimumPoint in MinimumPoints)
            {
                Chart.Series[1].Points.AddXY(MinimumPoint[0], MinimumPoint[1]);
            }

            Chart.Series[0].Points.ResumeUpdates();
            Chart.Series[1].Points.ResumeUpdates();
        }

        #region Lists of Points

        List<double> GetPoint(MathParser parser, string Func, double x)
        {
            List<double> Point = new List<double>();
            string NowFunc = String.Copy(Func).ToLower();
            NowFunc = NowFunc.Replace("x", x.ToString());

            double y = parser.Parse(NowFunc, true);
            Point.Add(x);
            Point.Add(Math.Round(y, 8));

            return Point;
        }

        List<List<double>> GetPoints(MathParser parser, string Func, double Start, double End, double AmountPoints)
        {
            List<List<double>> Points = new List<List<double>>();
            List<double> xPoints = new List<double>();
            double x = Start;
            double Step = (End - Start) / AmountPoints;

            while (x <= End)
            {
                List<double> Point = GetPoint(parser, Func, x);
                x = Math.Round(x + Step, 4);
                if (Point[1] is double.NaN)
                {
                    continue;
                }
                Points.Add(Point);
                xPoints.Add(x);
            }

            if (!xPoints.Contains(End))
            {
                List<double> Point = GetPoint(parser, Func, End);
                Points.Add(Point);
                xPoints.Add(End);
            }

            return Points;
        }

        List<double> GetMinimumPoint(MathParser parser, string Func, double Start, double End, int Accuracy)
        {
            double x = 0;
            double y = 0;
            double Accur = 1 / Math.Pow(10, Accuracy);
            double Length = Accur / 2 - Accur / 4;

            do
            {
                x = (Start + End) / 2;
                double Startx = x - Length;
                double Endx = x + Length;
                double Starty = GetPoint(parser, Func, Startx)[1];
                double Endy =  GetPoint(parser, Func, Endx)[1];

                if (Starty is double.NaN)
                {
                    Starty = 0;
                }

                if (Endy is double.NaN)
                {
                    Endy = 0;
                }

                y = GetPoint(parser, Func, x)[1];

                if (Starty <= Endy)
                {
                    End = Endx;
                }
                else if (Starty > Endy)
                {
                    Start = Startx;
                }

            } while (Math.Abs(Start - End) >= Accur);

            List<double> MinimumPoint = new List<double>();
            MinimumPoint.Add(x);
            MinimumPoint.Add(y);

            return MinimumPoint;
        }

        List<List<double>> GetMinimumPoints(List<List<double>> Points, List<double> MinimumPoint, double Step)
        {
            List<List<double>> MinimumPoints = new List<List<double>>();
            double LastMinimumPointx = -999999;

            foreach (List<double> Point in Points)
            {
                double Diffx = Math.Abs(Point[0] - LastMinimumPointx);
                double Diffy = Math.Abs(Point[1] - MinimumPoint[1]);

                if (Diffy < 0.001)
                {
                    LastMinimumPointx = Point[0];
                    MinimumPoints.Add(Point);
                }
            }

            return MinimumPoints;
        }


        #endregion

        #region MenuItems

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Chart.Series[0].Points.Clear();
            Chart.Series[1].Points.Clear();

            #region Check

            bool IsDouble(string Line, string Name)
            {
                if (Double.TryParse(Line, out double Number))
                {
                    return true;
                }
                else
                {
                    MessageBox.Show($"Неправильный формат: {Name}");
                    return false;
                }
            }

            bool IsInt(string Line, string Name)
            {
                if (Int32.TryParse(Line, out int Number))
                {
                    if (Number >= 1 && Number <= 10)
                    {
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("Точность: 1-10 знаков после запятой");
                        return false;
                    }
                }
                else
                {
                    MessageBox.Show($"Неправильный формат: {Name}");
                    return false;
                }
            }

            #endregion

            string Func = InputFunc.Text.Replace("x", "(x)");
            bool A = IsDouble(InputA.Text, "a");
            bool B = IsDouble(InputB.Text, "b");
            bool Accuracy = IsInt(InputAccuracy.Text, "e");
            bool Formula = true;

            MathParser parser = new MathParser();

            try
            {
                List<double> TestPoint = GetPoint(parser, Func, 1);
            }
            catch
            {
                Formula = false;
                MessageBox.Show("Ошибка в формуле");
            }

            if (A && B && Accuracy && Formula)
            {
                double Start = Double.Parse(InputA.Text);
                double End = Double.Parse(InputB.Text);
                int Accur = Int32.Parse(InputAccuracy.Text);
                double AmountPoints = 10000;

                if (End > Start)
                {
                    try
                    {
                        List<List<double>> Points = GetPoints(parser, Func, Start, End, AmountPoints);
                        List<double> MinimumPoint = GetMinimumPoint(parser, Func, Start, End, Accur);
                        double Step = (End - Start) / AmountPoints;
                        List<List<double>> MinimumPoints = GetMinimumPoints(Points, MinimumPoint, Step);

                        Draw(Points, MinimumPoints);

                        if (MinimumPoint[1] is double.NaN)
                        {
                            Result.Text = "Min not found"; 
                        }
                        else
                        {
                            Result.Text = $"{MinimumPoint[0]}";
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Может быть, что-нибудь попроще");
                    }
                }
                else
                {
                    MessageBox.Show("Неправильный интервал");
                }
                

            }
        }
        private void cleanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Chart.Series[0].Points.Clear();
            Chart.Series[1].Points.Clear();
            InputFunc.Clear();
            InputA.Clear();
            InputB.Clear();
            InputAccuracy.Clear();
            Result.Text = "";
        }

        #endregion

    }
}