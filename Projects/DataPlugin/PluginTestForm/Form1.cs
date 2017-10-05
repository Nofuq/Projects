using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DataPlugin;

namespace PluginTestForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DataSource.OnLogWrite += DataSourceOnOnLogWrite;
            var btns = BtnClass.GetSampleBtn();
            int top = 0;
            foreach (var myBtn in btns)
            {
                Button b  = new Button();
                b.Click += (o, args) =>
                {
                    richTextBox1.Text =
                        DataSource.GetColumn(myBtn.BtnGuid)
                            .Select(a => a.Key + " - " + a.Value)
                            .Aggregate((a1, b1) => a1 += b1 + "\n");
                    dataGridView1.DataSource = DataSource.GetDataTableType(1,myBtn.BtnGuid, "91.203.70.105:8110", "admin", "resto#test", new DateTime(DateTime.Now.Year, DateTime.Now.Month-1, 1), new DateTime(DateTime.Now.Year, DateTime.Now.Month-1, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month-1)));
                };
                b.Text = myBtn.BtnName;
                b.Top = top;
                top += 25;
                panel1.Controls.Add(b);
            }
        }

        private void DataSourceOnOnLogWrite(string guid, string message)
        {
            richTextBox2.Text += DateTime.Now.ToString() + " : " + guid + " - " + message + "\r\n";
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
