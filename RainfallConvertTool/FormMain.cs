using RainfallConvertTool.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RainfallConvertTool
{
    public partial class FormMain : Form
    {
        private string[][][] _contents = null;
        public FormMain()
        {
            InitializeComponent();
        }

        private void SetUnEnable()
        {
            btnStart.Enabled = false;
            btnRun.Enabled = true;
            btnExit.Enabled = true;
            groupBox1.Enabled = false;
            groupBox2.Enabled = false;
            groupBox3.Enabled = false;
            progressBar1.Visible = true;
        }

        private void SetEnable()
        {
            if (btnStart.InvokeRequired)
            {
                btnStart.Invoke(new Action(delegate
                {
                    btnStart.Enabled = true;
                    btnRun.Enabled = false;
                    btnExit.Enabled = false;
                    groupBox1.Enabled = true;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = true;
                    progressBar1.Visible = false;

                }));
            }
            else
            {
                btnStart.Enabled = true;
                btnRun.Enabled = false;
                btnExit.Enabled = false;
                groupBox1.Enabled = true;
                groupBox2.Enabled = true;
                groupBox3.Enabled = true;
                progressBar1.Visible = false;
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            textBox1.BindConsole();
            progressBar1.BindProgressBar();
            toolleftTime.BindLabel();
            SetDateTime();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "文本文件|*.txt";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = string.Join(";", dialog.FileNames);
                List<string[][]> result = new List<string[][]>();
                int lines = 0;
                foreach (var item in  dialog.FileNames)
                {
                    string[][] temp = TxtReader.Read(item);
                  result.Add(temp);
                  lines += temp.Count();
                }
                _contents = result.ToArray();
                nums.Maximum = lines;
                mune.Maximum = lines;
                lblResult.Text = string.Format("共{0}行数据,属于{1}字段数据", lines, _contents[0][0].Count());
            }
        }

        private void txtFilePath_TextChanged(object sender, EventArgs e)
        {
            btnStart.Enabled = (!radioButton1.Checked) || (File.Exists(txtFilePath.Text.Split(';')[0]) && radioButton1.Checked);
            SetDateTime();
        }

        private void SetDateTime()
        {
            DateTime[] result= RainfallUtility.GetConditionData(radioButton1.Checked ? 0 : (radioButton2.Checked ? 1 : 2));
            if (result != null)
            {
                dtpStart.Value = result[0];
                dtpEnd.Value = result[1];
            }
        }

        private void SaveDateTime()
        {
            RainfallUtility.SaveConditonData(radioButton1.Checked ? 0 : (radioButton2.Checked ? 1 : 2), dtpStart.Value, dtpEnd.Value);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            txtState.Enabled = checkBox2.Checked;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            dtpStart.Enabled = checkBox1.Checked;
            dtpEnd.Enabled = checkBox1.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            nums.Enabled = checkBox3.Checked;
            mune.Enabled = checkBox3.Checked;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (btnRun.Text == "暂停")
            {
                RainfallUtility.CurrentThread.Suspend();
                btnRun.Text = "继续";
            }
            else
            {
                RainfallUtility.CurrentThread.Resume();
                btnRun.Text = "暂停";
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            if (RainfallUtility.CurrentThread != null)
            {
                if ((RainfallUtility.CurrentThread.ThreadState & System.Threading.ThreadState.Suspended) != 0)
                    RainfallUtility.CurrentThread.Resume();
                RainfallUtility.CurrentThread.Abort();
                SetEnable();
                MyConsole.AppendLine("雨量数据入库停止了..");
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            SetUnEnable();
            int? start = Convert.ToInt32(nums.Value);
            int? end = Convert.ToInt32(mune.Value);
            if (checkBox3.Checked == false)
            {
                start = null;
                end = null;
            }

            DateTime? startT = dtpStart.Value;
            DateTime? endT = dtpEnd.Value;
            if (checkBox1.Checked == false)
            {
                startT = null;
                endT = null;
            }
            if (radioButton1.Checked)
            {
                foreach (var item in _contents)
                {
                    RainfallUtility.InsertMetaData(item, checkBox2.Checked ? txtState.Text : null, start, end, startT, endT, checkBox4.Checked, SetEnable);
                }
            }
            else if (radioButton2.Checked)
            {
                // RainfallUtility.StaticDataNew(checkBox2.Checked ? txtState.Text : null, startT, endT, SetEnable, checkBox4.Checked);
                // RainfallUtility.StaticData(checkBox2.Checked ? txtState.Text : null, startT, endT);
                RainfallUtility.BulkStaticData(startT, endT, SetEnable);
            }
            else
            {
                RainfallUtility.StaticMaxData(checkBox2.Checked ? txtState.Text : null, startT, endT, SetEnable);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (checkBox1.Checked)
                SaveDateTime();
        }
    }
}
