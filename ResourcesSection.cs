using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.ARMTemplate.Common.Models
{
    public class ParametersSection : ObjectModel
    {
        public ParametersSection(JToken node, ArmTemplate tmpl) : base(node, tmpl)
        {
        }
    }

    public class VariablesSection : ObjectModel
    {
        public VariablesSection(JToken node, ArmTemplate tmpl) : base(node, tmpl)
        {
        }
    }

    public class ResourcesSection : ArrayModel
    {
        public ResourcesSection(JToken node, ArmTemplate tmpl) : base(node, tmpl)
        {
            VirtualMachines = (from c in Model
                               where c["type"].ToString() == "Microsoft.Compute/virtualMachines"
                               select new VirtualMachine(c, Root)).ToList();
            VirtualNets = (from c in Model
                           where c["type"].ToString() == "Microsoft.Network/virtualNetworks"
                           select new VirtualNet(c, Root)).ToList();
        }

        public IList<VirtualMachine> VirtualMachines { get; }
        public IList<VirtualNet> VirtualNets { get; }
    }

    public class OutputsSection : ObjectModel
    {
        public OutputsSection(JToken node, ArmTemplate tmpl) : base(node, tmpl)
        {
        }
    }
}
