#if NET8_0_OR_GREATER

public class DocsTests
{
    [Fact]
    public void Foo()
    {
        var solutionDirectory = ProjectFiles.SolutionDirectory;
        var docsDirectory = Path.Combine(solutionDirectory, "../docs");
        docsDirectory = Path.GetFullPath(docsDirectory);
        var includeFile = Path.Combine(docsDirectory, "index.include.md");

        var builder = new StringBuilder();
        var level = 0;
        AddFiles(builder, docsDirectory, docsDirectory, "");
        foreach (var nestedDirectory in Directory.EnumerateDirectories(docsDirectory))
        {
            AddDirectory(ref level, builder, nestedDirectory, docsDirectory);
        }

        File.Delete(includeFile);
        File.WriteAllText(includeFile, builder.ToString());
    }

    static string GetUrl(string docsDirectory, string file)
    {
        var fullPath = Path.GetFullPath(file);
        var suffix = fullPath.Replace(docsDirectory, "")
            .Replace('\\', '/');
        return $"/docs{suffix}";
    }

    static void AddDirectory(ref int level, StringBuilder builder, string directory, string docsDirectory)
    {
        level++;

        var directoryIndent = new string(' ', level * 2);
        var url = GetUrl(docsDirectory, directory);
        builder.AppendLine($"{directoryIndent}* [{Path.GetFileName(directory)}]({url})");
        foreach (var nestedDirectory in Directory.EnumerateDirectories(directory))
        {
            AddDirectory(ref level, builder, nestedDirectory, docsDirectory);
        }

        AddFiles(builder, directory, docsDirectory, directoryIndent);

        level--;
    }

    static void AddFiles(StringBuilder builder, string directory, string docsDirectory, string directoryIndent)
    {
        var readme = Path.Combine(directory, "readme.md");
        if (File.Exists(readme))
        {
            AddFile(builder, docsDirectory, readme, directoryIndent);
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.md"))
        {
            if (file.EndsWith("readme.md"))
            {
                continue;
            }
            if (file.EndsWith("include.md"))
            {
                continue;
            }

            try
            {
                AddFile(builder, docsDirectory, file, directoryIndent);
            }
            catch (Exception exception)
            {
                throw new($"{exception.Message}. {file}");
            }
        }
    }

    static void AddFile(StringBuilder builder, string docsDirectory, string file, string directoryIndent)
    {
        var url = GetUrl(docsDirectory, file);
        var title = File.ReadLines(file)
            .First()[2..];
        builder.AppendLine($"{directoryIndent}  * [{title}]({url})");
    }
}

#endif