namespace OpenApiChecker.Main.Models;

public record CompareResult(
        IEnumerable<string> Warnings,
        IEnumerable<string> Errors);
