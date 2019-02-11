﻿using System;
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
        int imgLength = 0;

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
            imgLength = (ConfigurationManager.AppSettings["ImgLength"] + "").GetIntValue();
        }

        private static bool _isCanceling = false;

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Random rnd = new Random();
            long latestID = 0;
            while (!_isCanceling)
            {
                if (sleepMinRangeStart >= sleepMinRangeEnd)
                    sleepMinRangeStart = sleepMinRangeEnd + 10;
                var sleepMinute = rnd.Next(sleepMinRangeStart, sleepMinRangeEnd);

                var searchResult = access.FetchTweetDataFoward(search, latestID);
                if (searchResult == null || searchResult.Count() == 0)
                {
                    AddLog(string.Format("SLEEPING FOR {0} Min", sleepMinute.ToString("N2")));
                    Thread.Sleep(sleepMinute * 60 * 1000);
                    continue;
                }

                for (int i = searchResult.Count() - 1; i >= 0; i--)
                {
                    var tw = searchResult.ElementAtOrDefault(i);
                    if (tw == null)
                        continue;
                    if (!tw.FullText.ToLower().Contains(search.ToLower()))
                        continue;

                    AddLog(string.Format("({2}) @{0} : {1}", tw.CreatedBy.ScreenName, tw.Text, tw.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                    if (latestID < tw.Id)
                        latestID = tw.Id;

                    int ops = rnd.Next(1, 101) % 7;
                    if (ops == 0 || ops == 1)
                    {
                        //LIKES
                        bool res = access.PostLike(tw.Id);
                        AddLog("POST LIKES: " + res);
                    }
                    else if (ops == 2 || ops == 3)
                    {
                        //RETWEET
                        var res = access.PostRetweet(tw.Id);
                        AddLog("POST RETWEET: " + (res != null));
                    }
                    else if (ops == 4)
                    {
                        //REPLY NO IMAGES

                        string msg = allMessages[rnd.Next(0, allMessages.Count)];
                        var res = access.PostReplyTweet(tw.Id, msg);
                        AddLog("POST REPLY: " + (res != null));
                    }
                    else if (ops == 5)
                    {
                        //REPLAY WITH IMAGES
                        var img = string.Format(@"{0}\{1}.jpg", imgPath, rnd.Next(0, imgLength));
                        if (File.Exists(img))
                        {
                            var res = access.PostReplyTweetWithImage(tw.Id, "", File.ReadAllBytes(img));
                            AddLog("POST REPLY WITH IMG: " + (res != null));
                        }
                        else
                        {
                            string msg = allMessages[rnd.Next(0, allMessages.Count)];
                            var res = access.PostReplyTweet(tw.Id, msg);
                            AddLog("POST REPLY: " + (res != null));
                        }
                    }
                    else if (ops == 6)
                    {
                        //REPLAY WITH IMAGES & TEXT
                        string msg = allMessages[rnd.Next(0, allMessages.Count)];
                        var img = string.Format(@"{0}\{1}.jpg", imgPath, rnd.Next(0, imgLength));
                        if (File.Exists(img))
                        {
                            var res = access.PostReplyTweetWithImage(tw.Id, msg, File.ReadAllBytes(img));
                            AddLog("POST REPLY WITH IMG & TEXT: " + (res != null));
                        }
                        else
                        {
                            var res = access.PostReplyTweet(tw.Id, msg);
                            AddLog("POST REPLY: " + (res != null));
                        }
                    }

                    if ((ops + 1) % 2 == 0)
                    {
                        var res = access.FollowUser(tw.CreatedBy.Id);
                        AddLog("FOLLOW USER: " + res);
                    }

                    Thread.Sleep(sleepMinute * 6 * 1000);
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
