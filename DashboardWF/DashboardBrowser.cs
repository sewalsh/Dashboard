using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;

namespace DashboardWF
{
    public partial class DashboardBrowser : Form
    {
        public ChromiumWebBrowser browser;
        public readonly string dashboardUrlList = @"https://seamus.party/dashboard/urls.json";

        public DashboardBrowser()
        {
            InitializeComponent();
            InitializeBrowser();
            this.Show();
            StartUrlCycle();
        }

        private void InitializeBrowser()
        {
            browser = new ChromiumWebBrowser("")
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

        public void StartUrlCycle()
        {
            int index = 0;

            bool neverStop = true;
            
            while (neverStop)
            {
                var urls = JsonConvert.DeserializeObject<List<DashboardUrls>>(new WebClient().DownloadString(dashboardUrlList));

                try
                {
                    var url = urls[index].url;

                    LoadUrl(urls[index].url);
                    Thread.Sleep((int)TimeSpan.FromSeconds(urls[index].duration).TotalMilliseconds);
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
            public string description { get; set; }
            public int duration { get; set; }
            
        }
    }
}
