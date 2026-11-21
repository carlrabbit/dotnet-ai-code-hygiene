using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNetAiWorkbench.Autoformat.Core;

/// <summary>Opinionated, source-only C# canonicalizations. This deliberately does not compile consumer projects.</summary>
internal static class CSharpCanonicalizer
{
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
        string newline = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        parseOptions ??= new CSharpParseOptions(LanguageVersion.Default);
        var tree = CSharpSyntaxTree.ParseText(source, parseOptions);
        SyntaxNode root = tree.GetRoot();
        ValidateRetains(root, messages);
        bool supportsCSharp12 = parseOptions.LanguageVersion >= LanguageVersion.CSharp12;
        SyntaxNode rewritten = new CanonicalRewriter(supportsCSharp12).Visit(root)!;
        rewritten = new CanonicalRewriter(supportsCSharp12).Visit(rewritten)!;
        rewritten = new LayoutRewriter(newline).Visit(rewritten)!;
        rewritten = new BlankLineRewriter().Visit(rewritten)!;
        string result = rewritten.ToFullString();
        int remainingLongLines = Microsoft.CodeAnalysis.Text.SourceText.From(result).Lines
            .Count(line => line.Span.Length > 120 && !result[line.Start..line.End].TrimStart().StartsWith("//", StringComparison.Ordinal));
        if (remainingLongLines > 0)
        {
            messages.Add($"{remainingLongLines} source line(s) remain over the 120-character soft target where no supported syntax wrapping boundary was applied.");
        }
        notices = messages.Distinct(StringComparer.Ordinal).ToArray();
        return result;
    }

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
        SyntaxTriviaList trivia = node.GetLeadingTrivia();
        string? text = trivia.LastOrDefault(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia)).ToString();
        if (text is null)
        {
            return false;
        }

        Match match = Regex.Match(text, @"^\s*//\s*autoformat:retain\s+(?<id>[a-z0-9.-]+)\s+--\s+(?<reason>\S.*\S|\S)\s*$", RegexOptions.CultureInvariant);
        return match.Success && match.Groups["id"].Value == id;
    }

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
            if (!supportsCSharp12 || node.Initializer is null || HasComments(node) || HasUntargetedVar(node) || Retained(node, "modernization.collection-expression"))
            {
                return node;
            }

            return SyntaxFactory.ParseExpression("[" + string.Join(", ", node.Initializer.Expressions.Select(e => e.ToString())) + "]").WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
        {
            node = (ImplicitArrayCreationExpressionSyntax)base.VisitImplicitArrayCreationExpression(node)!;
            if (!supportsCSharp12 || HasComments(node) || HasUntargetedVar(node) || Retained(node, "modernization.collection-expression"))
            {
                return node;
            }

            return SyntaxFactory.ParseExpression("[" + string.Join(", ", node.Initializer.Expressions.Select(e => e.ToString())) + "]").WithTriviaFrom(node);
        }

        public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            node = (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node)!;
            if (supportsCSharp12 && node.Initializer is { Expressions.Count: > 0 } init && !HasComments(node) && !HasUntargetedVar(node) && Retained(node, "modernization.collection-expression") == false
                && (node.Type.ToString().StartsWith("List<", StringComparison.Ordinal) || node.Type.ToString().EndsWith("[]", StringComparison.Ordinal)))
            {
                return SyntaxFactory.ParseExpression("[" + string.Join(", ", init.Expressions.Select(e => e.ToString())) + "]").WithTriviaFrom(node);
            }

            return node;
        }

        public override SyntaxNode? VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
        {
            node = (ImplicitObjectCreationExpressionSyntax)base.VisitImplicitObjectCreationExpression(node)!;
            if (supportsCSharp12 && node.Initializer is { Expressions.Count: > 0 } init && !HasComments(node) && !HasUntargetedVar(node) && !Retained(node, "modernization.collection-expression"))
            {
                return SyntaxFactory.ParseExpression("[" + string.Join(", ", init.Expressions.Select(e => e.ToString())) + "]").WithTriviaFrom(node);
            }

            return node;
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
            if (Retained(node, "modernization.expression-body") || node.ExpressionBody is not null || node.Body is null || node.Body.Statements.Count != 1)
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
            if (HasComments(node) || Retained(node, "modernization.switch-expression") || node.Sections.Any(s => s.Labels.Count != 1 || s.Statements.Count != 1))
            {
                return node;
            }

            var arms = new List<string>();
            bool returns = true;
            foreach (SwitchSectionSyntax section in node.Sections)
            {
                string? value = section.Statements[0] switch { ReturnStatementSyntax { Expression: not null } r => r.Expression.ToString(), _ => null };
                if (value is null)
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
            ExpressionSyntax converted = SyntaxFactory.ParseExpression(node.Expression + " switch { " + string.Join(", ", arms) + " }");
            return returns ? SyntaxFactory.ReturnStatement(converted).WithTriviaFrom(node) : node;
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
            if (Retained(ctor, "modernization.primary-constructor") || HasComments(ctor) || ctor.Initializer is not null
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
            return type.WithParameterList(SyntaxFactory.ParameterList(ctor.ParameterList.Parameters)).WithMembers(SyntaxFactory.List(members));
        }

        private static bool HasComments(SyntaxNode node) => node.DescendantTrivia(descendIntoTrivia: true)
            .Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

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
            if ((node.Parameters.Count < 3 && node.Parameters.ToString().Length < 100) || HasComments(node))
            {
                return node;
            }

            SeparatedSyntaxList<ParameterSyntax> parameters = node.Parameters;
            for (int i = 0; i < parameters.SeparatorCount; i++)
            {
                SyntaxToken separator = parameters.GetSeparator(i);
                parameters = parameters.ReplaceSeparator(separator, separator.WithTrailingTrivia(IndentTrivia(node, separator.TrailingTrivia)));
            }

            return node.WithParameters(parameters);
        }

        public override SyntaxNode? VisitArgumentList(ArgumentListSyntax node)
        {
            node = (ArgumentListSyntax)base.VisitArgumentList(node)!;
            if (node.ToString().Length < 120 || HasComments(node))
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
            if (node.ToString().Length < 120 || HasComments(node))
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
            if (node.ToString().Length < 120 || HasComments(node))
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
            if ((!node.IsKind(SyntaxKind.LogicalAndExpression) && !node.IsKind(SyntaxKind.LogicalOrExpression)) || node.Span.Length < 120 || HasComments(node))
            {
                return node;
            }

            return node.WithOperatorToken(node.OperatorToken.WithTrailingTrivia(IndentTrivia(node, node.OperatorToken.TrailingTrivia)));
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            node = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node)!;
            if (node.Parent is not InvocationExpressionSyntax invocation || invocation.Span.Length < 120 || HasComments(invocation))
            {
                return node;
            }

            return node.WithOperatorToken(node.OperatorToken.WithLeadingTrivia(IndentTrivia(node, node.OperatorToken.LeadingTrivia)));
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

                if (statements.Skip(i + 1).SelectMany(s => s.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                    .Count(id => id.Identifier.ValueText == variable.Identifier.ValueText) != 1 || HasComments(local) || HasComments(statements[i + 1]))
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
                if (previousFlow == currentFlow || (statements[i] is LocalFunctionStatementSyntax) || HasBlankBoundary(statements[i]))
                {
                    continue;
                }

                if (currentFlow || previousFlow)
                {
                    StatementSyntax statement = statements[i];
                    statement = statement.WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(newline)).AddRange(statement.GetLeadingTrivia()));
                    statements = statements.Replace(statements[i], statement);
                }
            }
            return node.WithStatements(statements);
        }

        private static bool IsFlow(StatementSyntax statement) => statement is IfStatementSyntax or SwitchStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax or TryStatementSyntax or UsingStatementSyntax or LockStatementSyntax;
        private static bool HasBlankBoundary(StatementSyntax statement) => statement.GetLeadingTrivia().ToFullString().Contains("\n\n", StringComparison.Ordinal) || statement.GetLeadingTrivia().ToFullString().Contains("\r\n\r\n", StringComparison.Ordinal);

        private static bool HasComments(SyntaxNode node) => node.DescendantTrivia(descendIntoTrivia: true)
            .Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

        private SyntaxTriviaList IndentTrivia(SyntaxNode node, SyntaxTriviaList existing)
        {
            if (existing.Any(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                return existing;
            }

            int depth = node.Ancestors().Count(ancestor => ancestor is TypeDeclarationSyntax or NamespaceDeclarationSyntax
                or FileScopedNamespaceDeclarationSyntax or BlockSyntax or SwitchSectionSyntax);
            string indent = new(' ', (depth + 1) * 4);
            return SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(newline), SyntaxFactory.Whitespace(indent));
        }
    }

    private sealed class BlankLineRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            token = base.VisitToken(token);
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
