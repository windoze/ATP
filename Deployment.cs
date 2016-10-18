using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Marketplace.ARMTemplate.Common.Models
{
    public class CopyObject
    {
        public CopyObject(string name, Expression count)
        {
            Name = name;
            Count = count;
        }

        public string Name { get; }
        public Expression Count { get; }
    }

    public class Deployment
    {
        private JObject _resourceGroup;
        private JObject _deployment;
        private string _subscription;
        private string _resourceGroupName;
        private string _deploymentName;
        private ArmTemplate _template;
        private JObject _parameters;
        private string _mode;
        public IList<CopyObject> CopyObjects;
        private Evaluator _evaluator;

        public Deployment(string subscription, string resourceGroup, string deploymentName, ArmTemplate tmpl,
            JObject parameters, string mode = "Complete")
        {
            _subscription = subscription;
            _resourceGroupName = resourceGroup;
            _deploymentName = deploymentName;
            _template = tmpl;
            _parameters = parameters;
            _mode = mode;
            // Build Deployment object
            var prop = new JObject
            {
                {"mode", _mode},
                {"provisioningState", ""}
            };
            if (_template != null) prop.Add("template", _template.Model);
            if (_parameters != null) prop.Add("parameters", _parameters);
            _deployment = new JObject
            {
                {"name", _deploymentName},
                {"properties", prop}
            };
            // Build ResourceGroup object
            prop = new JObject
            {
                {"provisioningState", ""}
            };
            _resourceGroup = new JObject
            {
                {"id", @"/subscriptions/{_subscription}/resourceGroups/{_resourceGroupName}"},
                {"name", _resourceGroupName},
                {"properties", prop}
            };
            _evaluator = new Evaluator(_resourceGroup, _deploymentName, _template, _parameters, _mode);
        }

        public Deployment(JObject resourceGroup, string deploymentName, ArmTemplate tmpl, JObject parameters,
            string mode = "Complete")
        {
            _deploymentName = deploymentName;
            _template = tmpl;
            _parameters = parameters;
            _mode = mode;
            // Build Deployment object
            var prop = new JObject
            {
                {"mode", _mode},
                {"provisioningState", ""}
            };
            // HACK:
            if (_template != null) prop.Add("template", _template.Model);
            if (_parameters != null) prop.Add("parameters", _parameters);
            _deployment = new JObject
            {
                {"name", _deploymentName},
                {"properties", prop}
            };
            // Build ResourceGroup object
            _resourceGroup = resourceGroup;
            _evaluator = new Evaluator(_resourceGroup, _deploymentName, _template, _parameters, _mode);
        }


        private string ExtractStorageAccount(Expression exp)
        {
            var re = new Regex(@"([a-z0-9-]+)\.blob\.core\.chinacloudapi\.cn");
            if (_evaluator.PartialEvaluate(exp))
            {
                // Fully evaluated
                string s = _evaluator.Evaluate(exp).Value<string>();
                var m = re.Match(s);
                if (m.Success)
                {
                    return m.Groups[0].ToString();
                }
            }
            else
            {
                // Partially evaluated
                foreach (var n in exp.PostOrderTraversal())
                {
                    if (n.Type == ExpressionTypes.String)
                    {
                        var m = re.Match(n.Content);
                        if (m.Success)
                        {
                            return m.Groups[0].ToString();
                        }
                    }
                }
            }
            return null;
        }

        private void ReplaceStorageAccount(Expression exp, string account)
        {
            if (exp == null)
            {
                return;
            }
            var re = new Regex(@"([a-z0-9-]+)?(\.blob\.core\.chinacloudapi\.cn)");
            if (_evaluator.PartialEvaluate(exp))
            {
                // Fully evaluated
                string s = _evaluator.Evaluate(exp).Value<string>();
                s = re.Replace(s, account);
                exp.SetContent(s);
            }
            else
            {
                // Partially evaluated, replace the 1st matching part
                foreach (var n in exp.PostOrderTraversal())
                {
                    if (n.Type == ExpressionTypes.String)
                    {
                        var s = re.Replace(n.Content, account);
                        n.SetContent(s);
                        return;
                    }
                }
            }
        }

        public string GetImageUri(Disk disk)
        {
            JToken token = _evaluator.Evaluate(disk.ImageUri);
            if (token != null)
            {
                return token.ToString();
            }
            return null;
        }

        public string GetVhdStorageAccount(Disk disk)
        {
            return ExtractStorageAccount(disk.VhdUri);
        }

        public void SetImageStorageAccount(Disk disk, string account)
        {
            ReplaceStorageAccount(disk.ImageUri, account);
        }

        public JToken Evaluate(string expr)
        {
            return Evaluate(new Expression(expr));
        }
        public JToken Evaluate(Expression expr)
        {
            return _evaluator.Evaluate(expr);
        }
    }
}
