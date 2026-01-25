using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Code2Viz.Execution;

namespace Code2Viz.Editor;

public class RoslynCompletionService
{
    private readonly IEnumerable<MetadataReference> _references;

    public RoslynCompletionService(IEnumerable<MetadataReference>? references = null)
    {
        _references = references ?? new ModuleCompiler().GetReferences();
    }

    public async Task<List<ICompletionData>> GetCompletionsAsync(string code, int position)
    {
        var completions = new List<ICompletionData>();

        try
        {
            // 1. Create Compilation
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create(
                "CompletionAnalysis",
                new[] { syntaxTree },
                _references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // 2. Determine Context
            var root = await syntaxTree.GetRootAsync();
            var token = root.FindToken(position);
            
            // Debugging context
            // System.Diagnostics.Debug.WriteLine($"Token: {token.Kind()}, Parent: {token.Parent?.Kind()}");

            // 3. Lookup Symbols
            ImmutableArray<ISymbol> symbols;
            
            // Handle Member Access (dot)
            var tokenLeft = position > 0 ? root.FindToken(position - 1) : default; // Get token BEFORE cursor
            if (tokenLeft != default && tokenLeft.IsKind(SyntaxKind.DotToken) && tokenLeft.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                 var lhsType = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
                 if (lhsType != null)
                 {
                     symbols = await Task.Run(() => 
                        semanticModel.LookupSymbols(position, container: lhsType, includeReducedExtensionMethods: true));
                 }
                 else
                 {
                     symbols = ImmutableArray<ISymbol>.Empty;
                 }
            }
            else
            {
                // Global/Local completion
                symbols = await Task.Run(() => 
                    semanticModel.LookupSymbols(position, includeReducedExtensionMethods: true));
            }

            // 4. Convert to Completion Data
            foreach (var symbol in symbols)
            {
                if (ShouldHide(symbol)) continue;

                // Create CompletionData based on symbol kind
                var kind = ConvertToCompletionKind(symbol.Kind);
                
                // Special handling for methods to add parentheses
                var text = symbol.Name; 
                
                completions.Add(new CompletionData(text, GetDescription(symbol), kind));
            }
            
            // 5. Add Keywords (Simple fallback if not handled by LookupSymbols which primarily does identifiers)
            // Roslyn's RecommendSymbols API is better for keywords but internal/more complex. 
            // We can stick to a basic list or use the existing keyword list from CompletionProvider for now.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Roslyn Completion Error: {ex.Message}");
        }

        return completions;
    }

    private bool ShouldHide(ISymbol symbol)
    {
        // Hide backing fields, generated code, etc.
        if (symbol.Name.Contains("<") || symbol.Name.Contains("$")) return true;
        
        // Hide constructor methods (they appear as .ctor)
        if (symbol.IsImplicitlyDeclared && symbol.Kind == SymbolKind.Method) return true;
        if (symbol.Name == ".ctor") return true;

        return false;
    }

    private CompletionKind ConvertToCompletionKind(SymbolKind kind)
    {
        return kind switch
        {
            SymbolKind.Field => CompletionKind.Property, // Reusing Property color for fields
            SymbolKind.Property => CompletionKind.Property,
            SymbolKind.Method => CompletionKind.Method,
            SymbolKind.Local => CompletionKind.Property, // Local variable
            SymbolKind.Parameter => CompletionKind.Property,
            SymbolKind.Event => CompletionKind.Property,
            SymbolKind.NamedType => CompletionKind.Type,
            SymbolKind.Namespace => CompletionKind.Type, // Use Type color for namespace
            _ => CompletionKind.Property
        };
    }

    private string GetDescription(ISymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }

    public async Task<(List<string> Signatures, int CurrentParameterIndex)> GetSignatureHelpAsync(string code, int position)
    {
        try 
        {
             var syntaxTree = CSharpSyntaxTree.ParseText(code);
             var compilation = CSharpCompilation.Create(
                "SignatureAnalysis",
                new[] { syntaxTree },
                _references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

             var semanticModel = compilation.GetSemanticModel(syntaxTree);
             var root = await syntaxTree.GetRootAsync();
             var token = root.FindToken(position);
             
             // Move back if we are at the closing parenthesis or inside
             if (token.IsKind(SyntaxKind.CloseParenToken))
             {
                 token = root.FindToken(position - 1);
             }

             // Find Invocation or ObjectCreation
             var node = token.Parent;
             while (node != null && 
                    !(node is InvocationExpressionSyntax) && 
                    !(node is ObjectCreationExpressionSyntax) &&
                    !(node is BaseObjectCreationExpressionSyntax)) // Covers both new T() and implicit new()
             {
                 node = node.Parent;
             }

             if (node == null) return (new List<string>(), 0);

             ArgumentListSyntax? argList = null;
             if (node is InvocationExpressionSyntax inv) argList = inv.ArgumentList;
             else if (node is ObjectCreationExpressionSyntax obj) argList = obj.ArgumentList;
             // else if (node is ImplicitObjectCreationExpressionSyntax impl) argList = impl.ArgumentList; // C# 9

             if (argList == null) return (new List<string>(), 0);

             // Calculate current parameter index
             int paramIndex = 0;
             var spanBefore = TextSpan.FromBounds(argList.OpenParenToken.Span.End, position);
             // Count commas in the span
             var textInSpan = code.Substring(spanBefore.Start, spanBefore.Length);
             paramIndex = textInSpan.Count(c => c == ',');

             // Get symbols
             var symbolInfo = semanticModel.GetSymbolInfo(node);
             var candidates = symbolInfo.CandidateSymbols.Any() ? symbolInfo.CandidateSymbols : (symbolInfo.Symbol != null ? ImmutableArray.Create(symbolInfo.Symbol) : ImmutableArray<ISymbol>.Empty);

             var signatures = new List<string>();
             foreach (var symbol in candidates)
             {
                 if (symbol is IMethodSymbol method)
                 {
                     signatures.Add(method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                 }
             }

             return (signatures, paramIndex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Signature Help Error: {ex.Message}");
            return (new List<string>(), 0);
        }
    }
    public async Task<(string Kind, string TypeName, string Name, string Documentation)?> GetQuickInfoAsync(string code, int position)
    {
        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create(
                "QuickInfoAnalysis",
                new[] { syntaxTree },
                _references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = await syntaxTree.GetRootAsync();
            var token = root.FindToken(position);

            // If we are on a keyword or whitespace, maybe move slightly?
            // Actually FindToken(position) finds the token that contains the position.
            
            // Check lookup symbol
            var node = token.Parent;
            if (node == null) return null;

            ISymbol? symbol = null;
            
            // Try GetSymbolInfo
            var symbolInfo = semanticModel.GetSymbolInfo(node);
            symbol = symbolInfo.Symbol;

            // Fallback for declarations (GetDeclaredSymbol)
            if (symbol == null)
            {
                 symbol = semanticModel.GetDeclaredSymbol(node);
            }

            if (symbol == null) return null;

            // Extract Info
            var kind = GetKindString(symbol);
            var typeName = GetSymbolType(symbol);
            var name = symbol.Name;
            var doc = symbol.GetDocumentationCommentXml();
            
            // If internal XML doc is empty, try to get standard description
            if (string.IsNullOrEmpty(doc))
            {
                // We will rely on UI to render simple description if doc is missing
                // Or we can parse the XML here.
                // Let's return the raw XML or summary.
                // For simplicity, let's just return the DisplayString as fallback documentation if XML is missing?
                // Actually, let's leave documentation null if missing, UI handles it.
            }
            else
            {
                // Parse XML to just get summary
                try 
                {
                    var xmlDoc = new System.Xml.XmlDocument();
                    xmlDoc.LoadXml(doc);
                    var summary = xmlDoc.SelectSingleNode("//summary")?.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(summary)) doc = summary;
                }
                catch { /* ignore xml parse error */ }
            }

            return (kind, typeName, name, doc);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"QuickInfo Error: {ex.Message}");
            return null;
        }
    }

    private string GetKindString(ISymbol symbol)
    {
        return symbol.Kind switch
        {
            SymbolKind.Local => "local",
            SymbolKind.Parameter => "parameter",
            SymbolKind.Field => "field",
            SymbolKind.Property => "property",
            SymbolKind.Method => "method",
            SymbolKind.NamedType => symbol is INamedTypeSymbol nt && nt.TypeKind == TypeKind.Interface ? "interface" : 
                                    symbol is INamedTypeSymbol nt2 && nt2.TypeKind == TypeKind.Struct ? "struct" : "class",
            _ => "symbol"
        };
    }

    private string GetSymbolType(ISymbol symbol)
    {
        if (symbol is ILocalSymbol local) return local.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        if (symbol is IParameterSymbol param) return param.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        if (symbol is IFieldSymbol field) return field.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        if (symbol is IPropertySymbol prop) return prop.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        if (symbol is IMethodSymbol method) return method.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        if (symbol is INamedTypeSymbol type) return type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        
        return symbol.Name;
    }
}
