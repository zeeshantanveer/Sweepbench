using Sweepbench.Core.Cleaners;

namespace Sweepbench.Core.Tests.Cleaners;

public class RegistryPathHeuristicsTests
{
    [Theory]
    [InlineData(@"C:\Program Files\App\app.exe", true)]
    [InlineData(@"\\server\share\app.exe", true)]
    [InlineData("app.exe", false)]
    [InlineData(@"C:\Program Files\App\app.dll", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    public void LooksLikeExecutablePath_ClassifiesCorrectly(string? path, bool expected)
    {
        Assert.Equal(expected, RegistryPathHeuristics.LooksLikeExecutablePath(path));
    }

    [Theory]
    [InlineData(@"""C:\Program Files\App\uninst.exe"" /S", @"C:\Program Files\App\uninst.exe")]
    [InlineData(@"C:\Tools\uninst.exe /S", @"C:\Tools\uninst.exe")]
    [InlineData(@"C:\Tools\uninst.exe", @"C:\Tools\uninst.exe")]
    public void ExtractLeadingExecutablePath_HandlesQuotedAndUnquotedPaths(string commandLine, string expected)
    {
        Assert.Equal(expected, RegistryPathHeuristics.ExtractLeadingExecutablePath(commandLine));
    }

    [Theory]
    [InlineData("MsiExec.exe /X{12345678-1234-1234-1234-123456789012}")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"""C:\unterminated quote /S")]
    // Unquoted path containing spaces is inherently ambiguous — where the path ends
    // and arguments begin can't be determined without quotes, so this must return
    // null (refuse to guess) rather than truncate at the first space.
    [InlineData(@"C:\Program Files\App\uninst.exe /S")]
    public void ExtractLeadingExecutablePath_ReturnsNullWhenUnverifiable(string? commandLine)
    {
        Assert.Null(RegistryPathHeuristics.ExtractLeadingExecutablePath(commandLine));
    }
}
