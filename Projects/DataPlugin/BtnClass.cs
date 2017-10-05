using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DataPlugin
{
    public static class BtnClass
    {
        public static List<myBtn> GetSampleBtn()
        {
            List<myBtn> btn_list = new List<myBtn>();
            // создаем папки для источников данных 
            myBtn _btnRoot = new myBtn();
            _btnRoot.ParentId = "62c04cfb-7801-4e30-af96-950f3c43106b";
            //Uri uri = new Uri("pack://application:,,,/SmartLibrary;component/Images/folder.png");
            //BitmapImage bitmap = new BitmapImage(uri);
            //_btnRoot.images = bitmap;
            _btnRoot.BtnName = "Дополнительные источники данных";
            _btnRoot.BtnDatatype = -1;
            _btnRoot.type = 1;
            _btnRoot.BtnGuid = "b625757d-be46-442e-88c4-7ef2ab6513c3";
            btn_list.Add(_btnRoot);

           


            foreach (var plugin in Plugins.PluginList)
            {

                myBtn _btnRoot1 = new myBtn();
                if (plugin.ParentId!=null)
                {
                    _btnRoot1.ParentId = plugin.ParentId;  
                }
                else
                {
                    _btnRoot1.ParentId = "b625757d-be46-442e-88c4-7ef2ab6513c3";
                }
                
                Uri uri1 = null;
                if(plugin.type==1)
                {
                   // uri1 = new Uri("pack://application:,,,/SmartLibrary;component/Images/folder.png"); 
                }
                else
                {
                   //uri1 = new Uri("pack://application:,,,/SmartLibrary;component/Images/database_table.png"); 
                }
                
                ///BitmapImage bitmap1 = new BitmapImage(uri1);
               // _btnRoot1.images = bitmap1;
                _btnRoot1.BtnName = plugin.Name;
                _btnRoot1.BtnDatatype = -1;
                _btnRoot1.type = plugin.type;
                _btnRoot1.BtnGuid = plugin.GUID;
                _btnRoot1.DataSourceName = plugin.DataSourceName;
                _btnRoot1.BtnComment = plugin.commentary;
                btn_list.Add(_btnRoot1);
            }
            return btn_list;
            
        }
    }

    public class myBtn
    {

        public string ParentId { get; set; } //id группы
        public int type { get; set; } // 0 - DataSource 1- папка
        public ImageSource images { get; set; }
        public string BtnName { get; set; } // надпись на кнопке
        public int BtnDatatype { get; set; } // индекс источника данных
        public string DataSourceName { get; set; } // название таблицы (указывается латинскими буквами слитно)
        public string BtnGuid { get; set; } //guid источника
        public string BtnText { get; set; } // описание на русском языке
        public string BtnComment { get; set; } // описание источника
    }
}

