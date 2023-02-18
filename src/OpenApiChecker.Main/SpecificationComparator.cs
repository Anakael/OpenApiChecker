namespace OpenApiChecker.Main;

using SpecOperations = IDictionary<OperationType, OpenApiOperation>;
using SpecProperties = IDictionary<string, OpenApiSchema>;
using SpecContents = IDictionary<string, OpenApiMediaType>;

public class SpecificationComparator
{
    private const string JsonApplication = "application/json";

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
            string typeDisplay = type.GetDisplayName().ToUpper();
            Console.WriteLine($"Checking {typeDisplay} {path}");
            if (!inputOperations.TryGetValue(type, out OpenApiOperation? inputOperation))
            {
                List<string> dest = options.NotImplemented.Contains($"{typeDisplay.ToLower()} {path}")
                    ? ref warnings
                    : ref errors;
                dest.Add($"Missing {path} operation: {typeDisplay}");

                continue;
            }

            string operationPath = $"{typeDisplay} {path}";

            CompareParameters(operationPath, inputOperation.Parameters, docOperation.Parameters);
            if (docOperation.RequestBody is not null)
            {
                if (inputOperation.RequestBody is null)
                {
                    errors.Add($"Missing {operationPath}: requestBody");
                    continue;
                }

                CompareRequestBodies(operationPath, inputOperation.RequestBody, docOperation.RequestBody);
            }

            if (docOperation.Responses is not null)
            {
                if (inputOperation.Responses is null)
                {
                    errors.Add($"Missing {operationPath}: responses");
                    continue;
                }

                CompareResponses($"{operationPath} responses", inputOperation.Responses, docOperation.Responses);
            }
        }
    }

    private void CompareRequestBodies(string path, OpenApiRequestBody inputBody, OpenApiRequestBody docBody)
    {
        Console.WriteLine($"Checking {path} requestBody");
        CompareContents($"{path} body", inputBody.Content, docBody.Content);
    }

    private void CompareContents(string path, SpecContents inputContents, SpecContents docContents)
    {
        if (!docContents.TryGetValue(JsonApplication, out OpenApiMediaType? docMediaType))
        {
            return;
        }

        if (!inputContents.TryGetValue(JsonApplication, out OpenApiMediaType? inputMediaType))
        {
            errors.Add($"Missing {JsonApplication} mediatype for {path}");
            return;
        }

        OpenApiSchema inputSchema = inputMediaType.Schema;
        OpenApiSchema docSchema = docMediaType.Schema;
        CompareSchemas(path, inputSchema, docSchema);

    }

    private void CompareSchemas(string path, OpenApiSchema inputSchema, OpenApiSchema docSchema)
    {
        CompareTypes(path, inputSchema.Type, docSchema.Type);
        switch (docSchema.Type)
        {
            case "array":
                CompareSchemas($"{path} array", inputSchema.Items, docSchema.Items);
                break;
            case "object":
                CompareProperties($"{path} object", inputSchema.Properties, docSchema.Properties);
                break;
        };
    }

    private void CompareTypes(string path, string inputType, string docType)
    {
        if (inputType == docType)
        {
            return;
        }

        errors.Add($"{path}: has invalid {inputType} type. Expected: {docType}");
    }

    private void CompareProperties(string path, SpecProperties inputProperties, SpecProperties docProperties)
    {
        foreach ((string name, OpenApiSchema docSchema) in docProperties)
        {
            if (!inputProperties.TryGetValue(name, out OpenApiSchema? inputSchema))
            {
                errors.Add($"Missing {path} property: {name}");

                continue;
            }

            CompareSchemas(path, inputSchema, docSchema);
        }
    }

    private void CompareResponses(string path, OpenApiResponses inputResponses, OpenApiResponses docResponses)
    {
        Console.WriteLine($"Checking {path} responses");
        foreach ((string code, OpenApiResponse docResponse) in docResponses)
        {
            if (!code.StartsWith("2"))
            {
                continue;
            }

            if (!inputResponses.TryGetValue(code, out OpenApiResponse? inputResponse))
            {
                errors.Add($"Missing {path} response: {code}");

                continue;
            }

            Console.WriteLine($"Checking {path} {code}");
            CompareContents($"{path} {code}", inputResponse.Content, docResponse.Content);
        }
    }
}
