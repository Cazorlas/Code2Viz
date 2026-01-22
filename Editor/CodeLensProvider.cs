using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Code2Viz.Editor
{
    /// <summary>
    /// Provides Code Lens functionality - shows reference counts above methods and types.
    /// Uses a visual line element generator to display reference counts inline.
    /// </summary>
    public class CodeLensGenerator : VisualLineElementGenerator
    {
        private readonly TextDocument _document;
        private readonly object _lock = new();
        private List<CodeLensItem> _items = new();
        private bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                CurrentContext?.TextView?.Redraw();
            }
        }

        public CodeLensGenerator(TextDocument document)
        {
            _document = document;
        }

        /// <summary>
        /// Updates code lens information by analyzing the code.
        /// </summary>
        public void UpdateCodeLens(string code)
        {
            if (!_enabled)
            {
                lock (_lock)
                {
                    _items.Clear();
                }
                return;
            }

            var newItems = new List<CodeLensItem>();

            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create(
                    "CodeLensAnalysis",
                    new[] { syntaxTree },
                    new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );

                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot();

                // Build a map of symbol usages
                var usageMap = BuildUsageMap(root);

                // Snapshot the document line count to avoid race conditions
                var lineCount = _document.LineCount;

                // Find class/struct/interface declarations
                foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    var typeName = typeDecl.Identifier.Text;
                    var refCount = usageMap.TryGetValue(typeName, out var count) ? count : 0;

                    var lineSpan = typeDecl.Identifier.GetLocation().GetLineSpan();
                    var lineNumber = lineSpan.StartLinePosition.Line + 1; // 1-based

                    // Validate line number is within bounds
                    if (lineNumber < 1 || lineNumber > lineCount) continue;

                    // Get the start of the line (before any content)
                    var line = _document.GetLineByNumber(lineNumber);

                    newItems.Add(new CodeLensItem
                    {
                        Offset = line.Offset,
                        Line = lineNumber,
                        Text = $"{refCount} reference{(refCount != 1 ? "s" : "")}",
                        Kind = CodeLensKind.Type,
                        SymbolName = typeName
                    });
                }

                // Find method declarations
                foreach (var methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var methodName = methodDecl.Identifier.Text;
                    var refCount = usageMap.TryGetValue(methodName, out var count) ? count : 0;

                    var lineSpan = methodDecl.Identifier.GetLocation().GetLineSpan();
                    var lineNumber = lineSpan.StartLinePosition.Line + 1;

                    // Validate line number is within bounds
                    if (lineNumber < 1 || lineNumber > lineCount) continue;

                    var line = _document.GetLineByNumber(lineNumber);

                    newItems.Add(new CodeLensItem
                    {
                        Offset = line.Offset,
                        Line = lineNumber,
                        Text = $"{refCount} reference{(refCount != 1 ? "s" : "")}",
                        Kind = CodeLensKind.Method,
                        SymbolName = methodName
                    });
                }

                // Find property declarations
                foreach (var propDecl in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    var propName = propDecl.Identifier.Text;
                    var refCount = usageMap.TryGetValue(propName, out var count) ? count : 0;

                    var lineSpan = propDecl.Identifier.GetLocation().GetLineSpan();
                    var lineNumber = lineSpan.StartLinePosition.Line + 1;

                    // Validate line number is within bounds
                    if (lineNumber < 1 || lineNumber > lineCount) continue;

                    var line = _document.GetLineByNumber(lineNumber);

                    newItems.Add(new CodeLensItem
                    {
                        Offset = line.Offset,
                        Line = lineNumber,
                        Text = $"{refCount} reference{(refCount != 1 ? "s" : "")}",
                        Kind = CodeLensKind.Property,
                        SymbolName = propName
                    });
                }

                // Sort by offset
                newItems.Sort((a, b) => a.Offset.CompareTo(b.Offset));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CodeLens error: {ex.Message}");
            }

            // Atomically swap the items list
            lock (_lock)
            {
                _items = newItems;
            }
        }

        private Dictionary<string, int> BuildUsageMap(SyntaxNode root)
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);

            // Count all identifier usages
            foreach (var identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                var name = identifier.Identifier.Text;

                // Skip if it's part of a declaration
                if (identifier.Parent is VariableDeclaratorSyntax ||
                    identifier.Parent is MethodDeclarationSyntax ||
                    identifier.Parent is PropertyDeclarationSyntax ||
                    identifier.Parent is ClassDeclarationSyntax ||
                    identifier.Parent is StructDeclarationSyntax ||
                    identifier.Parent is InterfaceDeclarationSyntax)
                {
                    continue;
                }

                if (map.TryGetValue(name, out var count))
                {
                    map[name] = count + 1;
                }
                else
                {
                    map[name] = 1;
                }
            }

            // Also count type references in type syntax
            foreach (var typeSyntax in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (typeSyntax is IdentifierNameSyntax) continue; // Already counted

                var name = typeSyntax.Identifier.Text;
                if (map.TryGetValue(name, out var count))
                {
                    map[name] = count + 1;
                }
                else
                {
                    map[name] = 1;
                }
            }

            return map;
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            if (!_enabled) return -1;

            try
            {
                List<CodeLensItem> snapshot;
                lock (_lock)
                {
                    snapshot = _items;
                }

                if (snapshot.Count == 0) return -1;

                foreach (var item in snapshot)
                {
                    if (item.Offset >= startOffset)
                        return item.Offset;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CodeLens GetFirstInterestedOffset error: {ex.Message}");
            }

            return -1;
        }

        public override VisualLineElement? ConstructElement(int offset)
        {
            if (!_enabled) return null;

            try
            {
                List<CodeLensItem> snapshot;
                lock (_lock)
                {
                    snapshot = _items;
                }

                var item = snapshot.FirstOrDefault(i => i.Offset == offset);
                if (item == null) return null;

                return new CodeLensElement(item.Text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CodeLens ConstructElement error: {ex.Message}");
                return null;
            }
        }
    }

    public class CodeLensItem
    {
        public int Offset { get; set; }
        public int Line { get; set; }
        public string Text { get; set; } = "";
        public CodeLensKind Kind { get; set; }
        public string SymbolName { get; set; } = "";
    }

    public enum CodeLensKind
    {
        Type,
        Method,
        Property,
        Field
    }

    /// <summary>
    /// Visual element that renders code lens text inline (at the start of the line).
    /// </summary>
    public class CodeLensElement : VisualLineElement
    {
        private readonly string _text;

        public CodeLensElement(string text) : base(ComputeVisualLength(text), 0)
        {
            // Visual length must match the TextRun length
            // Document length = 0 (doesn't consume any document characters)
            _text = text + " | ";
        }

        private static int ComputeVisualLength(string text)
        {
            return (text + " | ").Length;
        }

        public override TextRun CreateTextRun(
            int startVisualColumn,
            ITextRunConstructionContext context)
        {
            try
            {
                var props = new CodeLensTextRunProperties(context.GlobalTextRunProperties);
                return new CodeLensTextRun(_text, props);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CodeLens CreateTextRun error: {ex.Message}");
                // Return a minimal text run on error
                return new TextCharacters(" ", context.GlobalTextRunProperties);
            }
        }
    }

    public class CodeLensTextRun : TextRun
    {
        private readonly char[] _textChars;
        private readonly TextRunProperties _properties;

        public CodeLensTextRun(string text, TextRunProperties properties)
        {
            _textChars = text.ToCharArray();
            _properties = properties;
        }

        public override CharacterBufferReference CharacterBufferReference =>
            new CharacterBufferReference(_textChars, 0);

        public override int Length => _textChars.Length;

        public override TextRunProperties Properties => _properties;
    }

    public class CodeLensTextRunProperties : TextRunProperties
    {
        private readonly TextRunProperties _baseProperties;

        public CodeLensTextRunProperties(TextRunProperties baseProperties)
        {
            _baseProperties = baseProperties;
        }

        public override Brush BackgroundBrush => Brushes.Transparent;

        public override System.Globalization.CultureInfo CultureInfo => _baseProperties.CultureInfo;

        public override double FontHintingEmSize => _baseProperties.FontHintingEmSize * 0.8;

        public override double FontRenderingEmSize => _baseProperties.FontRenderingEmSize * 0.8;

        public override Brush ForegroundBrush => new SolidColorBrush(Color.FromRgb(120, 120, 120));

        public override System.Windows.TextDecorationCollection? TextDecorations => null;

        public override System.Windows.Media.TextEffectCollection? TextEffects => null;

        public override Typeface Typeface => new Typeface(
            _baseProperties.Typeface.FontFamily,
            FontStyles.Italic,
            _baseProperties.Typeface.Weight,
            _baseProperties.Typeface.Stretch);
    }
}
