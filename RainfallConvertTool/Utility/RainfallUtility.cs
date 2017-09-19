﻿using RainfallConvertTool.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace RainfallConvertTool.Utility
{
    public class RainfallUtility
    {
        /// <summary>
        /// 插入源数据
        /// </summary>
        /// <param name="content"></param>
        /// <param name="statelimit"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        public static void InsertMetaData(string[][] content, string statelimit, int? start, int? end, DateTime? startTime, DateTime? endTime)
        {
            int colsCount = content[0].Length;
            MyConsole.AppendLine(string.Format("共找到{0}行数据..", content.Count()));

            Thread thread = new Thread(new ParameterizedThreadStart(delegate
            {
                //52位数据 站点 年 月 日 20~23雨量 0~19雨量 20~23控制码 0~19控制码
                if (colsCount == 52)
                {
                    for (int index = 0; index < content.Count(); index++)
                    {
                        MyConsole.ShowProgress(index * 100 / content.Count());
                        //筛选条数
                        if ((start.HasValue && start < index) || (end.HasValue && end > index))
                            continue;
                        //区站号 -1
                        string state = content[index][0];
                        //筛选站点
                        if (string.IsNullOrEmpty(statelimit) == false && state != statelimit)
                            continue;
                        int year = int.Parse(content[index][1]); //年
                        int month = int.Parse(content[index][2]); //月
                        int date = int.Parse(content[index][3]); //日
                        for (int i = 0; i < 20; i++)
                        {
                            int control = int.Parse(content[index][i + 32]);

                            decimal? rainfall = null;
                            if (content[index][i + 8] == "32766")
                                rainfall = 0;
                            else
                                rainfall = Decimal.Parse(content[index][i + 8]) / 10;
                            DateTime dateTemp = new DateTime(year, month, date, i, 0, 0);
                            //筛选时间
                            if ((startTime.HasValue && dateTemp < startTime) || (endTime.HasValue && dateTemp > endTime))
                                continue;
                            RainfallModel temp = new RainfallModel(state, null, null, null, dateTemp, rainfall, control);
                            InsertToSql(temp);
                            //Thread.Sleep(100);
                        }
                        for (int i = 20; i < 24; i++)
                        {
                            int control = int.Parse(content[index][i + 8]);
                            decimal? rainfall = null;
                            if (content[index][i -16] == "32766")
                                rainfall = 0;
                            else
                                rainfall = Decimal.Parse(content[index][i -16]) / 10;
                            DateTime dateTemp = new DateTime(year, month, date, i, 0, 0);
                            //筛选时间
                            if ((startTime.HasValue && dateTemp < startTime) || (endTime.HasValue && dateTemp > endTime))
                                continue;

                            RainfallModel temp = new RainfallModel(state, null, null, null, dateTemp, rainfall, control);
                            InsertToSql(temp);
                            //Thread.Sleep(100);
                        }
                    }
                }
                //6字段     Sta       Lon       Lat     Alt      Date       Pre
                else if(colsCount == 6)
                {
                    for (int index = 1; index < content.Count(); index++)
                    {
                        MyConsole.ShowProgress(index * 100 / content.Count());
                        //筛选条数
                        if ((start.HasValue && start < index) || (end.HasValue && end > index))
                            continue;
                        //区站号 -1
                        string state = content[index][0];
                        //筛选站点
                        if (string.IsNullOrEmpty(statelimit) == false && state != statelimit)
                            continue;
                        int value = 0;  //控制码默认为0
                        decimal? key = Decimal.Parse(content[index][5]);
                        DateTime dateTemp = DateTime.ParseExact(content[index][4],"yyyyMMddHH",null);
                        //筛选时间
                        if ((startTime.HasValue && dateTemp < startTime) || (endTime.HasValue && dateTemp > endTime))
                            continue;
                        RainfallModel temp = new RainfallModel(state, Decimal.Parse(content[index][1]), Decimal.Parse(content[index][2]), Decimal.Parse(content[index][3]), dateTemp, key, value);
                        InsertToSql(temp);
                        //Thread.Sleep(100);
                    }
                }
                //3字段 DEVICECODE	RAINFALL	MONITORTIME
                else if (colsCount == 3)
                {
                    for (int index = 1; index < content.Count(); index++)
                    {
                        MyConsole.ShowProgress(index * 100 / content.Count());
                        //筛选条数
                        if ((start.HasValue && start < index) || (end.HasValue && end > index))
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
                        RainfallModel temp = new RainfallModel(state, null, null, null, dateTemp, key, value);
                        InsertToSql(temp);
                        //Thread.Sleep(100);
                    }
                }

            }));
            thread.IsBackground = true;
            thread.Start();
        }

     
        public static void StaticMaxData(string statelimit,DateTime? startTime, DateTime? endTime)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(delegate
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
                            string commmandText = "select MAX([RAINFALL _1_DAY]),MAX([RAINFALL _3_DAY]),MAX([RAINFALL _5_DAY]),MAX([RAINFALL _7_DAY]),MAX([RAINFALL _15_DAY]),MAX([RAINFALL _30_DAY]) from RAINFALL_DAY where datepart(yy,TIME)='" + year + "'";
                            ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commmandText);
                            decimal? day = ConvertDecimal(ds.Tables[0].Rows[0][0]);
                            if (day == null)
                            {
                                MyConsole.AppendLine("当前年缺少数据来源，无法统计..");
                                continue;
                            }
                            decimal? day_3 = ConvertDecimal(ds.Tables[0].Rows[0][1]);
                            decimal? day_5 = ConvertDecimal(ds.Tables[0].Rows[0][2]);
                            decimal? day_7 = ConvertDecimal(ds.Tables[0].Rows[0][3]);
                            decimal? day_15 = ConvertDecimal(ds.Tables[0].Rows[0][4]);
                            decimal? day_30 = ConvertDecimal(ds.Tables[0].Rows[0][5]);

                            commmandText = "select MAX([RAINFALL _1_HOUR]),MAX([RAINFALL _3_HOUR]),MAX([RAINFALL _6_HOUR]),MAX([RAINFALL _12_HOUR]),MAX([RAINFALL _24_HOUR]),MAX([RAINFALL _48_HOUR]),MAX([RAINFALL _72_HOUR]) from RAINFALL_HOUR where datepart(yy,TIME)='" + year + "'";
                            ds = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commmandText);
                            decimal? hour = ConvertDecimal(ds.Tables[0].Rows[0][0]);
                            decimal? hour_3 = ConvertDecimal(ds.Tables[0].Rows[0][1]);
                            decimal? hour_6 = ConvertDecimal(ds.Tables[0].Rows[0][2]);
                            decimal? hour_12 = ConvertDecimal(ds.Tables[0].Rows[0][3]);
                            decimal? hour_24 = ConvertDecimal(ds.Tables[0].Rows[0][4]);
                            decimal? hour_48 = ConvertDecimal(ds.Tables[0].Rows[0][5]);
                            decimal? hour_72 = ConvertDecimal(ds.Tables[0].Rows[0][6]);

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

            }));
            thread.IsBackground = true;
            thread.Start();
        }

        private static void InsertToSql(RainfallModel model)
        {
            try
            {
                string commandText = "insert into [DB_RainMonitor].[dbo].[RAINFALL_STATE] values(@GUID,@MONITORNUM,@LON,@LAT,@ALT,@RecordDate,@RAINFALL,@Controller)";
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
                int line = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnSting(), System.Data.CommandType.Text, commandText, result.ToArray());

                MyConsole.AppendLine(string.Format("导入数据站点{0},时间{1}{2}..", model.MONITORNUM, model.RecordDate, line == 1 ? "成功" : "失败"));
            }
            catch (Exception ex)
            {
                MyConsole.AppendLine(string.Format("导入数据站点{0},时间{1}异常：{2}..", model.MONITORNUM, model.RecordDate, ex.Message));
            }
        }

        #region 旧版效率低

        public static void StaticData(string statelimit, DateTime? startTime, DateTime? endTime)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(delegate
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
                        //月循环
                        for (int month = 1; month < 13; month++)
                        {
                            //天循环
                            for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
                            {
                                //开始统计天 1,3,5,7,15,30
                                DateTime currentDate = new DateTime(year, month, day);
                                ExcuteStaticDay(currentDate, state[0], lon, lat, alt);
                                commandText = string.Empty;
                                //时循环
                                for (int hour = 0; hour < 24; hour++)
                                {
                                    currentDate = new DateTime(year, month, day, hour, 0, 0);
                                    commandText += ExcuteStaticHour(currentDate, state[0], lon, lat, alt);
                                }
                                try
                                {
                                    int result = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnection(), CommandType.Text, commandText);

                                    MyConsole.AppendLine(string.Format("插入24条小时统计数据:站点{0},时间{1}{2}..", state[0], currentDate, result != 24 ? "失败" : "成功"));
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
            thread.IsBackground = true;
            thread.Start();
        }

        private static void ExcuteStaticDay(DateTime endDate, string state, decimal lon, decimal lat, decimal alt)
        {
            //插入一个站点的一天的天统计数据
            string commandText = string.Empty;
         commandText += GetStaticsResult(StaticsEnum.Day, endDate, state);
         commandText += Environment.NewLine;
         commandText += GetStaticsResult(StaticsEnum.Day_3, endDate, state);
         commandText += Environment.NewLine;
         commandText += GetStaticsResult(StaticsEnum.Day_5, endDate, state);
         commandText += Environment.NewLine;
         commandText += GetStaticsResult(StaticsEnum.Day_7, endDate, state);
         commandText += Environment.NewLine;
         commandText += GetStaticsResult(StaticsEnum.Day_15, endDate, state);
         commandText += Environment.NewLine;
         commandText += GetStaticsResult(StaticsEnum.Day_30, endDate, state);
         DataSet sums = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commandText);
         decimal? hour = null;
         if (sums.Tables[0].Rows[0][0].GetType() != typeof(DBNull))
         {
             hour = Convert.ToDecimal(sums.Tables[0].Rows[0][0]);
         }
         decimal? hour_3 = null;
         if (sums.Tables[1].Rows[0][0].GetType() != typeof(DBNull))
         {
             hour_3 = Convert.ToDecimal(sums.Tables[1].Rows[0][0]);
         }
         decimal? hour_6 = null;
         if (sums.Tables[2].Rows[0][0].GetType() != typeof(DBNull))
         {
             hour_6 = Convert.ToDecimal(sums.Tables[2].Rows[0][0]);
         }
         decimal? hour_12 = null;
         if (sums.Tables[3].Rows[0][0].GetType() != typeof(DBNull))
         {
             hour_12 = Convert.ToDecimal(sums.Tables[3].Rows[0][0]);
         }
         decimal? hour_24 = null;
         if (sums.Tables[4].Rows[0][0].GetType() != typeof(DBNull))
         {
             hour_24 = Convert.ToDecimal(sums.Tables[4].Rows[0][0]);
         }
         decimal? hour_48 = null;
         if (sums.Tables[5].Rows[0][0].GetType() != typeof(DBNull))
         {
             hour_48 = Convert.ToDecimal(sums.Tables[5].Rows[0][0]);
         }
            try
            {
                commandText = string.Format("insert into [DB_RainMonitor].[dbo].[RAINFALL_DAY] values('{0}','{1}',{2},{3},{4},'{5}',{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17})", Guid.NewGuid(), state, lon, lat, alt, endDate, hour ?? 0, hour_3 ?? 0, hour_6 ?? 0, hour_12 ?? 0, hour_24 ?? 0, hour_48 ?? 0, hour.HasValue ? "0" : "1", hour_3.HasValue ? "0" : "1", hour_6.HasValue ? "0" : "1", hour_12.HasValue ? "0" : "1", hour_24.HasValue ? "0" : "1", hour_48.HasValue ? "0" : "1");
                int result = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnection(), CommandType.Text, commandText);
                MyConsole.AppendLine(string.Format("插入一条统计数据:站点{0},时间{1}{2}..", state, endDate, result == 0 ? "失败" : "成功"));
            }
            catch (Exception ex)
            {
                MyConsole.AppendLine(string.Format("插入一条统计数据:站点{0},时间{1}失败-{2}..", state, endDate, ex.Message));
            }
        }

        private static string ExcuteStaticHour(DateTime endDate, string state, decimal lon, decimal lat, decimal alt)
        {
            string commandText = string.Empty;
            commandText += GetStaticsResult(StaticsEnum.Hour, endDate, state);
            commandText += Environment.NewLine;
            commandText += GetStaticsResult(StaticsEnum.Hour_3, endDate, state);
            commandText += Environment.NewLine;
            commandText += GetStaticsResult(StaticsEnum.Hour_6, endDate, state);
            commandText += Environment.NewLine;
            commandText += GetStaticsResult(StaticsEnum.Hour_12, endDate, state);
            commandText += Environment.NewLine;
            commandText += GetStaticsResult(StaticsEnum.Hour_24, endDate, state);
            commandText += Environment.NewLine;
            commandText += GetStaticsResult(StaticsEnum.Hour_48, endDate, state);
            commandText += Environment.NewLine;
            commandText += GetStaticsResult(StaticsEnum.Hour_72, endDate, state);

            DataSet sums = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, commandText);
            decimal? hour = null;
            if (sums.Tables[0].Rows[0][0].GetType() != typeof(DBNull))
            {
                hour = Convert.ToDecimal(sums.Tables[0].Rows[0][0]);
            }
            decimal? hour_3 = null;
            if (sums.Tables[1].Rows[0][0].GetType() != typeof(DBNull))
            {
                hour_3 = Convert.ToDecimal(sums.Tables[1].Rows[0][0]);
            }
            decimal? hour_6 = null;
            if (sums.Tables[2].Rows[0][0].GetType() != typeof(DBNull))
            {
                hour_6 = Convert.ToDecimal(sums.Tables[2].Rows[0][0]);
            }
            decimal? hour_12 = null;
            if (sums.Tables[3].Rows[0][0].GetType() != typeof(DBNull))
            {
                hour_12 = Convert.ToDecimal(sums.Tables[3].Rows[0][0]);
            }
            decimal? hour_24 = null;
            if (sums.Tables[4].Rows[0][0].GetType() != typeof(DBNull))
            {
                hour_24 = Convert.ToDecimal(sums.Tables[4].Rows[0][0]);
            }
            decimal? hour_48 = null;
            if (sums.Tables[5].Rows[0][0].GetType() != typeof(DBNull))
            {
                hour_48 = Convert.ToDecimal(sums.Tables[5].Rows[0][0]);
            }
            decimal? hour_72 = null;
            if (sums.Tables[6].Rows[0][0].GetType() != typeof(DBNull))
            {
                hour_72 = Convert.ToDecimal(sums.Tables[6].Rows[0][0]);
            }

            //插入一个站点的一个小时的小时统计数据

            commandText = string.Format("insert into [DB_RainMonitor].[dbo].[RAINFALL_HOUR] values('{0}','{1}',{2},{3},{4},'{5}',{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19})", Guid.NewGuid(), state, lon, lat, alt, endDate, hour ?? 0, hour_3 ?? 0, hour_6 ?? 0, hour_12 ?? 0, hour_24 ?? 0, hour_48 ?? 0, hour_72 ?? 0, hour.HasValue ? "0" : "1", hour_3.HasValue ? "0" : "1", hour_6.HasValue ? "0" : "1", hour_12.HasValue ? "0" : "1", hour_24.HasValue ? "0" : "1", hour_48.HasValue ? "0" : "1", hour_72.HasValue ? "0" : "1");
            return commandText + Environment.NewLine;
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
            return string.Format("SELECT SUM(RAINFALL) as sum FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{0}' and RecordDate<='{1}' and MONITORNUM='{2}'", startDate, endTime, state);

          
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

        public static void StaticDataNew(string statelimit, DateTime? startTime, DateTime? endTime)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(delegate
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
                                for (int hour = 0; hour < 24; hour++)
                                {
                                    currentDate = new DateTime(year, month, day, hour, 0, 0);
                                    if (currentDate < minDate || currentDate > maxDate)
                                        continue;
                                    InsertHour(state, currentDate);
                                }
                            }
                        }
                    }
                }

            }));
            thread.IsBackground = true;
            thread.Start();
        }

        private static  void InsertHour(string state, DateTime endDate)
        {
            //结束时间，站号，1，3，6，12，24，48，72
            string commandText = string.Format("insert into [DB_RainMonitor].[dbo].[RAINFALL_HOUR] select NEWID(),* from (((((((SELECT '{1}' as MONITORNUM,SUM(LON) as LON,SUM(LAT) as LAT,SUM(ALT) AS ALT,'{0}' as TIME,SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{2}' and RecordDate<='{0}' and MONITORNUM='{1}') as t1 right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{3}' and RecordDate<='{0}' and MONITORNUM='{1}') as t2 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{4}' and RecordDate<='{0}' and MONITORNUM='{1}') as t3 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{5}' and RecordDate<='{0}' and MONITORNUM='{1}') as t4 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{6}' and RecordDate<='{0}' and MONITORNUM='{1}') as t5 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{7}' and RecordDate<='{0}' and MONITORNUM='{1}') as t6 on 1=1) right join (SELECT SUM(RAINFALL) as sum1,0 as c1,0 as c2,0 as c3,0 as c4,0 as c5,0 as c6,0 as c7 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{8}' and RecordDate<='{0}' and MONITORNUM='{1}') as t7 on 1=1)", endDate, state, endDate.AddHours(-1), endDate.AddHours(-3), endDate.AddHours(-6), endDate.AddHours(-12), endDate.AddHours(-24), endDate.AddHours(-48), endDate.AddHours(-72));

            try
            {
                int result = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnection(), CommandType.Text, commandText);
                MyConsole.AppendLine(string.Format("插入一条小时统计数据:站点{0},时间{1}{2}..", state, endDate, result == 0 ? "失败" : "成功"));
            }
            catch (Exception ex)
            {
                MyConsole.AppendLine(string.Format("插入一条小时统计数据:站点{0},时间{1}失败-{2}..", state, endDate, ex.Message));
            }
        }

        private static void InsertDay(string state, DateTime endDate)
        {
            //结束时间，站号，1，3，6，12，24，48，72
            string commandText = string.Format("insert into [DB_RainMonitor].[dbo].[RAINFALL_DAY] select NEWID(),* from ((((((SELECT '{1}' as MONITORNUM,SUM(LON) as LON,SUM(LAT) as LAT,SUM(ALT) AS ALT,'{0}' as TIME,SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{2}' and RecordDate<='{0}' and MONITORNUM='{1}') as t1 right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{3}' and RecordDate<='{0}' and MONITORNUM='{1}') as t2 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{4}' and RecordDate<='{0}' and MONITORNUM='{1}') as t3 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{5}' and RecordDate<='{0}' and MONITORNUM='{1}') as t4 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{6}' and RecordDate<='{0}' and MONITORNUM='{1}') as t5 on 1=1) right join (SELECT SUM(RAINFALL) as sum1,0 as c1,0 as c2,0 as c3,0 as c4,0 as c5,0 as c6 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{7}' and RecordDate<='{0}' and MONITORNUM='{1}') as t6 on 1=1)", endDate, state, endDate.AddDays(-1), endDate.AddDays(-3), endDate.AddDays(-5), endDate.AddDays(-7), endDate.AddDays(-15), endDate.AddDays(-30));

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


        public static void StaticDataAllNew(string statelimit, DateTime? startTime, DateTime? endTime)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(delegate
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
            thread.IsBackground = true;
            thread.Start();
        }

        private static string InsertHourStr(string state, DateTime endDate)
        {
            //结束时间，站号，1，3，6，12，24，48，72
            string commandText = string.Format("insert into [DB_RainMonitor].[dbo].[RAINFALL_HOUR] select NEWID(),* from (((((((SELECT '{1}' as MONITORNUM,SUM(LON) as LON,SUM(LAT) as LAT,SUM(ALT) AS ALT,'{0}' as TIME,SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{2}' and RecordDate<='{0}' and MONITORNUM='{1}') as t1 right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{3}' and RecordDate<='{0}' and MONITORNUM='{1}') as t2 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{4}' and RecordDate<='{0}' and MONITORNUM='{1}') as t3 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{5}' and RecordDate<='{0}' and MONITORNUM='{1}') as t4 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{6}' and RecordDate<='{0}' and MONITORNUM='{1}') as t5 on 1=1) right join (SELECT SUM(RAINFALL) as sum1 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{7}' and RecordDate<='{0}' and MONITORNUM='{1}') as t6 on 1=1) right join (SELECT SUM(RAINFALL) as sum1,0 as c1,0 as c2,0 as c3,0 as c4,0 as c5,0 as c6,0 as c7 FROM [DB_RainMonitor].[dbo].[RAINFALL_STATE] where RecordDate>'{8}' and RecordDate<='{0}' and MONITORNUM='{1}') as t7 on 1=1)", endDate, state, endDate.AddHours(-1), endDate.AddHours(-3), endDate.AddHours(-6), endDate.AddHours(-12), endDate.AddHours(-24), endDate.AddHours(-48), endDate.AddHours(-72));

            return commandText + Environment.NewLine;
        }

       
    }
}
