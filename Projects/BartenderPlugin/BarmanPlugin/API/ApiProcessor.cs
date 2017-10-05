using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Web.Script.Serialization;
using BeOpen.BartenderPlugin;
using System.Collections.Specialized;

namespace BeOpen.BartenderPlugin.API.ApiProcessor
{
    class ApiProcessor
    {
        private readonly string _pin_code = Properties.Settings.Default.Pin.ToString();
        private readonly string _login = Properties.Settings.Default.Login.ToString();
        private readonly string _password = Properties.Settings.Default.Password.ToString();
        private readonly string _token;
        public WebClient _client = new WebClient();
        private WebClient _bartenderClient= new WebClient();
        private JavaScriptSerializer JSS = new JavaScriptSerializer();
        public ApiProcessor()
        {
            
            _client.BaseAddress = "https://test.dxbx.ru/apis/auth";
            _client.Headers.Add("Content-Type", "application/json");
            _client.Headers.Add("ApiVersion", "v1");
            _client.Headers.Add("User-Agent", "BartenderPlugin (iiko;1.0)");
            _token = login();


        }
        public string login()
        {
            var data = new Dictionary<string,string>();
            data.Add("login", _login);
            data.Add("password", _password);
            var jsonreq = JSS.Serialize(data);
            var response = _client.UploadString("auth/authenticate", JSS.Serialize(data));
            var result = JSS.Deserialize<dynamic>(response);
            return result["subjects"]["token"].ToString();
            
            
        }
        public void getBottleInfo()
        {
            NameValueCollection data = new NameValueCollection();
            data.Add("login", _login);
            data.Add("password", _password);
            var json = _bartenderClient.UploadValues("bartender/bottle/info", data);
            string response = Encoding.UTF8.GetString(json);
            var result = JSS.Deserialize<dynamic>(response);
        }
    }
}
