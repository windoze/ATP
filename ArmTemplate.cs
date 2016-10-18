using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.ARMTemplate.Common.Models
{
    public class ArmTemplate : BaseModel
    {
        public ArmTemplate(JToken template) : base(template)
        {
            ParametersSection = new ParametersSection(Model["parameters"], this);
            Variables = new VariablesSection(Model["variables"], this);
            Resources = new ResourcesSection(Model["resources"], this);
            OutputsSection = new OutputsSection(Model["outputs"], this);
        }

        public ParametersSection ParametersSection { get; }
        public VariablesSection Variables { get; }
        public ResourcesSection Resources { get; }
        public OutputsSection OutputsSection { get; }
        public JObject Model => (JObject)Node;
    }
}
