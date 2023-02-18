namespace OpenApiChecker.Main;

public static class SpecificationParser
{
    public static OpenApiDocument Read(string path)
    {
        Console.WriteLine($"Reading {path}");
        OpenApiReaderSettings settings = new()
        {
            ReferenceResolution = ReferenceResolutionSetting.ResolveAllReferences,

        };
        using FileStream stream = File.OpenRead(path);
        OpenApiDocument? document = new OpenApiStreamReader().Read(stream, out OpenApiDiagnostic? diagnostic);
        if (diagnostic?.Errors?.Any() == true)
        {
            IEnumerable<OpenApiError> errors = diagnostic.Errors;
            string joinedErrors = string.Join("\n", errors.Select(x => $"Error at {x.Pointer}: {x.Message}"));
            Console.WriteLine(joinedErrors);
            throw new BadSpecificationException();
        }

        if (diagnostic?.Warnings?.Any() == true)
        {
            IEnumerable<OpenApiError> warnings = diagnostic.Warnings;
            string joinedWarnings = string.Join("\n", warnings.Select(x => $"Warning at {x.Pointer}: {x.Message}"));
            Console.WriteLine(joinedWarnings);
        }

        if (document is null)
        {
            Console.WriteLine("Document can not be parsed");
            throw new BadSpecificationException();
        }

        return document;
    }


}
