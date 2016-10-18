using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Marketplace.ARMTemplate.Common.Models;
using Newtonsoft.Json.Linq;

namespace ATP
{
    public class ARMResources
    {
        public int Index { get; set; }
        public string Type { get; set; }
        public bool IsMMPUI { get; set; }
        public string Value { get; set; }
    }

    class Program
    {
        string ReplaceExistingResources(IList<ARMResources> repl, string template, string parameters, string resourceGroupName, string deploymentName)
        {
            var t = new ArmTemplate(JToken.Parse(template));
            var p = JObject.Parse(parameters);
            var dep = new Deployment("subscription", resourceGroupName, deploymentName, t, p);
            var index = 0;
            var toBeDeleted = new List<int>();

            foreach (var rsrc in t.Resources.Model)
            {
                foreach (var armResourcese in repl)
                {
                    if (armResourcese.Index == index)
                    {
                        if (!string.IsNullOrEmpty(armResourcese.Value))
                        {
                            // To be deleted
                            toBeDeleted.Add(index);
                            // Replace dependency for all resources depend on this one
                        }
                    }
                }
                index++;
            }
            // Delete in reversed order
            toBeDeleted.Sort();
            toBeDeleted.Reverse();
            return "";
        }
        static void Main(string[] args)
        {
            var wc = new System.Net.WebClient();
            var tmpl= wc.DownloadString("https://raw.githubusercontent.com/windoze/azure-china-swarm/master/azuredeploy.json");
            var par=
            wc.DownloadString(
                "https://raw.githubusercontent.com/windoze/azure-china-swarm/master/azuredeploy.parameters.json");

            var t=new ArmTemplate(JToken.Parse(tmpl));
            var p = JObject.Parse(par);

            var dep=new Deployment("subscription", "resourceGroup", "deploymentName", t, p);
            t.Resources[1];
        }
    }
}
