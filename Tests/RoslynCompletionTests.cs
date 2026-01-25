using Xunit;
using Code2Viz.Editor;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Code2Viz.Tests;

public class RoslynCompletionTests
{
    [Fact]
    public async Task GetCompletions_ShouldReturnLocalVariables()
    {
        var code = @"
using System;
class Test {
    void Method() {
        int myVar = 10;
        my//CURSOR
    }
}";
        var position = code.IndexOf("//CURSOR");
        var service = new RoslynCompletionService();

        var completions = await service.GetCompletionsAsync(code, position);

        Assert.Contains(completions, c => c.Text == "myVar");
    }

    [Fact]
    public async Task GetCompletions_ShouldReturnMembers()
    {
        var code = @"
using System;
class Test {
    void Method() {
        string s = ""hello"";
        s.//CURSOR
    }
}";
        var position = code.IndexOf("//CURSOR");
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };
        var service = new RoslynCompletionService(references);

        var completions = await service.GetCompletionsAsync(code, position);

        Assert.Contains(completions, c => c.Text == "Length");
        Assert.Contains(completions, c => c.Text == "Substring");
    }
}
