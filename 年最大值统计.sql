 
  /***
  查询所有需要统计的年份
  ***/
  select distinct(datepart(YEAR,recorddate)) from [DB_RainMonitor].[dbo].[RAINFALL_STATE]
  
  /***
  
  统计各个站点的年最大值
  
  ***/
  
 select * from 
 (select CONVERT(char(36),NEWID()) as GUID,
          MONITORNUM,
          MAX(LON) as LON,MAX(LAT) as LAT,
          MAX(ALT) as ALT,'1961' as YEAR,
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
          where TIME>='1961-1-1 0:00:00' and TIME<'1962-1-1 0:00:00' 
          group by MONITORNUM) as A left join 
          (select * from 
          (select MAX(RAINFALL_1_DAY) as MAX_1_DAY,
          MAX(RAINFALL_3_DAY) as MAX_3_DAY,
          MAX(RAINFALL_5_DAY) as MAX_5_DAY,
          MAX(RAINFALL_7_DAY) as MAX_7_DAY,
          MAX(RAINFALL_15_DAY) as MAX_15_DAY,
          MAX(RAINFALL_30_DAY) as MAX_30_DAY,
          MONITORNUM as MONITORNUM1,
          '1961' as YEAR1 
          from [DB_RainMonitor].[dbo].[RAINFALL_DAY] 
          where TIME>='1961-1-1 0:00:00' and TIME<'1962-1-1 0:00:00' group by MONITORNUM) B) as B 
          on A.MONITORNUM = B.MONITORNUM1 and A.YEAR = B.YEAR1