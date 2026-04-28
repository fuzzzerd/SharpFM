using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SharpFM.Model;
using SharpFM.Model.Parsing;
using SharpFM.Model.Schema;

namespace SharpFM;

/// <summary>
/// Code-generation helpers operating on a parsed <see cref="Clip"/>. Today
/// only table clips can produce a class; layouts and scripts throw
/// <see cref="NotSupportedException"/>.
/// </summary>
public static class ClipCodeGenExtensions
{
    /// <summary>
    /// Generate a C# class with one <c>[DataMember]</c> property per field
    /// in this table clip. Throws if the clip isn't a parsed table.
    /// </summary>
    public static string CreateClass(this Clip clip)
    {
        if (clip.Parsed is not ParseSuccess { Model: TableClipModel tableModel })
        {
            throw new NotSupportedException(
                "Code generation is only supported for table clips (Mac-XMTB / Mac-XMFD).");
        }

        var table = tableModel.Table;
        return CreateClassFromTable(table, table.Fields.Select(f => f.Name));
    }

    /// <summary>
    /// Generate a C# class for a table, projecting only the named fields.
    /// </summary>
    public static string CreateClass(this Clip clip, IEnumerable<string> fieldProjectionList)
    {
        if (clip.Parsed is not ParseSuccess { Model: TableClipModel tableModel })
        {
            throw new NotSupportedException(
                "Code generation is only supported for table clips (Mac-XMTB / Mac-XMFD).");
        }

        return CreateClassFromTable(tableModel.Table, fieldProjectionList);
    }

    private static string CreateClassFromTable(FmTable table, IEnumerable<string> fieldProjectionList)
    {
        var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("SharpFM.CodeGen")).NormalizeWhitespace();
        @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));
        @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.Serialization")));

        var dataContractAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("DataContract"));
        var classDeclaration = SyntaxFactory.ClassDeclaration(table.Name);
        classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        classDeclaration = classDeclaration.AddAttributeLists(
            SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(dataContractAttribute)));

        var projection = new HashSet<string>(fieldProjectionList, StringComparer.Ordinal);

        var properties = new List<PropertyDeclarationSyntax>();
        foreach (var field in table.Fields.Where(f => projection.Contains(f.Name)))
        {
            var propertyType = MapFieldDataType(field);
            var propertyTypeSyntax = SyntaxFactory.ParseTypeName(propertyType);
            var dataMemberAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("DataMember"));

            var propertyDeclaration = SyntaxFactory.PropertyDeclaration(propertyTypeSyntax, field.Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                    .NormalizeWhitespace(indentation: "", eol: " ")
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(dataMemberAttribute)))
                .NormalizeWhitespace();

            properties.Add(propertyDeclaration);
        }

        classDeclaration = classDeclaration.AddMembers(properties.ToArray());
        @namespace = @namespace.AddMembers(classDeclaration);

        return @namespace.NormalizeWhitespace().ToFullString().FormatAutoPropertiesOnOneLine();
    }

    private static string MapFieldDataType(FmField field)
    {
        var raw = field.DataType.ToString();
        var clr = raw switch
        {
            "Text" => "string",
            "Number" => "int",
            "Binary" => "byte[]",
            "Date" => "DateTime",
            "Time" => "TimeSpan",
            "TimeStamp" => "DateTime",
            _ => "string",
        };

        if (!field.NotEmpty && clr != "string")
        {
            clr += "?";
        }
        return clr;
    }

    private static readonly Regex AutoPropRegex = new(@"\s*\{\s*get;\s*set;\s*}\s");

    private static string FormatAutoPropertiesOnOneLine(this string str) =>
        AutoPropRegex.Replace(str, " { get; set; }");
}
