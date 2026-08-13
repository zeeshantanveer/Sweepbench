using Sweepbench.Core.Cleaners;
using Sweepbench.Core.Tests.TestSupport;

namespace Sweepbench.Core.Tests.Cleaners;

public class DirectorySizeTests
{
    [Fact]
    public void Calculate_SumsFilesRecursively()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("a.txt", 100);
        dir.WriteFile(System.IO.Path.Combine("nested", "b.txt"), 250);

        var size = DirectorySize.Calculate(dir.Path);

        Assert.Equal(350, size);
    }

    [Fact]
    public void Calculate_NonexistentPath_ReturnsZero()
    {
        var size = DirectorySize.Calculate(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sweepbench-does-not-exist-" + Guid.NewGuid()));

        Assert.Equal(0, size);
    }

    [Fact]
    public void Calculate_EmptyDirectory_ReturnsZero()
    {
        using var dir = new TempDirectory();

        var size = DirectorySize.Calculate(dir.Path);

        Assert.Equal(0, size);
    }
}
