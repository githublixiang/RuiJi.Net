﻿using Amib.Threading;
using Newtonsoft.Json;
using Quartz;
using RuiJi.Net.Core.Expression;
using RuiJi.Net.Core.Extractor;
using RuiJi.Net.Core.Extractor.Selector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuiJi.Net.Node.Feed.LTS
{
    public class FeedExtractJob : IJob
    {
        public static bool IsRunning = false;

        private static string baseDir;

        private string baseUrl;

        static FeedExtractJob()
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory;

            if (!Directory.Exists(baseDir + @"/history"))
            {
                Directory.CreateDirectory(baseDir + @"/history");
            }
            if (!Directory.Exists(baseDir + @"/pre"))
            {
                Directory.CreateDirectory(baseDir + @"/pre");
            }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (!IsRunning)
            {
                IsRunning = true;

                baseUrl = context.JobDetail.JobDataMap.Get("baseUrl").ToString();

                try
                {

                    MoveDelayFeed();

                    var task = Task.Factory.StartNew(() =>
                    {
                        var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"snapshot");

                        var stpStartInfo = new STPStartInfo
                        {
                            IdleTimeout = 3000,
                            MaxWorkerThreads = 8,
                            MinWorkerThreads = 0
                        };

                        var pool = new SmartThreadPool(stpStartInfo);
                        var waits = new List<IWorkItemResult>();
                        foreach (var file in files)
                        {
                            var item = pool.QueueWorkItem((fileName) =>
                            {
                                DoTask(fileName);
                            }, file);
                            waits.Add(item);
                        }

                        SmartThreadPool.WaitAll(waits.ToArray());
                        pool.Shutdown(true, 1000);
                        pool.Dispose();
                        pool = null;
                        waits.Clear();
                    });

                    await task;
                }
                catch { }

                IsRunning = false;
            }
        }

        public void MoveDelayFeed()
        {
            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"delay");
            foreach (var file in files)
            {
                var filename = file.Substring(file.LastIndexOf(@"\") + 1);
                var sp = filename.Split('_');
                var ticks = sp[1].Substring(0, sp[1].LastIndexOf("."));

                if (long.Parse(ticks) < DateTime.Now.Ticks)
                {
                    var desFile = file.Replace("delay", "snapshot");
                    File.Move(file, desFile);
                }
            }
        }

        public void DoTask(string path)
        {
            try
            {
                var filename = path.Substring(path.LastIndexOf(@"\") + 1);
                var sp = filename.Split('_');
                var feedId = Convert.ToInt32(sp[0]);
                var content = File.ReadAllText(path);

                var feed = JsonConvert.DeserializeObject<FeedSnapshot>(content);
                var url = feed.Url;

                var urls = ExtractAddress(feed);

                var hisFile = AppDomain.CurrentDomain.BaseDirectory + @"history\" + feedId + ".txt";
                var urlsHistory = new string[0];
                if (File.Exists(hisFile))
                {
                    urlsHistory = File.ReadAllLines(hisFile, Encoding.UTF8);
                }

                File.WriteAllLines(hisFile, urls, Encoding.UTF8);

                urls.RemoveAll(m => urlsHistory.Contains(m));
                urls.RemoveAll(m => string.IsNullOrEmpty(m));
                urls.RemoveAll(m => !Uri.IsWellFormedUriString(m, UriKind.Absolute));

                foreach (var u in urls)
                {
                    var qm = new QueueModel();
                    qm.FeedId = feedId;
                    qm.Url = u;

                    ContentQueue.Instance.Enqueue(qm);
                }

                var destFile = path.Replace("snapshot", "pre").Replace(filename, feedId + ".txt");
                if (File.Exists(destFile))
                    File.Delete(destFile);

                File.Move(path, destFile);
            }
            catch { }
        }

        public List<string> ExtractAddress(FeedSnapshot feed)
        {
            var block = new ExtractBlock();
            block.TileSelector.Selectors.Add(new CssSelector("a", "href"));

            if (feed.UseBlock)
            {
                if (!string.IsNullOrEmpty(feed.BlockExpression))
                    block = JsonConvert.DeserializeObject<ExtractBlock>(feed.BlockExpression);
            }
            else
            {
                if (!string.IsNullOrEmpty(feed.RuiJiExpression))
                {
                    block.TileSelector.Selectors.Clear();

                    var parser = new RuiJiParser();

                    var s = RuiJiBlockParser.ParserBase(feed.RuiJiExpression).Selectors;
                    block.TileSelector.Selectors.AddRange(s);
                }
            }

            var result = RuiJiExtractor.Extract(feed.Content, block);
            var results = new List<string>();

            if (result.Tiles != null)
            {
                foreach (var item in result.Tiles)
                {
                    var href = item.Content.ToString();
                    if (href.Contains("#"))
                    {
                        href = href.Substring(0, href.IndexOf('#'));
                    }
                    if (Uri.IsWellFormedUriString(href, UriKind.Relative))
                        href = new Uri(new Uri(feed.Url), href).AbsoluteUri.ToString();
                    results.Add(href);
                }
            }

            return results.Distinct().ToList();
        }
    }
}