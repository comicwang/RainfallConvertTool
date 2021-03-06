﻿using RainfallConvertTool.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace RainfallConvertTool.Utility
{
    public class RainfallUtility
    {
        public static Thread CurrentThread;

        static int total = 0;
        static int successed = 0;
        static int failed = 0;

        /// <summary>
        /// 判断去重的委托方法
        /// </summary>
        static Func<RAINFALL_STATE, List<RAINFALL_STATE>, bool> rainfallFunc = (item, lstTtem) =>
          {
              var result = lstTtem.Where(t => t.RecordDate == item.RecordDate && t.MONITORNUM == item.MONITORNUM).FirstOrDefault();
              return result == null;
          };

        #region 基础数据入库

        /// <summary>
        /// 插入源数据
        /// </summary>
        /// <param name="content"></param>
        /// <param name="statelimit"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        public static void InsertMetaData(string[][] content, string statelimit, int? start, int? end, DateTime? startTime, DateTime? endTime,bool update,Action action)
        {
            int colsCount = content[0].Length;
            MyConsole.AppendLine(string.Format("共找到{0}行数据..", content.Count()));

            CurrentThread = new Thread(new ParameterizedThreadStart(delegate
            {
                DateTime cuurentDate = DateTime.Now;
                total = 0;
                successed = 0;
                failed = 0;
                List<RAINFALL_STATE> RainFallModels = new List<RAINFALL_STATE>();
                //52位数据 站点 年 月 日 21~23雨量 0~20雨量 21~23控制码 0~20控制码
                if (colsCount == 52)
                {
                    for (int index = 0; index < content.Count(); index++)
                    {
                        if (RainFallModels.Count>=20000)
                        {
                            InsertBulk(RainFallModels);
                        }
                        MyConsole.ShowProgress(index * 100 / content.Count());
                        //筛选条数
                        if ((start.HasValue && start > index) || (end.HasValue && end < index))
                            continue;
                        //区站号 -1
                        string state = content[index][0];
                        //筛选站点
                        if (string.IsNullOrEmpty(statelimit) == false && state != statelimit)
                            continue;
                        int year = int.Parse(content[index][1]); //年
                        int month = int.Parse(content[index][2]); //月
                        int date = int.Parse(content[index][3]); //日
                        for (int i = 0; i < 21; i++)
                        {
                            int control = int.Parse(content[index][i + 31]);

                            decimal? rainfall = null;
                            if (content[index][i + 7] == "32766")
                                rainfall = 0;
                            else if (int.Parse(content[index][i + 7]) == 9990)
                            {
                                control = 9990;
                                rainfall = 0;
                            }
                            else
                                rainfall = Decimal.Parse(content[index][i + 7]);
                            DateTime dateTemp = new DateTime(year, month, date, i, 0, 0);
                            //筛选时间
                            if ((startTime.HasValue && dateTemp < startTime) || (endTime.HasValue && dateTemp > endTime))
                                continue;
                            RAINFALL_STATE temp = new RAINFALL_STATE(state, null, null, null, dateTemp, rainfall, control);
                            //InsertToSql(temp, update);
                            if (update)
                            {
                                RainFallModels.Add(temp, rainfallFunc);
                            }
                            else
                                RainFallModels.Add(temp);
                            //Thread.Sleep(100);
                        }
                        for (int i = 21; i < 24; i++)
                        {
                            int control = int.Parse(content[index][i + 7]);
                            decimal? rainfall = null;
                            if (content[index][i - 17] == "32766")
                                rainfall = 0;
                            else if (int.Parse(content[index][i - 17]) == 9990)
                            {
                                control = 9990;
                                rainfall = 0;
                            }
                            else
                                rainfall = Decimal.Parse(content[index][i - 17]) ;
                            DateTime dateTemp = new DateTime(year, month, date, i, 0, 0);
                            //筛选时间
                            if ((startTime.HasValue && dateTemp < startTime) || (endTime.HasValue && dateTemp > endTime))
                                continue;

                            RAINFALL_STATE temp = new RAINFALL_STATE(state, null, null, null, dateTemp, rainfall, control);
                            if (update)
                            {
                                RainFallModels.Add(temp, rainfallFunc);
                            }
                            else
                                RainFallModels.Add(temp);
                            //InsertToSql(temp, update);
                            //Thread.Sleep(100);
                        }
                    }
                }
                //6字段     Sta       Lon       Lat     Alt      Date       Pre
                else if (colsCount == 6)
                {
                    for (int index = 1; index < content.Count(); index++)
                    {
                        if (RainFallModels.Count >= 20000)
                        {
                            InsertBulk(RainFallModels);
                        }
                        MyConsole.ShowProgress(index * 100 / content.Count());
                        //筛选条数
                        if ((start.HasValue && start > index) || (end.HasValue && end < index))
                            continue;
                        //区站号 -1
                        string state = content[index][0];
                        //筛选站点
                        if (string.IsNullOrEmpty(statelimit) == false && state != statelimit)
                            continue;
                        int value = 0;  //控制码默认为0
                        decimal? key = Decimal.Parse(content[index][5]);
                        if (key == 32766)
                        {
                            value = 32766;
                            key = 0;
                        }
                        else if (key == 9990)
                        {
                            key = 0;
                            value = 9990;
                        }
                        DateTime dateTemp = DateTime.MinValue;
                        RAINFALL_STATE temp = null;
                        if (content[0][1] != "Lon")
                        {
                            dateTemp = new DateTime(int.Parse(content[index][1]), int.Parse(content[index][2]), int.Parse(content[index][3]), int.Parse(content[index][4]), 0, 0);
                            temp = new RAINFALL_STATE(state, 0, 0, 0, dateTemp, key, value);
                        }
                        else
                        {
                            dateTemp = DateTime.ParseExact(content[index][4], "yyyyMMddHH", null);
                            temp = new RAINFALL_STATE(state, Decimal.Parse(content[index][1]), Decimal.Parse(content[index][2]), Decimal.Parse(content[index][3]), dateTemp, key * 10, value);
                        }
                        //筛选时间
                        if ((startTime.HasValue && dateTemp < startTime) || (endTime.HasValue && dateTemp > endTime))
                            continue;
                        if (update)
                        {
                            RainFallModels.Add(temp, rainfallFunc);
                        }
                        else
                            RainFallModels.Add(temp);
                        //InsertToSql(temp, update);
                        //Thread.Sleep(100);
                    }
                }
                //3字段 DEVICECODE	RAINFALL	MONITORTIME
                else if (colsCount == 3)
                {
                    for (int index = 1; index < content.Count(); index++)
                    {
                        if (RainFallModels.Count >= 20000)
                        {
                            InsertBulk(RainFallModels);
                        }
                        MyConsole.ShowProgress(index * 100 / content.Count());
                        //筛选条数
                        if ((start.HasValue && start > index) || (end.HasValue && end < index))
                            continue;
                        //区站号 -1
                        string state = content[index][0];
                        //筛选站点
                        if (string.IsNullOrEmpty(statelimit) == false && state != statelimit)
                            continue;
                        int value = 0;  //控制码默认为0
                        decimal? key = Decimal.Parse(content[index][1]);
                        DateTime dateTemp = DateTime.Parse(content[index][2]);
                        //筛选时间
                        if ((startTime.HasValue && dateTemp < startTime) || (endTime.HasValue && dateTemp > endTime))
                            continue;
                        RAINFALL_STATE temp = new RAINFALL_STATE(state, null, null, null, dateTemp, key, value);
                        if (update)
                        {
                            RainFallModels.Add(temp, rainfallFunc);
                        }
                        else
                            RainFallModels.Add(temp);
                        //InsertToSql(temp, update);
                        //Thread.Sleep(100);
                    }
                }
                InsertBulk(RainFallModels);
                MyConsole.AppendLine(string.Format("导入数据完成，成功{0}条，失败{1}条，共{2}条，耗时{3}秒",successed,failed,total,(DateTime.Now-cuurentDate).TotalSeconds));
                action.Invoke();

            }));
            CurrentThread.IsBackground = true;
            CurrentThread.Start();
        }

        /// <summary>
        /// 批量插入数据
        /// </summary>
        /// <param name="RainFallModels"></param>
        private static void InsertBulk(List<RAINFALL_STATE> RainFallModels)
        {
            try
            {
                total += RainFallModels.Count;
                SqlHelper.GetConnection().BulkCopy(RainFallModels, RainFallModels.Count, "RAINFALL_STATE", 3600);
                successed += RainFallModels.Count;
            }
            catch
            {
                failed += RainFallModels.Count;
            }
            RainFallModels.Clear();
        }

        /// <summary>
        /// 将读取的元数据插入数据库
        /// </summary>
        /// <param name="model"></param>
        private static void InsertToSql(RAINFALL_STATE model, bool update)
        {
            total++;
            try
            {
                string commandText = string.Empty;
                int line = -1;
                if (update)
                {
                    //先判断数据是否存在，存在更新，不存在新增
                    commandText = string.Format("select * from [DB_RainMonitor].[dbo].[RAINFALL_STATE] where MONITORNUM='{0}' and RecordDate='{1}'", model.MONITORNUM, model.RecordDate);
                    object obj = SqlHelper.ExecuteScalar(SqlHelper.GetConnSting(), System.Data.CommandType.Text, commandText);
                    if (obj != null)
                    {
                        commandText = string.Format("update RAINFALL_STATE set RAINFALL={0} where MONITORNUM='{1}' and RecordDate='{2}'", model.RAINFALL, model.MONITORNUM, model.RecordDate);
                        line = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnSting(), System.Data.CommandType.Text, commandText);
                        MyConsole.AppendLine(string.Format("更新数据站点{0},时间{1}{2}..", model.MONITORNUM, model.RecordDate, line == 1 ? "成功" : "失败"));
                        if (line == 1)
                            successed++;
                        else
                            failed++;
                        return;
                    }
                }
                commandText = "insert into [DB_RainMonitor].[dbo].[RAINFALL_STATE] values(@GUID,@MONITORNUM,@LON,@LAT,@ALT,@RecordDate,@RAINFALL,@Controller)";
                //commandText = "InsertMetaData";  //存储过程名称
                List<SqlParameter> result = new List<SqlParameter>();
                SqlParameter param = new SqlParameter("@GUID", model.GUID);
                param.DbType = System.Data.DbType.StringFixedLength;
                result.Add(param);
                SqlParameter param1 = new SqlParameter("@MONITORNUM", model.MONITORNUM);
                param1.DbType = System.Data.DbType.String;
                result.Add(param1);
                SqlParameter param2 = new SqlParameter("@LON", model.LON);
                param2.DbType = System.Data.DbType.Double;
                result.Add(param2);
                SqlParameter param3 = new SqlParameter("@LAT", model.LAT);
                param3.DbType = System.Data.DbType.Double;
                result.Add(param3);
                SqlParameter param4 = new SqlParameter("@ALT", model.ALT);
                param4.DbType = System.Data.DbType.Double;
                result.Add(param4);
                SqlParameter param5 = new SqlParameter("@RecordDate", model.RecordDate);
                param5.DbType = System.Data.DbType.DateTime;
                result.Add(param5);
                SqlParameter param6 = new SqlParameter("@RAINFALL", model.RAINFALL);
                param6.DbType = System.Data.DbType.Double;
                result.Add(param6);
                SqlParameter param7 = new SqlParameter("@Controller", model.Controller);
                param7.DbType = System.Data.DbType.Int32;
                result.Add(param7);
                line = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnSting(), System.Data.CommandType.Text, commandText, result.ToArray());

                MyConsole.AppendLine(string.Format("导入数据站点{0},时间{1}{2}..", model.MONITORNUM, model.RecordDate, line == 1 ? "成功" : "失败"));
                if (line == 1)
                    successed++;
                else
                    failed++;
            }
            catch (Exception ex)
            {
                MyConsole.AppendLine(string.Format("导入数据站点{0},时间{1}异常：{2}..", model.MONITORNUM, model.RecordDate, ex.Message));
                failed++;
            }
        }

        private static void InsertToSqllite(RAINFALL_STATE model)
        {
            try
            {
                string commandText = string.Format("insert into RAINFALL_STATE values('{0}','{1}',{2},{3},{4},'{5}',{6},{7})", model.GUID, model.MONITORNUM, model.LON, model.LAT, model.ALT, model.RecordDate.ToString("yyyy-MM-dd hh:mm:ss"), model.RAINFALL, model.Controller);
             
                int line = SQLiteHelper.ExecuteNonQuery(commandText);

                MyConsole.AppendLine(string.Format("导入数据站点{0},时间{1}{2}..", model.MONITORNUM, model.RecordDate, line == 1 ? "成功" : "失败"));
            }
            catch (Exception ex)
            {
                MyConsole.AppendLine(string.Format("导入数据站点{0},时间{1}异常：{2}..", model.MONITORNUM, model.RecordDate, ex.Message));
            }
        }

        #endregion

        #region 统计单点小时和天雨量数据

        #region 旧版效率低

        public static void StaticData(string statelimit, DateTime? startTime, DateTime? endTime)
        {
            CurrentThread = new Thread(new ParameterizedThreadStart(delegate
            {
                List<string[]> states = new List<string[]>();
                if (string.IsNullOrEmpty(statelimit) == false)
                {
                    //查询所有站点
                    string sql = " SELECT MONITORNUM,LON,LAT,ALT FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where MONITORNUM='" + statelimit + "' group by MONITORNUM,LON,LAT,ALT";
                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        states.Add(new string[] { item[0].ToString(), item[1].ToString(), item[2].ToString(), item[3].ToString() });
                        break;
                    }
                }
                else
                {
                    //查询所有站点
                    string sql = " SELECT MONITORNUM,LON,LAT,ALT FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] group by MONITORNUM,LON,LAT,ALT";
                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        states.Add(new string[] { item[0].ToString(), item[1].ToString(), item[2].ToString(), item[3].ToString() });
                    }
                }
                List<RAINFALL_HOUR> lstRain_hour = new List<RAINFALL_HOUR>();
                List<RAINFALL_DAY> lstRain_day = new List<RAINFALL_DAY>();
                StringBuilder hourStr = new StringBuilder();
                StringBuilder dayStr = new StringBuilder();
                int index = 0;
                foreach (string[] state in states)
                {
                    index++;
                    MyConsole.ShowProgress(index * 100 / states.Count());
                    //查询需要统计时间范围
                    string commandText = "select MAX(RecordDate),MIN(RecordDate) FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where MONITORNUM='" + state[0] + "'";

                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commandText);
                    DateTime maxDate = DateTime.Parse(ds.Tables[0].Rows[0][0].ToString());
                    DateTime minDate = DateTime.Parse(ds.Tables[0].Rows[0][1].ToString());
                    //筛选时间
                    if (startTime.HasValue && minDate < startTime)
                        minDate = startTime.Value;
                    if (endTime.HasValue && maxDate > endTime)
                        maxDate = endTime.Value;

                    MyConsole.AppendLine(string.Format("统计站点{2}开始时间为{0},结束时间为{1}..", minDate, maxDate,state[0]));

                    decimal lon = string.IsNullOrEmpty(state[1]) ? 0 : Convert.ToDecimal(state[1]);
                    decimal lat = string.IsNullOrEmpty(state[2]) ? 0 : Convert.ToDecimal(state[2]);
                    decimal alt = string.IsNullOrEmpty(state[3]) ? 0 : Convert.ToDecimal(state[3]);

                    //年循环
                    for (int year = minDate.Year; year <= maxDate.Year; year++)
                    {
                        if (lstRain_day.Count >= 200)
                        {
                            GetDayModel(lstRain_day, dayStr);
                            SqlHelper.GetConnection().BulkCopy(lstRain_day, lstRain_day.Count, "RAINFALL_DAY", 3600);

                            dayStr.Clear();
                            lstRain_day.Clear();
                        }
                        //月循环
                        for (int month = 1; month < 13; month++)
                        {
                            //天循环
                            for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
                            {
                                if (lstRain_hour.Count > 200)
                                {
                                    GetHourModel(lstRain_hour, hourStr);
                                    SqlHelper.GetConnection().BulkCopy(lstRain_hour, lstRain_hour.Count, "RAINFALL_HOUR", 3600);

                                    hourStr.Clear();
                                    lstRain_hour.Clear();
                                }
                                //开始统计天 1,3,5,7,15,30
                                DateTime currentDate = new DateTime(year, month, day);
                                lstRain_day.Add(ExcuteStaticDay(currentDate, state[0], lon, lat, alt, ref dayStr));
                               // commandText = string.Empty;
                                //时循环
                                for (int hour = 0; hour < 24; hour++)
                                {
                                    currentDate = new DateTime(year, month, day, hour, 0, 0);

                                    MyConsole.AppendLine(string.Format("加入统计站点{1},统计时间{0}..", currentDate, state[0]));
                                    lstRain_hour.Add(ExcuteStaticHour(currentDate, state[0], lon, lat, alt, ref hourStr));
                                }
                            }
                        }
                    }
                }
                GetDayModel(lstRain_day, dayStr);
                SqlHelper.GetConnection().BulkCopy(lstRain_day, lstRain_day.Count, "RAINFALL_DAY", 3600);

                dayStr.Clear();
                lstRain_day.Clear();

                GetHourModel(lstRain_hour, hourStr);
                SqlHelper.GetConnection().BulkCopy(lstRain_hour, lstRain_hour.Count, "RAINFALL_HOUR", 3600);

                hourStr.Clear();
                lstRain_hour.Clear();

            }));
            CurrentThread.IsBackground = true;
            CurrentThread.Start();
        }

        private static RAINFALL_DAY ExcuteStaticDay(DateTime endDate, string state, decimal lon, decimal lat, decimal alt, ref StringBuilder commandText)
        {
            //插入一个站点的一天的天统计数据
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Day, endDate, state));
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Day_3, endDate, state));
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Day_5, endDate, state));
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Day_7, endDate, state));
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Day_15, endDate, state));
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Day_30, endDate, state));

            RAINFALL_DAY rain_day = new RAINFALL_DAY();
            rain_day.GUID = new Guid().ToString();
            rain_day.TIME = endDate;
            rain_day.MONITORNUM = state;
            rain_day.LAT = lat;
            rain_day.LON = lon;
            rain_day.ALT = alt;

            return rain_day;
        }

        private static List<RAINFALL_DAY> GetDayModel(List<RAINFALL_DAY> dayModels,StringBuilder commandText)
        {
            DataSet sums = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commandText.ToString());

            for (int i = 0; i < dayModels.Count; i++)
            {
                //decimal? hour = null;
                if (sums.Tables[0].Rows[0][0].GetType() != typeof(DBNull))
                {
                    dayModels[i].RAINFALL_1_DAY = Convert.ToDecimal(sums.Tables[i*6].Rows[0][0]);
                    dayModels[i].RAINFALL_1_DAY_C = int.Parse(sums.Tables[i * 6].Rows[0][1].ToString());
                }
                //decimal? hour_3 = null;
                if (sums.Tables[1].Rows[0][0].GetType() != typeof(DBNull))
                {
                    dayModels[i].RAINFALL_3_DAY = Convert.ToDecimal(sums.Tables[i * 6 + 1].Rows[0][0]);
                    dayModels[i].RAINFALL_3_DAY_C = int.Parse(sums.Tables[i * 6 + 1].Rows[0][1].ToString());
                }
                //decimal? hour_6 = null;
                if (sums.Tables[2].Rows[0][0].GetType() != typeof(DBNull))
                {
                    dayModels[i].RAINFALL_5_DAY = Convert.ToDecimal(sums.Tables[i * 6 + 2].Rows[0][0]);
                    dayModels[i].RAINFALL_5_DAY_C = int.Parse(sums.Tables[i * 6 + 2].Rows[0][1].ToString());
                }
                //decimal? hour_12 = null;
                if (sums.Tables[3].Rows[0][0].GetType() != typeof(DBNull))
                {
                    dayModels[i].RAINFALL_7_DAY = Convert.ToDecimal(sums.Tables[i * 6 + 3].Rows[0][0]);
                    dayModels[i].RAINFALL_7_DAY_C = int.Parse(sums.Tables[i * 6 + 3].Rows[0][1].ToString());
                }
                // decimal? hour_24 = null;
                if (sums.Tables[4].Rows[0][0].GetType() != typeof(DBNull))
                {
                    dayModels[i].RAINFALL_15_DAY = Convert.ToDecimal(sums.Tables[i * 6 + 4].Rows[0][0]);
                    dayModels[i].RAINFALL_15_DAY_C = int.Parse(sums.Tables[i * 6 + 4].Rows[0][1].ToString());
                }
                // decimal? hour_48 = null;
                if (sums.Tables[5].Rows[0][0].GetType() != typeof(DBNull))
                {
                    dayModels[i].RAINFALL_30_DAY = Convert.ToDecimal(sums.Tables[i * 6 + 5].Rows[0][0]);
                    dayModels[i].RAINFALL_30_DAY_C = int.Parse(sums.Tables[i * 6 + 5].Rows[0][1].ToString());
                }

            }
         
            return dayModels;
        }

        private static RAINFALL_HOUR ExcuteStaticHour(DateTime endDate, string state, decimal lon, decimal lat, decimal alt,ref StringBuilder commandText)
        {
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Hour, endDate, state));
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Hour_3, endDate, state));
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Hour_6, endDate, state));
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Hour_12, endDate, state));
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Hour_24, endDate, state));
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Hour_48, endDate, state));
            commandText.AppendLine(GetStaticsResult(StaticsEnum.Hour_72, endDate, state));
            RAINFALL_HOUR rain_hour = new RAINFALL_HOUR();
            rain_hour.GUID = new Guid().ToString();
            rain_hour.TIME = endDate;
            rain_hour.MONITORNUM = state;
            rain_hour.LAT = lat;
            rain_hour.LON = lon;
            rain_hour.ALT = alt;
            return rain_hour;
        }

        private static void GetHourModel(List<RAINFALL_HOUR> hourModels,StringBuilder commandText)
        {

            DataSet sums = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commandText.ToString());
            for (int i = 0; i < hourModels.Count; i++)
            {
                //decimal? hour = null;
                if (sums.Tables[0].Rows[0][0].GetType() != typeof(DBNull))
                {
                    hourModels[i].RAINFALL_1_HOUR = Convert.ToDecimal(sums.Tables[i*7].Rows[0][0]);
                    hourModels[i].RAINFALL_1_HOUR_C = int.Parse(sums.Tables[i * 7].Rows[0][1].ToString());
                }
                //decimal? hour_3 = null;
                if (sums.Tables[1].Rows[0][0].GetType() != typeof(DBNull))
                {
                    hourModels[i].RAINFALL_3_HOUR = Convert.ToDecimal(sums.Tables[i * 7 + 1].Rows[0][0]);
                    hourModels[i].RAINFALL_3_HOUR_C = int.Parse(sums.Tables[i * 7 + 1].Rows[0][1].ToString());
                }
                // decimal? hour_6 = null;
                if (sums.Tables[2].Rows[0][0].GetType() != typeof(DBNull))
                {
                    hourModels[i].RAINFALL_6_HOUR = Convert.ToDecimal(sums.Tables[i * 7 + 2].Rows[0][0]);
                    hourModels[i].RAINFALL_6_HOUR_C = int.Parse(sums.Tables[i * 7 + 2].Rows[0][1].ToString());
                }
                //decimal? hour_12 = null;
                if (sums.Tables[3].Rows[0][0].GetType() != typeof(DBNull))
                {
                    hourModels[i].RAINFALL_12_HOUR = Convert.ToDecimal(sums.Tables[i * 7 + 3].Rows[0][0]);
                    hourModels[i].RAINFALL_12_HOUR_C = int.Parse(sums.Tables[i * 7 + 3].Rows[0][1].ToString());
                }
                //decimal? hour_24 = null;
                if (sums.Tables[4].Rows[0][0].GetType() != typeof(DBNull))
                {
                    hourModels[i].RAINFALL_24_HOUR = Convert.ToDecimal(sums.Tables[i * 7 + 4].Rows[0][0]);
                    hourModels[i].RAINFALL_24_HOUR_C = int.Parse(sums.Tables[i * 7 + 4].Rows[0][1].ToString());
                }
                // decimal? hour_48 = null;
                if (sums.Tables[5].Rows[0][0].GetType() != typeof(DBNull))
                {
                    hourModels[i].RAINFALL_48_HOUR = Convert.ToDecimal(sums.Tables[i * 7 + 5].Rows[0][0]);
                    hourModels[i].RAINFALL_48_HOUR_C = int.Parse(sums.Tables[i * 7 + 5].Rows[0][1].ToString());
                }
                //decimal? hour_72 = null;
                if (sums.Tables[6].Rows[0][0].GetType() != typeof(DBNull))
                {
                    hourModels[i].RAINFALL_72_HOUR = Convert.ToDecimal(sums.Tables[i * 7 + 6].Rows[0][0]);
                    hourModels[i].RAINFALL_72_HOUR_C = int.Parse(sums.Tables[i * 7 + 6].Rows[0][1].ToString());
                }
            }
        }

        private static string GetStaticsResult(StaticsEnum hourEnum, DateTime endTime, string state)
        {
            DateTime startDate = endTime;
            switch (hourEnum)
            {
                case StaticsEnum.Hour:
                    startDate = startDate.AddHours(-1);
                    break;
                case StaticsEnum.Hour_3:
                    startDate = startDate.AddHours(-3);
                    break;
                case StaticsEnum.Hour_6:
                    startDate = startDate.AddHours(-6);
                    break;
                case StaticsEnum.Hour_12:
                    startDate = startDate.AddHours(-12);
                    break;
                case StaticsEnum.Hour_24:
                    startDate = startDate.AddDays(-1);
                    break;
                case StaticsEnum.Hour_48:
                    startDate = startDate.AddDays(-2);
                    break;
                case StaticsEnum.Hour_72:
                    startDate = startDate.AddDays(-3);
                    break;
                case StaticsEnum.Day:
                    startDate = startDate.AddDays(-1);
                    break;
                case StaticsEnum.Day_3:
                    startDate = startDate.AddDays(-3);
                    break;
                case StaticsEnum.Day_5:
                    startDate = startDate.AddDays(-5);
                    break;
                case StaticsEnum.Day_7:
                    startDate = startDate.AddDays(-7);
                    break;
                case StaticsEnum.Day_15:
                    startDate = startDate.AddDays(-15);
                    break;
                case StaticsEnum.Day_30:
                    startDate = startDate.AddMonths(-1);
                    break;
                default:
                    break;
            }
            return string.Format("SELECT SUM(RAINFALL) as sum,SUM(Controller) as controller FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{0}' and RecordDate<='{1}' and MONITORNUM='{2}'", startDate, endTime, state);

          
        }

        private static decimal? ConvertDecimal(object obj)
        {
            decimal result = 0;
            bool successed= decimal.TryParse(obj.ToString(), out result);
            if (successed)
                return result;
            return null;
        }

        #endregion

        #region 单小时添加

        /// <summary>
        /// 插入统计小时数据和天数据
        /// </summary>
        /// <param name="statelimit"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        public static void StaticDataNew(string statelimit, DateTime? startTime, DateTime? endTime, Action action, bool update = false)
        {
            CurrentThread = new Thread(new ParameterizedThreadStart(delegate
            {
                DateTime currentDateTime = DateTime.Now;
                List<string> states = new List<string>();
                string totalStr = string.Empty;
                if (string.IsNullOrEmpty(statelimit) == false)
                {
                    //查询所有站点
                    string sql = " SELECT MONITORNUM FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where MONITORNUM='" + statelimit + "' group by MONITORNUM";
                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        states.Add(item[0].ToString());
                        break;
                    }
                    totalStr = "select count(*) from [DB_RainMonitor].[dbo].[RAINFALL_STATE] where MONITORNUM='" + statelimit + "'";
                }
                else
                {
                    //查询所有站点
                    string sql = " SELECT MONITORNUM FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] group by MONITORNUM";
                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        states.Add(item[0].ToString());
                    }
                    totalStr = "select count(*) from [DB_RainMonitor].[dbo].[RAINFALL_STATE] ";
                }
                DataSet dsTotal = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, totalStr);

                int total = int.Parse(dsTotal.Tables[0].Rows[0][0].ToString());
               


                int index = 0;

                foreach (string state in states)
                {
                    //查询需要统计时间范围
                    string commandText = "select MAX(RecordDate),MIN(RecordDate) FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where MONITORNUM='" + state + "'";

                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commandText);
                    DateTime maxDate = DateTime.Parse(ds.Tables[0].Rows[0][0].ToString());
                    DateTime minDate = DateTime.Parse(ds.Tables[0].Rows[0][1].ToString());
                    //筛选时间
                    if (startTime.HasValue && minDate < startTime)
                        minDate = startTime.Value;
                    if (endTime.HasValue && maxDate > endTime)
                        maxDate = endTime.Value;

                    MyConsole.AppendLine(string.Format("统计开始时间为{0},结束时间为{1}..", minDate, maxDate));

                    #region 查询所有非空雨量的值，进行统计

                    commandText = "select RecordDate from RAINFALL_STATE where MONITORNUM='" + state + "'" + " and RAINFALL!=0 order by RecordDate";

                    ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commandText);
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        DateTime currentTime = DateTime.Parse(item[0].ToString());
                        //开始统计天 1,3,5,7,15,30
                        if (currentTime < minDate || currentTime > maxDate)
                            continue;
                        InsertDay(state, currentTime, update);
                        InsertHour(state, currentTime, update);
                        index++;
                        MyConsole.ShowProgress(index * 100 / total);
                        int leftCount = total - index;
                        string text = string.Empty;
                        if (leftCount / 36000 > 0)
                            text = string.Format("{0}小时", leftCount / 18000);
                        else if (leftCount / 600 > 0)
                            text = string.Format("{0}分钟", leftCount / 300);
                        else
                            text = string.Format("{0}秒", leftCount / 5);
                        MyConsole.ShowLabel(string.Format("共{0}条统计数据,剩余{1}条，大概需要{2}", total, leftCount, text));
                    }                   
                    #endregion

                    #region 历史统计(作废)

                    ////年循环
                    //for (int year = minDate.Year; year <= maxDate.Year; year++)
                    //{
                    //    //月循环
                    //    for (int month = 1; month < 13; month++)
                    //    {
                    //        //天循环
                    //        for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
                    //        {
                    //            //开始统计天 1,3,5,7,15,30
                    //            DateTime currentDate = new DateTime(year, month, day);
                    //            if (currentDate < minDate || currentDate > maxDate)
                    //                continue;
                    //            InsertDay(state, currentDate);
                    //            commandText = string.Empty;
                    //            //时循环
                    //            for (int hour = 0; hour < 24; hour++)
                    //            {
                    //                currentDate = new DateTime(year, month, day, hour, 0, 0);
                    //                if (currentDate < minDate || currentDate > maxDate)
                    //                    continue;
                    //                InsertHour(state, currentDate);
                    //            }
                    //        }
                    //    }
                    //}

                    #endregion
                }
                MyConsole.AppendLine("统计结束,耗时" + (DateTime.Now - currentDateTime).TotalSeconds + "秒");
                action.Invoke();

            }));
            CurrentThread.IsBackground = true;
            CurrentThread.Start();
        }

        /// <summary>
        /// 插入小时数据
        /// </summary>
        /// <param name="state"></param>
        /// <param name="endDate"></param>
        private static void InsertHour(string state, DateTime endDate,bool update)
        {
            string commandText = string.Empty;
            if (update)
            {
                try
                {
                    //存在就删除
                    commandText = string.Format("SELECT * FROM [DB_RainMonitor].[dbo].[RAINFALL_HOUR] where MONITORNUM='{0}' and TIME='{1}'", state, endDate);
                    object obj = SqlHelper.ExecuteScalar(SqlHelper.GetConnSting(), System.Data.CommandType.Text, commandText);
                    if (obj != null)
                    {
                        commandText = string.Format("delete FROM [DB_RainMonitor].[dbo].[RAINFALL_HOUR] where MONITORNUM='{0}' and TIME='{1}'", state, endDate);
                        int result = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnSting(), System.Data.CommandType.Text, commandText);
                        MyConsole.AppendLine(string.Format("删除一条小时统计数据:站点{0},时间{1}{2}..", state, endDate, result == 0 ? "失败" : "成功"));
                    }
                }
                catch (Exception ex)
                {
                    MyConsole.AppendLine("执行数据库失败：" + ex.Message);
                }
            }
            try
            {
                //结束时间，站号，1，3，6，12，24，48，72
                commandText = string.Format("insert into [DB_RainMonitor].[dbo].[RAINFALL_HOUR] select NEWID(),* from (((((((SELECT '{1}' as MONITORNUM,SUM(LON) as LON,SUM(LAT) as LAT,SUM(ALT) AS ALT,'{0}' as TIME,SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{2}' and RecordDate<='{0}' and MONITORNUM='{1}') as t1 right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{3}' and RecordDate<='{0}' and MONITORNUM='{1}') as t2 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{4}' and RecordDate<='{0}' and MONITORNUM='{1}') as t3 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{5}' and RecordDate<='{0}' and MONITORNUM='{1}') as t4 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{6}' and RecordDate<='{0}' and MONITORNUM='{1}') as t5 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{7}' and RecordDate<='{0}' and MONITORNUM='{1}') as t6 on 1=1) right join (SELECT SUM(RAINFALL) as sum1,0 as c1,0 as c2,0 as c3,0 as c4,0 as c5,0 as c6,0 as c7 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{8}' and RecordDate<='{0}' and MONITORNUM='{1}') as t7 on 1=1)", endDate, state, endDate.AddHours(-1), endDate.AddHours(-3), endDate.AddHours(-6), endDate.AddHours(-12), endDate.AddHours(-24), endDate.AddHours(-48), endDate.AddHours(-72));

                int result = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnection(), CommandType.Text, commandText);
                MyConsole.AppendLine(string.Format("插入一条小时统计数据:站点{0},时间{1}{2}..", state, endDate, result == 0 ? "失败" : "成功"));
            }
            catch (Exception ex)
            {
                MyConsole.AppendLine(string.Format("插入一条小时统计数据:站点{0},时间{1}失败-{2}..", state, endDate, ex.Message));
            }
        }

        /// <summary>
        /// 插入天数据
        /// </summary>
        /// <param name="state"></param>
        /// <param name="endDate"></param>
        private static void InsertDay(string state, DateTime endDate,bool update=false)
        {
            string commandText = string.Empty;
            if (update)
            {
                try
                {
                    //存在就删除
                    commandText = string.Format("SELECT * FROM [DB_RainMonitor].[dbo].[RAINFALL_DAY] where MONITORNUM='{0}' and TIME='{1}'", state, endDate);
                    object obj = SqlHelper.ExecuteScalar(SqlHelper.GetConnSting(), System.Data.CommandType.Text, commandText);
                    if (obj != null)
                    {
                        commandText = string.Format("delete FROM [DB_RainMonitor].[dbo].[RAINFALL_DAY] where MONITORNUM='{0}' and TIME='{1}'", state, endDate);
                        int result = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnSting(), System.Data.CommandType.Text, commandText);
                        MyConsole.AppendLine(string.Format("删除一条天统计数据:站点{0},时间{1}{2}..", state, endDate, result == 0 ? "失败" : "成功"));
                    }
                }
                catch (Exception ex)
                {
                    MyConsole.AppendLine("执行数据库失败：" + ex.Message);
                }
            }

            //结束时间，站号，1，3，6，12，24，48，72
            commandText = string.Format("insert into [DB_RainMonitor].[dbo].[RAINFALL_DAY] select NEWID(),* from ((((((SELECT '{1}' as MONITORNUM,SUM(LON) as LON,SUM(LAT) as LAT,SUM(ALT) AS ALT,'{0}' as TIME,SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{2}' and RecordDate<='{0}' and MONITORNUM='{1}') as t1 right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{3}' and RecordDate<='{0}' and MONITORNUM='{1}') as t2 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{4}' and RecordDate<='{0}' and MONITORNUM='{1}') as t3 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{5}' and RecordDate<='{0}' and MONITORNUM='{1}') as t4 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{6}' and RecordDate<='{0}' and MONITORNUM='{1}') as t5 on 1=1) right join (SELECT SUM(RAINFALL) as sum1,0 as c1,0 as c2,0 as c3,0 as c4,0 as c5,0 as c6 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{7}' and RecordDate<='{0}' and MONITORNUM='{1}') as t6 on 1=1)", endDate, state, endDate.AddDays(-1), endDate.AddDays(-3), endDate.AddDays(-5), endDate.AddDays(-7), endDate.AddDays(-15), endDate.AddDays(-30));

            try
            {
                int result = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnection(), CommandType.Text, commandText);
                MyConsole.AppendLine(string.Format("插入一条天统计数据:站点{0},时间{1}{2}..", state, endDate, result == 0 ? "失败" : "成功"));
            }
            catch (Exception ex)
            {
                MyConsole.AppendLine(string.Format("插入一条天统计数据:站点{0},时间{1}失败-{2}..", state, endDate, ex.Message));
            }
        }

        #endregion

        #region 24小时一次插入

        /// <summary>
        /// 24小时一次插入
        /// </summary>
        /// <param name="statelimit"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        public static void StaticDataAllNew(string statelimit, DateTime? startTime, DateTime? endTime)
        {
            CurrentThread = new Thread(new ParameterizedThreadStart(delegate
            {
                List<string> states = new List<string>();
                if (string.IsNullOrEmpty(statelimit) == false)
                {
                    //查询所有站点
                    string sql = " SELECT MONITORNUM FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where MONITORNUM='" + statelimit + "' group by MONITORNUM";
                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        states.Add(item[0].ToString());
                        break;
                    }
                }
                else
                {
                    //查询所有站点
                    string sql = " SELECT MONITORNUM FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] group by MONITORNUM";
                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        states.Add(item[0].ToString());
                    }
                }

                foreach (string state in states)
                {
                    //查询需要统计时间范围
                    string commandText = "select MAX(RecordDate),MIN(RecordDate) FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where MONITORNUM='" + state + "'";

                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commandText);
                    DateTime maxDate = DateTime.Parse(ds.Tables[0].Rows[0][0].ToString());
                    DateTime minDate = DateTime.Parse(ds.Tables[0].Rows[0][1].ToString());
                    //筛选时间
                    if (startTime.HasValue && minDate < startTime)
                        minDate = startTime.Value;
                    if (endTime.HasValue && maxDate > endTime)
                        maxDate = endTime.Value;

                    MyConsole.AppendLine(string.Format("统计开始时间为{0},结束时间为{1}..", minDate, maxDate));

                    //年循环
                    for (int year = minDate.Year; year <= maxDate.Year; year++)
                    {
                        //月循环
                        for (int month = 1; month < 13; month++)
                        {
                            //天循环
                            for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
                            {
                                //开始统计天 1,3,5,7,15,30
                                DateTime currentDate = new DateTime(year, month, day);
                                if (currentDate < minDate || currentDate > maxDate)
                                    continue;
                                InsertDay(state, currentDate);
                                commandText = string.Empty;
                                //时循环
                                commandText = string.Empty;
                                for (int hour = 0; hour < 24; hour++)
                                {
                                    currentDate = new DateTime(year, month, day, hour, 0, 0);
                                    if (currentDate < minDate || currentDate > maxDate)
                                        continue;
                                    commandText += InsertHourStr(state, currentDate);
                                }
                                try
                                {
                                    int result = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnection(), CommandType.Text, commandText);
                                    MyConsole.AppendLine(string.Format("插入24条小时统计数据:站点{0},时间{1}{2}..", state, currentDate, result != 24 ? "失败" : "成功"));
                                }
                                catch (Exception ex)
                                {
                                    MyConsole.AppendLine(string.Format("插入24条小时统计数据:站点{0},时间{1}失败-{2}..", state, currentDate, ex.Message));
                                }
                            }
                        }
                    }
                }

            }));
            CurrentThread.IsBackground = true;
            CurrentThread.Start();
        }

        /// <summary>
        /// 获取小时插入字符串
        /// </summary>
        /// <param name="state"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        private static string InsertHourStr(string state, DateTime endDate)
        {
            //结束时间，站号，1，3，6，12，24，48，72
            string commandText = string.Format("insert into [DB_RainMonitor].[dbo].[RAINFALL_HOUR] select NEWID(),* from (((((((SELECT '{1}' as MONITORNUM,SUM(LON) as LON,SUM(LAT) as LAT,SUM(ALT) AS ALT,'{0}' as TIME,SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{2}' and RecordDate<='{0}' and MONITORNUM='{1}') as t1 right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{3}' and RecordDate<='{0}' and MONITORNUM='{1}') as t2 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{4}' and RecordDate<='{0}' and MONITORNUM='{1}') as t3 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{5}' and RecordDate<='{0}' and MONITORNUM='{1}') as t4 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{6}' and RecordDate<='{0}' and MONITORNUM='{1}') as t5 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{7}' and RecordDate<='{0}' and MONITORNUM='{1}') as t6 on 1=1) right join (SELECT SUM(RAINFALL) as sum1,0 as c1,0 as c2,0 as c3,0 as c4,0 as c5,0 as c6,0 as c7 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{8}' and RecordDate<='{0}' and MONITORNUM='{1}') as t7 on 1=1)", endDate, state, endDate.AddHours(-1), endDate.AddHours(-3), endDate.AddHours(-6), endDate.AddHours(-12), endDate.AddHours(-24), endDate.AddHours(-48), endDate.AddHours(-72));

            return commandText + Environment.NewLine;
        }

        #endregion

        #region Buk统计插入

        public static void BulkStaticData(DateTime? startTime, DateTime? endTime,Action action)
        {
             CurrentThread = new Thread(new ParameterizedThreadStart(delegate
             {
                 DateTime currentDateTime = DateTime.Now;
                 DataTable hourTable = null;
                 DataTable dayTable = null;
                 int hourCount = 0;
                 int dayCount = 0;
                 //计算所有时间
                 List<string> lstDate = new List<string>();
                 //查询所有站点
                 string sql = "select distinct(RecordDate) from DB_RainMonitor.dbo.RAINFALL_STATE";
                 if (startTime.HasValue && endTime.HasValue)
                     sql += $" where RecordDate>='{startTime}' and RecordDate<'{endTime}' order by RecordDate";
                 else
                     sql += "  order by RecordDate";
                 DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                 foreach (DataRow item in ds.Tables[0].Rows)
                 {
                     lstDate.Add(item[0].ToString());
                 }


                 //筛选天日期
                 List<DateTime> lsDays = new List<DateTime>();


                 for (int i = 0; i < lstDate.Count; i++)
                 {
                     MyConsole.ShowProgress(i * 100 / lstDate.Count);
                     MyConsole.AppendLine("统计时间：" + lstDate[i]);
                     MyConsole.ShowLabel(string.Format("剩余{0}条,小时数据{1}条/天数据{2},成功导入小时数据{3}/天数据{4}", lstDate.Count - i, hourTable == null ? 0 : hourTable.Rows.Count, dayTable == null ? 0 : dayTable.Rows.Count, hourCount, dayCount));

                     #region 统计小时数据
                     if (hourTable != null && hourTable.Rows.Count >= 20000)
                     {
                         SqlHelper.GetConnection().BulkCopy(hourTable, hourTable.Rows.Count, "RAINFALL_HOUR", 3600);
                         MyConsole.AppendLine("共导入小时统计数据：" + hourTable.Rows.Count + "条");
                         hourCount += hourTable.Rows.Count;
                         hourTable.Rows.Clear();
                     }
                     sql = $@"select CONVERT(char(36),NEWID()),MONITORNUM,
                                    max(lon) as LON,
                                    max(lon) as LAT,
                                    max(alt) as ALT,
                                    '{lstDate[i]}' as TIME,
                                    sum(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-1)}' then RAINFALL end) as RAINFALL_1_HOUR,
                                    sum(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-2)}' then RAINFALL end) as RAINFALL_2_HOUR,
                                    sum(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-3)}' then RAINFALL end) as RAINFALL_3_HOUR,
                                    sum(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-6)}' then RAINFALL end) as RAINFALL_6_HOUR,
                                    sum(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-12)}' then RAINFALL end) as RAINFALL_12_HOUR,
                                    sum(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-24)}' then RAINFALL end) as RAINFALL_24_HOUR,
                                    sum(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-48)}' then RAINFALL end) as RAINFALL_48_HOUR,
                                    sum(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-72)}' then RAINFALL end) as RAINFALL_72_HOUR,
                                    max(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-1)}' and Controller >0 then Controller end) as            RAINFALL_1_HOUR_C,
                                    max(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-2)}' and Controller >0 then Controller end) as            RAINFALL_2_HOUR_C,
                                    max(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-3)}' and Controller >0 then Controller end) as            RAINFALL_3_HOUR_C,
                                    max(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-6)}' and Controller >0 then Controller end) as            RAINFALL_6_HOUR_C,
                                    max(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-12)}' and Controller >0 then Controller end) as            RAINFALL_12_HOUR_C,
                                    max(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-24)}' and Controller >0 then Controller end) as            RAINFALL_24_HOUR_C,
                                    max(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-48)}' and Controller >0 then Controller end) as            RAINFALL_48_HOUR_C,
                                    max(case when RecordDate<='{lstDate[i]}' and RecordDate>'{DateTime.Parse(lstDate[i]).AddHours(-72)}' and Controller >0 then Controller end) as            RAINFALL_72_HOUR_C   
                                    from DB_RainMonitor.dbo.[RAINFALL_STATE] 
                                    where RecordDate<='{lstDate[i]}' and RecordDate>='{DateTime.Parse(lstDate[i]).AddDays(-3)}' group by MONITORNUM";
                     ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                     //MyConsole.AppendLine("共查询小时统计数据：" + ds.Tables[0].Rows.Count + "条");
                     if (hourTable == null)
                     {
                         hourTable = ds.Tables[0].Copy();
                     }
                     else
                     {
                         foreach (DataRow item in ds.Tables[0].Rows)
                         {
                             hourTable.ImportRow(item);
                         }
                     }

                     #endregion

                     DateTime dt = DateTime.Parse(lstDate[i]).Date;
                     if (!lsDays.Contains(dt))
                     {
                         lsDays.Add(dt);

                         #region 统计天数据
                         if (dayTable != null && dayTable.Rows.Count >= 20000)
                         {
                             SqlHelper.GetConnection().BulkCopy(dayTable, dayTable.Rows.Count, "RAINFALL_DAY", 3600);
                             MyConsole.AppendLine("共导入天统计数据：" + dayTable.Rows.Count + "条");
                             dayCount += dayTable.Rows.Count;
                             dayTable.Rows.Clear();
                         }
                         sql = $@"select CONVERT(char(36),NEWID()),MONITORNUM,
                                         max(lon) as LON,
                                         max(lon) as LAT,
                                         max(alt) as ALT,
                                        '{dt}' as TIME,
                                        sum(case when RecordDate<='{dt}' and RecordDate>'{dt.AddDays(-1)}' then RAINFALL end) as RAINFALL_1_DAY,
                                        sum(case when RecordDate<='{dt}' and RecordDate>'{dt.AddDays(-3)}' then RAINFALL end) as RAINFALL_3_DAY,
                                        sum(case when RecordDate<='{dt}' and RecordDate>'{dt.AddDays(-5)}' then RAINFALL end) as RAINFALL_5_DAY,
                                        sum(case when RecordDate<='{dt}' and RecordDate>'{dt.AddDays(-7)}' then RAINFALL end) as RAINFALL_7_DAY,
                                        sum(case when RecordDate<='{dt}' and RecordDate>'{dt.AddDays(-15)}' then RAINFALL end) as RAINFALL_15_DAY,
                                        sum(case when RecordDate<='{dt}' and RecordDate>'{dt.AddMonths(-1)}' then RAINFALL end) as RAINFALL_30_DAY,
                                        max(case when RecordDate<='{dt}' and RecordDate>'{dt.AddDays(-1)}' and Controller >0 then Controller end) as RAINFALL_1_DAY_C,
                                        max(case when RecordDate<='{dt}' and RecordDate>'{dt.AddDays(-3)}' and Controller >0 then Controller end) as RAINFALL_3_DAY_C,
                                        max(case when RecordDate<='{dt}' and RecordDate>'{dt.AddDays(-5)}' and Controller >0 then Controller end) as RAINFALL_5_DAY_C,
                                        max(case when RecordDate<='{dt}' and RecordDate>'{dt.AddDays(-7)}' and Controller >0 then Controller end) as RAINFALL_7_DAY_C,
                                        max(case when RecordDate<='{dt}' and RecordDate>'{dt.AddDays(-15)}' and Controller >0 then Controller end) as RAINFALL_15_DAY_C,
                                        max(case when RecordDate<='{dt}' and RecordDate>'{dt.AddMonths(-1)}' and Controller >0 then Controller end) as RAINFALL_30_DAY_C
                                        from DB_RainMonitor.dbo.[RAINFALL_STATE] 
                                        where RecordDate<='{dt}' and RecordDate>='{dt.AddMonths(-1)}' group by MONITORNUM";
                         ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                         //MyConsole.AppendLine("共查询天统计数据：" + ds.Tables[0].Rows.Count + "条");
                         if (dayTable == null)
                         {
                             dayTable = ds.Tables[0].Copy();
                         }
                         else
                         {
                             foreach (DataRow item in ds.Tables[0].Rows)
                             {
                                 dayTable.ImportRow(item);
                             }
                         }

                         #endregion
                     }
                 }

                 SqlHelper.GetConnection().BulkCopy(hourTable, hourTable.Rows.Count, "RAINFALL_HOUR", 3600);
                 MyConsole.AppendLine("共导入小时统计数据：" + hourTable.Rows.Count + "条");
                 hourTable.Rows.Clear();

                 SqlHelper.GetConnection().BulkCopy(hourTable, dayTable.Rows.Count, "RAINFALL_DAY", 3600);
                 MyConsole.AppendLine("共导入天统计数据：" + dayTable.Rows.Count + "条");
                 dayTable.Rows.Clear();

                 MyConsole.AppendLine("统计结束,耗时" + (DateTime.Now - currentDateTime).TotalSeconds + "秒");
                 action.Invoke();

             }));
             CurrentThread.IsBackground = true;
             CurrentThread.Start();
        }

        #endregion

        #endregion

        #region 统计年最大值

        /// <summary>
        /// 统计年最大值
        /// </summary>
        /// <param name="statelimit"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        public static void StaticMaxData(string statelimit, DateTime? startTime, DateTime? endTime,Action action)
        {
            CurrentThread = new Thread(new ParameterizedThreadStart(delegate
            {
                DateTime currentDate = DateTime.Now;
                List<string[]> states = new List<string[]>();
                if (string.IsNullOrEmpty(statelimit) == false)
                {
                    //查询所有站点
                    string sql = " SELECT MONITORNUM,LON,LAT,ALT FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where MONITORNUM='" + statelimit + "' group by MONITORNUM,LON,LAT,ALT";
                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        states.Add(new string[] { item[0].ToString(), item[1].ToString(), item[2].ToString(), item[3].ToString() });
                        break;
                    }
                }
                else
                {
                    //查询所有站点
                    string sql = " SELECT MONITORNUM,LON,LAT,ALT FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] group by MONITORNUM,LON,LAT,ALT";
                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        states.Add(new string[] { item[0].ToString(), item[1].ToString(), item[2].ToString(), item[3].ToString() });
                    }
                }

                foreach (string[] state in states)
                {
                    //查询需要统计时间范围
                    string commandText = "select MAX(RecordDate),MIN(RecordDate) FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where MONITORNUM='" + state[0] + "'";

                    DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commandText);
                    DateTime maxDate = DateTime.Parse(ds.Tables[0].Rows[0][0].ToString());
                    DateTime minDate = DateTime.Parse(ds.Tables[0].Rows[0][1].ToString());
                    //筛选时间
                    if (startTime.HasValue && minDate < startTime)
                        minDate = startTime.Value;
                    if (endTime.HasValue && maxDate > endTime)
                        maxDate = endTime.Value;

                    MyConsole.AppendLine(string.Format("统计开始时间为{0},结束时间为{1}..", minDate, maxDate));

                    decimal lon = string.IsNullOrEmpty(state[1]) ? 0 : Convert.ToDecimal(state[1]);
                    decimal lat = string.IsNullOrEmpty(state[2]) ? 0 : Convert.ToDecimal(state[2]);
                    decimal alt = string.IsNullOrEmpty(state[3]) ? 0 : Convert.ToDecimal(state[3]);

                    //年循环
                    for (int year = minDate.Year; year <= maxDate.Year; year++)
                    {
                        try
                        {
                            MyConsole.ShowProgress(year * 100 / maxDate.Year);

                            //获取天最大值
                            string commmandText = "select MAX(RAINFALL_1_DAY),MAX(RAINFALL_3_DAY),MAX(RAINFALL_5_DAY),MAX(RAINFALL_7_DAY),MAX(RAINFALL_15_DAY),MAX(RAINFALL_30_DAY) from RAINFALL_DAY where datepart(yy,TIME)='" + year + "'";
                            ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commmandText);
                            decimal? day = ConvertDecimal(ds.Tables[0].Rows[0][0]);
                            if (day == null)
                            {
                                MyConsole.AppendLine(string.Format("站点{0}年份{1}缺少数据来源，无法统计..",state[0],year));
                                continue;
                            }
                            decimal? day_3 = ConvertDecimal(ds.Tables[0].Rows[0][1]);
                            decimal? day_5 = ConvertDecimal(ds.Tables[0].Rows[0][2]);
                            decimal? day_7 = ConvertDecimal(ds.Tables[0].Rows[0][3]);
                            decimal? day_15 = ConvertDecimal(ds.Tables[0].Rows[0][4]);
                            decimal? day_30 = ConvertDecimal(ds.Tables[0].Rows[0][5]);

                            commmandText = "select MAX(RAINFALL_1_HOUR),MAX(RAINFALL_3_HOUR),MAX(RAINFALL_6_HOUR),MAX(RAINFALL_12_HOUR),MAX(RAINFALL_24_HOUR),MAX(RAINFALL_48_HOUR),MAX(RAINFALL_72_HOUR) from RAINFALL_HOUR where datepart(yy,TIME)='" + year + "'";
                            ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commmandText);
                            decimal? hour = ConvertDecimal(ds.Tables[0].Rows[0][0]);
                            decimal? hour_3 = ConvertDecimal(ds.Tables[0].Rows[0][1]);
                            decimal? hour_6 = ConvertDecimal(ds.Tables[0].Rows[0][2]);
                            decimal? hour_12 = ConvertDecimal(ds.Tables[0].Rows[0][3]);
                            decimal? hour_24 = ConvertDecimal(ds.Tables[0].Rows[0][4]);
                            decimal? hour_48 = ConvertDecimal(ds.Tables[0].Rows[0][5]);
                            decimal? hour_72 = ConvertDecimal(ds.Tables[0].Rows[0][6]);

                            try
                            {
                                commandText = string.Format("SELECT * FROM [DB_RainMonitor].[dbo].[RAINFALL_YEAR_MAX] where MONITORNUM='{0}' and YEAR={1}", state[0], year);
                                object obj = SqlHelper.ExecuteScalar(SqlHelper.GetConnSting(), System.Data.CommandType.Text, commandText);
                                if (obj != null)
                                {
                                    commandText = string.Format("delete FROM [DB_RainMonitor].[dbo].[RAINFALL_YEAR_MAX] where MONITORNUM='{0}' and YEAR={1}", state[0], year);
                                    int result = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnSting(), System.Data.CommandType.Text, commandText);
                                    MyConsole.AppendLine(string.Format("删除一条年统计数据:站点{0},年份{1}{2}..", state[0], year, result == 0 ? "失败" : "成功"));
                                }
                            }
                            catch (Exception ex)
                            {
                                MyConsole.AppendLine("执行数据库失败：" + ex.Message);
                            }

                            commandText = string.Format("insert into RAINFALL_YEAR_MAX values('{0}','{1}',{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20})", Guid.NewGuid(), state[0], lon, lat, alt, year, "null", "null", hour, hour_3, hour_6, hour_12, hour_24, hour_48, hour_72, day, day_3, day_5, day_7, day_15, day_30);
                            int line = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnection(), CommandType.Text, commandText);

                            //插入一条年统计数据
                            MyConsole.AppendLine(string.Format("插入年统计数据:站点{0},年份{1},{2}", state[0], year, line == 0 ? "失败" : "成功"));
                        }
                        catch (Exception ex)
                        {
                            MyConsole.AppendLine(string.Format("插入年统计数据:站点{0},年份{1},失败-{2}", state[0], year, ex.Message));
                        }
                    }
                }
                MyConsole.AppendLine("统计结束,耗时" + (DateTime.Now - currentDate).TotalSeconds + "秒");
                action.Invoke();
            }));
            CurrentThread.IsBackground = true;
            CurrentThread.Start();
        }

        public static void BulkStaticMax(DateTime? startTime, DateTime? endTime, Action action)
        {
            CurrentThread = new Thread(new ParameterizedThreadStart(delegate
            {
                DateTime currentDate = DateTime.Now;

                //获取所有年份信息
                List<string> lstYear = new List<string>();
                DataTable maxTable = null;
                int maxCount = 0;
                string sql = "select distinct(datepart(YEAR,recorddate)) from [DB_RainMonitor].[dbo].[RAINFALL_STATE]";
                if (startTime.HasValue && endTime.HasValue)
                    sql += $" where RecordDate>='{startTime}' and RecordDate<'{endTime}'";
                DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                foreach (DataRow item in ds.Tables[0].Rows)
                {
                    lstYear.Add(item[0].ToString());
                }
                for (int i = 0; i < lstYear.Count; i++)
                {
                    MyConsole.ShowProgress(i * 100 / lstYear.Count);
                    MyConsole.AppendLine("统计时间：" + lstYear[i]);
                    MyConsole.ShowLabel($"剩余{lstYear.Count - i - 1}条数据，查询统计数据{(maxTable==null?0:maxTable.Rows.Count)}条,共导入年份最大统计数据{maxCount}条");
                    if (maxTable != null && maxTable.Rows.Count >= 500)
                    {
                        maxTable.Columns.Remove("MONITORNUM1");
                        maxTable.Columns.Remove("YEAR1");
                        SqlHelper.GetConnection().BulkCopy(maxTable, maxTable.Rows.Count, "RAINFALL_YEAR_MAX", 3600);
                        MyConsole.AppendLine("导入年最大值统计数据：" + maxTable.Rows.Count + "条");
                        maxCount += maxTable.Rows.Count;
                        maxTable = null;
                    }
                    sql = $@"select * from 
                                     (select CONVERT(char(36),NEWID()) as GUID,
                                         MONITORNUM,
                                         MAX(LON) as LON,MAX(LAT) as LAT,
                                         MAX(ALT) as ALT,'{i}' as YEAR,
                                         null as MAX_10_MIN,
                                         null as MAX_30_MIN,
                                         MAX(RAINFALL_1_HOUR) as MAX_1_HOUR,
                                         MAX(RAINFALL_3_HOUR) as MAX_3_HOUR,
                                         MAX(RAINFALL_6_HOUR) as MAX_6_HOUR,
                                         MAX(RAINFALL_12_HOUR) as MAX_12_HOUR,
                                         MAX(RAINFALL_24_HOUR) as MAX_24_HOUR,
                                         MAX(RAINFALL_48_HOUR) as MAX_48_HOUR,
                                         MAX(RAINFALL_72_HOUR) as MAX_72_HOUR 
                                         from [DB_RainMonitor].[dbo].[RAINFALL_HOUR] 
                                         where TIME>='{i}-1-1 0:00:00' and TIME<'{i+1}-1-1 0:00:00' 
                                         group by MONITORNUM) as A left join 
                                         (select * from 
                                         (select MAX(RAINFALL_1_DAY) as MAX_1_DAY,
                                         MAX(RAINFALL_3_DAY) as MAX_3_DAY,
                                         MAX(RAINFALL_5_DAY) as MAX_5_DAY,
                                         MAX(RAINFALL_7_DAY) as MAX_7_DAY,
                                         MAX(RAINFALL_15_DAY) as MAX_15_DAY,
                                         MAX(RAINFALL_30_DAY) as MAX_30_DAY,
                                         MONITORNUM,
                                         '{i}' as YEAR 
                                         from [DB_RainMonitor].[dbo].[RAINFALL_DAY] 
                                         where TIME>='{i}-1-1 0:00:00' and TIME<'{i+1}-1-1 0:00:00' group by MONITORNUM) B) as B 
                                         on A.MONITORNUM = B.MONITORNUM and A.YEAR = B.YEAR";

                    ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sql);
                    //MyConsole.AppendLine("共查询小时统计数据：" + ds.Tables[0].Rows.Count + "条");
                    if (maxTable == null)
                    {
                        maxTable = ds.Tables[0].Copy();
                    }
                    else
                    {
                        foreach (DataRow item in ds.Tables[0].Rows)
                        {
                            maxTable.ImportRow(item);
                        }
                    }
                }

                maxTable.Columns.Remove("MONITORNUM1");
                maxTable.Columns.Remove("YEAR1");
                SqlHelper.GetConnection().BulkCopy(maxTable, maxTable.Rows.Count, "RAINFALL_YEAR_MAX", 3600);
                MyConsole.AppendLine("导入年最大值统计数据：" + maxTable.Rows.Count + "条");
                maxCount += maxTable.Rows.Count;
                maxTable = null;

                MyConsole.AppendLine("统计结束,耗时" + (DateTime.Now - currentDate).TotalSeconds + "秒");
                action.Invoke();
            }));
            CurrentThread.IsBackground = true;
            CurrentThread.Start();
        }

        #endregion

        #region 保存条件记录数据

        /// <summary>
        /// 保存入库条件数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static void SaveConditonData(int type, DateTime? start, DateTime? end)
        {
            string commandText = $@"delete FROM [DB_RainMonitor].[dbo].[TEMP_RAIN] where putType={type};
insert into [DB_RainMonitor].[dbo].[TEMP_RAIN] values(newid(),{type},'{start}',0);
insert into [DB_RainMonitor].[dbo].[TEMP_RAIN] values(newid(),{type},'{end}',1);";

            SqlHelper.ExecuteNonQuery(SqlHelper.GetConnection(), CommandType.Text, commandText);
        }

        /// <summary>
        /// 根据类型获取入库的时间
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DateTime[] GetConditionData(int type)
        {
            try
            {
                string commandText = $"select * from [DB_RainMonitor].[dbo].[TEMP_RAIN] where putType={type}";

                DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commandText);

                if (ds.Tables[0].Rows.Count == 0)
                {
                    commandText = "select MIN(RecordDate),MAX(RecordDate) from DB_RainMonitor.dbo.RAINFALL_STATE";
                    ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commandText);
                    if (ds.Tables[0].Rows.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return new DateTime[] { Convert.ToDateTime(ds.Tables[0].Rows[0][0]), Convert.ToDateTime(ds.Tables[0].Rows[0][1]) };
                    }
                }
                else
                {
                    List<DateTime> result = new List<DateTime>();
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        result.Add(Convert.ToDateTime(row[2]));
                    }
                    return result.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
