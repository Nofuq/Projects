using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace PluginSDK
{
    public static class Log
    {
        public delegate void MyLogHandler(string guid, string message);
        public static event MyLogHandler OnLogWrite;


        public static void WriteToLog(string guid, string message)
        {
            if (OnLogWrite != null) OnLogWrite(guid, message);
        }
    }

    public class LogEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string ReportGUID { get; set; }
    }

    public class OlapRequest
    {
        public string reportType { get; set; }
        public string[] groupByRowFields { get; set; }
        public string[] aggregateFields { get; set; }
        public Dictionary<string, Dictionary<string, string>> filters { get; set; }
    }
    public class OlapResponse
    {
        public List<Dictionary<string, string>> data { get; set; }
        public string error { get; set; }
    }
    public static class OrdersApi
    {

        public static OlapResponse Olap(string tomcat, string login, string password, string[] rows, string[] columns, DateTime from, DateTime to, string reportType="SALES")
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                OlapRequest request = new OlapRequest();
                request.reportType = reportType;
                request.groupByRowFields = rows;
                request.aggregateFields = columns;



                var rangFilter = new Dictionary<string, string>();
                rangFilter.Add("filterType", "DateRange");
                rangFilter.Add("periodType", "CUSTOM");
                rangFilter.Add("from", from.ToString("yyyy-MM-ddTHH:mm:ss"));
                rangFilter.Add("to", to.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss"));
                rangFilter.Add("includeLow", "true");
                rangFilter.Add("includeHigh", "false");

                if (reportType == "SALES")
                {
                    request.filters = new Dictionary<string, Dictionary<string, string>>();
                    request.filters.Add("OpenDate", rangFilter);
                }

                var js = new JsonSerializer();
                var sb = new StringBuilder();
                var tw = new StringWriter(sb);
                js.Serialize(tw, request, typeof(OlapRequest));

                var requestString = sb.ToString();

                WebClient wc = new WebClient();
                wc.Headers.Add("Content-type: Application/json; charset=utf-8");
                var response = wc.UploadString("http://" + tomcat + "/resto/api/v2/reports/olap?key=" + key, "POST", requestString);
                OlapResponse res = (OlapResponse)js.Deserialize(new StringReader(response), typeof(OlapResponse));
                return res;
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                {
                    var resp = new StreamReader(((WebException)ex).Response.GetResponseStream()).ReadToEnd();
                    //return new OlapResponse() { error = resp };
                }
                return null;
            }
            finally
            {
                DeAutorize(tomcat, key);
            }
        }



        public static eventsList Events(string tomcat, string login, string password, DateTime from, DateTime to, string[] events)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                // фильтрация
                
                eventsRequestData requestData = new eventsRequestData();
                requestData.Items = new object[]
                {
                    new eventsRequestDataEvents()
                    {
                        @event = events.Select(a=>new eventsRequestDataEventsEvent() {Value =a}).ToArray()
                    }
                };


                XmlSerializer xmlser = new XmlSerializer(typeof(eventsRequestData));
                var xns = new XmlSerializerNamespaces();
                xns.Add(string.Empty, string.Empty);
                StringBuilder sb = new StringBuilder();
                xmlser.Serialize(new StringWriter(sb), requestData, xns);



                var body = sb.ToString().Split('\n').Skip(1).Aggregate((a, b) => a += b + "\n");

                WebClient wc = new WebClient();
                wc.Headers.Add("Content-Type:application/xml");
                wc.Encoding = Encoding.UTF8;
                var url = string.Format("http://{0}/resto/api/events?key={1}&from_time={2}&to_time={3}", tomcat,
                    key, from.ToString(), to.ToString());
                var result =
                    wc.UploadString(url,"POST", body);
                XmlSerializer xml = new XmlSerializer(typeof(eventsList));
                var eventList = (eventsList) xml.Deserialize(new StringReader(result));

                //Еще получаем расшифровку событий


                url = string.Format("http://{0}/resto/api/events/metadata?key={1}", tomcat,
                    key, from.ToString(), to.ToString());
                result =
                    wc.DownloadString(url);
                xml = new XmlSerializer(typeof(groupsList));
                var groups = (groupsList) xml.Deserialize(new StringReader(result));


                foreach (var ev in eventList.@event)
                {
                    var gr = groups.group.FirstOrDefault(a => a.type?.FirstOrDefault(b => b.id == ev.type) != null);
                    if (gr != null)
                    {
                        ev.GroupName = gr.name;
                        var type = gr.type.FirstOrDefault(b => b.id == ev.type);
                        ev.Name = type.name;
                    }
                }

                return eventList;


            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                DeAutorize(tomcat,key);
            }
            return null;
        }

        public static budgetPlanItemDtoes Plan(string tomcat, string login, string password, DateTime from, DateTime to, string depId)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                var url = string.Format("http://{0}/resto/api/reports/monthlyIncomePlan?key={1}&dateFrom={2}&dateTo={3}&department={4}", tomcat,
                    key, from.ToString(), to.ToString(), depId);
                var result =
                    wc.DownloadString(url);
                XmlSerializer xml = new XmlSerializer(typeof(budgetPlanItemDtoes));
                var plan = (budgetPlanItemDtoes)xml.Deserialize(new StringReader(result));

                return plan;

            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                DeAutorize(tomcat, key);
            }
            return null;
        }

        public static corporateItemDtoes Departments(string tomcat, string login, string password)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                var url = string.Format("http://{0}/resto/api/corporation/departments?key={1}", tomcat,
                    key);
                var result =
                    wc.DownloadString(url);
                XmlSerializer xml = new XmlSerializer(typeof(corporateItemDtoes));
                var corps = (corporateItemDtoes)xml.Deserialize(new StringReader(result));

                return corps;

            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                DeAutorize(tomcat, key);
            }
            return null;
        }

        static string Autorize(string tomcat, string login, string password)
        {
            var wc = new WebClient();
            try
            {

                var key = wc.DownloadString("http://" + tomcat + "/resto/api/auth?login=" + login + "&pass=" + HashCode(password).ToLower());
                //Logger.Instance.Write("Авторизовались дкя получения заказов");
                return key;
                //Авторизовалися. Надеюсь без куки
            }
            catch (Exception ex)
            {
                //Логгируем ошибку!
            }
            return "";
        }
        static void DeAutorize(string tomcat, string key)
        {
            var wc = new WebClient();
            try
            {
                wc.DownloadString("http://" + tomcat + "/resto/api/logout?key=" + key);
                //Logger.Instance.Write("Деавторизовались для получения заказов");
                //Авторизовалися. Надеюсь без куки
            }
            catch (Exception ex)
            {
                //Логгируем ошибку!
            }
        }
        public static string HashCode(string str)
        {
            try
            {
                var encoder = new System.Text.ASCIIEncoding();
                byte[] buffer = encoder.GetBytes(str);
                var cryptoTransformSHA1 =
                    new SHA1CryptoServiceProvider();
                string hash = BitConverter.ToString(
                    cryptoTransformSHA1.ComputeHash(buffer)).Replace("-", "");

                return hash;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static employees GetEmployees(string tomcat, string login, string password)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                var url = string.Format("http://{0}/resto/api/employees?key={1}", tomcat,
                    key);
                var result =
                    wc.DownloadString(url);
                XmlSerializer xml = new XmlSerializer(typeof(employees));
                var empls = (employees)xml.Deserialize(new StringReader(result));

                return empls;

            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                DeAutorize(tomcat, key);
            }
            return null;
        }

        public static employeeRoles GetRoles(string tomcat, string login, string password)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                var url = string.Format("http://{0}/resto/api/employees/roles?key={1}", tomcat,
                    key);
                var result =
                    wc.DownloadString(url);
                XmlSerializer xml = new XmlSerializer(typeof(employeeRoles));
                var empls = (employeeRoles)xml.Deserialize(new StringReader(result));

                return empls;

            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                DeAutorize(tomcat, key);
            }
            return null;
        }


        public static schedules GetSchedules(string tomcat, string login, string password, DateTime from, DateTime to, bool withPaymentDetails)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                var url = string.Format("http://{0}/resto/api/employees/schedule/?from={2}&to={3}&withPaymentDetails={4}&key={1}", tomcat,
                    key, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), withPaymentDetails.ToString());
                var result =
                    wc.DownloadString(url);
                XmlSerializer xml = new XmlSerializer(typeof(schedules));
                var schedules = (schedules)xml.Deserialize(new StringReader(result));

                return schedules;

            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                DeAutorize(tomcat, key);
            }
            return null;
        }

        /// <summary>
        /// Можно посылать код подразделения И/ИЛИ код сотрудника. Т.е есть три разных варианта. Неиспользуемое поле должно быть равно NULL или пустой строке
        /// "КодПодразделения",NULL
        /// NULL,"Код сотрудника"
        /// NULL,NULL
        /// </summary>
        /// <param name="tomcat"></param>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="withPaymentDetails"></param>
        /// <param name="departmentCode"></param>
        /// <param name="employeeID"></param>
        /// <returns></returns>
        public static schedules GetSchedules(string tomcat, string login, string password, DateTime from, DateTime to, bool withPaymentDetails, string departmentCode, string employeeID)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;

                string url = "";
                if (string.IsNullOrEmpty(departmentCode) && string.IsNullOrEmpty(employeeID))
                {
                    return null;
                }
                if (string.IsNullOrEmpty(departmentCode))
                    url = string.Format("http://{0}/resto/api/employees/schedule/byEmployee/{5}/?from={2}&to={3}&withPaymentDetails={4}&key={1}", tomcat,
                        key, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), withPaymentDetails.ToString(), employeeID);
                else
                    if (string.IsNullOrEmpty(employeeID))
                    url = string.Format("http://{0}/resto/api/employees/schedule/byDepartment/{5}/?from={2}&to={3}&withPaymentDetails={4}&key={1}", tomcat,
                        key, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), withPaymentDetails.ToString(), departmentCode);
                else
                    url = string.Format("http://{0}/resto/api/employees/schedule/byDepartment/{5}/byEmployee/{6}/?from={2}&to={3}&withPaymentDetails={4}&key={1}", tomcat,
                        key, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), withPaymentDetails.ToString(), departmentCode, employeeID);
                var result =
                    wc.DownloadString(url);
                XmlSerializer xml = new XmlSerializer(typeof(schedules));
                var schedules = (schedules)xml.Deserialize(new StringReader(result));

                return schedules;

            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                DeAutorize(tomcat, key);
            }
            return null;
        }

        public static attendances Getattendances(string tomcat, string login, string password, DateTime from, DateTime to, bool withPaymentDetails, string departmentCode, string employeeID)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;

                string url = "";
                if (string.IsNullOrEmpty(departmentCode) && string.IsNullOrEmpty(employeeID))
                {
                    return null;
                }
                if (string.IsNullOrEmpty(departmentCode))
                    url = string.Format("http://{0}/resto/api/employees/attendance/byEmployee/{5}/?from={2}&to={3}&withPaymentDetails={4}&key={1}", tomcat,
                        key, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), withPaymentDetails.ToString(), employeeID);
                else
                    if (string.IsNullOrEmpty(employeeID))
                    url = string.Format("http://{0}/resto/api/employees/attendance/byDepartment/{5}/?from={2}&to={3}&withPaymentDetails={4}&key={1}", tomcat,
                        key, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), withPaymentDetails.ToString(), departmentCode);
                else
                    url = string.Format("http://{0}/resto/api/employees/attendance/byDepartment/{5}/byEmployee/{6}/?from={2}&to={3}&withPaymentDetails={4}&key={1}", tomcat,
                        key, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), withPaymentDetails.ToString(), departmentCode, employeeID);
                var result =
                    wc.DownloadString(url);
                XmlSerializer xml = new XmlSerializer(typeof(attendances));
                var schedules = (attendances)xml.Deserialize(new StringReader(result));

                return schedules;

            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                DeAutorize(tomcat, key);
            }
            return null;
        }


        public static attendances Getattendances(string tomcat, string login, string password, DateTime from, DateTime to, bool withPaymentDetails)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                var url = string.Format("http://{0}/resto/api/employees/attendance/?from={2}&to={3}&withPaymentDetails={4}&key={1}", tomcat,
                    key, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), withPaymentDetails.ToString());
                var result =
                    wc.DownloadString(url);
                XmlSerializer xml = new XmlSerializer(typeof(attendances));
                var schedules = (attendances)xml.Deserialize(new StringReader(result));

                return schedules;

            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                DeAutorize(tomcat, key);
            }
            return null;
        }


        public static attendance CreateAttendance(string tomcat, string login, string password, attendance at)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);


                var xml = new XmlSerializer(typeof(attendance));

                var requestString = SerializeToString(at);

                WebClient wc = new WebClient();
                wc.Headers.Add("Content-type: Application/xml; charset=utf-8");
                wc.Encoding = Encoding.UTF8;
                var response = wc.UploadString("http://" + tomcat + "/resto/api/employees/attendance/create?key=" + key, "POST", requestString);
                attendance res = (attendance)xml.Deserialize(new StringReader(response));
                return res;
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                {
                    var resp = new StreamReader(((WebException)ex).Response.GetResponseStream()).ReadToEnd();
                }
                return null;
            }
            finally
            {
                DeAutorize(tomcat, key);
            }
        }

        public static attendance UpdateAttendance(string tomcat, string login, string password, attendance at)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);


                var xml = new XmlSerializer(typeof(attendance));


                var requestString = SerializeToString(at);

                WebClient wc = new WebClient();
                wc.Headers.Add("Content-type: Application/xml; charset=utf-8");
                wc.Encoding = Encoding.UTF8;
                var response = wc.UploadString("http://" + tomcat + "/resto/api/employees/attendance/update?key=" + key, "POST", requestString);
                attendance res = (attendance)xml.Deserialize(new StringReader(response));
                return res;
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                {
                    var resp = new StreamReader(((WebException)ex).Response.GetResponseStream()).ReadToEnd();
                }
                return null;
            }
            finally
            {
                DeAutorize(tomcat, key);
            }
        }


        public static schedule CreateSchedule(string tomcat, string login, string password, schedule sc)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);


                var xml = new XmlSerializer(typeof(schedule));

                var requestString = SerializeToString(sc);

                WebClient wc = new WebClient();
                wc.Headers.Add("Content-type: Application/xml; charset=utf-8");
                wc.Encoding = Encoding.UTF8;
                var response = wc.UploadString("http://" + tomcat + "/resto/api/employees/schedule/create?key=" + key, "POST", requestString);
                schedule res = (schedule)xml.Deserialize(new StringReader(response));
                return res;
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                {
                    var resp = new StreamReader(((WebException)ex).Response.GetResponseStream()).ReadToEnd();
                }
                return null;
            }
            finally
            {
                DeAutorize(tomcat, key);
            }
        }

        public static schedule UpdateSchedule(string tomcat, string login, string password, schedule sc)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);

                var xml = new XmlSerializer(typeof(schedule));


                var requestString = SerializeToString(sc);
                
                WebClient wc = new WebClient();
                wc.Headers.Add("Content-type: Application/xml; charset=utf-8");
                wc.Encoding = Encoding.UTF8;
                var response = wc.UploadString("http://" + tomcat + "/resto/api/employees/schedule/update?key=" + key, "POST", requestString);
                schedule res = (schedule)xml.Deserialize(new StringReader(response));
                return res;
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                {
                    var resp = new StreamReader(((WebException)ex).Response.GetResponseStream()).ReadToEnd();
                }
                return null;
            }
            finally
            {
                DeAutorize(tomcat, key);
            }
        }


        public static employeeScheduleTypes GetScheduleTypes(string tomcat, string login, string password)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                var url = string.Format("http://{0}/resto/api/employees/schedule/types?&key={1}", tomcat,
                    key);
                var result =
                    wc.DownloadString(url);
                XmlSerializer xml = new XmlSerializer(typeof(employeeScheduleTypes));
                var schedules = (employeeScheduleTypes)xml.Deserialize(new StringReader(result));

                return schedules;

            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                DeAutorize(tomcat, key);
            }
            return null;
        }

        public static employeeAttendanceTypes GetAttendanceTypes(string tomcat, string login, string password, bool deleted)
        {
            string key = "";
            try
            {
                key = Autorize(tomcat, login, password);
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                var url = string.Format("http://{0}/resto/api/employees/attendance/types?includeDeleted={2}&key={1}", tomcat,
                    key, deleted);
                var result =
                    wc.DownloadString(url);
                XmlSerializer xml = new XmlSerializer(typeof(employeeAttendanceTypes));
                var schedules = (employeeAttendanceTypes)xml.Deserialize(new StringReader(result));

                return schedules;

            }
            catch (WebException ex)
            {
                var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                DeAutorize(tomcat, key);
            }
            return null;
        }

        public static string SerializeToString<T>(T value)
        {
            var emptyNamepsaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var serializer = new XmlSerializer(value.GetType());
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;

            using (var stream = new StringWriter())
            using (var writer = XmlWriter.Create(stream, settings))
            {
                serializer.Serialize(writer, value, emptyNamepsaces);
                return stream.ToString();
            }
        }
    }
       
    public interface IPlugin
    {
        string ParentId { get; }
        string Name { get; }
        string GUID { get; }
        int type { get; }
        string DataSourceName { get; }
        string commentary { get; }
        Dictionary<string, string > GetColumns();
        DataTable GetTable();
        DataTable GetData(int period, string report_guid, string tomcat, string login, string passw, DateTime FirstDate, DateTime LastDate);
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class @event
    {

        private eventAttribute[] attributeField;

        private System.DateTime dateField;

        private bool dateFieldSpecified;

        private string departmentIdField;

        private string idField;

        private string typeField;


        public string GroupName { get; set; }
        public string Name { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("attribute", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public eventAttribute[] attribute
        {
            get
            {
                return this.attributeField;
            }
            set
            {
                this.attributeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public System.DateTime date
        {
            get
            {
                return this.dateField;
            }
            set
            {
                this.dateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool dateSpecified
        {
            get
            {
                return this.dateFieldSpecified;
            }
            set
            {
                this.dateFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string departmentId
        {
            get
            {
                return this.departmentIdField;
            }
            set
            {
                this.departmentIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class eventAttribute
    {

        private string nameField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class eventsList
    {

        private @event[] eventField;

        private int revisionField;

        private bool revisionFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("event")]
        public @event[] @event
        {
            get
            {
                return this.eventField;
            }
            set
            {
                this.eventField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public int revision
        {
            get
            {
                return this.revisionField;
            }
            set
            {
                this.revisionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool revisionSpecified
        {
            get
            {
                return this.revisionFieldSpecified;
            }
            set
            {
                this.revisionFieldSpecified = value;
            }
        }
    }


    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class eventsRequestData
    {

        private object[] itemsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("events", typeof(eventsRequestDataEvents), Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        [System.Xml.Serialization.XmlElementAttribute("orderNums", typeof(eventsRequestDataOrderNums), Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class eventsRequestDataEvents
    {

        private eventsRequestDataEventsEvent[] eventField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("event", Form = System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable = true)]
        public eventsRequestDataEventsEvent[] @event
        {
            get
            {
                return this.eventField;
            }
            set
            {
                this.eventField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class eventsRequestDataEventsEvent
    {

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class eventsRequestDataOrderNums
    {

        private string orderNumField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string orderNum
        {
            get
            {
                return this.orderNumField;
            }
            set
            {
                this.orderNumField = value;
            }
        }
    }


    [XmlRoot("group")]
    public partial class Group
    {

        private string idField;

        private string nameField;


        private type[] typeField;

        public Group()
        {
        }

        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        [XmlElement("type")]
        public type[] type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }
    }

    [XmlRoot("type")]
    public partial class type
    {


        private attribute[] attributeField;

        private string idField;

        private string nameField;

        private string severityField;

        public type()
        {
        }

        [XmlElement("attribute")]
        public attribute[] attribute
        {
            get
            {
                return this.attributeField;
            }
            set
            {
                this.attributeField = value;
            }
        }

        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        public string severity
        {
            get
            {
                return this.severityField;
            }
            set
            {
                this.severityField = value;
            }
        }
    }

    [XmlRoot("attribute")]
    public partial class attribute
    {

        private string idField;

        private string nameField;

        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
    }

    [XmlRoot("groupsList")]
    public partial class groupsList
    {

        private Group[] groupField;

        public groupsList()
        {
        }

        [XmlElement("group")]
        public Group[] group
        {
            get
            {
                return this.groupField;
            }
            set
            {
                this.groupField = value;
            }
        }
    }

    public partial class corporateItemDto
    {

        private string idField;

        private string parentIdField;

        private string codeField;

        private string nameField;

        private string typeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string parentId
        {
            get
            {
                return this.parentIdField;
            }
            set
            {
                this.parentIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string code
        {
            get
            {
                return this.codeField;
            }
            set
            {
                this.codeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class corporateItemDtoes
    {

        private corporateItemDto[] itemsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("corporateItemDto")]
        public corporateItemDto[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class budgetPlanItemDtoes
    {

        private budgetPlanItemDto[] itemsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("budgetPlanItemDto")]
        public budgetPlanItemDto[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class budgetPlanItemDto
    {

        private string dateField;

        private decimal planValueField;

        private bool planValueFieldSpecified;

        private budgetPlanItemValueType valueTypeField;

        private bool valueTypeFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string date
        {
            get
            {
                return this.dateField;
            }
            set
            {
                this.dateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public decimal planValue
        {
            get
            {
                return this.planValueField;
            }
            set
            {
                this.planValueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool planValueSpecified
        {
            get
            {
                return this.planValueFieldSpecified;
            }
            set
            {
                this.planValueFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public budgetPlanItemValueType valueType
        {
            get
            {
                return this.valueTypeField;
            }
            set
            {
                this.valueTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool valueTypeSpecified
        {
            get
            {
                return this.valueTypeFieldSpecified;
            }
            set
            {
                this.valueTypeFieldSpecified = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    public enum budgetPlanItemValueType
    {

        /// <remarks/>
        ABSOLUTE,

        /// <remarks/>
        PERCENT,

        /// <remarks/>
        AUTOMATIC,
    }
}
