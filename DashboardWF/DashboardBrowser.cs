using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;

namespace DashboardWF
{
    public partial class DashboardBrowser : Form
    {
        public ChromiumWebBrowser browser;
        public readonly string dashboardUrlList = ReadSetting("url-list");
        public int index = 0;

        //some props for hiding mouse cursor + window chrome
        public Timer activityTimer = new Timer();
        public TimeSpan activityThreshold = TimeSpan.FromSeconds(Convert.ToInt32(ReadSetting("hide-cursor-toolbars")));
        public bool cursorHidden = false;
        public bool toolbarsHidden = false;

        public DashboardBrowser()
        {
            InitializeComponent();
            InitializeBrowser();

            PeriodicLoadUrl(new CancellationToken());
        }

        private void InitializeBrowser()
        {
            CefSettings settings = new CefSettings();

            if (!Directory.Exists(Path.GetDirectoryName(ReadSetting("cookie-path"))))
            {
                try
                {
                    Directory.CreateDirectory(ReadSetting("cookie-path"));
                }
                catch
                {
                    MessageBox.Show("Unable to create the cookie folder. Try creating it manually", "Oops");
                }
            }

            settings.CachePath = ReadSetting("cookie-path");
            settings.PersistSessionCookies = true;

            CefSharp.Cef.Initialize(settings);

            browser = new ChromiumWebBrowser(String.Empty)
            {
                Dock = DockStyle.Fill,
            };

            this.Controls.Add(browser);
        }

        public void LoadUrl(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                browser.Load(url);
            }
        }

        public async Task PeriodicLoadUrl(CancellationToken cancellationToken)
        {
            List<DashboardUrls> urls = new List<DashboardUrls>();

            while (true)
            {
                try
                {
                    urls = JsonConvert.DeserializeObject<List<DashboardUrls>>(new WebClient().DownloadString(dashboardUrlList));
                }
                catch (Exception e)
                {
                    MessageBox.Show("Unable to read the JSON list of URLs.\n\nIs the location still correct?\nIs the JSON valid?", "Oops", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }

                try
                {

                    urls = JsonConvert.DeserializeObject<List<DashboardUrls>>(new WebClient().DownloadString(dashboardUrlList));

                    LoadUrl(urls[index].url);

                    await Task.Delay((int)TimeSpan.FromSeconds(urls[index].duration).TotalMilliseconds, cancellationToken);
                }
                catch
                {
                    index = -1;
                }
                finally
                {
                    index++;
                }
            }
        }

        public class DashboardUrls
        {
            public string url { get; set; }
            public int duration { get; set; }
        }

        static String ReadSetting(string key)
        {
            string result = String.Empty;

            try
            {
                result = ConfigurationManager.AppSettings[key];
            }
            catch (ConfigurationErrorsException)
            {
                MessageBox.Show("Unable to find the app.config value. Would you mind checking for me?", "Oops");
            }

            return result;
        }

        void MouseTimer_Tick(object sender, EventArgs e)
        {
            bool shouldHide = User32Interop.GetLastInput() > activityThreshold;
            if (cursorHidden != shouldHide)
            {
                if (shouldHide)
                {
                    Cursor.Hide();
                    HideToolBars();
                }
                else
                {
                    Cursor.Show();
                    ShowToolBars();
                }
                cursorHidden = shouldHide;
            }
        }

        public void HideToolBars()
        {
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
        }

        public void ShowToolBars()
        {
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
        }

    }

    public static class User32Interop
    {
        public static TimeSpan GetLastInput()
        {
            var plii = new LASTINPUTINFO();
            plii.cbSize = (uint)Marshal.SizeOf(plii);

            if (GetLastInputInfo(ref plii))
                return TimeSpan.FromMilliseconds(Environment.TickCount - plii.dwTime);
            else
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
    }

}
