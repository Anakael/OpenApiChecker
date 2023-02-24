Parser.Default.ParseArguments<Options>(args)
    .MapResult(Run, _ => 1);

static int Run(Options opts)
{
    OpenApiDocument inputSpec;
    OpenApiDocument docSpec;

    try
    {
        inputSpec = SpecificationParser.Read(opts.Input);
        docSpec = SpecificationParser.Read(opts.Doc);
    }
    catch (BadSpecificationException)
    {
        return 1;
    }

    string[] ignore = string.IsNullOrEmpty(opts.NotImplemented)
        ? Enumerable.Empty<string>().ToArray()
        : File.ReadAllLines(opts.NotImplemented);

    CompareOptions compareOptions = new(ignore.Select(x => x.ToLower()).ToHashSet());
    SpecificationComparator comparator = new(compareOptions);
    (IEnumerable<string> warnings, IEnumerable<string> errors) = comparator.Compare(inputSpec, docSpec);

    if (warnings.Any())
    {
        Console.WriteLine("Warnings:");
        warnings.ToList().ForEach(Console.WriteLine);
    }

    if (errors.Any())
    {
        Console.WriteLine("Errors:");
        errors.ToList().ForEach(Console.WriteLine);

    }

    return errors.Any()
        ? 1
        : 0;
}

internal class Options
{
    [Option('s', "serv", Required = true, HelpText = "Input spec to be checked")]
    public string Input { get; set; } = string.Empty;

    [Option('d', "doc", Required = true, HelpText = "Doc spec to be checked against")]
    public string Doc { get; set; } = string.Empty;

    [Option('w', "wip", HelpText = "Not implementd API")]
    public string NotImplemented { get; set; } = string.Empty;
}
