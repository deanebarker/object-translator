namespace DeaneBarker.ObjectTranslator;

// This parses the spec
// I moved this to its own class, because the code ain't that great...

public static class ObjectTranslator
{
    public static string Serialize(string config, object model)
    {
        var vg = Parse(config);
        var obj = vg.ToObject(model);
        return System.Text.Json.JsonSerializer.Serialize(obj);
    }

    public static object Translate(string config, object model)
    {
        var vg = Parse(config);
        return vg.ToObject(model);
    }

    // This forms a nested set of ValueGenerators from a string
    public static ValueGenerator Parse(string config)
    {
        var lines = config
            .Split(new string[] { "\n", "\r\n", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        var rootGenerator = new ValueGenerator("root", null);
        var currentGenerator = rootGenerator;

        for (var i = 0; i < lines.Count(); i++)
        {
            var line = lines.ToList()[i];

            var indentThis = getIndent(line);
            var indentBefore = getIndent(lines.ToList().ElementAtOrDefault(i - 1));
            var indentAfter = getIndent(lines.ToList().ElementAtOrDefault(i + 1));

            if (indentThis < indentBefore)
            {
                currentGenerator = currentGenerator.Parent;
            }

            var vg = new ValueGenerator(line, currentGenerator);
            currentGenerator.Generators.Add(vg);

            if (indentAfter > indentThis)
            {
                currentGenerator = vg;
            }
        }

        return rootGenerator;

        int getIndent(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return 0;
            }
            return line.Length - line.TrimStart().Length;
        }
    }

}