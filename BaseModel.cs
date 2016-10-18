using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.ARMTemplate.Common.Models
{
    public class BaseModel
    {
        protected BaseModel(JToken node)
        {
            Node = node;
        }

        protected JToken Node { get; }

        public override string ToString()
        {
            return Node.ToString();
        }
    }

    public class SubModel : BaseModel
    {
        protected SubModel(JToken node, ArmTemplate tmpl) : base(node)
        {
            Root = tmpl;
        }

        protected ArmTemplate Root { get; }
    }

    public class ObjectModel : SubModel
    {
        protected ObjectModel(JToken node, ArmTemplate tmpl) : base(node, tmpl)
        {
        }

        public JObject Model => (JObject)Node;
    }

    public class ArrayModel : SubModel
    {
        protected ArrayModel(JToken node, ArmTemplate tmpl) : base(node, tmpl)
        {
        }

        public JArray Model => (JArray)Node;
    }

    public class Parameters : ObjectModel
    {
        public Parameters(JToken node, ArmTemplate tmpl) : base(node, tmpl)
        {
        }
    }
}
