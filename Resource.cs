using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Marketplace.ARMTemplate.Common.Models;

namespace Marketplace.ARMTemplate.Common.Models
{
    public class Disk : ObjectModel
    {
        public Disk(JToken node, ArmTemplate tmpl) : base(node, tmpl)
        {
        }

        public Expression ImageUri => IsCustomImage ? new Expression(Model["image"]["uri"].ToString()) : null;

        public void SetImageUri(string s)
        {
            Model["image"]["uri"].Replace(s);
        }

        public Expression VhdUri => new Expression(Model["vhd"]["uri"].ToString());

        public bool IsCustomImage => Model["image"] != null;
    }

    public class Resource : ObjectModel
    {
        protected Resource(JToken node, ArmTemplate tmpl) : base(node, tmpl)
        {
            DependsOn = (from c in Model["dependsOn"]
                select new Expression(c.ToString())).ToList();
        }

        public bool IsCluster => Model["copy"] != null;

        public string CopyName => Model["copy"]["name"].ToString();
        public Expression Count => new Expression(IsCluster ? Model["copy"]["count"].ToString() : "1");

        public IList<Expression> DependsOn{get;}
    }

    public class VirtualMachine : Resource
    {
        public VirtualMachine(JToken node, ArmTemplate tmpl) : base(node, tmpl)
        {
            if (Model["properties"] != null && Model["properties"]["storageProfile"] != null)
            {
                if (Model["properties"]["storageProfile"]["osDisk"] != null)
                {
                    OsDisk = new Disk(Model["properties"]["storageProfile"]["osDisk"], Root);
                }
                else
                {
                    // Shouldn't happen
                    OsDisk = null;
                }
                if (Model["properties"]["storageProfile"]["dataDisks"] != null)
                {
                    DataDisks = (from d in Model["properties"]["storageProfile"]["dataDisks"]
                                 select new Disk(d, Root)).ToList();
                }
            }
            if (DataDisks == null)
            {
                // Empty list
                DataDisks = new LinkedList<Disk>();
            }
        }

        public Disk OsDisk { get; }
        public IEnumerable<Disk> DataDisks { get; }
    }

    public class VirtualNet : Resource
    {
        public VirtualNet(JToken node, ArmTemplate tmpl) : base(node, tmpl)
        {
        }
    }
}
