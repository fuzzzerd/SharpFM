using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Core
{
    public class LayoutAsFMDataModel
    {
        private readonly FileMakerClip _clip;

        public LayoutAsFMDataModel(FileMakerClip clip)
        {
            _clip = clip;
        }

        /// <summary>
        /// Create a class from scratch.
        /// </summary>
        public string CreateClass()
        {
            // Create a namespace: (namespace CodeGenerationSample)
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("SharpFM.CodeGen")).NormalizeWhitespace();

            // Add System using statement: (using System)
            @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));

            //  Create a class: (class Order)
            var classDeclaration = SyntaxFactory.ClassDeclaration(_clip.Name);

            // Add the public modifier: (public class Order)
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            //// Inherit BaseEntity<T> and implement IHaveIdentity: (public class Order : BaseEntity<T>, IHaveIdentity)
            //classDeclaration = classDeclaration.AddBaseListTypes(
            //    SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("BaseEntity<Order>")),
            //    SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IHaveIdentity")));

            // Create a string variable: (bool canceled;)
            var variableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("bool"))
                .AddVariables(SyntaxFactory.VariableDeclarator("canceled"));

            // Create a field declaration: (private bool canceled;)
            var fieldDeclaration = SyntaxFactory.FieldDeclaration(variableDeclaration)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

            List<PropertyDeclarationSyntax> fieldsToBeAddedAsProperties = new List<PropertyDeclarationSyntax>(_clip.Fields.Count());
            foreach(var field in _clip.Fields)
            {
                var propertyTypeCSharp = field.DataType.Replace("Text", "string").Replace("Number", "int");
                var propertyTypeSyntax = SyntaxFactory.ParseTypeName("int");

                var propertyDeclaration = SyntaxFactory.PropertyDeclaration(propertyTypeSyntax, field.Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
                
                fieldsToBeAddedAsProperties.Add(propertyDeclaration);
            }

            //// Create a Property: (public int Quantity { get; set; })
            //var propertyDeclaration = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("int"), "Quantity")
            //    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            //    .AddAccessorListAccessors(
            //        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
            //        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            // Create a stament with the body of a method.
            var syntax = SyntaxFactory.ParseStatement("canceled = true;");

            // Create a method
            var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "MarkAsCanceled")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(SyntaxFactory.Block(syntax));

            // Add the field, the property and method to the class.
            classDeclaration = classDeclaration.AddMembers(fieldDeclaration, methodDeclaration).AddMembers(fieldsToBeAddedAsProperties.ToArray());

            // Add the class to the namespace.
            @namespace = @namespace.AddMembers(classDeclaration);

            // Normalize and get code as string.
            var code = @namespace.NormalizeWhitespace().ToFullString();

            // Output new code to the console.
            return code;
        }
    }
}
