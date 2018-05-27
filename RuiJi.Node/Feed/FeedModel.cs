﻿using Newtonsoft.Json;
using RuiJi.Core.Crawler;
using RuiJi.Core.Extracter;
using RuiJi.Core.Extracter.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RuiJi.Node.Feed
{
    public enum FeedTypeEnum
    {
        HTML,
        XML,
        JS,
        JSON,
        JSONP
    }

    public enum FeedStatus
    {
        ON,
        OFF
    }

    public class FeedModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("sitename")]
        public string SiteName { get; set; }

        [JsonProperty("railling")]
        public string Railling { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("delay")]
        public int Delay { get; set; }

        [JsonProperty("type")]
        public FeedTypeEnum Type { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("postParam")]
        public string PostParam { get; set; }

        [JsonProperty("ua")]
        public string UA { get; set; }

        [JsonProperty("headers")]
        public List<WebHeader> Headers { get; set; }

        [JsonProperty("scheduling")]
        public string Scheduling { get; set; }

        [JsonProperty("status")]
        public FeedStatus Status { get; set; }

        [JsonProperty("block")]
        public ExtractBlock ExtractBlock { get; set; }
    }
}