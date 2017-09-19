using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainfallConvertTool.Utility
{
    /// <summary>
    /// 读取元数据文件
    /// </summary>
    public class TxtReader
    {
        /// <summary>
        /// 读取Text的内容
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string[][] Read(string path)
        {
            List<string[]> result = new List<string[]>();
            string[] lines=File.ReadAllLines(path);
            foreach (string item in lines)
            {
                List<string> tempFilted = new List<string>();
                string[] lineConten=item.Split('\t');
                if(lineConten.Length<3)
                {
                    lineConten = item.Split(' ');
                }
                foreach (string v in lineConten)
                {
                    string temp = v.Trim();
                    if (!string.IsNullOrEmpty(temp))
                    {
                        tempFilted.Add(temp);
                    }
                }
                result.Add(tempFilted.ToArray());
            }

            return result.ToArray();

        }

    }
}
