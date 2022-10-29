using Fluid;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

namespace DeaneBarker.ObjectTranslator;

// This is a single value from the spec
public class ValueGenerator
{
    public string Label { get; set; }

    [AllowNull]
    public string Token { get; set; }

    [AllowNull]
    public string Template { get; set; }

    public List<ValueGenerator> Generators { get; set; } = new List<ValueGenerator>();
    public ValueGenerator Parent { get; set; }

    public static Func<object, string, string, object> ValueExtractor = GetValue;

    public ValueGenerator(string spec, ValueGenerator parent)
    {
        Parent = parent;

        spec = spec.Trim();

        if (!spec.Contains(':'))
        {
            // this is simple
            // We are just COPYING with no changes
            Label = spec;
            Token = spec;
        }
        else
        {
            // The label comes before the colon
            Label = spec.Split(":".ToCharArray()).First();

            var afterColon = spec.Substring(spec.IndexOf(":") + 1).Trim();

            if (afterColon.All(c => char.IsLetterOrDigit(c) || c == '.'))
            {
                // There's nothing but letters, digits, and dots; no whitespace
                // We are COPYING
                Token = afterColon;
            }
            else
            {
                // This is a Liquid expression
                // We are CALCULATING
                Template = afterColon;
            }
        }
    }

    public static object GetValue(object model, string token, string template)
    {
        if (token == "_")
        {
            return model;
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            // This is a simple, resolvable value

            var currentValue = model;
            foreach (var segment in token.Split("."))
            {
                if (currentValue == null)
                {
                    return null;
                }

                var property = currentValue.GetType().GetProperties().FirstOrDefault(p => p.Name.ToLower() == segment.ToLower());
                if (property == null)
                {
                    return null;
                }

                currentValue = property.GetValue(currentValue);

            }
            return currentValue;
        }

        // This is a Liquid template expression

        var context = new TemplateContext(model);
        context.SetValue("_", model);

        TemplateOptions.Default.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
        var templateString = "{{ " + template + " }}";
        var parser = new FluidParser();
        var parsedTemplate = parser.Parse(templateString);

        return parsedTemplate.Render(context);
    }

    public ExpandoObject ToObject(object model)
    {
        var obj = new ExpandoObject();

        // IMPORTANT: We are generator-driven
        // We spin the generators, then ask each one to extract a value from the model
        foreach (var generator in Generators)
        {
            // This extracts a value from the model based on the template of the generator
            var value = ValueExtractor(model, generator.Token, generator.Template);

            // If it's a list of stuff, we have to handle every element individually
            if (value is ICollection)
            {
                var list = new List<object>();
                foreach (var childValue in (IEnumerable)value)
                {
                    if(childValue == null)
                    {
                        // This seems super dumb, but it might be desired in some output?
                        // I don't know...
                        list.Add(null);
                        continue;
                    }

                    // We have no sub-generators, so just turn it into a string, I guess?
                    // I mean, what else are we going to do?
                    // In some cases, this is because it's a simple thing, like a string
                    if (generator.Generators.Count == 0)
                    {
                        list.Add(childValue.ToString());
                        continue;
                    }

                    // This is an object and we have sub-generators, so recurse into it
                    list.Add(generator.ToObject(childValue));
                }

                value = list;
            }

            // Finally, we have the value!
            // We add this to the expando, and it will turn into a normal-looking property when it serializes
            ((IDictionary<string, object>)obj).Add(generator.Label, value);
        }
        return obj;
    }
}
