using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CurrencyVisualization
{
    public partial class ChartForm : Form
    {
        private Dictionary<string, List<DailyRates>> data;
        delegate void ArgReturningVoidDelegate();


        public ChartForm()
        {
            Debug.WriteLine("Chart Initialized");
            InitializeComponent();
            MyRefresh();
        }

        public void MyRefresh()
        {
            if (this.chart.InvokeRequired)
            {
                ArgReturningVoidDelegate d = new ArgReturningVoidDelegate(MyRefresh);
                this.Invoke(d);
            }
            else
            {
                PrepareChart();
                
                data = Program.allSymbols;
                

                int top = 10;
                int left = 150;

                lock(this)
                {
                    try
                    {
                        foreach (var item in data)
                        {
                            Button button = new Button();
                            button.Left = left;
                            button.Top = top;
                            button.Text = item.Key;
                            button.Click += (sender, e) => myButtonClickFunction(sender, e, item.Key);

                            this.Controls.Add(button);
                            left += button.Width + 5;
                        }
                    }
                    catch
                    {
                        MyRefresh();
                    }
                }

            }

        }

        void myButtonClickFunction(object sender, EventArgs e, string symbol)
        {
            MyRefresh();
            PrepareChart();
            
            var data = Program.allSymbols[symbol];

            double max = 0;
            double min = 10000;

            foreach (var item in data)
            {
                if (item.High > max) max = item.High;
                if (item.Low < min) min = item.Low;
            }

            chart.ChartAreas[0].AxisY.Maximum = max + 0.01;
            chart.ChartAreas[0].AxisY.Minimum = min - 0.01;

            chart.DataSource = data;
            chart.DataBind();
        }


        private void btnRefresh_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Refresh clicked");
            MyRefresh();
            PrepareChart();
        }

        private void PrepareChart()
        {
            chart.ChartAreas["ChartArea1"].AxisX.MajorGrid.LineWidth = 0;
            chart.ChartAreas["ChartArea1"].AxisY.MajorGrid.LineWidth = 0;

            chart.Series["Daily"].XValueMember = "Date";
            chart.Series["Daily"].YValueMembers = "High, Low, Open, Close";

            chart.Series["Daily"].XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Date;
            chart.Series["Daily"].CustomProperties = "PriceDownColor=Red,PriceUpColor=Blue";

            chart.Series["Daily"]["OpenCloseStyle"] = "Triangle";
            chart.Series["Daily"]["ShowOpenClose"] = "Both";
            chart.DataManipulator.IsStartFromFirst = true;
        }
    }
}
