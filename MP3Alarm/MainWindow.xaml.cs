using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.ComponentModel;
using System.Media;
using Microsoft.Windows.Controls;
using System.Drawing;
using System.Windows.Forms;

namespace MP3Alarm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        public static extern SafeWaitHandle CreateWaitableTimer(IntPtr lpTimerAttributes, bool bManualReset, string lpTimerName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWaitableTimer(SafeWaitHandle hTimer, [In] ref long pDueTime, int lPeriod, IntPtr pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, bool fResume);

        // Variables
        protected MediaPlayer player;
        protected System.Windows.Forms.NotifyIcon MyNotifyIcon;
        protected string sAlarm;
        protected BackgroundWorker bw = new BackgroundWorker();
        protected long duetime;

        public MainWindow()
        {
            InitializeComponent();

            // Prep threading 
            bw.WorkerReportsProgress = false;
            bw.WorkerSupportsCancellation = false;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);

            // Prep tray icon
            MyNotifyIcon = new System.Windows.Forms.NotifyIcon();
            MyNotifyIcon.Icon = new System.Drawing.Icon("ClockIcon.ico");
            MyNotifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(MyNotifyIcon_MouseDoubleClick);
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            // Perform a time consuming operation in the background

            // Set up the wake up call and wait...
            using (SafeWaitHandle handle = CreateWaitableTimer(IntPtr.Zero, true, "MP3AlarmTimer"))
            {
                if (SetWaitableTimer(handle, ref duetime, 0, IntPtr.Zero, IntPtr.Zero, true))
                {
                    using (EventWaitHandle wh = new EventWaitHandle(false, EventResetMode.AutoReset))
                    {
                        wh.SafeWaitHandle = handle;
                        wh.WaitOne();
                    }
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Execute actions on wake up
            button1.IsEnabled = true;
            this.WindowState = System.Windows.WindowState.Normal;
            PlayMusic();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (!System.IO.File.Exists(filePath.Text))
            {
                System.Windows.MessageBox.Show("Please select a valid MP3 file.");
                return;
            }

            if (calendar1.SelectedDate == null)
            {
                System.Windows.MessageBox.Show("Please select a date.");
                return;
            }

            // Disable button
            button1.IsEnabled = false;

            // Get the alarm time
            DateTime date = calendar1.SelectedDate.Value;
            DateTime time = alarmtime.Value.Value;
            DateTime alarmTime = DateTime.Parse(date.ToString("MM/dd/yyyy") + " " + time.ToString("h:mm tt"));
            sAlarm = date.ToString("MM/dd/yyyy") + " " + time.ToString("h:mm tt");
            duetime = alarmTime.ToFileTime();

            // Minimize to tray
            this.WindowState = System.Windows.WindowState.Minimized;            

            // Call thread
            if (bw.IsBusy != true)
            {
                bw.RunWorkerAsync();
            }
        }

        private void browseButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            // Also set initial directory to MY MUSIC
            dlg.DefaultExt = ".mp3";
            dlg.Filter = "MP3 Files (.mp3)|*.mp3";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic); 

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                filePath.Text = filename;
            }
        }

        private void PlayMusic()
        {
            try
            {
                player = new MediaPlayer();
                player.Open(new Uri(filePath.Text));
                player.Play();
            }
            catch
            {
                System.Windows.MessageBox.Show("Could not start music player.");
                for (int i = 0; i < 100; i++)
                {
                    SystemSounds.Beep.Play();
                    Thread.Sleep(500);
                }
            }
        }

        void MyNotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Normal;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                MyNotifyIcon.BalloonTipTitle = "MP3Alarm";
                MyNotifyIcon.BalloonTipText = "Alarm Set: " + sAlarm;
                MyNotifyIcon.Visible = true; 
                MyNotifyIcon.ShowBalloonTip(1000);
            }
            else if (this.WindowState == System.Windows.WindowState.Normal)
            {
                MyNotifyIcon.Visible = false;
                this.ShowInTaskbar = true;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MyNotifyIcon.Visible = false;
            MyNotifyIcon.Dispose();
        }
    }
}
