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
    public class GuestAnalysis : IPlugin
    {
        public string ParentId { get { return "b625757d-be46-442e-88c4-7ef2ab6513c3"; } }
        public string Name { get { return "GuestAnalysis"; } }
        public string GUID { get { return "c8bbe772-0cf3-4a48-8717-32cdab22fced"; } }
        public int type { get { return 0; } }

        public string DataSourceName { get { return "GuestAnalysis"; } }
        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("GuestAnalysis_Comment", Thread.CurrentThread.CurrentCulture);
        }
        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string, string> cols = new Dictionary<string, string>();
            cols.Add("Week", rm.GetString("Week", Thread.CurrentThread.CurrentCulture));
            cols.Add("Department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));
            cols.Add("Guests", rm.GetString("Guests", Thread.CurrentThread.CurrentCulture));
            cols.Add("GuestsAnalysis", rm.GetString("GuestsAnalysis", Thread.CurrentThread.CurrentCulture));

            return cols;
        }
        public DataTable GetTable()
        {
            DataTable table2 = new DataTable();
            table2.Columns.Add("Week", typeof(string));
            table2.Columns.Add("Department", typeof(string));
            table2.Columns.Add("Guests", typeof(double));
            table2.Columns.Add("GuestsAnalysis", typeof(double));



            return table2;
        }

        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime from,
            DateTime to)
        {
            DataTable tableRes = GetTable();
            //1 неделя
            var res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department" },
        new string[] { "GuestNum" }, from.AddDays(-34), to.AddDays(-28));
            var table = new List<Dictionary<string, object>>();
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString());
                }
            }
            foreach (var a in res.data)
            {

                try
                {
                    var t = new Dictionary<string, object>();
                    t.Add("Week", "1");
                    t.Add("Department", a["Department"]);
                    t.Add("Guests", decimal.Parse(a["GuestNum"]));
                    t.Add("GuestsAnalysis", 0);
                    table.Add(t);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            //2 неделя
            res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department" },
        new string[] { "GuestNum" }, from.AddDays(-27), to.AddDays(-21));
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]].Replace(".", ",");
                }
            }
            foreach (var a in res.data)
            {
                try
                {
                    var t = new Dictionary<string, object>();
                    t.Add("Week", "2");
                    t.Add("Department", a["Department"]);
                    t.Add("Guests", decimal.Parse(a["GuestNum"]));
                    var GuestsLastWeek = table.FirstOrDefault(b => b["Week"].ToString() == "1" && b["Department"].ToString() == a["Department"])["Guests"];
                    t.Add("GuestsAnalysis", ((decimal)GuestsLastWeek > 0 ? (decimal.Parse(a["GuestNum"]) * 100 / (decimal)GuestsLastWeek - 100) : 0));
                    table.Add(t);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            //3 неделя
            res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department" },
        new string[] { "GuestNum" }, from.AddDays(-20), to.AddDays(-14));
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]].Replace(".", ",");
                }
            }
            foreach (var a in res.data)
            {
                try
                {
                    var t = new Dictionary<string, object>();
                    t.Add("Week", "3");
                    t.Add("Department", a["Department"]);                    
                    t.Add("Guests", decimal.Parse(a["GuestNum"]));
                    var GuestsLastWeek = table.FirstOrDefault(b => b["Week"].ToString() == "2" && b["Department"].ToString() == a["Department"])["Guests"];
                    t.Add("GuestsAnalysis", ((decimal)GuestsLastWeek > 0 ? (decimal.Parse(a["GuestNum"]) * 100 / (decimal)GuestsLastWeek - 100) : 0));
                    table.Add(t);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            //4 неделя
            res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department" },
        new string[] { "GuestNum" }, from.AddDays(-13), to.AddDays(-7));
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]].Replace(".", ",");
                }
            }

            foreach (var a in res.data)
            {
                try
                {
                    var t = new Dictionary<string, object>();
                    t.Add("Week", "4");
                    t.Add("Department", a["Department"]);                    
                    t.Add("Guests", decimal.Parse(a["GuestNum"]));
                    var GuestsLastWeek = table.FirstOrDefault(b => b["Week"].ToString() == "3" && b["Department"].ToString() == a["Department"])["Guests"];
                    t.Add("GuestsAnalysis", ((decimal)GuestsLastWeek > 0 ? (decimal.Parse(a["GuestNum"]) * 100 / (decimal)GuestsLastWeek - 100) : 0));
                    table.Add(t);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            //5 неделя
            res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department" },
        new string[] { "GuestNum" }, from.AddDays(-6), to);
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]].Replace(".", ",");
                }
            }

            foreach (var a in res.data)
            {
                try
                {
                    var t = new Dictionary<string, object>();
                    t.Add("Week", "5");
                    t.Add("Department", a["Department"]);
                    t.Add("Guests", decimal.Parse(a["GuestNum"]));
                    var GuestsLastWeek = table.FirstOrDefault(b => b["Week"].ToString() == "4" && b["Department"].ToString() == a["Department"])["Guests"];
                    t.Add("GuestsAnalysis", ((decimal)GuestsLastWeek > 0 ? (decimal.Parse(a["GuestNum"]) * 100 / (decimal)GuestsLastWeek - 100) : 0));
                    table.Add(t);
                }
                catch (Exception ex)
                {
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
