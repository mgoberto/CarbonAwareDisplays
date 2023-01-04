using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LightMeasure
{
    internal class WattTime
    {
        private const string URL = "https://api2.watttime.org/";

        #region GetRealEmission
        /// <summary>
        /// Get Real-Time Emission from Region
        /// </summary>
        /// <param name="ba"></param>
        /// <returns>CO2 lbs/MWh</returns>
        public static double GetRealEmission(string ba)
        {
            string tkn = Login();
            try
            {
                HttpClient client = new HttpClient();
                Uri baseUri = new Uri(URL);
                client.BaseAddress = baseUri;
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.ConnectionClose = true;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tkn);

                var requestMessage = new HttpRequestMessage(HttpMethod.Get, "v2/index?ba=" + ba);
                var task = client.SendAsync(requestMessage);
                var response = task.Result;
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;
                var reg = JsonSerializer.Deserialize<WTEmission>(responseBody);
                if (reg != null)
                    return Convert.ToDouble(reg.moer);
            }
            catch
            {
                return -1;
            }
            return 0;
        }
        #endregion

        #region GetRegion
        /// <summary>
        /// Get Region or List of Region
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <returns></returns>
        public static List<Location> GetRegion(double lat, double lng)
        {
            string tkn = Login();
            List<Location> lLocation = new List<Location>();

            if (lat > 0)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    Uri baseUri = new Uri(URL);
                    client.BaseAddress = baseUri;
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.ConnectionClose = true;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tkn);
                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, "v2/ba-from-loc?latitude=" + lat.ToString(CultureInfo.GetCultureInfo("en-GB")) + "&longitude=" + lng.ToString(CultureInfo.GetCultureInfo("en-GB")));
                    var task = client.SendAsync(requestMessage);
                    var response = task.Result;
                    if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        response.EnsureSuccessStatusCode();
                        string responseBody = response.Content.ReadAsStringAsync().Result;
                        var reg = JsonSerializer.Deserialize<WTFromLocation>(responseBody);
                        if (reg != null)
                            lLocation.Add(new Location { code = reg.abbrev, name = reg.name });
                    }
                }
                catch
                {
                    //Do Nothing
                }
            }
            if (lLocation.Count == 0)
            {

                try
                {
                    HttpClient client = new HttpClient();
                    Uri baseUri = new Uri(URL);
                    client.BaseAddress = baseUri;
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.ConnectionClose = true;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tkn);
                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, "v2/ba-access?all=true");
                    var task = client.SendAsync(requestMessage);
                    var response = task.Result;
                    response.EnsureSuccessStatusCode();
                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    var lWT = JsonSerializer.Deserialize<IList<WTLocation>>(responseBody);
                    if (lWT != null)
                        foreach (var item in lWT)
                        {
                            if (item.access)
                                lLocation.Add(new Location { code = item.ba, name = item.name });
                        }
                    lLocation = lLocation.OrderBy(_ => _.name).ToList();
                }
                catch
                {
                    lLocation.Add(new Location { code = "ERR", name = "Error Loading Region" });
                }
            }
            return lLocation;
        }
        #endregion

        #region Login
        /// <summary>
        /// Authenticate WattTime API
        /// </summary>
        /// <returns>Token</returns>
        private static string Login()
        {
            string Username = "gsf_marcelo569";
            string Password = "BI6++3PRPGgenA)$4";
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(URL);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.ConnectionClose = true;
                client.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(Username, Password);
                HttpResponseMessage response = client.GetAsync(URL + "/v2/login").Result;
                HttpContent content = response.Content;
                string responseBody = response.Content.ReadAsStringAsync().Result;
                return JsonSerializer.Deserialize<WTToken>(responseBody).token;
            }
            catch 
            {
                throw new Exception("Fail Carbon Aware API");
            }
            return String.Empty;
        }
        #endregion

    }

    #region Objects
    class Location
    {
        public string name { get; set; }
        public string code { get; set; }
    }
    public class WTToken
    {
        public string token { get; set; }
    }


    public class WTLocation
    {
        public string ba { get; set; }
        public string name { get; set; }
        public bool access { get; set; }
        public string datatype { get; set; }
    }

    public class WTFromLocation
    {
        public string abbrev { get; set; }
        public int id { get; set; }
        public string name { get; set; }
    }

    public class WTEmission
    {
        public string moer { get; set; }
        public string percent { get; set; }
        public string ba { get; set; }
        public string freq { get; set; }
        public string point_time { get; set; }
    }
    #endregion
}
