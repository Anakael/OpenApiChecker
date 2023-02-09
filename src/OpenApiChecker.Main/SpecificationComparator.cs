namespace OpenApiChecker.Main;

using SpecOperations = IDictionary<OperationType, OpenApiOperation>;

public class SpecificationComparator
{
    private readonly List<string> warnings = new();
    private readonly List<string> errors = new();

    private readonly CompareOptions options;

    private OpenApiPaths inputPaths = null!;
    private OpenApiPaths docPaths = null!;


    public SpecificationComparator(CompareOptions options)
    {
        this.options = options;
    }

    public CompareResult Compare(OpenApiDocument input, OpenApiDocument doc)
    {
        (inputPaths, docPaths) = (input.Paths, doc.Paths);

        if (!docPaths.Any())
        {
            warnings.Add("Doc paths is empty");
            return new CompareResult(warnings, errors);
        }

        ComparePaths(inputPaths, docPaths);

        CompareResult result = new(warnings.ToArray(), errors.ToArray());
        warnings.Clear();
        errors.Clear();
        return result;
    }

    private void ComparePaths(OpenApiPaths inputPaths, OpenApiPaths docPaths)
    {
        IEnumerable<string> inputPathsKeys = inputPaths.Keys.Select(x => x.ToLower());

        foreach ((string path, OpenApiPathItem pathItem) in docPaths)
        {
            Console.WriteLine($"Checking {path}");
            if (!inputPathsKeys.Contains(path.ToLower()))
            {
                List<string> dest = options.NotImplemented.Contains(path)
                    ? ref warnings
                    : ref errors;
                dest.Add($"Missing path {path} in input spec");

                continue;
            }

            CompareParameters(path, inputPaths[path].Parameters, pathItem.Parameters);
            CompareOperations(path, inputPaths[path].Operations, pathItem.Operations);
        }
    }

    private void CompareParameters(string path, IEnumerable<OpenApiParameter> inputParameters, IEnumerable<OpenApiParameter> docParameters)
    {
        foreach (OpenApiParameter docParam in docParameters)
        {
            string docParamNameLower = docParam.Name.ToLower();

            OpenApiParameter? inputParam = inputParameters.FirstOrDefault(x => x.Name.ToLower() == docParamNameLower);
            if (inputParam is null)
            {
                errors.Add($"Missing {path} param: {docParam.Name}");
                continue;
            }
        }
    }

    private void CompareOperations(string path, SpecOperations inputOperations, SpecOperations docOperations)
    {
        foreach ((OperationType type, OpenApiOperation docOperation) in docOperations)
        {
            Console.WriteLine($"Checking {path}");
            if (!inputOperations.ContainsKey(type))
            {
                string typeDisplay = type.GetDisplayName();
                List<string> dest = options.NotImplemented.Contains($"{typeDisplay.ToLower()} {path}")
                    ? ref warnings
                    : ref errors;
                dest.Add($"Missing {path} operation: {typeDisplay.ToUpper()}");

                continue;
            }
        }
    }
}
