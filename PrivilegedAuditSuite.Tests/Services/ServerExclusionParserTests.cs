using PrivilegedAuditSuite.Application.Services;

namespace PrivilegedAuditSuite.Tests.Services;

public sealed class ServerExclusionParserTests
{
    private readonly ServerExclusionParser _parser = new();

    [Fact]
    public void ParseManualEntries_RemovesBlanksAndDuplicates()
    {
        var result = _parser.ParseManualEntries("""
            sql01.contoso.local

            web01.contoso.local
            SQL01.contoso.local
            """);

        Assert.Equal(["sql01.contoso.local", "web01.contoso.local"], result);
    }

    [Fact]
    public void ParseImportedContent_ForTxt_UsesOneServerPerLine()
    {
        var result = _parser.ParseImportedContent(
            "excluded.txt",
            """
            sql01.contoso.local
            web01.contoso.local
            """);

        Assert.Equal(["sql01.contoso.local", "web01.contoso.local"], result);
    }

    [Fact]
    public void ParseImportedContent_ForCsv_UsesFirstNonEmptyColumn()
    {
        var result = _parser.ParseImportedContent(
            "excluded.csv",
            """
            Server,Comment,Extra
            sql01.contoso.local,DB,
            ,web01.contoso.local,comment
            WEB01.contoso.local,duplicate,
            """);

        Assert.Equal(["sql01.contoso.local", "web01.contoso.local"], result);
    }

    [Fact]
    public void Merge_DeduplicatesAcrossSources()
    {
        var result = _parser.Merge(
            ["sql01.contoso.local", "web01.contoso.local"],
            ["SQL01.contoso.local", "jump01.contoso.local"]);

        Assert.Equal(["jump01.contoso.local", "sql01.contoso.local", "web01.contoso.local"], result);
    }
}
