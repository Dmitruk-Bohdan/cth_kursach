namespace CTH.Database.Abstractions;

public interface ISqlQueryProvider
{
    string GetQuery(string relativePath);
}
