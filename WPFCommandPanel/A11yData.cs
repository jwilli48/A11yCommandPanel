using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace My
{
    public class A11yData
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int Severity { get; set; }
        [JsonProperty("Descriptive Error", NullValueHandling = NullValueHandling.Ignore)]
        public string DescriptiveError { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Notes { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Location { get; set; }
        [JsonProperty("Issue Type", NullValueHandling = NullValueHandling.Ignore)]
        public string IssueType { get; set; }
        [JsonProperty("Completed?", NullValueHandling = NullValueHandling.Ignore)]
        public bool Completed { get; set; }
        [JsonProperty("Completed", NullValueHandling = NullValueHandling.Ignore)]
        private bool Completed2 { get { return Completed; } set { Completed = value; } }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string html { get; set; }
    }
}
