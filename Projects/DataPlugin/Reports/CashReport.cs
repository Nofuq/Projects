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
    public class MixTypePayment : IPlugin
    {
        public string ParentId
        {
            get
            {
                return "b625757d-be46-442e-88c4-7ef2ab6513c3";
            }
        }
        public string GUID
        {
            get
            {
                return "2a639be1-7011-462e-8367-1f9007147a7e";
            }
        }
        public string Name
        {
            get
            {
                return "MixTypePayment";
            }

        }
        public string DataSourceName
        {
            get
            {
                return "MixTypePayment";
            }

        }

        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("MixTypePayment_Comment", Thread.CurrentThread.CurrentCulture);
        }

        public int type
        {
            get
            {
                return 0;
            }

        }

        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string, string> res = new Dictionary<string, string>();
              res.Add("Department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));            
                res.Add("OpenDate", rm.GetString("OpenDate", Thread.CurrentThread.CurrentCulture));
                res.Add("CashRegisterName", rm.GetString("CashRegisterName", Thread.CurrentThread.CurrentCulture));
                res.Add("SessionNum", rm.GetString("SessionNum", Thread.CurrentThread.CurrentCulture));
                res.Add("Visa", rm.GetString("Visa", Thread.CurrentThread.CurrentCulture));
                res.Add("Credit", rm.GetString("Credit", Thread.CurrentThread.CurrentCulture));
                res.Add("Nal", rm.GetString("Nal", Thread.CurrentThread.CurrentCulture));
                res.Add("NoPay", rm.GetString("NoPay", Thread.CurrentThread.CurrentCulture));
                res.Add("SummPay", rm.GetString("SummPay", Thread.CurrentThread.CurrentCulture));
                return res;
           
        }
        public DataTable GetTable()
        {
            // создаем таблицу  
            DataTable table = new DataTable();
            table.Columns.Add("Department", typeof(string));
            table.Columns.Add("OpenDate", typeof(DateTime));
            table.Columns.Add("CashRegisterName", typeof(string));
            table.Columns.Add("SessionNum", typeof(int));
            table.Columns.Add("Visa", typeof(double));
            table.Columns.Add("Credit", typeof(double));
            table.Columns.Add("Nal", typeof(double));
            table.Columns.Add("NoPay", typeof(double));
            table.Columns.Add("SummPay", typeof(double));
            return table;
        }
        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime FirstDate, DateTime LastDate)
        {
            DataTable table = GetTable();
            var res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department", "CashRegisterName", "SessionNum", "PayTypes", "OpenDate.Typed" },
new string[] { "DishSumInt" }, FirstDate, LastDate);


            var tempTable = new List<Dictionary<string, object>>();
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]] != null ? line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString()) : null;
                }
            }

            bool debug = false;
            //для начала получаем все номера смен(группируя по ним)
            var SessionNums = res.data.GroupBy(a => a["SessionNum"]);


            String SummPayAll = "0";
            //пошли в цикл по всем сменам
            foreach (var b in SessionNums)
            {
                try
                {
                    var SessionData = res.data.Where(a => a["SessionNum"] == b.Key);

                    var cashes = SessionData.Select(a => a["CashRegisterName"]).Distinct();
                    foreach (var cash in cashes)
                    {

                        var CashSessionData = SessionData.Where(a => a["CashRegisterName"] == cash);
                        //  Console.WriteLine(SessionData.Count());
                        var t = new Dictionary<string, object>();
                        //тут будем пробовать заполнить дикшенари таблицы
                        string Department = "";
                        String CashName = "";
                        String Visa = "0";
                        String Credit = "0";
                        String Nal = "0";
                        String NoPay = "0";
                        String SummPay = "0";
                        String SessionDate = "";
                        foreach (var c in CashSessionData)
                        {
                            string t_CashName = "";
                            if (c.TryGetValue("CashRegisterName", out t_CashName))
                            {
                                CashName = t_CashName;
                            }
                            string t_PayTypes = "";
                            if (c.TryGetValue("PayTypes", out t_PayTypes))
                            {
                                if (t_PayTypes == "Visa")
                                {
                                    c.TryGetValue("DishSumInt", out Visa);
                                }
                                if (t_PayTypes == "В кредит")
                                {
                                    c.TryGetValue("DishSumInt", out Credit);
                                }
                                if (t_PayTypes == "(без оплаты)")
                                {
                                    c.TryGetValue("DishSumInt", out NoPay);
                                }
                                if (t_PayTypes == "Наличные")
                                {
                                    c.TryGetValue("DishSumInt", out Nal);
                                }

                            }
                            string t_SessionDate = "";
                            if (c.TryGetValue("OpenDate.Typed", out t_SessionDate))
                            {
                                SessionDate = t_SessionDate;
                            }
                            string t_Department = "";
                            if (c.TryGetValue("Department", out t_Department))
                            {
                                Department = t_Department;
                            }
                        }
                        SummPay =
                            (decimal.Parse(Visa) + decimal.Parse(Credit) + decimal.Parse(Nal) + decimal.Parse(NoPay))
                                .ToString();
                        t.Add("Department", Department);
                        t.Add("OpenDate", DateTime.Parse(SessionDate));
                        t.Add("CashRegisterName", CashName);
                        t.Add("SessionNum", int.Parse(b.Key));
                        t.Add("Visa", double.Parse(Visa));
                        t.Add("Credit", double.Parse(Credit));
                        t.Add("Nal", double.Parse(Nal));
                        t.Add("NoPay", double.Parse(NoPay));
                        t.Add("SummPay", double.Parse(SummPay));

                        tempTable.Add(t);
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
                foreach (var line in tempTable)
                {
                    var data = line.Select(a => a.Value).Select(a => (object)a).ToArray();
                    table.Rows.Add(data);
                }
            }
            catch (Exception ex)
            {
                Log.WriteToLog(GUID,ex.Message);
            }
            Log.WriteToLog(GUID,string.Format("Таблица сформирована. Количество записей: {0}", table.Rows.Count));
            return table;
        }
    }

    public class ProductMix : IPlugin
    {
        public string ParentId { get { return "b625757d-be46-442e-88c4-7ef2ab6513c3"; } }
        public string Name { get { return "ProductMix"; } }
        public string GUID { get { return "d8c4ff9d-9410-4dfe-b7bf-3b5f3d119751"; } }
        public int type { get { return 0; } }

        public string DataSourceName
        {
            get { return "ProductMix"; } 
        }
        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("ProductMix_Comment", Thread.CurrentThread.CurrentCulture);
        }

        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string,string> cols = new Dictionary<string, string>();
            cols.Add("Department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));
            cols.Add("DishCategory", rm.GetString("DishCategory", Thread.CurrentThread.CurrentCulture));
            cols.Add("DishName", rm.GetString("DishName", Thread.CurrentThread.CurrentCulture));
            cols.Add("Sum", rm.GetString("Sum", Thread.CurrentThread.CurrentCulture));
            cols.Add("Amount", rm.GetString("Amount", Thread.CurrentThread.CurrentCulture));
            cols.Add("Cost", rm.GetString("Cost", Thread.CurrentThread.CurrentCulture));
            cols.Add("AvgPrict", rm.GetString("AvgPrict", Thread.CurrentThread.CurrentCulture));
            cols.Add("GrossProffit", rm.GetString("GrossProffit", Thread.CurrentThread.CurrentCulture));
            cols.Add("SumPercent", rm.GetString("SumPercent", Thread.CurrentThread.CurrentCulture));
            cols.Add("CostPercent", rm.GetString("CostPercent", Thread.CurrentThread.CurrentCulture));
            return cols;
        }

        public DataTable GetTable()
        {
            DataTable table1 = new DataTable();
            table1.Columns.Add("Department", typeof(string));
            table1.Columns.Add("DishCategory", typeof(string));
            table1.Columns.Add("DishName", typeof(string));
            table1.Columns.Add("Sum", typeof(double));
            table1.Columns.Add("Amount", typeof(double));
            table1.Columns.Add("Cost", typeof(double));
            table1.Columns.Add("AvgPrict", typeof(double));
            table1.Columns.Add("GrossProffit", typeof(double));
            table1.Columns.Add("SumPercent", typeof(double));
            table1.Columns.Add("CostPercent", typeof(double));
            return table1;
        }

        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime FirstDate,
            DateTime LastDate)
        {
            // создаем таблицу  
            DataTable table = GetTable();


            var res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department", "DishName", "DishCategory" },
                new string[] { "DishAmountInt", "ProductCostBase.ProductCost", "DishDiscountSumInt" }, FirstDate, LastDate);

            var tempTable = new List<Dictionary<string, object>>();
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]] != null ? line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString()) : null;
                }
            }



            var sumPerc = res.data.Select(a => a["DishDiscountSumInt"]).Where(a => a != null).Select(a => double.Parse(a)).Sum();

            var sumSeb =
                        res.data.Select(a => a["ProductCostBase.ProductCost"]).Where(a => a != null).Select(a => double.Parse(a)).Sum();

            foreach (var a in res.data)
            {
                try
                {
                    var t = new Dictionary<string, object>();
                    t.Add("Точка продаж", a["Department"]);
                    t.Add("Категория", a["DishCategory"]);
                    t.Add("Наименование", a["DishName"]);
                    t.Add("Выручка", double.Parse(a["DishDiscountSumInt"]));
                    t.Add("Количество", double.Parse(a["DishAmountInt"]));
                    t.Add("Себестоимость", double.Parse(a["ProductCostBase.ProductCost"]));
                    t.Add("Средняя цена",
                        (double.Parse(a["DishDiscountSumInt"]) / double.Parse(a["DishAmountInt"])));
                    t.Add("Валовая прибыль",
                        (double.Parse(a["DishDiscountSumInt"]) - double.Parse(a["ProductCostBase.ProductCost"])));
                    if (sumPerc != 0)
                        t.Add("% от выручки", (double.Parse(a["DishDiscountSumInt"])*100 / sumPerc));
                    else t.Add("% от выручки", 0);
                    if (sumSeb != 0)
                        t.Add("% от себестоимости", (double.Parse(a["ProductCostBase.ProductCost"])*100 / sumSeb));
                    else t.Add("% от себестоимости", 0);
                    tempTable.Add(t);
                }
                catch (Exception ex)
                {
                    //Логгируем ошибку и идем дальше!
                    Console.WriteLine(ex.Message);
                }

            }

            try
            {
                foreach (var line in tempTable)
                {
                    var data = line.Select(a => a.Value).Select(a => (object)a).ToArray();
                    table.Rows.Add(data);
                }
            }
            catch (Exception ex)
            {

            }

            // заполняем данными.


            // отдаем
            return table;
        }
    }

    public class BasicParametersPeriods : IPlugin
    {
        public string ParentId { get { return "b625757d-be46-442e-88c4-7ef2ab6513c3"; } }
        public string Name { get { return "BasicParametersPeriods"; } }
        public string GUID { get { return "19eee4f0-ea8b-4dc3-9549-c67afd282d49"; } }
        public int type { get { return 0; } }

        public string DataSourceName
        {
            get { return "BasicParametersPeriods"; }
        }

        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("Report1_Comment", Thread.CurrentThread.CurrentCulture);
        }
        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string,string> cols = new Dictionary<string, string>();
            cols.Add("Department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));
            cols.Add("Sum", rm.GetString("Sum", Thread.CurrentThread.CurrentCulture));
            cols.Add("Checks", rm.GetString("Checks", Thread.CurrentThread.CurrentCulture));
            cols.Add("Guests", rm.GetString("Guests", Thread.CurrentThread.CurrentCulture));
            cols.Add("AvgCheck", rm.GetString("AvgCheck", Thread.CurrentThread.CurrentCulture));
            cols.Add("CostPers", rm.GetString("CostPers", Thread.CurrentThread.CurrentCulture));
            

            cols.Add("SumLastWeek", rm.GetString("SumLastWeek", Thread.CurrentThread.CurrentCulture));
            cols.Add("ChecksLastWeek", rm.GetString("ChecksLastWeek", Thread.CurrentThread.CurrentCulture));
            cols.Add("AvgCheckLastWeek", rm.GetString("AvgCheckLastWeek", Thread.CurrentThread.CurrentCulture));
            cols.Add("GuestsLastWeek", rm.GetString("GuestsLastWeek", Thread.CurrentThread.CurrentCulture));

            cols.Add("SumLastMonth", rm.GetString("SumLastMonth", Thread.CurrentThread.CurrentCulture));
            cols.Add("ChecksLastMonth", rm.GetString("ChecksLastMonth", Thread.CurrentThread.CurrentCulture));
            cols.Add("AvgCheckLasMonth", rm.GetString("AvgCheckLasMonth", Thread.CurrentThread.CurrentCulture));
            cols.Add("GuestsLastMonth", rm.GetString("GuestsLastMonth", Thread.CurrentThread.CurrentCulture));

            cols.Add("SumLastYear", rm.GetString("SumLastYear", Thread.CurrentThread.CurrentCulture));
            cols.Add("ChecksLastYear", rm.GetString("ChecksLastYear", Thread.CurrentThread.CurrentCulture));
            cols.Add("AvgCheckLasYear", rm.GetString("AvgCheckLasYear", Thread.CurrentThread.CurrentCulture));
            cols.Add("GuestsLastYear", rm.GetString("GuestsLastYear", Thread.CurrentThread.CurrentCulture));
            return cols;
        }

        public DataTable GetTable()
        {
            DataTable table2 = new DataTable();
            table2.Columns.Add("Department", typeof(string));
            table2.Columns.Add("Sum", typeof(double));
            table2.Columns.Add("Checks", typeof(double));
            table2.Columns.Add("Guests", typeof(double));
            table2.Columns.Add("AvgCheck", typeof(double));
            table2.Columns.Add("CostPers", typeof(double));
            

            table2.Columns.Add("SumLastWeek", typeof(double));
            table2.Columns.Add("ChecksLastWeek", typeof(double));
            table2.Columns.Add("AvgCheckLastWeek", typeof(double));
            table2.Columns.Add("GuestsLastWeek", typeof(double));

            table2.Columns.Add("SumLastMonth", typeof(double));
            table2.Columns.Add("ChecksLastMonth", typeof(double));
            table2.Columns.Add("AvgCheckLasMonth", typeof(double));
            table2.Columns.Add("GuestsLastMonth", typeof(double));

            table2.Columns.Add("SumLastYear", typeof(double));
            table2.Columns.Add("ChecksLastYear", typeof(double));
            table2.Columns.Add("AvgCheckLasYear", typeof(double));
            table2.Columns.Add("GuestsLastYear", typeof(double));

            return table2;
        }

        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime from,
            DateTime to)
        {
            // создаем таблицу  
            DataTable tableRes = GetTable();
            //Текущий период
            var res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department"},
        new string[] { "DishDiscountSumInt", "DishAmountInt", "ProductCostBase.ProductCost", "GuestNum", "UniqOrderId" }, from, to);
            var table = new List<Dictionary<string, object>>();
            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString());
                }
            }
            var sumSeb =
                        res.data.Select(a => a["ProductCostBase.ProductCost"]).Where(a => a != null).Select(a => decimal.Parse(a)).Sum();
            foreach (var a in res.data)
            {
                try
                {
                    var t = new Dictionary<string, object>();
                    t.Add("Ресторан", a["Department"]);
                   
                    t.Add("Продажи", decimal.Parse(a["DishDiscountSumInt"]));
                    t.Add("Чеки", decimal.Parse(a["UniqOrderId"]));
                    t.Add("Guests", decimal.Parse(a["GuestNum"]));

                    t.Add("Средний чек",
                        (decimal.Parse(a["DishDiscountSumInt"]) / decimal.Parse(a["UniqOrderId"])));

                    if (sumSeb != 0)
                        t.Add("Себестоимость, %", (decimal.Parse(a["ProductCostBase.ProductCost"])*100 / sumSeb));
                    else t.Add("Себестоимость, %", 0);

                    table.Add(t);
                }
                catch (Exception ex)
                {
                    //Логгируем ошибку и идем дальше!
                    Console.WriteLine(ex.Message);
                }
            }

            //Неделя назад
            res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department" },
        new string[] { "DishDiscountSumInt", "DishAmountInt", "ProductCostBase.ProductCost", "GuestNum", "UniqOrderId" }, from.AddDays(-7), to.AddDays(-7));
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
                    var t = table.FirstOrDefault(b => b["Ресторан"].ToString() == a["Department"]);
                    if (t != null)
                    {

                        t.Add("Продажи к прошлой неделе", decimal.Parse(a["DishDiscountSumInt"]) > 0 ? ((decimal)t["Продажи"]*100 / decimal.Parse(a["DishDiscountSumInt"])) : 0);
                        t.Add("Чеки к прошлой неделе", decimal.Parse(a["UniqOrderId"]) > 0 ? ((decimal)t["Чеки"]*100 / decimal.Parse(a["UniqOrderId"])) : 0);
                        t.Add("Средний чек к прошлой неделеле",
                          decimal.Parse(a["UniqOrderId"]) > 0 ? ((decimal)t["Средний чек"]*100 / (decimal.Parse(a["DishDiscountSumInt"]) / decimal.Parse(a["UniqOrderId"]))) : 0);
                        t.Add("GuestsLasWeek", decimal.Parse(a["GuestNum"]) > 0 ? ((decimal)t["Guests"] * 100 / decimal.Parse(a["GuestNum"])) : 0);

                    }
                }
                catch (Exception ex)
                {
                    //Логгируем ошибку и идем дальше!
                    Console.WriteLine(ex.Message);
                }
            }

            //Месяц назад
            res = OrdersApi.Olap(tomcat, login, passw,  new string[] { "Department"},
        new string[] { "DishDiscountSumInt", "DishAmountInt", "ProductCostBase.ProductCost", "GuestNum" , "UniqOrderId" }, from.AddYears(-1), to.AddYears(-1));
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
                    var t = table.FirstOrDefault(b => b["Ресторан"].ToString() == a["Department"]);
                    if (t != null)
                    {
                        t.Add("Продажи к прошлому месяцу", decimal.Parse(a["DishDiscountSumInt"]) > 0 ? ((decimal)t["Продажи"]*100 / decimal.Parse(a["DishDiscountSumInt"])) : 0);
                        t.Add("Чеки к прошлому месяцу", decimal.Parse(a["UniqOrderId"]) > 0 ? ((decimal)t["Чеки"] * 100 / decimal.Parse(a["UniqOrderId"])) : 0);
                        t.Add("Средний чек к прошлому месяцу",
                          decimal.Parse(a["UniqOrderId"]) > 0 ? ((decimal)t["Средний чек"]*100 / (decimal.Parse(a["DishDiscountSumInt"]) / decimal.Parse(a["UniqOrderId"]))) : 0);
                        t.Add("GuestsLasMonth", decimal.Parse(a["GuestNum"]) > 0 ? ((decimal)t["Guests"] * 100 / decimal.Parse(a["GuestNum"])) : 0);
                    }
                }
                catch (Exception ex)
                {
                    //Логгируем ошибку и идем дальше!
                    Console.WriteLine(ex.Message);
                }
            }


            //Год назад
            res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department"},
        new string[] { "DishDiscountSumInt", "DishAmountInt", "ProductCostBase.ProductCost", "GuestNum", "UniqOrderId" }, from.AddMonths(-1), to.AddMonths(-1));
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
                    var t = table.FirstOrDefault(b => b["Ресторан"].ToString() == a["Department"] );
                    if (t != null)
                    {
                        t.Add("Продажи к прошлому году", decimal.Parse(a["DishDiscountSumInt"]) > 0 ? ((decimal)t["Продажи"]*100 / decimal.Parse(a["DishDiscountSumInt"])) : 0);
                        t.Add("Чеки к прошлому году", decimal.Parse(a["UniqOrderId"]) > 0 ? ((decimal)t["Чеки"] * 100 / decimal.Parse(a["UniqOrderId"])) : 0);
                        t.Add("Средний чек к прошлому году",
                          decimal.Parse(a["UniqOrderId"]) > 0 ? ((decimal)t["Средний чек"]*100 / (decimal.Parse(a["DishDiscountSumInt"]) / decimal.Parse(a["UniqOrderId"]))) : 0);
                        t.Add("GuestsLasYear", decimal.Parse(a["GuestNum"]) > 0 ? ((decimal)t["Guests"] * 100 / decimal.Parse(a["GuestNum"])) : 0);
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

            }

            return tableRes;
        }
    }

    public class PLAN_FACT : IPlugin
    {
        public string ParentId { get { return "b625757d-be46-442e-88c4-7ef2ab6513c3"; } }
        public string Name { get { return "PLAN_FACT"; } }
        public string GUID { get { return "84755efb-71a6-47e7-a691-019993b77a66"; } }
        public int type { get { return 0; } }

        public string DataSourceName { get { return "PLAN_FACT"; } }
        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("PLAN_FACT_Comment", Thread.CurrentThread.CurrentCulture);
        }

        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string, string> cols = new Dictionary<string, string>();
            cols.Add("Department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));
            cols.Add("date", rm.GetString("date", Thread.CurrentThread.CurrentCulture));
            cols.Add("sum_fact", rm.GetString("sum_fact", Thread.CurrentThread.CurrentCulture));
            cols.Add("sum_plan", rm.GetString("sum_plan", Thread.CurrentThread.CurrentCulture));
            cols.Add("sum_delta", rm.GetString("sum_delta", Thread.CurrentThread.CurrentCulture));
            cols.Add("sum_delta_perc", rm.GetString("sum_delta_perc", Thread.CurrentThread.CurrentCulture));
            return cols;
        }

        public DataTable GetTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Department", typeof(string));
            dt.Columns.Add("date", typeof(DateTime));
            dt.Columns.Add("sum_fact", typeof(double));
            dt.Columns.Add("sum_plan", typeof(double));
            dt.Columns.Add("sum_delta", typeof(double));
            dt.Columns.Add("sum_delta_perc", typeof(double));
            return dt;
        }

        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime FirstDate,
            DateTime LastDate)
        {
            DataTable table = GetTable();
            var res = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department", "OpenDate.Typed" },
                new string[] { "DishDiscountSumInt", "DishAmountInt" }, FirstDate, LastDate);

            foreach (var line in res.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]] != null ? line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString()) : null;
                }
            }

            var departments = OrdersApi.Departments(tomcat, login, passw);
            if (departments.Items.Count(a => a.type == "DEPARTMENT") > 1)
            {
                foreach (var dep in res.data.Select(a => a["Department"]).Distinct())
                {
                    var depId = departments.Items.FirstOrDefault(a => a.name == dep).id;
                    var plan = OrdersApi.Plan(tomcat, login, passw, FirstDate, LastDate, depId);
                    foreach (var data in res.data.Where(a => a["Department"] == dep))
                    {
                        var department = data["Department"];
                        var date = DateTime.Parse(data["OpenDate.Typed"]);
                        var sum_fact = decimal.Parse(data["DishDiscountSumInt"]);
                        if (plan != null && plan.Items != null && plan.Items.Length > 0)
                        {
                            var sum_plan = plan.Items.FirstOrDefault(a => a.date == date.ToString("dd.MM.yyyy")) != null
                                ? plan.Items.FirstOrDefault(a => a.date == date.ToString("dd.MM.yyyy")).planValue
                                : (decimal?) null;
                            var sum_delta = sum_fact - sum_plan;
                            var sum_delta_perc = sum_fact*100/sum_plan;


                            table.Rows.Add(department, date, sum_fact, sum_plan.Value, sum_delta.Value,
                                sum_delta_perc.Value);
                        }
                        else
                        {
                            table.Rows.Add(department, date, sum_fact, 0,0,0);

                        }
                    }
                }
            }
            else
            {
                var depId = departments.Items.FirstOrDefault().id;
                var plan = OrdersApi.Plan(tomcat, login, passw, FirstDate, LastDate, depId);
                foreach (var data in res.data)
                {
                    var department = data["Department"];
                    var date = DateTime.Parse(data["OpenDate.Typed"]);
                    var sum_fact = decimal.Parse(data["DishDiscountSumInt"]);
                    if (plan != null && plan.Items != null && plan.Items.Length > 0)
                    {
                        var sum_plan = plan.Items.FirstOrDefault(a => a.date == data["OpenDate.Typed"]) != null
                            ? plan.Items.FirstOrDefault(a => a.date == data["OpenDate.Typed"]).planValue
                            : (decimal?) null;
                        var sum_delta = sum_fact - sum_plan;
                        var sum_delta_perc = sum_fact*100/sum_plan;

                        table.Rows.Add(department, date, sum_fact, sum_plan, sum_delta, sum_delta_perc);
                    }
                    else
                    {
                        table.Rows.Add(department, date, sum_fact, 0,0,0);
                    }

                }
            }

            return table;
        }
    }

    public class ABS : IPlugin
    {
        public string ParentId { get { return "b625757d-be46-442e-88c4-7ef2ab6513c3"; } }
        public string Name { get { return "ABC"; } }
        public string GUID { get { return "a2e71d34-811b-4a98-a3f4-aade9aecdad8"; } }
        public int type { get { return 0; } }

        public string DataSourceName { get { return "ABC"; } }
        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("ABC_Comment", Thread.CurrentThread.CurrentCulture);
        }
        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string, string> cols = new Dictionary<string, string>();
            cols.Add("Department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));
            cols.Add("DishCategory", rm.GetString("DishCategory", Thread.CurrentThread.CurrentCulture));
            cols.Add("Goods", rm.GetString("Goods", Thread.CurrentThread.CurrentCulture));
            cols.Add("Amount", rm.GetString("Amount", Thread.CurrentThread.CurrentCulture));
            cols.Add("AmountPercent", rm.GetString("AmountPercent", Thread.CurrentThread.CurrentCulture));
            cols.Add("AmountItog", rm.GetString("AmountItog", Thread.CurrentThread.CurrentCulture));


            cols.Add("DishSumInt", rm.GetString("DishSumInt", Thread.CurrentThread.CurrentCulture));
            cols.Add("DishSumIntPercent", rm.GetString("DishSumIntPercent", Thread.CurrentThread.CurrentCulture));
            cols.Add("DishSumIntItog", rm.GetString("DishSumIntItog", Thread.CurrentThread.CurrentCulture));


            cols.Add("ProductCost_Profit", rm.GetString("ProductCost_Profit", Thread.CurrentThread.CurrentCulture));
            cols.Add("ProductCost_ProfitPercent", rm.GetString("ProductCost_ProfitPercent", Thread.CurrentThread.CurrentCulture));
            cols.Add("ProductCost_ProfitItog", rm.GetString("ProductCost_ProfitItog", Thread.CurrentThread.CurrentCulture));

            return cols;
        }

        public DataTable GetTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Department", typeof(string));
            dt.Columns.Add("DishCategory", typeof(string));
            dt.Columns.Add("Goods", typeof(string));

            dt.Columns.Add("Amount", typeof(double));
            dt.Columns.Add("AmountPercent", typeof(double));
            dt.Columns.Add("AmountItog", typeof(double));


            dt.Columns.Add("DishSumInt", typeof(double));
            dt.Columns.Add("DishSumIntPercent", typeof(double));
            dt.Columns.Add("DishSumIntItog", typeof(double));


            dt.Columns.Add("ProductCost_Profit", typeof(double));
            dt.Columns.Add("ProductCost_ProfitPercent", typeof(double));
            dt.Columns.Add("ProductCost_ProfitItog", typeof(double));

            return dt;
        }

        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime FirstDate,
            DateTime LastDate)
        {
            var table = GetTable();
            var data = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department", "DishCategory", "DishName" }, new string[] { "DishAmountInt", "ProductCostBase.Profit", "DishSumInt" }, FirstDate, LastDate);
            foreach (var line in data.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]] != null ? line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString()) : null;
                }
            }
            var sumAmount = data.data.Sum(a => decimal.Parse(a["DishAmountInt"]));
            var sumDishSumInt = data.data.Sum(a => decimal.Parse(a["DishSumInt"]));
            var sumProfit = data.data.Sum(a => decimal.Parse(a["ProductCostBase.Profit"]));

            decimal AmountItog = 0;
            decimal SumItog = 0;
            decimal ProfitItog = 0;

            foreach (var line in data.data.OrderBy(a => decimal.Parse(a["DishAmountInt"])).Reverse())
            {
                line.Add("DishAmountIntPerc", (decimal.Parse(line["DishAmountInt"])*100 / sumAmount).ToString());
                AmountItog += (decimal.Parse(line["DishAmountInt"])*100 / sumAmount);
                line.Add("DishAmountIntItog", AmountItog.ToString());

            }
            foreach (var line in data.data.OrderBy(a => decimal.Parse(a["DishSumInt"])).Reverse())
            {
                line.Add("DishSumIntPerc", (decimal.Parse(line["DishSumInt"]) * 100 / sumDishSumInt).ToString());
                SumItog += (decimal.Parse(line["DishSumInt"]) * 100 / sumDishSumInt);
                line.Add("DishSumIntItog", SumItog.ToString());

            }
            foreach (var line in data.data.OrderBy(a => decimal.Parse(a["ProductCostBase.Profit"])).Reverse())
            {
                line.Add("ProductCostBase.ProfitPerc", (decimal.Parse(line["ProductCostBase.Profit"]) * 100 / sumProfit).ToString());
                ProfitItog += (decimal.Parse(line["ProductCostBase.Profit"]) * 100 / sumProfit);
                line.Add("ProductCostBase.ProfitItog", ProfitItog.ToString());
            }

            foreach (var line in data.data)
            {
                var department = line["Department"];
                var category = line["DishCategory"];
                var goods = line["DishName"];

                var amount = line["DishAmountInt"];
                var amountPerc = line["DishAmountIntPerc"];
                var amountItog = line["DishAmountIntItog"];


                var DishSumInt = line["DishSumInt"];
                var DishSumIntPerc = line["DishSumIntPerc"];
                var DishSumIntItog = line["DishSumIntItog"];


                var ProductCost_Profit = line["ProductCostBase.Profit"];
                var ProductCost_ProfitPerc = line["ProductCostBase.ProfitPerc"];
                var ProductCost_ProfitItog = line["ProductCostBase.ProfitItog"];


                table.Rows.Add(department, category, goods, amount, amountPerc, amountItog,  DishSumInt,
                    DishSumIntPerc, DishSumIntItog,  ProductCost_Profit, ProductCost_ProfitPerc,
                    ProductCost_ProfitItog);
            }

            return table;
        }

    }

    public class EventReport : IPlugin
    {
        public string ParentId
        {
            get
            {
                return "b625757d-be46-442e-88c4-7ef2ab6513c3";
            }
        }
        public string GUID
        {
            get
            {
                return "10001eee-2410-4b3e-9785-9423d9c3afa6";
            }
        }
        public string Name
        {
            get
            {
                return "SalesDeleteChtReport";
            }

        }
        public string DataSourceName
        {
            get
            {
                return "SalesDeleteChtReport";
            }

        }
        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("SalesDeleteChtReport_Comment", Thread.CurrentThread.CurrentCulture);
        }

        public int type
        {
            get
            {
                return 0;
            }

        }

        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string, string> res = new Dictionary<string, string>();
            res.Add("department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));
            res.Add("sum", rm.GetString("Sum", Thread.CurrentThread.CurrentCulture));
            res.Add("SumWithDisc", rm.GetString("SumWithDisc", Thread.CurrentThread.CurrentCulture));
            res.Add("discSum", rm.GetString("discSum", Thread.CurrentThread.CurrentCulture));
            res.Add("discPerc", rm.GetString("discPerc", Thread.CurrentThread.CurrentCulture));
            res.Add("avgCheck", rm.GetString("AvgCheck", Thread.CurrentThread.CurrentCulture));
            res.Add("avgCheckDisc", rm.GetString("avgCheckDisc", Thread.CurrentThread.CurrentCulture));
            res.Add("amount", rm.GetString("Amount", Thread.CurrentThread.CurrentCulture));
            res.Add("orderCancelPrechequeCount", rm.GetString("orderCancelPrechequeCount", Thread.CurrentThread.CurrentCulture));
            res.Add("orderCancelPrechequeCountPerc", rm.GetString("orderCancelPrechequeCountPerc", Thread.CurrentThread.CurrentCulture));
            res.Add("deletedNewItemsSum", rm.GetString("deletedNewItemsSum", Thread.CurrentThread.CurrentCulture));
            res.Add("deletedNewItemsSumPerc", rm.GetString("deletedNewItemsSumPerc", Thread.CurrentThread.CurrentCulture));
            res.Add("complimentCount", rm.GetString("complimentCount", Thread.CurrentThread.CurrentCulture));
            res.Add("complimentPerc", rm.GetString("complimentPerc", Thread.CurrentThread.CurrentCulture));
            return res;
        }

        public DataTable GetTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("department", typeof(string));
            table.Columns.Add("sum", typeof(double));
            table.Columns.Add("SumWithDisc", typeof(double));
            table.Columns.Add("discSum", typeof(double));
            table.Columns.Add("discPerc", typeof(double));
            table.Columns.Add("avgCheck", typeof(double));
            table.Columns.Add("avgCheckDisc", typeof(double));
            table.Columns.Add("amount", typeof(double));
            table.Columns.Add("orderCancelPrechequeCount", typeof(double));
            table.Columns.Add("orderCancelPrechequeCountPerc", typeof(double));
            table.Columns.Add("deletedNewItemsSum", typeof(double));
            table.Columns.Add("deletedNewItemsSumPerc", typeof(double));
            table.Columns.Add("complimentCount", typeof(double));
            table.Columns.Add("complimentPerc", typeof(double));
            return table;
        }

        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime FirstDate,
            DateTime LastDate)
        {
            var table = GetTable();
            var events = OrdersApi.Events(tomcat, login, passw, FirstDate, LastDate, new string[] { "orderCancelPrecheque", "deletedNewItems" });

            var orderCancelPrecheque = events==null?null:events.@event.Where(a => a.type == "orderCancelPrecheque").ToArray(); //Получаем нужные евенты
            var deletedNewItems = events == null ? null : events.@event.Where(a => a.type == "deletedNewItems").ToArray(); //Получаем нужные евенты

            var data = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department", "PayTypes.IsPrintCheque" },
                new string[] { "DishSumInt", "DishDiscountSumInt", "UniqOrderId", "ProductCostBase.ProductCost"}, FirstDate, LastDate);
            foreach (var line in data.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]] != null ? line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString()) : null;
                }
            }
            var departments = OrdersApi.Departments(tomcat, login, passw);
            if (departments!=null && departments.Items.Count(a => a.type == "DEPARTMENT") > 1)
            {
                //работаем с DepartmentID
                var SumWithDiscAll = data.data.Select(a => decimal.Parse(a["DishDiscountSumInt"])).Sum();
                var sumAll = data.data.Select(a => decimal.Parse(a["DishSumInt"])).Sum();
                var complimentsCountAll = data.data.Where(a => a["PayTypes.IsPrintCheque"] != "FISCAL").Select(a => decimal.Parse(a["UniqOrderId"])).Sum(); //TODO: в настройки слово Фискальный
                var discSumAll = sumAll - SumWithDiscAll;


                var deletedNewItemsSumAll =
                       deletedNewItems != null ? deletedNewItems.Sum(b => decimal.Parse(b.attribute.FirstOrDefault(c => c.name == "sum").value.Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString()), System.Globalization.NumberStyles.Float)) : (decimal?) null;
                foreach (var a in data.data.Select(a => a["Department"]).Distinct())
                {
                    var department = a;
                    var sum = data.data.Where(b => b["Department"] == a).Sum(c => decimal.Parse(c["DishSumInt"]));
                    var SumWithDisc = data.data.Where(b => b["Department"] == a).Sum(c => decimal.Parse(c["DishDiscountSumInt"]));
                    var discSum = sum - SumWithDisc;
                    //var discPerc = discSum / discSumAll;
                    var discPerc = discSum * 100 / discSumAll;
                    var amount = data.data.Where(b => b["Department"] == a).Sum(c => decimal.Parse(c["UniqOrderId"]));
                    var avgCheck = sum / amount;
                    var avgCheckDisc = discSum / amount;
                    var complimentCount = data.data.Where(b => b["Department"] == a).Where(c => c["PayTypes.IsPrintCheque"] != "FISCAL").Sum(c => decimal.Parse(c["UniqOrderId"]));
                    var complimentPerc = complimentCount * 100 / complimentsCountAll;

                    if (orderCancelPrecheque != null && deletedNewItems != null)
                    {

                        var orderCancelPrechequeCount =
                            orderCancelPrecheque.Where(
                                b => b.departmentId == departments.Items.FirstOrDefault(c => c.name == a).code).Count();
                        var orderCancelPrechequeCountPerc = orderCancelPrechequeCount*100/orderCancelPrecheque.Length;
                        var deletedNewItemsSum =
                            deletedNewItems
                                .Where(b => b.departmentId == departments.Items.FirstOrDefault(c => c.name == a).code)
                                .Sum(
                                    b =>
                                        decimal.Parse(
                                            b.attribute.FirstOrDefault(c => c.name == "sum")
                                                .value.Replace(".",
                                                    Convert.ToChar(
                                                        CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                                        .ToString()), System.Globalization.NumberStyles.Float));
                        var deletedNewItemsSumPerc = deletedNewItemsSum*100/deletedNewItemsSumAll;



                        table.Rows.Add(department, sum, SumWithDisc, discSum, discPerc, avgCheck, avgCheckDisc, amount,
                            orderCancelPrechequeCount, orderCancelPrechequeCountPerc, deletedNewItemsSum,
                            deletedNewItemsSumPerc, complimentCount, complimentPerc);
                    }
                    else
                    {
                        table.Rows.Add(department, sum, SumWithDisc, discSum, discPerc, avgCheck, avgCheckDisc, amount,
                           0,0,0,0, complimentCount, complimentPerc);
                    }
                }
            }
            else
            {
                var SumWithDiscAll = data.data.Select(a => decimal.Parse(a["DishDiscountSumInt"])).Sum();
                var sumAll = data.data.Select(a => decimal.Parse(a["DishSumInt"])).Sum();
                var complimentsCountAll = data.data.Where(a => a["PayTypes.IsPrintCheque"] != "FISCAL").Select(a => decimal.Parse(a["UniqOrderId"])).Sum(); //TODO: в настройки слово Фискальный
                var discSumAll = sumAll - SumWithDiscAll;
                foreach (var a in data.data.Select(a => a["Department"]).Distinct())
                {
                    var department = a;
                    var sum = data.data.Where(b => b["Department"] == a).Sum(c => decimal.Parse(c["DishSumInt"]));
                    var SumWithDisc = data.data.Where(b => b["Department"] == a).Sum(c => decimal.Parse(c["DishDiscountSumInt"]));
                    var discSum = sum - SumWithDisc;
                    //var discPerc = discSum / discSumAll;
                    var discPerc = discSum*100/discSumAll;
                    var amount = data.data.Where(b => b["Department"] == a).Sum(c => decimal.Parse(c["UniqOrderId"]));
                    var avgCheck = sum / amount;
                    var avgCheckDisc = discSum / amount;
                    var complimentCount = data.data.Where(b => b["Department"] == a).Where(c => c["PayTypes.IsPrintCheque"] != "FISCAL").Sum(c => decimal.Parse(c["UniqOrderId"]));
                    var complimentPerc = complimentCount*100/ complimentsCountAll;

                    if (orderCancelPrecheque != null && deletedNewItems != null)
                    {
                        var orderCancelPrechequeCount = orderCancelPrecheque.Length;
                        var orderCancelPrechequeCountPerc = 100;
                        var deletedNewItemsSum =
                            deletedNewItems.Sum(
                                b =>
                                    decimal.Parse(b.attribute.FirstOrDefault(c => c.name == "sum")
                                        .value.Replace(".",
                                            Convert.ToChar(
                                                CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                                .ToString()), System.Globalization.NumberStyles.Float));
                        var deletedNewItemsSumPerc = 100;



                        table.Rows.Add(department, sum, SumWithDisc, discSum, discPerc, avgCheck, avgCheckDisc, amount,
                            orderCancelPrechequeCount, orderCancelPrechequeCountPerc, deletedNewItemsSum,
                            deletedNewItemsSumPerc, complimentCount, complimentPerc);
                    }
                    else
                    {
                        table.Rows.Add(department, sum, SumWithDisc, discSum, discPerc, avgCheck, avgCheckDisc, amount,
                            0,0,0,0, complimentCount, complimentPerc);
                    }
                }



            }
            return table;
        }
    }

    public class TimeReport : IPlugin
    {
        public string ParentId { get { return "b625757d-be46-442e-88c4-7ef2ab6513c3"; } }
        public string Name { get { return "TimeReport"; } }
        public string GUID { get { return "54ad55ed-c614-46b4-84ba-3472ddd1e607"; } }
        public int type { get { return 0; } }

        public string DataSourceName { get { return "TimeReport"; } }
        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("TimeReport_Comment", Thread.CurrentThread.CurrentCulture);
        }
        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string, string> cols = new Dictionary<string, string>();
            cols.Add("Department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));
            cols.Add("Cashier", rm.GetString("Cashier", Thread.CurrentThread.CurrentCulture));
            cols.Add("OrderWaiter.Name", rm.GetString("OrderWaiter.Name", Thread.CurrentThread.CurrentCulture));
            cols.Add("OpenDate.Typed", rm.GetString("date", Thread.CurrentThread.CurrentCulture));
            cols.Add("OrderNum", rm.GetString("OrderNum", Thread.CurrentThread.CurrentCulture));

            cols.Add("CashierTime", rm.GetString("CashierTime", Thread.CurrentThread.CurrentCulture));
            cols.Add("CookingTime", rm.GetString("CookingTime", Thread.CurrentThread.CurrentCulture));
            cols.Add("DishDiscountSumInt", rm.GetString("DishDiscountSumInt", Thread.CurrentThread.CurrentCulture));

            return cols;
        }

        public DataTable GetTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Department", typeof(string));
            dt.Columns.Add("Cashier", typeof(string));
            dt.Columns.Add("OrderWaiter.Name", typeof(string));
            dt.Columns.Add("OpenDate.Typed", typeof(DateTime));
            dt.Columns.Add("OrderNum", typeof(int));

            dt.Columns.Add("CashierTime", typeof(DateTime));
            dt.Columns.Add("CookingTime", typeof(DateTime));
            dt.Columns.Add("DishDiscountSumInt", typeof(decimal));
            return dt;
        }

        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime FirstDate,
            DateTime LastDate)
        {
            var table = GetTable();
            var data = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department", "OpenDate.Typed", "DishName", "OpenTime", "CloseTime", "OrderNum", "Cashier", "OrderWaiter.Name" }, new string[] { "Cooking.KitchenTime.Avg", "DishDiscountSumInt" }, FirstDate, LastDate);
            foreach (var line in data.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]] != null ? line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString()) : null;
                }
            }

            foreach (var dep in data.data.Select(a => a["Department"]).Distinct())
            {
                foreach (var line in data.data.Where(b=>b["Department"]==dep).Select(a => a["OrderNum"]).Distinct())
                {
                    var lines = data.data.Where(b => b["Department"] == dep).Where(a => a["OrderNum"] == line).ToArray();

                    var department = dep;
                    var cashier = lines.First()["Cashier"];
                    var waiter = lines.First()["OrderWaiter.Name"];
                    var date = DateTime.Parse(lines.First()["OpenDate.Typed"]);
                    var orderNum = line;
                    var orderSum = decimal.Parse(lines.First()["DishDiscountSumInt"]);

                    DateTime cashierTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                    try
                    {
                        var time = DateTime.Parse(lines.First()["CloseTime"].Split(',')[0]).Subtract(
                            (DateTime.Parse(lines.First()["OpenTime"])));
                        cashierTime =
                    new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, time.Hours, time.Minutes, time.Seconds);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteToLog(this.GUID, ex.Message + " " + lines.First()["OpenTime"] + " " + lines.First()["CloseTime"]);
                    }


                    var MaxTime = (int)

                        lines.Max(a => decimal.Parse(a["Cooking.KitchenTime.Avg"] ?? "0"));
                    var CookingTime =
                        new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, MaxTime / 3600, (MaxTime % 3600 / 60), MaxTime % 60);

                    table.Rows.Add(department, cashier, waiter, date, orderNum, cashierTime, CookingTime, orderSum);
                }
            }

            
            


            return table;
        }

    }

    public class TimeReportAdv: IPlugin
    {
        public string ParentId { get { return "b625757d-be46-442e-88c4-7ef2ab6513c3"; } }
        public string Name { get { return "TimeReportAdvanced"; } }
        public string GUID { get { return "05d23944-4465-410c-ae6a-54e8c5d91d1c"; } }
        public int type { get { return 0; } }

        public string DataSourceName { get { return "TimeReportAdvanced"; } }
        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("TimeReportAdvanced_Comment", Thread.CurrentThread.CurrentCulture);
        }
        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string, string> cols = new Dictionary<string, string>();
            cols.Add("Department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));
            cols.Add("CashierAvgTime", rm.GetString("CashierAvgTime", Thread.CurrentThread.CurrentCulture));
            cols.Add("CookingAvgTime", rm.GetString("CookingAvgTime", Thread.CurrentThread.CurrentCulture));
            cols.Add("CheckCount", rm.GetString("CheckCount", Thread.CurrentThread.CurrentCulture));
            return cols;
        }

        public DataTable GetTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Department", typeof(string));

            dt.Columns.Add("CashierAvgTime", typeof(DateTime));
            dt.Columns.Add("CookingAvgTime", typeof(DateTime));
            dt.Columns.Add("CheckCount", typeof(int));
            return dt;
        }

        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime FirstDate,
            DateTime LastDate)
        {
            TimeReport report = new TimeReport();
            var data = report.GetData(period, "", tomcat, login, passw, FirstDate, LastDate).Rows.Cast<DataRow>();

            var table = GetTable();

            foreach (var line in data.Select(a => a["Department"].ToString()).Distinct())
            {
                DateTime dt;
                var cashTime = data.Where(a => a["Department"].ToString() == line)
                    .Where(a => DateTime.TryParse(a["CashierTime"].ToString(), out dt))
                    .Select(a => DateTime.Parse(a["CashierTime"].ToString()));
                var tmp = (int) cashTime.Select(a => a.TimeOfDay.TotalSeconds).Average();   
                           
                var cashierTime =
                        new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, tmp / 3600, (tmp % 3600 / 60), tmp % 60);

                var _cookingTime =
                    data.Where(a => a["Department"].ToString() == line)
                        .Where(a => DateTime.TryParse(a["CashierTime"].ToString(), out dt))
                        .Select(a => DateTime.Parse(a["CookingTime"].ToString()));
                tmp = (int) _cookingTime.Select(a => a.TimeOfDay.TotalSeconds).Average();

                var cookingTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, tmp / 3600, (tmp % 3600 / 60), tmp % 60);


                table.Rows.Add(line, cashierTime, cookingTime, _cookingTime.Count());

            }


            return table;
        }
    }


    public class ChangeEffect: IPlugin
    {
        public string ParentId { get { return "b625757d-be46-442e-88c4-7ef2ab6513c3"; } }
        public string Name { get { return "ChangeEffect"; } }
        public string GUID { get { return "b0906b02-76a4-46ce-bdfd-9808c2a0e367"; } }
        public int type { get { return 0; } }

        public string DataSourceName { get { return "ChangeEffect"; } }
        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("ChangeEffect_Comment", Thread.CurrentThread.CurrentCulture);
        }
        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string, string> cols = new Dictionary<string, string>();
            cols.Add("Department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));
            cols.Add("DishName", rm.GetString("DishName", Thread.CurrentThread.CurrentCulture));
            cols.Add("AmountCurrentMonth", rm.GetString("AmountCurrentMonth", Thread.CurrentThread.CurrentCulture));
            cols.Add("AvgPriceCurrentMonth", rm.GetString("AvgPriceCurrentMonth", Thread.CurrentThread.CurrentCulture));
            cols.Add("AvgPriceLastMonth", rm.GetString("AvgPriceLastMonth", Thread.CurrentThread.CurrentCulture));
            cols.Add("ChangeEffect", rm.GetString("ChangeEffect", Thread.CurrentThread.CurrentCulture));
            return cols;
        }

        public DataTable GetTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Department", typeof(string));
            dt.Columns.Add("DishName", typeof(string));

            dt.Columns.Add("AmountCurrentMonth", typeof(decimal));
            dt.Columns.Add("AvgPriceCurrentMonth", typeof(decimal));
            dt.Columns.Add("AvgPriceLastMonth", typeof(decimal));
            dt.Columns.Add("ChangeEffect", typeof(decimal));
            return dt;
        }

        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime FirstDate, DateTime LastDate)
        {
            var table = GetTable();

            if (
                !(FirstDate.Day == 1 && LastDate.Day == DateTime.DaysInMonth(LastDate.Year, LastDate.Month) &&
                  FirstDate.Month == LastDate.Month && FirstDate.Year == LastDate.Year))
                return table; 
            //текущий месяц


            var data = OrdersApi.Olap(tomcat, login, passw, new string[] { "TransactionType", "Department", "Product.Name" }, new string[] { "Amount.In", "Sum.Incoming" }, FirstDate, LastDate, "TRANSACTIONS");
            foreach (var line in data.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]] != null ? line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString()) : null;
                }
            }

            foreach (var line in data.data.Where(a=>a["TransactionType"] == "INVOICE"))
            {
                table.Rows.Add(line["Department"], line["Product.Name"], Math.Round(decimal.Parse(line["Amount.In"]),2),
                    Math.Round(decimal.Parse(line["Sum.Incoming"]),2)==0?0: Math.Round( (decimal.Parse(line["Sum.Incoming"])/ decimal.Parse(line["Amount.In"])),2));
            }

            data = OrdersApi.Olap(tomcat, login, passw, new string[] { "TransactionType", "Department", "Product.Name" }, new string[] { "Amount.In", "Sum.Incoming" }, FirstDate.AddMonths(-1), LastDate.AddMonths(-1), "TRANSACTIONS");
            foreach (var line in data.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]] != null ? line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString()) : null;
                }
            }

            foreach (DataRow row in table.Rows)
            {
                var line =
                    data.data.Where(a => a["TransactionType"] == "INVOICE").FirstOrDefault(
                        a => a["Department"] == row[0].ToString() && a["Product.Name"] == row[1].ToString());
                if (line == null)
                {
                }
                else
                {
                    row[4] = decimal.Parse(line["Sum.Incoming"])==0?0: Math.Round(decimal.Parse(line["Sum.Incoming"])/ decimal.Parse(line["Amount.In"]),2);
                    row[5] = Math.Round(((decimal.Parse(line["Sum.Incoming"])==0?0: Math.Round(decimal.Parse(line["Sum.Incoming"])/decimal.Parse(line["Amount.In"]),2)) -
                              decimal.Parse(row[3].ToString()))*(decimal.Parse(row[2].ToString())),2);
                }
            }

            return table;
        }
    }

   


    /*public class RawMatrix : IPlugin
    {
        public string ParentId { get { return "b625757d-be46-442e-88c4-7ef2ab6513c3"; } }
        public string Name { get { return "RawMatrix"; } }
        public string GUID { get { return "54ad55ed-c614-46b4-84ba-3472ddd1e607"; } }
        public int type { get { return 0; } }

        public string DataSourceName { get { return "RawMatrix"; } }
        public string commentary { get { return GetComment(); } }

        public string GetComment()
        {

            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            return rm.GetString("RawMatrix_Comment", Thread.CurrentThread.CurrentCulture);
        }
        public Dictionary<string, string> GetColumns()
        {
            var rm = new ResourceManager("Reports.Resources.lang", Assembly.GetExecutingAssembly());
            Dictionary<string, string> cols = new Dictionary<string, string>();
            cols.Add("Department", rm.GetString("Department", Thread.CurrentThread.CurrentCulture));
            cols.Add("DishName", rm.GetString("DishName", Thread.CurrentThread.CurrentCulture));
            cols.Add("fullSum", rm.GetString("fullSum", Thread.CurrentThread.CurrentCulture));
            return cols;
        }

        public DataTable GetTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Department", typeof(string));
            dt.Columns.Add("DishName", typeof(string));
            dt.Columns.Add("fullSum", typeof(decimal));
            return dt;
        }

        public DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime FirstDate,
            DateTime LastDate)
        {
            var table = GetTable();
            var data = OrdersApi.Olap(tomcat, login, passw, new string[] { "Department", "DishName" }, new string[] { "fullSum"}, FirstDate, LastDate);
            foreach (var line in data.data)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    line[line.Keys.ToList()[i]] = line[line.Keys.ToList()[i]] != null ? line[line.Keys.ToList()[i]].Replace(".", Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).ToString()) : null;
                }
            }

            foreach (var line in data.data)
            {

            }
            

            return table;
        }

    }*/
}
