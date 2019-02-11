using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Edgeworks.Common;
using Tweetinvi;
using Tweetinvi.Models;

namespace SetanSosmed
{
    public partial class Form1 : Form
    {
        private List<string> allMessages = new List<string>();
        private TwitterAccess access = null;
        string userName = "";
        string consumerKey = "";
        string consumerSecret = "";
        string accessToken = "";
        string accessTokenScreet = "";
        string imagesDirectory = "";
        string search = "";
        int sleepMinRangeStart = 60;
        int sleepMinRangeEnd = 240;
        string imgPath = "";

        public Form1()
        {
            InitializeComponent();
            search = ConfigurationManager.AppSettings["Search"] + "";
            userName = ConfigurationManager.AppSettings["UserName"] + "";
            consumerKey = ConfigurationManager.AppSettings["ConsumerKey"] + "";
            consumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"] + "";
            accessToken = ConfigurationManager.AppSettings["AccessToken"] + "";
            accessTokenScreet = ConfigurationManager.AppSettings["AccessTokenScreet"] + "";
            imagesDirectory = ConfigurationManager.AppSettings["ImagesDirectory"] + "";
            sleepMinRangeStart = (ConfigurationManager.AppSettings["SleepMinRangeStart"] + "").GetIntValue();
            sleepMinRangeEnd = (ConfigurationManager.AppSettings["SleepMinRangeEnd"] + "").GetIntValue();
            if (sleepMinRangeStart <= 0)
                sleepMinRangeStart = 60;
            if (sleepMinRangeEnd <= 0)
                sleepMinRangeEnd = 240;
            sleepMinRangeEnd++;
            access = new TwitterAccess(consumerKey, consumerSecret, accessToken, accessTokenScreet);
            allMessages = File.ReadAllLines(Application.StartupPath + "//allMessages.txt").ToList();
            imgPath = Application.StartupPath + @"\Img";
        }

        private static bool _isCanceling = false;

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Random rnd = new Random();
            while (!_isCanceling)
            {
                if (sleepMinRangeStart >= sleepMinRangeEnd)
                    sleepMinRangeStart = sleepMinRangeEnd + 10;
                var sleepMinute = rnd.Next(sleepMinRangeStart, sleepMinRangeEnd);

                long latestID = 0;
                var searchResult = access.FetchTweetDataFoward(search, DateTime.Now.AddHours(-1));
                if (searchResult == null)
                {
                    AddLog(string.Format("SLEEPING FOR {0} Min", sleepMinute.ToString("N2")));
                    Thread.Sleep(sleepMinute * 60 * 1000);
                    continue;
                }

                foreach (var tw in searchResult)
                {
                    if (!tw.FullText.ToLower().Contains(search.ToLower()))
                        continue;
                    AddLog(string.Format("{0} : {1}", tw.CreatedBy.ScreenName, tw.FullText));

                    if (tw.Id > latestID)
                        latestID = tw.Id;

                    //int iMsg = rnd.Next(0, allMessages.Count);
                    //string msg = allMessages[iMsg];

                    var isSucess = access.PostLike(tw.Id);

                    if (!isSucess)
                        AddLog(GetLastError());

                    //AddLog(string.Format("SLEEPING FOR {0} Min", sleepMinute.ToString("N2")));
                    //Thread.Sleep(sleepMinute * 60 * 1000);
                }
            }
        }

        private void AddLog(string log)
        {
            txtLog.BeginInvoke((Action)(() =>
            {
                txtLog.Text = string.Format("-{0}: {1}", DateTime.Now.ToString("HH:mm:ss"), log + Environment.NewLine + txtLog.Text);
            }));
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnStart.Text = "START";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (btnStart.Text == "START")
            {
                if (!worker.IsBusy)
                {
                    _isCanceling = false;
                    worker.RunWorkerAsync();
                }
            }
            else
            {
                _isCanceling = true;
            }

            btnStart.Text = btnStart.Text == "START" ? "STOP" : "START";
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private string GetLastError()
        {
            string lastError = "";
            try
            {
                lastError = ExceptionHandler.GetLastException().TwitterDescription;
            }
            catch (Exception ex)
            {
                AddLog(ex.Message);
            }

            return lastError;
        }
    }
}
