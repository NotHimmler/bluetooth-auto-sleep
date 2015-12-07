using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using InTheHand;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.IO;
using System.Threading;
using System.Timers;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Timers.Timer deviceAvailableCheck;
        BluetoothDeviceInfo selectedDevice;
        int numTimesNotFound = 0;

        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("C:\\Users\\heinr\\documents\\visual studio 2015\\Projects\\WpfApplication1\\WpfApplication1\\Assets\\Icon1.ico");
            ni.Visible = true;
            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };

            CurrentDeviceTB.Text = Settings1.Default.SleepDeviceName;
            CheckIntervalTB.Text = Settings1.Default.CheckTime.ToString();
            if (Settings1.Default.StartMinimised)
            {
                StartMinimisedCB.IsChecked = true;
                this.Hide();
            }
            if (Settings1.Default.StartWhenOpen)
            {
                StartOnOpenCB.IsChecked = true;

                StartStopBtn.Content = "Stop";
                Settings1.Default.AppRunning = true;
                setTimer(Settings1.Default.CheckTime * 60000);
            } else
            {
                StartStopBtn.Content = "Start";
                Settings1.Default.AppRunning = false;
            }


            if (Settings1.Default.StartWithWindows)
            {
                WinStartCB.IsChecked = true;
            }
        }

        static void RunSleep()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "rundll32.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "powrprof.dll,SetSuspendState 0,1,0";

            try
            {
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch
            {

            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }

        private void findDevices()
        {
            BluetoothDeviceInfo[] devices;

            BluetoothRadio primary = BluetoothRadio.PrimaryRadio;

            if (primary == null)
            {
            } else
            {
                BluetoothClient cli = new BluetoothClient();
                devices = cli.DiscoverDevices();

                this.Dispatcher.Invoke((Action)(() =>
                {
                    btDevices.ItemsSource = devices;
                }));
            }
        }

        private bool queryDevice(string addr)
        {

            //Attempst to pair with bluetooth device - if successful returns true, else false
            bool isPaired = BluetoothSecurity.PairRequest(BluetoothAddress.Parse(addr), "123456");

            return isPaired;
        }

        private void queryDevice(object source, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                RunStatusTB.Text = "Looking for device...";
            }));

            if (!queryDevice(Settings1.Default.SleepDevice) && numTimesNotFound<3)
            {
                numTimesNotFound++;
                this.Dispatcher.Invoke((Action)(() =>
                {
                    RunStatusTB.Text = string.Format("Device not found: {0} times", numTimesNotFound);
                }));
                
                if (numTimesNotFound == 3)
                {
                    numTimesNotFound = 0;
                    RunSleep();
                }

            } else
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    RunStatusTB.Text = string.Format("Device present.");
                }));
            }
        }


        private void btDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedDevice = (BluetoothDeviceInfo)btDevices.SelectedItem;
        }

        private void SetDefaultDeviceBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!selectedDevice.DeviceAddress.Equals(string.Empty) && selectedDevice.Authenticated)
            {
                Settings1.Default.SleepDevice = selectedDevice.DeviceAddress.ToString();
                Settings1.Default.SleepDeviceName = selectedDevice.DeviceName;
                CurrentDeviceTB.Text = selectedDevice.DeviceName;
                Settings1.Default.Save();
            }
        }

        private void GetDevicesBtn_Click(object sender, RoutedEventArgs e)
        {
            Thread checkThread = new Thread(new ThreadStart(findDevices));
            checkThread.Start();
        }

        private void WinStartCB_Click(object sender, RoutedEventArgs e)
        {
            Settings1.Default.StartWithWindows = (bool)WinStartCB.IsChecked;
            Settings1.Default.Save();
        }

        private void StartStopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Settings1.Default.AppRunning)
            {
                StartStopBtn.Content = "Start";
                Settings1.Default.AppRunning = false;
                deviceAvailableCheck.Stop();
                deviceAvailableCheck.Dispose();
            } else if (!Settings1.Default.SleepDevice.Equals(String.Empty))
            {
                StartStopBtn.Content = "Stop";
                Settings1.Default.AppRunning = true;
                setTimer(Settings1.Default.CheckTime*60000);
                RunStatusTB.Text = "Running...";
            }
        }

        private void setTimer(double duration)
        {
            deviceAvailableCheck = new System.Timers.Timer(duration);
            deviceAvailableCheck.Elapsed += queryDevice;

            deviceAvailableCheck.AutoReset = true;
            deviceAvailableCheck.Enabled = true;
        }

        private void CheckIntervalTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int value = Math.Abs(int.Parse(CheckIntervalTB.Text));

                Settings1.Default.CheckTime = Math.Abs(value);
                Settings1.Default.Save();
            } catch
            {
                CheckIntervalTB.Text = "Invalid Input";
                CheckIntervalTB.SelectAll();
            }
        }

        private void CheckIntervalTB_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                int value = int.Parse(CheckIntervalTB.Text);
            }
            catch
            {
                CheckIntervalTB.SelectAll();
            }

        }

        private void StartOnOpenCB_Click(object sender, RoutedEventArgs e)
        {
            Settings1.Default.StartWhenOpen = (bool)StartOnOpenCB.IsChecked;
            Settings1.Default.Save();
        }

        private void StartMinimisedCB_Click(object sender, RoutedEventArgs e)
        {
            Settings1.Default.StartMinimised = (bool)StartMinimisedCB.IsChecked;
            Settings1.Default.Save();
        }
    }
}
