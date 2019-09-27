using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpFM.Core
{
    public static class FileMakerClipExtensions
    {
        /// <summary>
        /// Create a class from scratch.
        /// </summary>
        public static string CreateClass(this FileMakerClip _clip, FileMakerClip fieldProjectionLayout = null)
        {
            var fieldProjectionList = new List<string>();
            if (fieldProjectionLayout != null && fieldProjectionLayout.ClipboardFormat == "Layout")
            {
                // a clip that is of type layout, only has name attribute (since the rest isn't available)
                // and we only need the name to skip it down below
                fieldProjectionList.AddRange(fieldProjectionLayout.Fields.Select(f => f.Name));
            }
            else
            {
                // otherwise include all fields
                fieldProjectionList.AddRange(_clip.Fields.Select(f => f.Name));
            }

            return _clip.CreateClass(fieldProjectionList);
        }

        /// <summary>
        /// Create a class from scratch.
        /// </summary>
        public static string CreateClass(this FileMakerClip _clip, IEnumerable<string> fieldProjectionList)
        {
            // Create a namespace: (namespace CodeGenerationSample)
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("SharpFM.CodeGen")).NormalizeWhitespace();

            // Add System using statement: (using System)
            @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));

            //  Create a class: (class [_clip.Name])
            var classDeclaration = SyntaxFactory.ClassDeclaration(_clip.Name);

            // Add the public modifier: (public class [_clip.Name])
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            // add each field from the underling _clip as a public property with the data member attribute
            List<PropertyDeclarationSyntax> fieldsToBeAddedAsProperties = new List<PropertyDeclarationSyntax>(_clip.Fields.Count());
            // include the field projection
            foreach (var field in _clip.Fields.Where(fmF => fieldProjectionList.Contains(fmF.Name)))
            {
                // filemaker to C# data type mapping
                var propertyTypeCSharp = field.DataType
                    .Replace("Text", "string")
                    .Replace("Number", "int")
                    .Replace("Binary", "byte[]")
                    .Replace("Date", "DateTime")
                    .Replace("Time", "TimeStamp")
                    .Replace("TimeStamp", "DateTime");

                var propertyTypeSyntax = SyntaxFactory.ParseTypeName(propertyTypeCSharp);

                var dataMemberAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("DataMember"));

                var propertyDeclaration = SyntaxFactory.PropertyDeclaration(propertyTypeSyntax, field.Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                .NormalizeWhitespace()
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(dataMemberAttribute)));

                fieldsToBeAddedAsProperties.Add(propertyDeclaration);
            }

            // Add the field, the property and method to the class.
            classDeclaration = classDeclaration.AddMembers(fieldsToBeAddedAsProperties.ToArray());

            // Add the class to the namespace.
            @namespace = @namespace.AddMembers(classDeclaration);

            // Normalize and get code as string.
            var code = @namespace.NormalizeWhitespace().ToFullString();

            // Output new code to the console.
            return code;
        }
    }
}
