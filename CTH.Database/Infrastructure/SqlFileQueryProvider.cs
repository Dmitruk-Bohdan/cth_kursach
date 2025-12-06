using System.Collections.Concurrent;
using CTH.Database.Abstractions;
using Microsoft.Extensions.Logging;

namespace CTH.Database.Infrastructure;

public class SqlFileQueryProvider : ISqlQueryProvider
{
    private readonly ILogger<SqlFileQueryProvider> _logger;
    private readonly ConcurrentDictionary<string, string> _queryCache = new();
    private readonly string _useCasesDirectory;

    public SqlFileQueryProvider(ILogger<SqlFileQueryProvider> logger)
    {
        _logger = logger;
        _useCasesDirectory = Path.Combine(AppContext.BaseDirectory, "UseCases");
        Directory.CreateDirectory(_useCasesDirectory);
    }

    public string GetQuery(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Path to SQL query cannot be empty.", nameof(relativePath));
        }

        return _queryCache.GetOrAdd(relativePath, LoadQueryFromFile);
    }

    private string LoadQueryFromFile(string relativePath)
    {
        var sanitizedPath = relativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        var finalFileName = sanitizedPath.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            ? sanitizedPath
            : $"{sanitizedPath}.sql";

        var absolutePath = Path.Combine(_useCasesDirectory, finalFileName);

        if (!File.Exists(absolutePath))
        {
            _logger.LogError("SQL query file not found at path {AbsolutePath}", absolutePath);
            throw new FileNotFoundException($"SQL query file was not found at path: {absolutePath}", absolutePath);
        }

        return File.ReadAllText(absolutePath);
    }
}
