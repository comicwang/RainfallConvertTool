/****** Script for SelectTopNRows command from SSMS  ******/
select convert(varchar(100),t.RecordDate,23) [date] ,count(RAINFALL) num 
from  DB_RainMonitor.dbo.RAINFALL_STATE t
where MONITORNUM = '56096' AND RecordDate >= '1907-07-24' AND RecordDate < '2017-08-16' 
group by convert(varchar(100),t.RecordDate,23)


select T.MONITORNUM,T.RecordDate,sum(T.RAINFALL) as number from
(
select o.MONITORNUM,o.RAINFALL,o.RecordDate,datename(hh,o.RecordDate)/4 as groupid from DB_RainMonitor.dbo.RAINFALL_STATE as o 
where o.RecordDate>'2013-10-20 00:00' and o.RecordDate<'2013-10-20 6:00'
) as T
group by T.groupid,T.MONITORNUM,T.RecordDate
order by groupid,T.MONITORNUM,T.RecordDate

select datename(mi,'2018-9-10 23:10:00')
select * from DB_RainMonitor.dbo.RAINFALL_STATE where MONITORNUM='56192' and RecordDate='2013-10-20 2:00'

/***
查出所有时间点

***/
select distinct(RecordDate) from DB_RainMonitor.dbo.RAINFALL_STATE 

/***
统计各个时间点各站点的小时数据
***/

select CONVERT(char(36),NEWID()),MONITORNUM,
       avg(case when LON is not null then lon end) as LON,
       avg(case when lat is not null then lat end) as LAT,
       avg(case when alt is not null then alt end) as ALT,
       '2013-10-20 2:00' as TIME,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-1,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_1_HOUR,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-3,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_3_HOUR,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-6,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_6_HOUR,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-12,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_12_HOUR,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-24,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_24_HOUR,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-48,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_48_HOUR,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-72,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_72_HOUR,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-1,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_1_HOUR_C,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-3,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_3_HOUR_C,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-6,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_6_HOUR_C,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-12,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_12_HOUR_C,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-24,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_24_HOUR_C,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-48,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_48_HOUR_C,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(hh,-72,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_72_HOUR_C  
       from DB_RainMonitor.dbo.[RAINFALL_STATE] 
       where RecordDate<'2013-10-20 2:00' and RecordDate>=dateadd(DAY,-2,'2013-10-20 2:00') group by MONITORNUM

/***

统计各个时间各个站点的天数据

***/

select CONVERT(char(36),NEWID()) as GUID,MONITORNUM,
       avg(case when LON is not null then lon end) as LON,
       avg(case when lat is not null then lat end) as LAT,
       avg(case when alt is not null then alt end) as ALT,
       '2013-10-20 2:00' as TIME,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(DAY,-1,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_1_DAY,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(DAY,-3,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_3_DAY,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(DAY,-5,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_5_DAY,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(DAY,-7,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_7_DAY,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(DAY,-15,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_15_DAY,
       sum(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(MONTH,-1,'2013-10-20 2:00') then RAINFALL end) as RAINFALL_30_DAY,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(DAY,-1,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_1_DAY_C,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(DAY,-3,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_3_DAY_C,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(DAY,-5,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_5_DAY_C,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(DAY,-7,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_7_DAY_C,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(DAY,-15,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_15_DAY_C,
       avg(case when RecordDate<='2013-10-20 2:00' and RecordDate>dateadd(MONTH,-1,'2013-10-20 2:00') and Controller <>0 then Controller end) as RAINFALL_30_DAY_C
       from DB_RainMonitor.dbo.[RAINFALL_STATE] 
       where RecordDate<'2013-10-20 2:00' and RecordDate>=dateadd(MONTH,-1,'2013-10-20 2:00') group by MONITORNUM

