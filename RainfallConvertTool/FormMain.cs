﻿using RainfallConvertTool.Utility;
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

        private void button1_Click(object sender, EventArgs e)
        {
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
            foreach (var item in _contents)
            {
                RainfallUtility.InsertMetaData(item, checkBox2.Checked ? txtState.Text : null, start, end, startT, endT);
            }       
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            textBox1.BindConsole();
            progressBar1.BindProgressBar();
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
            groupBox1.Enabled = File.Exists(txtFilePath.Text);
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

        private void button3_Click(object sender, EventArgs e)
        {
            DateTime? startT = dtpStart.Value;
            DateTime? endT = dtpEnd.Value;
            if (checkBox1.Checked == false)
            {
                startT = null;
                endT = null;
            }
            RainfallUtility.StaticDataNew(checkBox2.Checked ? txtState.Text : null, startT, endT);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DateTime? startT = dtpStart.Value;
            DateTime? endT = dtpEnd.Value;
            if (checkBox1.Checked == false)
            {
                startT = null;
                endT = null;
            }
            RainfallUtility.StaticMaxData(checkBox2.Checked ? txtState.Text : null, startT, endT);
        }
    }
}
