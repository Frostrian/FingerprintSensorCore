using Microsoft.VisualBasic.Logging;
using System.Windows.Forms;

namespace FingerprintSensorCore
{
    public partial class FormFPSensor : Form
    {
        public FormFPSensor()
        {
            InitializeComponent();
        }

        private FPSensorCore fpsensor;

        private async void btnStart_Click(object sender, EventArgs e)
        {
            var config = new FPSensorConfig
            {
                DeviceId = txtDeviceId.Text,
                WifiSSID = txtSSID.Text,
                WifiPassword = txtWifiPass.Text,
                MqttHost = txtMqttHost.Text,
                MqttPort = int.Parse(txtMqttPort.Text),
                MqttUsername = txtMqttUser.Text,
                MqttPassword = txtMqttPass.Text,
                StaticIp = txtStaticIp.Text
            };

            fpsensor = new FPSensorCore(config, Log);
            await fpsensor.StartAsync();
        }

        private void Log(string message)
        {
            txtLog.Invoke((MethodInvoker)(() =>
            {
                txtLog.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
                txtLog.ScrollToCaret();
            }));
        }
    }
}
