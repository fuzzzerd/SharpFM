using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpFM.Core
{
    public static class FileMakerClipExtensions
    {
        /// <summary>
        /// Create a class from scratch.
        /// </summary>
        public static string CreateClass(this FileMakerClip _clip, FileMakerClip fieldProjectionLayout = null)
        {
            if(_clip == null) { return string.Empty; }

            var fieldProjectionList = new List<string>();
            if (fieldProjectionLayout != null && FileMakerClip.ClipTypes[fieldProjectionLayout.ClipboardFormat] == "Layout")
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
            @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Runtime.Serialization")));

            var dataContractAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("DataContract"));

            //  Create a class: (class [_clip.Name])
            var classDeclaration = SyntaxFactory.ClassDeclaration(_clip.Name);

            // Add the public modifier: (public class [_clip.Name])
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            classDeclaration = classDeclaration.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(dataContractAttribute)));

            // add each field from the underling _clip as a public property with the data member attribute
            List <PropertyDeclarationSyntax> fieldsToBeAddedAsProperties = new List<PropertyDeclarationSyntax>(_clip.Fields.Count());
            // include the field projection
            foreach (var field in _clip.Fields.Where(fmF => fieldProjectionList.Contains(fmF.Name)))
            {
                // filemaker to C# data type mapping
                var propertyTypeCSharp = string.Empty;

                switch (field.DataType)
                {
                    case "Text":
                        propertyTypeCSharp = "string";
                        break;
                    case "Number":
                        propertyTypeCSharp = "int";
                        break;
                    case "Binary":
                        propertyTypeCSharp = "byte[]";
                        break;
                    case "Date":
                        propertyTypeCSharp = "DateTime";
                        break;
                    case "Time":
                        propertyTypeCSharp = "TimeSpan";
                        break;
                    case "TimeStamp":
                        propertyTypeCSharp = "DateTime";
                        break;
                    default:
                        propertyTypeCSharp = "string";
                        break;
                }

                if(field.NotEmpty == false && propertyTypeCSharp != "string")
                {
                    propertyTypeCSharp += "?";
                }

                var propertyTypeSyntax = SyntaxFactory.ParseTypeName(propertyTypeCSharp);

                var dataMemberAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("DataMember"));

                var propertyDeclaration = SyntaxFactory.PropertyDeclaration(propertyTypeSyntax, field.Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                    .NormalizeWhitespace(indentation: "", eol: " ")
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(dataMemberAttribute)))
                .NormalizeWhitespace();

                fieldsToBeAddedAsProperties.Add(propertyDeclaration);
            }

            // Add the field, the property and method to the class.
            classDeclaration = classDeclaration.AddMembers(fieldsToBeAddedAsProperties.ToArray());

            // Add the class to the namespace.
            @namespace = @namespace.AddMembers(classDeclaration);

            // Normalize and get code as string.
            var code = @namespace.NormalizeWhitespace().ToFullString().FormatAutoPropertiesOnOneLine();

            // Output new code to the console.
            return code;
        }


        /// <summary>
        /// https://stackoverflow.com/a/52339795/86860
        /// </summary>
        private static readonly Regex AutoPropRegex = new Regex(@"\s*\{\s*get;\s*set;\s*}\s");

        /// <summary>
        /// https://stackoverflow.com/a/52339795/86860
        /// </summary>
        /// <param name="str">Code string to format.</param>
        /// <returns>The code string with auto properties formatted to a single line</returns>
        private static string FormatAutoPropertiesOnOneLine(this string str)
        {
            return AutoPropRegex.Replace(str, " { get; set; }");
        }
    }
}
