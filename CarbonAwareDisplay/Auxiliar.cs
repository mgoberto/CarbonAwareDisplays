using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Location;
using System.Configuration;

namespace LightMeasure
{
    internal class Auxiliar
    {
        /// <summary>
        /// Get Coordinate Device
        /// </summary>
        /// <returns></returns>
        public static (double lat, double lng) GetPosition()
        {

            double dlat = 0, dlng = 0;
            try
            {
                GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
                watcher.Start(); //started watcher
                GeoCoordinate coord = watcher.Position.Location;
                if (!watcher.Position.Location.IsUnknown)
                {
                    dlat = coord.Latitude; //latitude
                    dlng = coord.Longitude;  //logitude
                }
            }
            catch { }
            return (dlat, dlng);
        }

        ///// <summary>
        ///// Get Current Brightness First Screen 
        ///// </summary>
        ///// <returns></returns>
        //public static int GetCurrentBrightness()
        //{
        //    //define scope (namespace)
        //    System.Management.ManagementScope s = new System.Management.ManagementScope("root\\WMI");
        //    //define query
        //    System.Management.SelectQuery q = new System.Management.SelectQuery("WmiMonitorBrightness");
        //    //output current brightness
        //    System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(s, q);
        //    System.Management.ManagementObjectCollection moc = mos.Get();
        //    //store result
        //    byte curBrightness = 0;
        //    foreach (System.Management.ManagementObject o in moc)
        //    {
        //        curBrightness = (byte)o.GetPropertyValue("CurrentBrightness");
        //        break; //only work on the first object
        //    }
        //    moc.Dispose();
        //    mos.Dispose();
        //    return (int)curBrightness;
        //}

        public static void AddUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager
                                  .OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                //Console.WriteLine("Error writing app settings");
            }
        }
    }
}
