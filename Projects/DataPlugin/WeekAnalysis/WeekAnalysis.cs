using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PluginSDK;
using System.Resources;


namespace Reports
{
    public class WeekAnalysis : IPlugin
    {
        public string ParentId { get { return "b625757d-be46-442e-88c4-7ef2ab6513c3"; } }
        public string Name { get { return "WeekAnalysis"; } }
        public string GUID { get { return "2d69bb3f-cf4d-4cf5-9473-d840f51847a3"; } }
        public int type { get { return 0; } }

        public string DataSourceName { get { return "WeekAnalysis"; } }
        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("WeekAnalysis_Comment", Thread.CurrentThread.CurrentCulture);
        }
        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string, string> cols = new Dictionary<string, string>();
            cols.Add("Department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));
            cols.Add("Guests", rm.GetString("Guests", Thread.CurrentThread.CurrentCulture));
            cols.Add("AvgCheck", rm.GetString("AvgCheck", Thread.CurrentThread.CurrentCulture));
            cols.Add("GuestsFourWeeks", rm.GetString("GuestsFourWeeks", Thread.CurrentThread.CurrentCulture));
            cols.Add("AvgCheckFourWeeks", rm.GetString("AvgCheckFourWeeks", Thread.CurrentThread.CurrentCulture));

            cols.Add("GuestsLastWeek", rm.GetString("GuestsLastWeek", Thread.CurrentThread.CurrentCulture));
            cols.Add("AvgCheckLastWeek", rm.GetString("AvgCheckLastWeek", Thread.CurrentThread.CurrentCulture));
            cols.Add("GuestsLastFourWeeks", rm.GetString("GuestsLastFourWeeks", Thread.CurrentThread.CurrentCulture));
            cols.Add("AvgCheckLastFourWeeks", rm.GetString("AvgCheckLastFourWeeks", Thread.CurrentThread.CurrentCulture));
            cols.Add("WeekGuestsAnalysis", rm.GetString("WeekGuestsAnalysis", Thread.CurrentThread.CurrentCulture));
            cols.Add("WeekCheckAnalysis", rm.GetString("WeekCheckAnalysis", Thread.CurrentThread.CurrentCulture));
            cols.Add("FourWeekGuestsAnalysis", rm.GetString("FourWeekGuestsAnalysis", Thread.CurrentThread.CurrentCulture));
            cols.Add("FourWeekCheckAnalysis", rm.GetString("FourWeekCheckAnalysis", Thread.CurrentThread.CurrentCulture));

            return cols;
        }
        public DataTable GetTable()
        {
            DataTable table2 = new DataTable();
            table2.Columns.Add("Department", typeof(string));
            table2.Columns.Add("Guests", typeof(double));
            table2.Columns.Add("AvgCheck", typeof(double));

            table2.Columns.Add("GuestsFourWeeks", typeof(double));
            table2.Columns.Add("AvgCheckFourWeeks", typeof(double));

            table2.Columns.Add("GuestsLastWeek", typeof(double));
            table2.Columns.Add("AvgCheckLastWeek", typeof(double));

            table2.Columns.Add("WeekGuestsAnalysis", typeof(double));
            table2.Columns.Add("WeekCheckAnalysis", typeof(double));

            table2.Columns.Add("GuestsLastFourWeeks", typeof(double));
            table2.Columns.Add("AvgCheckLastFourWeeks", typeof(double));

            table2.Columns.Add("FourWeekGuestsAnalysis", typeof(double));
            table2.Columns.Add("FourWeekCheckAnalysis", typeof(double));


            return table2;
        }

        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime from,
            DateTime to)
        {
            DataTable tableRes = GetTable();
            //текущая неделя
            var res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department" },
        new string[] { "GuestNum", "DishDiscountSumInt", "UniqOrderId" }, from.AddDays(-6), to);
            var table = new List<Dictionary<string, object>>();
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString());
                }
            }
            if (res.data.Count == 0)
            {
                try
                {
                    var t = table.FirstOrDefault();
                    if (t != null)
                    {
                        t.Add("Guests", 0);
                        t.Add("Средний чек", 0);

                    }
                }
                catch (Exception ex)
                {
                    //Логгируем ошибку и идем дальше!
                    Console.WriteLine(ex.Message);
                }
            }
            foreach (var a in res.data)
            {

                try
                {
                    var t = new Dictionary<string, object>();
                    t.Add("Ресторан", a["Department"]);
                    t.Add("Guests", decimal.Parse(a["GuestNum"]));
                    t.Add("Средний чек",
                        (decimal.Parse(a["DishDiscountSumInt"]) / decimal.Parse(a["UniqOrderId"])));

                    table.Add(t);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            //текущие 4 недели
            res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department" },
        new string[] { "GuestNum", "DishDiscountSumInt", "UniqOrderId" }, from.AddDays(-27), to);
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]].Replace(".", ",");
                }
            }
            if (res.data.Count == 0)
            {
                try
                {
                    var t = table.FirstOrDefault();
                    if (t != null)
                    {
                        t.Add("GuestsFourWeeks", 0);
                        t.Add("Средний чек за 4 недели", 0);

                    }
                }
                catch (Exception ex)
                {
                    //Логгируем ошибку и идем дальше!
                    Console.WriteLine(ex.Message);
                }
            }

            foreach (var a in res.data)
            {
                try
                {
                    var t = table.FirstOrDefault(b => b["Ресторан"].ToString() == a["Department"]);
                    if (t != null)
                    {
                        t.Add("GuestsFourWeeks", decimal.Parse(a["GuestNum"]));
                        t.Add("Средний чек за 4 недели",
                            (decimal.Parse(a["DishDiscountSumInt"]) / decimal.Parse(a["UniqOrderId"])));
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            //предыдущая неделя

            res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department" },
        new string[] { "GuestNum", "DishDiscountSumInt", "UniqOrderId" }, from.AddDays(-13), to.AddDays(-7));
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]].Replace(".", ",");
                }
            }

            if (res.data.Count == 0)
            {
                try
                {
                    var t = table.FirstOrDefault();
                    if (t != null)
                    {
                        t.Add("GuestsLastWeek", 0);
                        t.Add("Средний чек к прошлой неделе", 0);
                        t.Add("Отношение гостей. Одна неделя", 0);
                        t.Add("Отношение чеков. Одна неделя", 0);

                    }
                }
                catch (Exception ex)
                {
                    //Логгируем ошибку и идем дальше!
                    Console.WriteLine(ex.Message);
                }
            }
            foreach (var a in res.data)
            {
                try
                {
                    var t = table.FirstOrDefault(b => b["Ресторан"].ToString() == a["Department"]);
                    if (t != null)
                    {
                        t.Add("GuestsLastWeek", decimal.Parse(a["GuestNum"]));
                        t.Add("Средний чек к прошлой неделе",
                            (decimal.Parse(a["DishDiscountSumInt"]) / decimal.Parse(a["UniqOrderId"])));
                        t.Add("Отношение гостей. Одна неделя", (decimal)t["GuestsLastWeek"] > 0 ? ((decimal)t["Guests"] * 100 / (decimal)t["GuestsLastWeek"] - 100) : 0);
                        t.Add("Отношение чеков. Одна неделя", (decimal)t["Средний чек к прошлой неделе"] > 0 ? 
                            ((decimal)t["Средний чек"] * 100 / (decimal)t["Средний чек к прошлой неделе"] - 100) : 0);

                    }
                }
                catch (Exception ex)
                {
                    //Логгируем ошибку и идем дальше!
                    Console.WriteLine(ex.Message);
                }
            }

            //предыдущие 4 недели
            res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department" },
        new string[] { "GuestNum", "DishDiscountSumInt", "UniqOrderId" }, from.AddDays(-55), to.AddDays(-28));
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]].Replace(".", ",");
                }
            }
            if (res.data.Count == 0)
            {
                try
                {
                    var t = table.FirstOrDefault();
                    if (t != null)
                    {
                        t.Add("GuestsLastFourWeeks", 0);
                        t.Add("Средний чек к прошлым 4 неделям", 0);
                        t.Add("Отношение гостей. Четыре недели", 0);
                        t.Add("Отношение чеков. Четыре недели", 0);

                    }
                }
                catch (Exception ex)
                {
                    //Логгируем ошибку и идем дальше!
                    Console.WriteLine(ex.Message);
                }
            }

            foreach (var a in res.data)
            {
                try
                {
                    var t = table.FirstOrDefault(b => b["Ресторан"].ToString() == a["Department"]);
                    if (t != null)
                    {
                        t.Add("GuestsLastFourWeek", decimal.Parse(a["GuestNum"]));
                        t.Add("Средний чек к прошлым 4 неделям",
                            (decimal.Parse(a["DishDiscountSumInt"]) / decimal.Parse(a["UniqOrderId"])));
                        t.Add("Отношение гостей. Четыре недели", (decimal)t["GuestsLastFourWeek"] > 0 ? ((decimal)t["GuestsFourWeeks"] * 100 / (decimal)t["GuestsLastFourWeek"] - 100) : 0);
                        t.Add("Отношение чеков. Четыре недели", (decimal)t["Средний чек к прошлым 4 неделям"] > 0 ? 
                            ((decimal)t["Средний чек за 4 недели"] * 100 / (decimal)t["Средний чек к прошлым 4 неделям"] - 100) : 0);

                    }
                }
                catch (Exception ex)
                {
                    //Логгируем ошибку и идем дальше!
                    Console.WriteLine(ex.Message);
                }
            }
            try
            {
                foreach (var line in table)
                {
                    var data = line.Select(a => a.Value).Select(a => (object)a).ToArray();
                    tableRes.Rows.Add(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
            return tableRes;
        }

    }
}