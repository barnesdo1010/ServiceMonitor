using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace ServiceMonitor
{
    public partial class Form1 : Form
    {
        List<string> services = new List<string>();

        System.Windows.Forms.Timer monitorTimer = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer autoRestartTimer = new System.Windows.Forms.Timer();

        ServiceController service = null;

        int AutoRestartTime = 60;
        int MonitorTime = 30;

        bool AutoRestartEnabled = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            services.Add("TS HSP Ladle Aisle BH data logger");
            services.Add("TS HSP Ladle Aisle BH 4 tag service");
            services.Add("TS HSP Ladle Aisle BH 5 tag service");
            services.Add("TS HSP Ladle Aisle BH 4 memshare producer");
            services.Add("TS HSP Ladle Aisle BH 5 memshare producer");

            monitorTimer.Tick += new System.EventHandler(monitorTimer_Tick);
            monitorTimer.Interval = 1800000;

            autoRestartTimer.Tick += new System.EventHandler(autoRestartTimer_Tick);
            autoRestartTimer.Interval = 3600000;
        }

        public void Log(string message)
        {
            // List View
            ListViewItem tmp = new ListViewItem(DateTime.Now.ToString());
            tmp.SubItems.Add(message);
            listView1.Invoke((MethodInvoker)delegate ()
            {
                listView1.Items.Insert(0, tmp);
                if (listView1.Items.Count > 50)
                    listView1.Items.RemoveAt(50);
            });
        }


        public void MonitorService()
        {
            foreach (var item in services)
            {
                service = new ServiceController(item);
                try
                {
                    if (!service.Status.Equals(ServiceControllerStatus.Running) || AutoRestartEnabled == true)
                    {
                        Log($"{item} is {service.Status} & is being restarted.");
                        RestartService(item, 10000);
                    }
                    else
                    {
                        Log($"{item} is {service.Status}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"error: {ex}");
                }

            }
        }

        public void RestartService(string serviceName, int timeoutMilliseconds)
        {
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                int millisec1 = Environment.TickCount;
                if (service.Status.Equals(ServiceControllerStatus.Running) || (service.Status.Equals(ServiceControllerStatus.StartPending)))
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    int millisec2 = Environment.TickCount;
                    timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }
                else if (service.Status.Equals(ServiceControllerStatus.Stopped))
                {
                    int millisec2 = Environment.TickCount;
                    timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }
                Log("Service has been reset and the status is " + service.Status);
            }
            catch (Exception ex)
            {
                Log($"error: {ex}");
            }

        }

        //********************************************************************************
        // start monitor
        //
        private void StartMonitor_Click(object sender, EventArgs e)
        {
            Log($"Service Monitor started.");
            MonitorTime = Convert.ToInt32(textBox2.Text);
            monitorTimer.Interval = (MonitorTime * 60000);
            MonitorService();
            monitorTimer.Start();
        }
        private void StopMonitor_Click(object sender, EventArgs e)
        {
            monitorTimer.Stop();
            Log($"Service Monitor stopped.");
        }
        private void monitorTimer_Tick(object sender, EventArgs e)
        {
            monitorTimer.Stop();
            AutoRestartEnabled = false;
            MonitorService();
            monitorTimer.Start();
        }
        //********************************************************************************

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // start auto restart
        //
        private void AutoRestartStart_Click(object sender, EventArgs e)
        {
            Log("AutoRestart active.");
            AutoRestartTime = Convert.ToInt32(textBox3.Text);
            autoRestartTimer.Interval = (AutoRestartTime * 60000);
            AutoRestartEnabled = true;
            MonitorService();
            AutoRestartEnabled = false;
            autoRestartTimer.Start();
        }

        private void AutoRestartStop_Click(object sender, EventArgs e)
        {
            Log("AutoRestart deactivated.");
            autoRestartTimer.Stop();
        }

        private void autoRestartTimer_Tick(object sender, EventArgs e)
        {
            autoRestartTimer.Stop();
            AutoRestartEnabled = true;
            MonitorService();
            AutoRestartEnabled = false;
            autoRestartTimer.Start();
        }
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    }
}
