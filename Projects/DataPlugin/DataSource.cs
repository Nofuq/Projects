using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using DataPlugin.Properties;
using PluginSDK;


namespace DataPlugin
{
    public static class DataSource
    {
        /// <summary>
        /// Событие, которое происходит при попытке записи сообщений в лог
        /// </summary>
        public delegate void MyLogHandler(string guid, string message);
        public static event MyLogHandler OnLogWrite;


        static DataSource()
        {
            PluginSDK.Log.OnLogWrite += (o, e) => {
                                                      if (OnLogWrite != null) OnLogWrite(o, e);
            }; //Проброс евента из PluginSDK
        }

        //period 0 - текущий период 1- прошлый период
        //datatype - тип кнопки всегда должен быть -1 
        //tomcat - ip:port сервера tomcat
        //login -  логин на  сервер tomcat
        //passw - пароль сервера tomcat
        //FirstDate - начальная дата периода
        //LastDate - конечная дата периода
        //report_guid - guid  отчета
        public static DataTable GetDataTableType(int period, string report_guid, string tomcat, string login, string passw, DateTime FirstDate, DateTime LastDate)
        {
            var plugin = Plugins.PluginList.FirstOrDefault(a => a.GUID == report_guid);
            if (plugin == null) return null;
            return plugin.GetData(period, report_guid, tomcat, login, passw, FirstDate, LastDate);
        }
       
        public static DataTable GetDataTable(string report_guide)
        {
            var plugin = Plugins.PluginList.FirstOrDefault(a => a.GUID == report_guide);
            if (plugin == null) return null;
            return plugin.GetTable();
        }

        public static Dictionary<string, string > GetColumn(string report_guide)
        {
            var plugin = Plugins.PluginList.FirstOrDefault(a => a.GUID == report_guide);
            if (plugin == null) return null;
            return plugin.GetColumns();
        }

    }


    public static class Plugins
    {
        private static IPlugin[] _plugins;
        public static IPlugin[] PluginList
        {
            get
            {
                if (_plugins == null)
                {
                    Logs("PluginList path = " + Environment.CurrentDirectory); 
                    _plugins = GetPlugins(Settings.Default.path);
                  
                    return _plugins;
                }
                else return _plugins;
            }
        }
        private static AppDomain domain;
        static List<Assembly> plugins = new List<Assembly>();
        public static IPlugin[] GetPlugins(string path)
        {
            if (plugins == null || plugins.Count==0)
                ScanFolder(path);
            List<IPlugin> buttons = new List<IPlugin>();
            foreach (var assembly in plugins)
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        var interfaceType = typeof(IPlugin);
                        if (type != interfaceType && interfaceType.IsAssignableFrom(type))
                        {
                            IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                            buttons.Add(plugin);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                
            }
            return buttons.ToArray();
        }
        static void ScanFolder(string path)
        {
            Logs("ScanFolder path = " + path);
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                try
                {
                    var assembly = Assembly.LoadFile(Path.GetFullPath(file));
                    plugins.Add(assembly);
                }
                catch (Exception ex)
                {

                }
            }
           Logs("Count = "+ plugins.Count);
            return;
            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                Logs("ScanFolder path = "+ directory);
                
                ScanFolder(Path.GetFullPath(directory));
              
            }
            
        }



        private static void Logs(string mes)
        {
            /*
            
            StreamWriter writer = new StreamWriter(@"C:\Logs\Monitoring.log", true);
            writer.WriteLine(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + ": " + mes);
            writer.Close();
            */
        }
    }

}
