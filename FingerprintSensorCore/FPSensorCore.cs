
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text;
using System.Timers;


namespace FingerprintSensorCore
{
    public class FPSensorCore
    {
        private IMqttClient mqttClient;
        private MqttFactory mqttFactory;
        private FPSensorConfig config;
        private Action<string> logAction;
        private bool authenticated = false;

        private System.Timers.Timer scanTimer;
        private Random rand = new Random();

        public FPSensorCore(FPSensorConfig cfg, Action<string> logCallback)
        {
            config = cfg;
            logAction = logCallback;
            mqttFactory = new MqttFactory();
        }

        public async Task StartAsync()
        {
            mqttClient = mqttFactory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(config.MqttHost, config.MqttPort)
                .WithClientId(config.DeviceId)
                .Build();

            mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                if (topic == $"auth/{config.DeviceId}/response")
                    HandleAuthResponse(payload);

                await Task.CompletedTask;
            };

            await mqttClient.ConnectAsync(options, CancellationToken.None);
            logAction("MQTT bağlantısı kuruldu.");

            await mqttClient.SubscribeAsync($"auth/{config.DeviceId}/response");

            var authPayload = new
            {
                deviceId = config.DeviceId,
                ip = config.StaticIp,
                username = config.MqttUsername,
                password = config.MqttPassword
            };

            var json = System.Text.Json.JsonSerializer.Serialize(authPayload);

            await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic("auth/request")
                .WithPayload(json)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());

            logAction("Giriş isteği gönderildi.");
        }

        private void HandleAuthResponse(string payload)
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(payload);

            if (result.status == "ok")
            {
                logAction("Doğrulama başarılı. Parmak izi sensörü aktif.");
                authenticated = true;
                StartSimulatedScans();
            }
            else
            {
                logAction("❌ Doğrulama başarısız: " + result.reason);
            }
        }

        private void StartSimulatedScans()
        {
            scanTimer = new System.Timers.Timer(rand.Next(15000, 45000));
            scanTimer.Elapsed += async (s, e) =>
            {
                scanTimer.Stop();
                logAction("⏱️ Timer Tick başladı"); // LOG BURAYA
                await SendFingerprintAccess();
                scanTimer.Interval = rand.Next(15000, 45000);
                scanTimer.Start();
            };
            scanTimer.AutoReset = false;
            scanTimer.Start();
            logAction("⏱️ Parmak izi tarayıcı timer'ı başlatıldı");
        }

        private async Task SendFingerprintAccess()
        {
            if (!authenticated)
            {
                logAction("❌ Yetki alınmamış. Veri gönderilmeyecek.");
                return;
            }

            if (!mqttClient.IsConnected)
            {
                logAction("❌ MQTT bağlantısı yok. Veri gönderilmeyecek.");
                return;
            }

            string[] users = { "U001", "U123", "U456", "U789", "U777" };
            string userId = users[rand.Next(users.Length)];
            string result = rand.NextDouble() > 0.2 ? "accepted" : "denied";

            var payload = $"{{\"deviceId\":\"{config.DeviceId}\",\"ip\":\"{config.StaticIp}\",\"userId\":\"{userId}\",\"result\":\"{result}\",\"timestamp\":\"{DateTime.Now:O}\"}}";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"fingerprint/{config.DeviceId}/access")
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await mqttClient.PublishAsync(message);
            logAction($"🔎 Parmak izi denemesi gönderildi ({userId} → {result})");
        }

        private class AuthResponse
        {
            public string status { get; set; }
            public string reason { get; set; }
        }
    }
}
