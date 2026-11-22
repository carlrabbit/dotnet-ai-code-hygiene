using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiWorkbench.Autoformat.Core;

/// <summary>Opinionated, source-only C# canonicalizations. This deliberately does not compile consumer projects.</summary>
internal static class CSharpCanonicalizer
{
    internal readonly record struct RetainedCommentIndentation(string Text, string Indentation);

    internal static readonly string[] Rules =
    [
        "modernization.var-style", "modernization.expression-body", "modernization.collection-expression",
        "modernization.primary-constructor", "modernization.switch-expression", "modernization.conditional",
        "modernization.object-initializer", "modernization.readonly-field", "modernization.inline-declaration",
        "readability.visual-block", "readability.parameters", "readability.long-line", "readability.comments"
    ];

    internal static string Normalize(string source, CSharpParseOptions? parseOptions, out IReadOnlyList<string> notices)
    {
        var messages = new List<string>();
        parseOptions ??= new CSharpParseOptions(LanguageVersion.Default);
        var tree = CSharpSyntaxTree.ParseText(source, parseOptions);
        SyntaxNode root = tree.GetRoot();
        ValidateRetains(root, messages);
        bool supportsCSharp12 = parseOptions.LanguageVersion >= LanguageVersion.CSharp12;
        SyntaxNode rewritten = new CanonicalRewriter(supportsCSharp12).Visit(root)!;
        rewritten = new CanonicalRewriter(supportsCSharp12).Visit(rewritten)!;
        string result = NormalizeLayout(rewritten.ToFullString(), out IReadOnlyList<string> layoutNotices);
        messages.AddRange(layoutNotices);
        notices = messages.Distinct(StringComparer.Ordinal).ToArray();
        return result;
    }

    internal static string NormalizeLayout(string source, out IReadOnlyList<string> notices)
    {
        string newline = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        SyntaxNode root = CSharpSyntaxTree.ParseText(source).GetRoot();
        SyntaxNode rewritten = new LayoutRewriter(newline).Visit(root)!;
        rewritten = new BlankLineRewriter().Visit(rewritten)!;
        string result = rewritten.ToFullString();
        int remainingLongLines = Microsoft.CodeAnalysis.Text.SourceText.From(result).Lines
            .Count(line => line.Span.Length > 120 && !result[line.Start..line.End].TrimStart().StartsWith("//", StringComparison.Ordinal));
        notices = remainingLongLines > 0
            ? [$"{remainingLongLines} source line(s) remain over the 120-character soft target where no supported syntax wrapping boundary was applied."]
            : [];
        return result;
    }

    internal static IReadOnlyList<RetainedCommentIndentation> CaptureRetainedCommentIndentation(string source)
    {
        SyntaxNode root = CSharpSyntaxTree.ParseText(source).GetRoot();
        return root.DescendantTrivia(descendIntoTrivia: true)
            .Where(IsCommentTrivia)
            .Where(trivia => trivia.Token.Parent is not null && Retained(trivia.Token.Parent, "readability.comments"))
            .Select(trivia => (Trivia: trivia, LineStart: source.LastIndexOf('\n', Math.Max(0, trivia.SpanStart - 1)) + 1))
            .Where(item => source.AsSpan(item.LineStart, item.Trivia.SpanStart - item.LineStart).IndexOfAnyExcept(' ', '\t') < 0)
            .Select(item => new RetainedCommentIndentation(item.Trivia.ToFullString(), source[item.LineStart..item.Trivia.SpanStart]))
            .ToArray();
    }

    internal static string RestoreRetainedCommentIndentation(string source, IReadOnlyList<RetainedCommentIndentation> retained)
    {
        if (retained.Count == 0)
        {
            return source;
        }

        var available = retained.GroupBy(item => item.Text, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => new Queue<string>(group.Select(item => item.Indentation)), StringComparer.Ordinal);
        SyntaxNode root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var changes = new List<(int Start, int Length, string Text)>();
        foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true).Where(IsCommentTrivia))
        {
            if (trivia.Token.Parent is null || !Retained(trivia.Token.Parent, "readability.comments")
                || !available.TryGetValue(trivia.ToFullString(), out Queue<string>? indentations) || indentations.Count == 0)
            {
                continue;
            }

            string indentation = indentations.Dequeue();
            int lineStart = source.LastIndexOf('\n', Math.Max(0, trivia.SpanStart - 1)) + 1;
            ReadOnlySpan<char> prefix = source.AsSpan(lineStart, trivia.SpanStart - lineStart);
            if (prefix.IndexOfAnyExcept(' ', '\t') < 0 && !prefix.SequenceEqual(indentation.AsSpan()))
            {
                changes.Add((lineStart, prefix.Length, indentation));
            }
        }

        foreach ((int start, int length, string text) in changes.OrderByDescending(change => change.Start))
        {
            source = source[..start] + text + source[(start + length)..];
        }

        return source;
    }

    private static bool IsCommentTrivia(SyntaxTrivia trivia) => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
        || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
        || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);

    private static void ValidateRetains(SyntaxNode root, List<string> notices)
    {
        foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true).Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia)))
        {
            string text = trivia.ToString();
            if (!text.TrimStart().StartsWith("// autoformat:retain", StringComparison.Ordinal))
            {
                continue;
            }

            Match match = Regex.Match(text, @"^\s*//\s*autoformat:retain\s+(?<id>[a-z0-9.-]+)\s+--\s+(?<reason>\S.*\S|\S)\s*$", RegexOptions.CultureInvariant);
            if (!match.Success || !Rules.Contains(match.Groups["id"].Value, StringComparer.Ordinal))
            {
                notices.Add($"Invalid autoformat:retain annotation at line {trivia.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {text.Trim()}");
            }
        }
    }

    private static bool Retained(SyntaxNode node, string id)
    {
        IEnumerable<SyntaxNode> owners = node.AncestorsAndSelf().Where(IsRetainOwner);
        if (id == "readability.comments")
        {
            foreach (SyntaxNode owner in owners)
            {
                if (HasRetain(owner, id))
                {
                    return true;
                }

                if (owner is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or PropertyDeclarationSyntax
                    or FieldDeclarationSyntax or BaseTypeDeclarationSyntax)
                {
                    break;
                }
            }

            return false;
        }

        SyntaxNode? directOwner = owners.FirstOrDefault();
        return directOwner is not null && HasRetain(directOwner, id);
    }

    private static bool HasRetain(SyntaxNode owner, string id) => owner.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia))
            .Select(t => Regex.Match(t.ToString(), @"^\s*//\s*autoformat:retain\s+(?<id>[a-z0-9.-]+)\s+--\s+(?<reason>\S.*\S|\S)\s*$", RegexOptions.CultureInvariant))
            .Any(match => match.Success && match.Groups["id"].Value == id);

    private static bool IsRetainOwner(SyntaxNode node) => node is StatementSyntax or BaseMethodDeclarationSyntax
        or PropertyDeclarationSyntax or FieldDeclarationSyntax or EventDeclarationSyntax or BaseTypeDeclarationSyntax
        or AccessorDeclarationSyntax or LocalFunctionStatementSyntax;

    private sealed class CanonicalRewriter(bool supportsCSharp12) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            node = (LocalDeclarationStatementSyntax)base.VisitLocalDeclarationStatement(node)!;
            if (!Retained(node, "modernization.var-style") && node.Declaration.Type.IsVar && node.Declaration.Variables.Count == 1)
            {
                ExpressionSyntax? value = node.Declaration.Variables[0].Initializer?.Value;
                TypeSyntax? type = value switch
                {
                    ObjectCreationExpressionSyntax creation => creation.Type,
                    ImplicitObjectCreationExpressionSyntax creation => null,
                    ArrayCreationExpressionSyntax array => SyntaxFactory.ArrayType(array.Type.ElementType, array.Type.RankSpecifiers),
                    ImplicitArrayCreationExpressionSyntax array => InferArrayType(array.Initializer),
                    CollectionExpressionSyntax collection => InferCollectionType(collection),
                    LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                    LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.NumericLiteralExpression) => NumericType(literal.Token.Value),
                    LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.TrueLiteralExpression) || literal.IsKind(SyntaxKind.FalseLiteralExpression) => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                    _ => null
                };
                if (type is not null)
                {
                    node = node.WithDeclaration(node.Declaration.WithType(type.WithTriviaFrom(node.Declaration.Type)));
                }
            }
            return node;
        }

        public override SyntaxNode? VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            node = (ArrayCreationExpressionSyntax)base.VisitArrayCreationExpression(node)!;
            if (!supportsCSharp12 || !IsSupportedArrayCreation(node) || HasComments(node) || HasUntargetedVar(node)
                || !HasSupportedCollectionTarget(node) || Retained(node, "modernization.collection-expression"))
            {
                return node;
            }

            return SyntaxFactory.ParseExpression("[" + string.Join(", ", node.Initializer!.Expressions.Select(e => e.ToString())) + "]").WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
        {
            node = (ImplicitArrayCreationExpressionSyntax)base.VisitImplicitArrayCreationExpression(node)!;
            if (!supportsCSharp12 || !IsSupportedArrayInitializer(node.Initializer) || HasComments(node) || HasUntargetedVar(node)
                || !HasSupportedCollectionTarget(node) || Retained(node, "modernization.collection-expression"))
            {
                return node;
            }

            return SyntaxFactory.ParseExpression("[" + string.Join(", ", node.Initializer.Expressions.Select(e => e.ToString())) + "]").WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            node = (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node)!;
            TypeSyntax? target = GetDirectTargetType(node);
            if (supportsCSharp12 && node.Initializer is { Expressions.Count: > 0 } init && IsCollectionInitializer(init)
                && !HasComments(node) && !HasUntargetedVar(node) && IsSupportedCollectionTarget(target)
                && !Retained(node, "modernization.collection-expression") && IsKnownListType(node.Type))
            {
                return SyntaxFactory.ParseExpression("[" + string.Join(", ", init.Expressions.Select(e => e.ToString())) + "]").WithTriviaFrom(node);
            }

            return node;
        }

        public override SyntaxNode? VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
        {
            node = (ImplicitObjectCreationExpressionSyntax)base.VisitImplicitObjectCreationExpression(node)!;
            TypeSyntax? target = GetDirectTargetType(node);
            if (supportsCSharp12 && target is not null && IsKnownListType(target)
                && node.Initializer is { Expressions.Count: > 0 } init && IsCollectionInitializer(init) && !HasComments(node)
                && !HasUntargetedVar(node) && !Retained(node, "modernization.collection-expression"))
            {
                return SyntaxFactory.ParseExpression("[" + string.Join(", ", init.Expressions.Select(e => e.ToString())) + "]").WithTriviaFrom(node);
            }

            // Target-typed `new()` is converted only when its direct syntactic target identifies List<T>.
            // Arbitrary DTO/object initializers remain object initializers.
            return node;
        }

        private static bool IsSupportedArrayCreation(ArrayCreationExpressionSyntax node) =>
            node.Type.RankSpecifiers.Count > 0 && node.Type.RankSpecifiers.All(rank => rank.Sizes.Count == 1)
            && IsSupportedArrayInitializer(node.Initializer);

        private static bool IsSupportedArrayInitializer(InitializerExpressionSyntax? initializer) =>
            initializer is { Expressions.Count: > 0 } && initializer.IsKind(SyntaxKind.ArrayInitializerExpression)
            && initializer.Expressions.All(expression => expression is not InitializerExpressionSyntax);

        private static bool IsCollectionInitializer(InitializerExpressionSyntax initializer) =>
            initializer.IsKind(SyntaxKind.CollectionInitializerExpression) && initializer.Expressions.Count > 0
            && initializer.Expressions.All(expression => expression is not InitializerExpressionSyntax);

        private static bool HasSupportedCollectionTarget(ExpressionSyntax node) => IsSupportedCollectionTarget(GetDirectTargetType(node));

        private static bool IsSupportedCollectionTarget(TypeSyntax? type)
        {
            if (type is ArrayTypeSyntax array)
            {
                return array.RankSpecifiers.All(rank => rank.Sizes.Count == 1);
            }

            if (type is not null && TryGetSimpleTypeName(type) is GenericNameSyntax generic)
            {
                return generic.Identifier.ValueText is "List" or "IEnumerable" or "IReadOnlyCollection" or "IReadOnlyList" or "ICollection" or "IList";
            }

            return false;
        }

        private static TypeSyntax? GetDirectTargetType(ExpressionSyntax node)
        {
            if (node.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variable }
                && variable.Parent is VariableDeclarationSyntax declaration && !declaration.Type.IsVar)
            {
                return declaration.Type;
            }

            if (node.Parent is EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax property })
            {
                return property.Type;
            }

            if (node.Parent is ReturnStatementSyntax)
            {
                SyntaxNode? returnOwner = node.Ancestors().FirstOrDefault(IsReturnOwnerBoundary);
                return returnOwner switch
                {
                    LocalFunctionStatementSyntax localFunction => localFunction.ReturnType,
                    MethodDeclarationSyntax method => method.ReturnType,
                    _ => null
                };
            }

            if (node.Parent is ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax expressionMethod })
            {
                return expressionMethod.ReturnType;
            }

            if (node.Parent is ArrowExpressionClauseSyntax { Parent: PropertyDeclarationSyntax expressionProperty })
            {
                return expressionProperty.Type;
            }

            if (node.Parent is ArrowExpressionClauseSyntax { Parent: LocalFunctionStatementSyntax expressionFunction })
            {
                return expressionFunction.ReturnType;
            }

            return null;
        }

        private static bool IsReturnOwnerBoundary(SyntaxNode node) => node is LocalFunctionStatementSyntax
            or MethodDeclarationSyntax or AnonymousFunctionExpressionSyntax;

        private static SimpleNameSyntax? TryGetSimpleTypeName(TypeSyntax type) => type switch
        {
            GenericNameSyntax generic => generic,
            QualifiedNameSyntax qualified => qualified.Right,
            AliasQualifiedNameSyntax alias => alias.Name,
            _ => null
        };

        private static bool IsKnownListType(TypeSyntax type)
        {
            return TryGetSimpleTypeName(type) is GenericNameSyntax genericName && genericName.Identifier.ValueText == "List";
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
            if (Retained(node, "modernization.expression-body") || node.ExpressionBody is not null || node.Body is null || node.Body.Statements.Count != 1)
            {
                return node;
            }

            if (HasProtectedTrivia(node.Body))
            {
                return node;
            }

            ExpressionSyntax? expression = node.Body.Statements[0] switch { ReturnStatementSyntax r when r.Expression is not null => r.Expression, ExpressionStatementSyntax e => e.Expression, _ => null };
            return expression is null ? node : node.WithBody(null).WithExpressionBody(SyntaxFactory.ArrowExpressionClause(expression)).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            node = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node)!;
            if (Retained(node, "modernization.expression-body") || node.ExpressionBody is not null || node.AccessorList?.Accessors.Count != 1)
            {
                return node;
            }

            AccessorDeclarationSyntax accessor = node.AccessorList.Accessors[0];
            if (accessor.Body is null || HasProtectedTrivia(accessor.Body))
            {
                return node;
            }

            ExpressionSyntax? expression = accessor.Body?.Statements.Count == 1 && accessor.Body.Statements[0] is ReturnStatementSyntax { Expression: not null } ret ? ret.Expression : null;
            if (expression is null || accessor.Keyword.IsKind(SyntaxKind.SetKeyword))
            {
                return node;
            }

            return node.WithAccessorList(null).WithExpressionBody(SyntaxFactory.ArrowExpressionClause(expression)).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
        {
            node = (IfStatementSyntax)base.VisitIfStatement(node)!;
            if (HasComments(node) || Retained(node, "modernization.conditional") || node.Else is null)
            {
                return node;
            }

            StatementSyntax yesStatement = Unwrap(node.Statement);
            StatementSyntax noStatement = Unwrap(node.Else.Statement);
            if (yesStatement is ReturnStatementSyntax { Expression: not null } yes && noStatement is ReturnStatementSyntax { Expression: not null } no)
            {
                return SyntaxFactory.ReturnStatement(SyntaxFactory.ConditionalExpression(node.Condition, yes.Expression, no.Expression)).WithTriviaFrom(node);
            }

            if (yesStatement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax yesAssign } && noStatement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax noAssign }
                && yesAssign.Left.ToString() == noAssign.Left.ToString())
            {
                return SyntaxFactory.ExpressionStatement(yesAssign.WithRight(SyntaxFactory.ConditionalExpression(node.Condition, yesAssign.Right, noAssign.Right))).WithTriviaFrom(node);
            }

            return node;
        }

        private static StatementSyntax Unwrap(StatementSyntax statement) => statement is BlockSyntax { Statements.Count: 1 } block ? block.Statements[0] : statement;

        public override SyntaxNode? VisitSwitchStatement(SwitchStatementSyntax node)
        {
            node = (SwitchStatementSyntax)base.VisitSwitchStatement(node)!;
            if (HasComments(node) || Retained(node, "modernization.switch-expression") || node.Sections.Any(s => s.Labels.Count != 1))
            {
                return node;
            }

            var arms = new List<string>();
            string? assignmentTarget = null;
            bool? returns = null;
            foreach (SwitchSectionSyntax section in node.Sections)
            {
                string? value;
                if (section.Statements.Count == 1 && section.Statements[0] is ReturnStatementSyntax { Expression: not null } r)
                {
                    if (returns == false)
                    {
                        return node;
                    }

                    returns = true;
                    value = r.Expression.ToString();
                }
                else if (section.Statements.Count == 2
                    && section.Statements[0] is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
                    && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                    && section.Statements[1] is BreakStatementSyntax)
                {
                    if (returns == true)
                    {
                        return node;
                    }

                    returns = false;
                    string target = assignment.Left.ToString();
                    if (assignmentTarget is not null && assignmentTarget != target)
                    {
                        return node;
                    }

                    assignmentTarget = target;
                    value = assignment.Right.ToString();
                }
                else
                {
                    return node;
                }

                string label = section.Labels[0] switch { CaseSwitchLabelSyntax c => c.Value.ToString(), DefaultSwitchLabelSyntax => "_", _ => "" };
                if (label.Length == 0)
                {
                    return node;
                }

                arms.Add(label + " => " + value);
            }
            if (returns is null)
            {
                return node;
            }
            ExpressionSyntax converted = SyntaxFactory.ParseExpression(node.Expression + " switch { " + string.Join(", ", arms) + " }");
            StatementSyntax replacement = returns.Value
                ? SyntaxFactory.ReturnStatement(converted)
                : SyntaxFactory.ParseStatement($"{assignmentTarget} = {converted};");
            return replacement.WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            node = (BlockSyntax)base.VisitBlock(node)!;
            var statements = node.Statements;
            for (int i = 0; i + 1 < statements.Count; i++)
            {
                if (statements[i] is LocalDeclarationStatementSyntax declaration && declaration.Declaration.Variables.Count == 1
                    && declaration.Declaration.Variables[0].Initializer?.Value is ObjectCreationExpressionSyntax creation
                    && creation.Initializer is null && statements[i + 1] is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
                    && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) && assignment.Left is MemberAccessExpressionSyntax access
                    && access.Expression.ToString() == declaration.Declaration.Variables[0].Identifier.ValueText
                    && assignment.Right.DescendantNodesAndSelf().All(n => n is not InvocationExpressionSyntax)
                    && !HasComments(declaration) && !HasComments(statements[i + 1]) && !Retained(declaration, "modernization.object-initializer"))
                {
                    var initializer = SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>([SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(access.Name.Identifier), assignment.Right)]));
                    var updatedCreation = creation.WithInitializer(initializer);
                    statements = statements.Replace(declaration, declaration.WithDeclaration(declaration.Declaration.WithVariables(declaration.Declaration.Variables.Replace(declaration.Declaration.Variables[0], declaration.Declaration.Variables[0].WithInitializer(declaration.Declaration.Variables[0].Initializer!.WithValue(updatedCreation))))));
                    statements = statements.RemoveAt(i + 1); i--;
                }
            }
            return node.WithStatements(statements);
        }

        public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            node = (FieldDeclarationSyntax)base.VisitFieldDeclaration(node)!;
            if (Retained(node, "modernization.readonly-field") || node.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) || node.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return node;
            }
            // Recognize fields assigned only by constructor bodies in this source file.
            SyntaxNode? root = node.SyntaxTree.GetRoot();
            string name = node.Declaration.Variables.Count == 1 ? node.Declaration.Variables[0].Identifier.ValueText : "";
            AssignmentExpressionSyntax[] writes = root.DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(a => LeftName(a.Left) == name).ToArray();
            if (name.Length > 0 && writes.Length > 0 && writes.All(a => a.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() is not null))
            {
                return node.WithModifiers(node.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
            }

            return node;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) => RewriteType((ClassDeclarationSyntax)base.VisitClassDeclaration(node)!);
        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node) => RewriteType((StructDeclarationSyntax)base.VisitStructDeclaration(node)!);

        private SyntaxNode RewriteType(TypeDeclarationSyntax type)
        {
            if (!supportsCSharp12 || type.ParameterList is not null || type.Members.OfType<ConstructorDeclarationSyntax>().Count() != 1)
            {
                return type;
            }

            ConstructorDeclarationSyntax ctor = type.Members.OfType<ConstructorDeclarationSyntax>().Single();
            SyntaxTriviaList nameTrailingTrivia = type.Identifier.TrailingTrivia;
            if (Retained(type, "modernization.primary-constructor") || Retained(ctor, "modernization.primary-constructor")
                // A primary constructor is publicly callable. Keep a restricted explicit
                // constructor when its access boundary cannot be represented by that form.
                || !ctor.Modifiers.Any(SyntaxKind.PublicKeyword)
                || nameTrailingTrivia.Any(trivia => !trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                || ctor.AttributeLists.Count > 0 || HasComments(ctor) || ctor.Initializer is not null
                || ctor.Modifiers.Any(SyntaxKind.StaticKeyword) || ctor.Body is null || ctor.Body.Statements.Count == 0)
            {
                return type;
            }

            var assignments = new List<(string Name, string Value)>();
            foreach (StatementSyntax statement in ctor.Body.Statements)
            {
                if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment } || !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    return type;
                }

                string name = LeftName(assignment.Left);
                string value = assignment.Right is IdentifierNameSyntax parameter ? parameter.Identifier.ValueText : "";
                if (name.Length == 0 || value.Length == 0 || !ctor.ParameterList.Parameters.Any(p => p.Identifier.ValueText == value))
                {
                    return type;
                }

                assignments.Add((name, value));
            }
            var members = type.Members.ToList();
            foreach ((string name, string value) in assignments)
            {
                int fieldIndex = members.FindIndex(member => member is FieldDeclarationSyntax field && field.Declaration.Variables.Any(v => v.Identifier.ValueText == name));
                if (fieldIndex < 0)
                {
                    return type;
                }

                var field = (FieldDeclarationSyntax)members[fieldIndex];
                VariableDeclaratorSyntax variable = field.Declaration.Variables.First(v => v.Identifier.ValueText == name);
                if (variable.Initializer is not null)
                {
                    return type;
                }

                FieldDeclarationSyntax updated = field.ReplaceNode(variable, variable.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(value))));
                if (!updated.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                {
                    updated = updated.WithModifiers(updated.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
                }

                members[fieldIndex] = updated;
            }
            members.Remove(ctor);
            ParameterListSyntax parameterList = SyntaxFactory.ParameterList(ctor.ParameterList.Parameters).WithTrailingTrivia(nameTrailingTrivia);
            return type.WithIdentifier(type.Identifier.WithTrailingTrivia(default(SyntaxTriviaList)))
                .WithParameterList(parameterList)
                .WithMembers(SyntaxFactory.List(members));
        }

        private static bool HasComments(SyntaxNode node) => HasProtectedTrivia(node);

        private static bool HasProtectedTrivia(SyntaxNode node) => node.DescendantTrivia(descendIntoTrivia: true)
            .Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)
                || t.IsKind(SyntaxKind.DisabledTextTrivia)
                || t.HasStructure && t.GetStructure() is DocumentationCommentTriviaSyntax or DirectiveTriviaSyntax);

        private static bool HasUntargetedVar(SyntaxNode node) => node.Ancestors().OfType<VariableDeclarationSyntax>().Any(v => v.Type.IsVar);
        private static string LeftName(ExpressionSyntax expression) => expression is MemberAccessExpressionSyntax member ? member.Name.Identifier.ValueText : expression is IdentifierNameSyntax identifier ? identifier.Identifier.ValueText : "";
        private static TypeSyntax? InferArrayType(InitializerExpressionSyntax initializer)
        {
            if (initializer.Expressions.Count == 0)
            {
                return null;
            }

            TypeSyntax? element = initializer.Expressions[0] switch
            {
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.NumericLiteralExpression) => NumericType(literal.Token.Value),
                _ => null
            };
            return element is null ? null : SyntaxFactory.ArrayType(element, SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression()))));
        }
        private static TypeSyntax? InferCollectionType(CollectionExpressionSyntax collection)
        {
            if (collection.Elements.Count == 0 || collection.Elements[0] is not ExpressionElementSyntax first)
            {
                return null;
            }

            TypeSyntax? element = first.Expression switch
            {
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.NumericLiteralExpression) => NumericType(literal.Token.Value),
                _ => null
            };
            return element is null ? null : SyntaxFactory.ArrayType(element, SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression()))));
        }

        private static TypeSyntax? NumericType(object? value) => value switch
        {
            int => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)), long => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword)),
            double => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword)), float => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
            decimal => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword)), _ => null
        };
    }

    private sealed class LayoutRewriter(string newline) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitParameterList(ParameterListSyntax node)
        {
            node = (ParameterListSyntax)base.VisitParameterList(node)!;
            if ((node.Parameters.Count < 3 && node.Parameters.ToString().Length < 100)
                || HasProtectedTrivia(node) || Retained(node, "readability.parameters"))
            {
                return node;
            }

            string indentation = new(' ', IndentDepth(node) * 4);
            string parameterIndentation = indentation + "    ";
            node = node.WithOpenParenToken(node.OpenParenToken.WithTrailingTrivia(NewLineIndent(parameterIndentation)));
            SeparatedSyntaxList<ParameterSyntax> parameters = node.Parameters;
            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterSyntax parameter = parameters[i];
                parameters = parameters.Replace(parameter, parameter.WithoutLeadingTrivia().WithoutTrailingTrivia());
            }

            for (int i = 0; i < parameters.SeparatorCount; i++)
            {
                SyntaxToken separator = parameters.GetSeparator(i);
                parameters = parameters.ReplaceSeparator(separator, separator.WithTrailingTrivia(NewLineIndent(parameterIndentation)));
            }

            return node.WithParameters(parameters).WithCloseParenToken(node.CloseParenToken.WithLeadingTrivia(NewLineIndent(indentation)));
        }

        public override SyntaxNode? VisitArgumentList(ArgumentListSyntax node)
        {
            node = (ArgumentListSyntax)base.VisitArgumentList(node)!;
            if (node.ToString().Length < 120 || HasProtectedTrivia(node) || Retained(node, "readability.long-line"))
            {
                return node;
            }

            var args = node.Arguments;
            for (int i = 0; i < args.SeparatorCount; i++)
            {
                SyntaxToken separator = args.GetSeparator(i);
                args = args.ReplaceSeparator(separator, separator.WithTrailingTrivia(IndentTrivia(node, separator.TrailingTrivia)));
            }

            return node.WithArguments(args);
        }

        public override SyntaxNode? VisitCollectionExpression(CollectionExpressionSyntax node)
        {
            node = (CollectionExpressionSyntax)base.VisitCollectionExpression(node)!;
            if (node.ToString().Length < 120 || HasProtectedTrivia(node) || Retained(node, "readability.long-line"))
            {
                return node;
            }

            var elements = node.Elements;
            for (int i = 0; i < elements.SeparatorCount; i++)
            {
                SyntaxToken separator = elements.GetSeparator(i);
                elements = elements.ReplaceSeparator(separator, separator.WithTrailingTrivia(IndentTrivia(node, separator.TrailingTrivia)));
            }

            return node.WithElements(elements);
        }

        public override SyntaxNode? VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            node = (InitializerExpressionSyntax)base.VisitInitializerExpression(node)!;
            if (node.ToString().Length < 120 || HasProtectedTrivia(node) || Retained(node, "readability.long-line"))
            {
                return node;
            }

            var expressions = node.Expressions;
            for (int i = 0; i < expressions.SeparatorCount; i++)
            {
                SyntaxToken separator = expressions.GetSeparator(i);
                expressions = expressions.ReplaceSeparator(separator, separator.WithTrailingTrivia(IndentTrivia(node, separator.TrailingTrivia)));
            }

            return node.WithExpressions(expressions);
        }

        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            node = (BinaryExpressionSyntax)base.VisitBinaryExpression(node)!;
            if ((!node.IsKind(SyntaxKind.LogicalAndExpression) && !node.IsKind(SyntaxKind.LogicalOrExpression)) || node.Span.Length < 120 || HasProtectedTrivia(node) || Retained(node, "readability.long-line"))
            {
                return node;
            }

            return node.WithOperatorToken(node.OperatorToken.WithTrailingTrivia(IndentTrivia(node, node.OperatorToken.TrailingTrivia)));
        }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            node = (BlockSyntax)base.VisitBlock(node)!;
            var statements = node.Statements;
            // Inline a single-use declaration into its following out argument.
            for (int i = 0; i + 1 < statements.Count; i++)
            {
                if (statements[i] is not LocalDeclarationStatementSyntax { Declaration.Variables.Count: 1 } local)
                {
                    continue;
                }

                VariableDeclaratorSyntax variable = local.Declaration.Variables[0];
                if (variable.Initializer is not null || Retained(local, "modernization.inline-declaration"))
                {
                    continue;
                }

                if (statements[i + 1].DescendantNodesAndSelf().OfType<ArgumentSyntax>().FirstOrDefault(a => a.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) && a.Expression is IdentifierNameSyntax id && id.Identifier.ValueText == variable.Identifier.ValueText) is not ArgumentSyntax argument)
                {
                    continue;
                }

                if (HasProtectedTrivia(local) || HasProtectedTrivia(statements[i + 1]))
                {
                    continue;
                }

                TypeSyntax type = local.Declaration.Type.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.Space);
                var declaration = SyntaxFactory.DeclarationExpression(type, SyntaxFactory.SingleVariableDesignation(variable.Identifier.WithoutTrivia()));
                statements = statements.Replace(statements[i + 1], statements[i + 1].ReplaceNode(argument, argument.WithExpression(declaration.WithTriviaFrom(argument.Expression))));
                statements = statements.RemoveAt(i); i--;
            }
            for (int i = 1; i < statements.Count; i++)
            {
                bool previousFlow = IsFlow(statements[i - 1]); bool currentFlow = IsFlow(statements[i]);
                if (previousFlow == currentFlow || (statements[i] is LocalFunctionStatementSyntax) || HasBlankBoundary(statements[i - 1], statements[i]))
                {
                    continue;
                }

                if ((currentFlow || previousFlow)
                    && !Retained(statements[i], "readability.visual-block")
                    && !Retained(statements[i - 1], "readability.visual-block")
                    && !HasDirectiveTrivia(statements[i]) && !HasDirectiveTrivia(statements[i - 1]))
                {
                    StatementSyntax statement = statements[i];
                    statement = statement.WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(newline)).AddRange(statement.GetLeadingTrivia()));
                    statements = statements.Replace(statements[i], statement);
                }
            }
            return node.WithStatements(statements);
        }

        private static bool IsFlow(StatementSyntax statement) => statement is IfStatementSyntax or SwitchStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax or TryStatementSyntax or UsingStatementSyntax or LockStatementSyntax;
        private static bool HasBlankBoundary(StatementSyntax previous, StatementSyntax current)
        {
            string boundaryTrivia = previous.GetTrailingTrivia().ToFullString() + current.GetLeadingTrivia().ToFullString();
            return Regex.IsMatch(boundaryTrivia, @"(?:\r\n|\r|\n)[ \t]*(?:\r\n|\r|\n)", RegexOptions.CultureInvariant);
        }

        private static bool HasProtectedTrivia(SyntaxNode node) => node.DescendantTrivia(descendIntoTrivia: true)
            .Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)
                || t.IsKind(SyntaxKind.DisabledTextTrivia)
                || t.HasStructure && t.GetStructure() is DocumentationCommentTriviaSyntax or DirectiveTriviaSyntax);

        private static bool HasDirectiveTrivia(SyntaxNode node) => node.DescendantTrivia(descendIntoTrivia: true)
            .Any(t => t.IsKind(SyntaxKind.DisabledTextTrivia)
                || t.HasStructure && t.GetStructure() is DirectiveTriviaSyntax);

        private SyntaxTriviaList NewLineIndent(string indentation) => SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(newline), SyntaxFactory.Whitespace(indentation));

        private SyntaxTriviaList IndentTrivia(SyntaxNode node, SyntaxTriviaList existing)
        {
            if (existing.Any(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                return existing;
            }

            string indent = new(' ', (IndentDepth(node) + 1) * 4);
            return SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(newline), SyntaxFactory.Whitespace(indent));
        }

        private static int IndentDepth(SyntaxNode node) => node.Ancestors().Count(ancestor => ancestor is TypeDeclarationSyntax or NamespaceDeclarationSyntax
            or FileScopedNamespaceDeclarationSyntax or BlockSyntax or SwitchSectionSyntax);
    }

    private sealed class BlankLineRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            token = base.VisitToken(token);
            if (token.Parent is not null && Retained(token.Parent, "readability.comments"))
            {
                return token;
            }

            return token.WithLeadingTrivia(Compact(token.LeadingTrivia)).WithTrailingTrivia(Compact(token.TrailingTrivia));
        }

        private static SyntaxTriviaList Compact(SyntaxTriviaList trivia)
        {
            if (trivia.Any(item => !item.IsKind(SyntaxKind.EndOfLineTrivia) && !item.IsKind(SyntaxKind.WhitespaceTrivia)))
            {
                return trivia;
            }

            SyntaxTrivia[] endings = trivia.Where(item => item.IsKind(SyntaxKind.EndOfLineTrivia)).ToArray();
            if (endings.Length <= 2)
            {
                return trivia;
            }

            SyntaxTrivia? indentation = trivia.LastOrDefault(item => item.IsKind(SyntaxKind.WhitespaceTrivia));
            var compact = SyntaxFactory.TriviaList(endings[^2], endings[^1]);
            if (indentation is { RawKind: not 0 })
            {
                compact = compact.Add(indentation.Value);
            }
            return compact;
        }
    }

}
