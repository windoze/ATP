using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.ARMTemplate.Common.Models
{
    public class Evaluator
    {
        public Evaluator(string subscription, string resourceGroup, string deploymentName, ArmTemplate tmpl,
            JObject parameters, string mode = "Complete")
        {
            _subscription = new JObject
            {
                { "subscriptionId", subscription }
            };
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
                {"id", $@"/subscriptions/{subscription}/resourceGroups/{_resourceGroupName}"},
                {"name", _resourceGroupName},
                {"properties", prop}
            };
        }

        public Evaluator(JObject resourceGroup, string deploymentName, ArmTemplate tmpl, JObject parameters,
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
        }

        private JObject _resourceGroup;
        private JObject _deployment;
        private JObject _subscription;
        private string _resourceGroupName;
        private string _deploymentName;
        private ArmTemplate _template;
        private JObject _parameters;
        private string _mode;

        public bool PartialEvaluate(Expression exp)
        {
            switch (exp.Type)
            {
                case ExpressionTypes.Number:
                case ExpressionTypes.String:
                    return true;
                case ExpressionTypes.Function:
                    bool evaluated = true;
                    foreach (var c in exp.Children)
                    {
                        evaluated = evaluated && PartialEvaluate(c);
                    }
                    foreach (var i in exp.Subindices)
                    {
                        if (i.Type == IndexType.Index)
                        {
                            bool ei = PartialEvaluate(i.Index);
                            if (ei)
                            {
                                i.Index.Content = Evaluate(i.Index).Value<long>().ToString();
                            }
                            evaluated = evaluated && ei;
                        }
                    }
                    if (Functions.ContainsKey(exp.Content))
                    {
                        if (evaluated)
                        {
                            var ret = Functions[exp.Content](from c in exp.Children select Evaluate(c), this);
                            foreach (var p in exp.Subindices)
                            {
                                ret = (p.Type == IndexType.Key)
                                    ? ret[p.Key]
                                    : ret[Evaluate(p.Index).Value<int>()];
                            }
                            switch (ret.Type)
                            {
                                case JTokenType.Integer:
                                    exp.Content = ret.Value<long>().ToString();
                                    break;
                                case JTokenType.String:
                                    exp.Content = '"' + ret.Value<string>() + '"';
                                    break;
                                default:
                                    return false;
                            }
                        }
                    }
                    else
                    {
                        evaluated = false;
                    }
                    return evaluated;
            }
            return false;
        }

        public JToken Evaluate(Expression exp)
        {
            switch (exp.Type)
            {
                case ExpressionTypes.Number:
                    return Int64.Parse(exp.Content);
                case ExpressionTypes.String:
                    return exp.Content;
                case ExpressionTypes.Function:
                    if (Functions.ContainsKey(exp.Content))
                    {
                        var ret = Functions[exp.Content](from c in exp.Children select Evaluate(c), this);
                        foreach (var p in exp.Subindices)
                        {
                            ret = (p.Type == IndexType.Key)
                                ? ret[p.Key]
                                : ret[Evaluate(p.Index).Value<int>()];
                        }
                        return ret;
                    }
                    else
                    {
                        throw new ExpressionException($"Function {exp.Content} not found");
                    }
            }
            return null;
        }

        private delegate JToken ArmFunction(IEnumerable<JToken> oprands, Evaluator eval);

        static JToken Add(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 2)
            {
                throw new ExpressionException("add function takes 2 oprands");
            }
            return os[0].Value<long>() + os[1].Value<long>();
        }

        static JToken Sub(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 2)
            {
                throw new ExpressionException("sub function takes 2 oprands");
            }
            return os[0].Value<long>() - os[1].Value<long>();
        }

        static JToken Mul(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 2)
            {
                throw new ExpressionException("mul function takes 2 oprands");
            }
            return os[0].Value<long>() * os[1].Value<long>();
        }

        static JToken Div(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 2)
            {
                throw new ExpressionException("div function takes 2 oprands");
            }
            return os[0].Value<long>() / os[1].Value<long>();
        }

        static JToken Mod(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 2)
            {
                throw new ExpressionException("mod function takes 2 oprands");
            }
            return os[0].Value<long>() % os[1].Value<long>();
        }

        static JToken Int(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 1)
            {
                throw new ExpressionException("int function takes 1 oprand");
            }
            return os[0].Value<long>();
        }

        static JToken Base64(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 1)
            {
                throw new ExpressionException("base64 function takes 1 oprand");
            }
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(os[0].Value<string>());
            return System.Convert.ToBase64String(plainTextBytes);
        }

        static JToken Concat(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count == 0)
            {
                throw new ExpressionException("base64 function takes at least 1 oprand");
            }
            if (os[0].Type == JTokenType.String)
            {
                var sb = new StringBuilder();
                foreach (var o in os)
                {
                    sb.Append(o);
                }
                return sb.ToString();
            }
            else
            {
                var ret = new JArray();
                foreach (var o in os)
                {
                    ret.Concat(o.ToArray());
                }
                return ret;
            }
        }

        static JToken Length(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 1)
            {
                throw new ExpressionException("length function takes 1 oprand");
            }

            return os[0].Count();
        }

        static JToken PadLeft(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count < 2 || os.Count > 3)
            {
                throw new ExpressionException("padLeft function takes 2 or 3 oprands");
            }
            var valueToPad = os[0].Value<string>();
            var totalLength = os[1].Value<int>();
            var paddingCharacter = " ";
            if (os.Count == 3)
            {
                paddingCharacter = os[2].Value<string>();
            }
            var pc = paddingCharacter[0];
            return valueToPad.PadLeft(totalLength, pc);
        }

        static JToken Replace(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 3)
            {
                throw new ExpressionException("replace function takes 3 oprands");
            }
            var originalString = os[0].Value<string>();
            var oldCharacter = os[1].Value<string>()[0];
            var newCharacter = os[2].Value<string>()[0];
            return originalString.Replace(oldCharacter, newCharacter);
        }

        static JToken Skip(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 2)
            {
                throw new ExpressionException("skip function takes 2 oprands");
            }
            var numberToSkip = os[1].Value<int>();
            if (os[0].Type == JTokenType.Array)
            {
                var originalValue = os[0].ToArray();
                return new JArray(originalValue.Skip(numberToSkip).ToArray());
            }
            else
            {
                var originalValue = os[0].Value<string>();
                return originalValue.Skip(numberToSkip).ToString();
            }
        }

        static JToken Split(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 2)
            {
                throw new ExpressionException("split function takes 2 oprands");
            }
            var inputString = os[0].Value<string>();
            if (os[1].Type == JTokenType.Array)
            {
                var delimiter = (from s in os[1].ToArray() select s.Value<string>()[0]).ToArray();
                return new JArray(inputString.Split(delimiter));
            }
            else
            {
                var delimiter = os[1].Value<string>();
                return new JArray(inputString.Split(delimiter.Substring(0, 1).ToArray()));
            }
        }

        static JToken String(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 1)
            {
                throw new ExpressionException("string function takes 1 oprand");
            }

            return os[0].Value<string>();
        }

        static JToken Substring(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count < 1 || os.Count > 3)
            {
                throw new ExpressionException("substring function takes 1 to 3 oprands");
            }
            var stringToParse = os[0].Value<string>();
            var startIndex = 0;
            if (os.Count >= 2)
                startIndex = os[1].Value<int>();
            var length = stringToParse.Length;
            if (os.Count == 3)
                length = os[2].Value<int>();
            return stringToParse.Substring(startIndex, length);
        }

        static JToken Take(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 2)
            {
                throw new ExpressionException("take function takes 2 oprands");
            }
            var numberToTake = os[1].Value<int>();
            return new JArray(os[0].Take(numberToTake).ToArray());
        }

        static JToken ToLower(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 1)
            {
                throw new ExpressionException("toLower function takes 1 oprand");
            }

            return os[0].Value<string>().ToLower();
        }

        static JToken ToUpper(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 1)
            {
                throw new ExpressionException("toUpper function takes 1 oprand");
            }

            return os[0].Value<string>().ToUpper();
        }

        static JToken Trim(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 1)
            {
                throw new ExpressionException("trim function takes 1 oprand");
            }

            return os[0].Value<string>().Trim();
        }

        static private string RandomString(int Size, Random random)
        {
            string input = "abcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < Size; i++)
            {
                ch = input[random.Next(0, input.Length)];
                builder.Append(ch);
            }
            return builder.ToString();
        }

        static JToken UniqueString(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count < 1)
            {
                throw new ExpressionException("uniqueString function takes at least 1 oprand");
            }
            int hash = os[0].Value<string>().GetHashCode();
            foreach (var o in os)
            {
                // HACK: Too lame...
                hash ^= o.GetHashCode();
            }
            var rnd = new Random(hash);
            return RandomString(36, rnd);
        }

        static JToken Uri(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 2)
            {
                throw new ExpressionException("uri function takes 2 oprands");
            }
            var baseUri = os[0].Value<string>();
            var relativeUri = os[1].Value<string>();
            var uri = new Uri(baseUri);
            return new Uri(uri, relativeUri).ToString();
        }

        static JToken Deployment(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 0)
            {
                throw new ExpressionException("deployment function takes 0 oprand");
            }
            return eval._deployment;
        }

        static JToken Parameters(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 1)
            {
                throw new ExpressionException("parameters function takes 1 oprand");
            }
            JObject p = eval._parameters as JObject;
            if (p == null) return null;
            if (p[os[0].ToString()] == null)
            {
                // Use default
                return eval._template.ParametersSection.Model[os[0].ToString()]["defaultValue"];
            }
            else
            {
                // Use parameters
                return p[os[0].ToString()]["value"];
            }
        }

        static JToken Variables(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 1)
            {
                throw new ExpressionException("variables function takes 1 oprand");
            }
            var vName = os[0].ToString();
            var ret = eval._template.Variables.Model[vName];
            if (ret.Type == JTokenType.String)
            {
                // If failed, need to throw exception
                var v = eval.Evaluate(new Expression(ret.ToString()));
                eval._template.Variables.Model[vName].Replace(v);
                return eval.Evaluate(new Expression(ret.ToString()));
            }
            return ret;
        }

        static JToken ResourceGroup(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 0)
            {
                throw new ExpressionException("resourceGroup function takes 0 oprand");
            }
            return eval._resourceGroup;
        }

        static JToken Subscription(IEnumerable<JToken> oprands, Evaluator eval)
        {
            var os = oprands.ToList();
            if (os.Count != 0)
            {
                throw new ExpressionException("subscription function takes 0 oprand");
            }
            return eval._subscription;
        }

        public static IEnumerable<Expression> PartialConcat(IEnumerable<Expression> oprands, Evaluator eval)
        {
            IList<Expression> evaluatedList = new List<Expression>();
            string current = "";
            bool ret = true;
            foreach (var o in oprands)
            {
                if (eval.PartialEvaluate(o))
                {
                    current = current + eval.Evaluate(o).Value<string>();
                }
                else
                {
                    if (current.Length > 0)
                    {
                        evaluatedList.Add(new Expression('"' + current + '"'));
                        current = "";
                    }
                    evaluatedList.Add(o);
                    ret = false;
                }
            }
            return evaluatedList;
        }

        private static readonly Dictionary<string, ArmFunction> Functions = new Dictionary<string, ArmFunction>()
        {
            {"add", Add},
            {"sub", Sub},
            {"mul", Mul},
            {"div", Div},
            {"mod", Mod},
            {"int", Int},
            {"base64", Base64},
            {"concat", Concat},
            {"length", Length},
            {"padLeft", PadLeft},
            {"replace", Replace},
            {"skip", Skip},
            {"split", Split},
            {"string", String},
            {"substring", Substring},
            {"take", Take},
            {"toUpper", ToUpper},
            {"toLower", ToLower},
            {"trim", Trim},
            {"uniqueString", UniqueString},
            {"deployment", Deployment},
            {"parameters", Parameters},
            {"variables", Variables},
            {"resourceGroup", ResourceGroup},
            {"subscription", Subscription}
        };
    }
}
