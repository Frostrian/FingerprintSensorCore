using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FingerprintSensorCore
{
    public class FPSensorConfig
    {
        public string DeviceId { get; set; }
        public string WifiSSID { get; set; }
        public string WifiPassword { get; set; }
        public string MqttHost { get; set; }
        public int MqttPort { get; set; }
        public string MqttUsername { get; set; }
        public string MqttPassword { get; set; }
        public string StaticIp { get; set; }

        public static FPSensorConfig Load(string path)
        {
            if (!File.Exists(path))
                return new FPSensorConfig(); // defaults

            var json = File.ReadAllText(path);
            return System.Text.Json.JsonSerializer.Deserialize<FPSensorConfig>(json);
        }

        public void Save(string path)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
