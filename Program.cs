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
        static string ReplaceExistingResources(IList<ARMResources> repl, string template, string parameters,
            string resourceGroupName, string deploymentName)
        {
            var t = new ArmTemplate(JToken.Parse(template));
            var p = JObject.Parse(parameters);
            var dep = new Deployment("subscription", resourceGroupName, deploymentName, t, p);
            var index = 0;
            var toBeDeleted = new List<KeyValuePair<int, string>>();

            foreach (var rsrc in t.Resources.Resources)
            {
                foreach (var armResourcese in repl)
                {
                    if (armResourcese.Index == index && armResourcese.Type.ToLower() == rsrc.Type.ToLower())
                    {
                        if (!string.IsNullOrEmpty(armResourcese.Value))
                        {
                            // To be deleted
                            toBeDeleted.Add(new KeyValuePair<int, string>(index, armResourcese.Value));
                            // Replace dependency for all resources depend on this one
                        }
                    }
                }
                index++;
            }
            // Process in reversed order
            toBeDeleted.Sort();
            toBeDeleted.Reverse();

            // Replace dependencies
            foreach (var keyValuePair in toBeDeleted)
            {
                var name = (t.Resources.Resources[keyValuePair.Key].Type+"/"+dep.Evaluate(t.Resources.Resources[keyValuePair.Key].Name)).ToString().ToLower();
                foreach (var resource in t.Resources.Resources)
                {
                    int depIndex = 0;
                    foreach (var dependsOn in resource.DependsOn)
                    {
                        try
                        {
                            var d = dep.Evaluate(dependsOn);
                            if (d.ToString().ToLower() == name)
                            {
                                // Update dependencies
                                resource.SetDependency(depIndex, keyValuePair.Value);
                            }
                        }
                        catch (ExpressionException)
                        {
                            // TODO: Warning
                            // Skip resource which is unable to be processed
                        }
                        depIndex++;
                    }
                }
            }
            // Delete resources
            foreach (var keyValuePair in toBeDeleted)
            {
                t.Resources.Model.RemoveAt(keyValuePair.Key);
            }
            return t.Model.ToString();
        }

        static void Main(string[] args)
        {
            var wc = new System.Net.WebClient();
            var tmpl =
                wc.DownloadString("https://raw.githubusercontent.com/windoze/azure-china-swarm/master/azuredeploy.json");
            var par =
                wc.DownloadString(
                    "https://raw.githubusercontent.com/windoze/azure-china-swarm/master/azuredeploy.parameters.json");

            var t = new ArmTemplate(JToken.Parse(tmpl));
//            t.Resources.Resources[5].SetDependency(1, "hahaha");
            var repl = new List<ARMResources>();
            repl.Add(new ARMResources {Index = 5, IsMMPUI = true, Type = "Microsoft.Network/virtualNetworks", Value = "Microsoft.Network/virtualNetworkName/someValue" });
            var ret=ReplaceExistingResources(repl, tmpl, par, "someResourceGroup", "someDeployment");
            Console.Out.WriteLine(ret);
            Console.In.ReadLine();
        }
    }
}