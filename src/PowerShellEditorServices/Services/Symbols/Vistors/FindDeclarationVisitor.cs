﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.EditorServices.Services.Symbols
{
    /// <summary>
    /// The visitor used to find the definition of a symbol
    /// </summary>
    internal class FindDeclarationVisitor : AstVisitor2
    {
        private readonly SymbolReference symbolRef;
        private readonly string variableName;

        public SymbolReference FoundDeclaration { get; private set; }

        public FindDeclarationVisitor(SymbolReference symbolRef)
        {
            this.symbolRef = symbolRef;
            if (this.symbolRef.SymbolType == SymbolType.Variable)
            {
                // converts `$varName` to `varName` or of the form ${varName} to varName
                variableName = symbolRef.SymbolName.TrimStart('$').Trim('{', '}');
            }
        }

        /// <summary>
        /// Decides if the current function definition is the right definition
        /// for the symbol being searched for. The definition of the symbol will be a of type
        /// SymbolType.Function and have the same name as the symbol
        /// </summary>
        /// <param name="functionDefinitionAst">A FunctionDefinitionAst in the script's AST</param>
        /// <returns>A decision to stop searching if the right FunctionDefinitionAst was found,
        /// or a decision to continue if it wasn't found</returns>
        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
        {
            // Extent for constructors and method trigger both this and VisitFunctionMember(). Covered in the latter.
            // This will not exclude nested functions as they have ScriptBlockAst as parent
            if (functionDefinitionAst.Parent is FunctionMemberAst)
            {
                return AstVisitAction.Continue;
            }

            // Get the start column number of the function name,
            // instead of the the start column of 'function' and create new extent for the functionName
            int startColumnNumber =
                functionDefinitionAst.Extent.Text.IndexOf(
                    functionDefinitionAst.Name, StringComparison.OrdinalIgnoreCase) + 1;

            IScriptExtent nameExtent = new ScriptExtent()
            {
                Text = functionDefinitionAst.Name,
                StartLineNumber = functionDefinitionAst.Extent.StartLineNumber,
                StartColumnNumber = startColumnNumber,
                EndLineNumber = functionDefinitionAst.Extent.StartLineNumber,
                EndColumnNumber = startColumnNumber + functionDefinitionAst.Name.Length,
                File = functionDefinitionAst.Extent.File
            };

            // We compare to the SymbolName instead of its text because it may have been resolved
            // from an alias.
            if (symbolRef.SymbolType.Equals(SymbolType.Function) &&
                nameExtent.Text.Equals(symbolRef.SymbolName, StringComparison.CurrentCultureIgnoreCase))
            {
                FoundDeclaration =
                    new SymbolReference(
                        SymbolType.Function,
                        nameExtent);

                return AstVisitAction.StopVisit;
            }

            return base.VisitFunctionDefinition(functionDefinitionAst);
        }

        /// <summary>
        /// Decides if the current type definition is the right definition
        /// for the symbol being searched for. The definition of the symbol will be a of type
        /// SymbolType.Enum or SymbolType.Class and have the same name as the symbol
        /// </summary>
        /// <param name="typeDefinitionAst">A TypeDefinitionAst in the script's AST</param>
        /// <returns>A decision to stop searching if the right TypeDefinitionAst was found,
        /// or a decision to continue if it wasn't found</returns>
        public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst)
        {
            SymbolType symbolType =
                typeDefinitionAst.IsEnum ?
                    SymbolType.Enum : SymbolType.Class;

            if ((symbolRef.SymbolType is SymbolType.Type || symbolRef.SymbolType.Equals(symbolType)) &&
                typeDefinitionAst.Name.Equals(symbolRef.SymbolName, StringComparison.CurrentCultureIgnoreCase))
            {
                // Show only type name. Offset by StartColumn to include indentation etc.
                int startColumnNumber =
                    typeDefinitionAst.Extent.StartColumnNumber +
                    typeDefinitionAst.Extent.Text.IndexOf(typeDefinitionAst.Name);

                IScriptExtent nameExtent = new ScriptExtent()
                {
                    Text = typeDefinitionAst.Name,
                    StartLineNumber = typeDefinitionAst.Extent.StartLineNumber,
                    EndLineNumber = typeDefinitionAst.Extent.StartLineNumber,
                    StartColumnNumber = startColumnNumber,
                    EndColumnNumber = startColumnNumber + typeDefinitionAst.Name.Length,
                    File = typeDefinitionAst.Extent.File
                };

                FoundDeclaration =
                    new SymbolReference(
                        symbolType,
                        nameExtent);

                return AstVisitAction.StopVisit;
            }

            return AstVisitAction.Continue;
        }

        /// <summary>
        /// Decides if the current function member is the right definition
        /// for the symbol being searched for. The definition of the symbol will be a of type
        /// SymbolType.Constructor or SymbolType.Method and have the same name as the symbol
        /// </summary>
        /// <param name="functionMemberAst">A FunctionMemberAst in the script's AST</param>
        /// <returns>A decision to stop searching if the right FunctionMemberAst was found,
        /// or a decision to continue if it wasn't found</returns>
        public override AstVisitAction VisitFunctionMember(FunctionMemberAst functionMemberAst)
        {
            SymbolType symbolType =
                functionMemberAst.IsConstructor ?
                    SymbolType.Constructor : SymbolType.Method;

            if (symbolRef.SymbolType.Equals(symbolType) &&
                functionMemberAst.Name.Equals(symbolRef.SymbolName, StringComparison.CurrentCultureIgnoreCase))
            {
                // Show only method/ctor name. Offset by StartColumn to include indentation etc.
                int startColumnNumber =
                    functionMemberAst.Extent.StartColumnNumber +
                    functionMemberAst.Extent.Text.IndexOf(functionMemberAst.Name);

                IScriptExtent nameExtent = new ScriptExtent()
                {
                    Text = functionMemberAst.Name,
                    StartLineNumber = functionMemberAst.Extent.StartLineNumber,
                    EndLineNumber = functionMemberAst.Extent.StartLineNumber,
                    StartColumnNumber = startColumnNumber,
                    EndColumnNumber = startColumnNumber + functionMemberAst.Name.Length,
                    File = functionMemberAst.Extent.File
                };

                FoundDeclaration =
                    new SymbolReference(
                        symbolType,
                        nameExtent);

                return AstVisitAction.StopVisit;
            }

            return AstVisitAction.Continue;
        }

        /// <summary>
        /// Decides if the current property member is the right definition
        /// for the symbol being searched for. The definition of the symbol will be a of type
        /// SymbolType.Property and have the same name as the symbol
        /// </summary>
        /// <param name="propertyMemberAst">A PropertyMemberAst in the script's AST</param>
        /// <returns>A decision to stop searching if the right PropertyMemberAst was found,
        /// or a decision to continue if it wasn't found</returns>
        public override AstVisitAction VisitPropertyMember(PropertyMemberAst propertyMemberAst)
        {
            if (symbolRef.SymbolType.Equals(SymbolType.Property) &&
                propertyMemberAst.Name.Equals(symbolRef.SymbolName, StringComparison.CurrentCultureIgnoreCase))
            {
                FoundDeclaration =
                    new SymbolReference(
                        SymbolType.Property,
                        propertyMemberAst.Extent);

                return AstVisitAction.StopVisit;
            }

            return AstVisitAction.Continue;
        }

        /// <summary>
        /// Decides if the current configuration definition is the right definition
        /// for the symbol being searched for. The definition of the symbol will be a of type
        /// SymbolType.Configuration and have the same name as the symbol
        /// </summary>
        /// <param name="configurationDefinitionAst">A ConfigurationDefinitionAst in the script's AST</param>
        /// <returns>A decision to stop searching if the right ConfigurationDefinitionAst was found,
        /// or a decision to continue if it wasn't found</returns>
        public override AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst)
        {
            string configurationName = configurationDefinitionAst.InstanceName.Extent.Text;

            if (symbolRef.SymbolType.Equals(SymbolType.Configuration) &&
                configurationName.Equals(symbolRef.SymbolName, StringComparison.CurrentCultureIgnoreCase))
            {
                // Show only configuration name. Offset by StartColumn to include indentation etc.
                int startColumnNumber =
                    configurationDefinitionAst.Extent.StartColumnNumber +
                    configurationDefinitionAst.Extent.Text.IndexOf(configurationName);

                IScriptExtent nameExtent = new ScriptExtent()
                {
                    Text = configurationName,
                    StartLineNumber = configurationDefinitionAst.Extent.StartLineNumber,
                    EndLineNumber = configurationDefinitionAst.Extent.StartLineNumber,
                    StartColumnNumber = startColumnNumber,
                    EndColumnNumber = startColumnNumber + configurationName.Length,
                    File = configurationDefinitionAst.Extent.File
                };

                FoundDeclaration =
                    new SymbolReference(
                        SymbolType.Configuration,
                        nameExtent);

                return AstVisitAction.StopVisit;
            }

            return AstVisitAction.Continue;
        }

        /// <summary>
        /// Check if the left hand side of an assignmentStatementAst is a VariableExpressionAst
        /// with the same name as that of symbolRef.
        /// </summary>
        /// <param name="assignmentStatementAst">An AssignmentStatementAst</param>
        /// <returns>A decision to stop searching if the right VariableExpressionAst was found,
        /// or a decision to continue if it wasn't found</returns>
        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
        {
            if (variableName == null)
            {
                return AstVisitAction.Continue;
            }

            // We want to check VariableExpressionAsts from within this AssignmentStatementAst so we visit it.
            FindDeclarationVariableExpressionVisitor visitor = new(symbolRef);
            assignmentStatementAst.Left.Visit(visitor);

            if (visitor.FoundDeclaration != null)
            {
                FoundDeclaration = visitor.FoundDeclaration;
                return AstVisitAction.StopVisit;
            }
            return AstVisitAction.Continue;
        }

        /// <summary>
        /// The private visitor used to find the variable expression that matches a symbol
        /// </summary>
        private class FindDeclarationVariableExpressionVisitor : AstVisitor
        {
            private readonly SymbolReference symbolRef;
            private readonly string variableName;

            public SymbolReference FoundDeclaration { get; private set; }

            public FindDeclarationVariableExpressionVisitor(SymbolReference symbolRef)
            {
                this.symbolRef = symbolRef;
                if (this.symbolRef.SymbolType == SymbolType.Variable)
                {
                    // converts `$varName` to `varName` or of the form ${varName} to varName
                    variableName = symbolRef.SymbolName.TrimStart('$').Trim('{', '}');
                }
            }

            /// <summary>
            /// Check if the VariableExpressionAst has the same name as that of symbolRef.
            /// </summary>
            /// <param name="variableExpressionAst">A VariableExpressionAst</param>
            /// <returns>A decision to stop searching if the right VariableExpressionAst was found,
            /// or a decision to continue if it wasn't found</returns>
            public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst)
            {
                if (variableExpressionAst.VariablePath.UserPath.Equals(variableName, StringComparison.OrdinalIgnoreCase))
                {
                    // TODO also find instances of set-variable
                    FoundDeclaration = new SymbolReference(SymbolType.Variable, variableExpressionAst.Extent);
                    return AstVisitAction.StopVisit;
                }
                return AstVisitAction.Continue;
            }

            public override AstVisitAction VisitMemberExpression(MemberExpressionAst functionDefinitionAst) =>
                // We don't want to discover any variables in member expressisons (`$something.Foo`)
                AstVisitAction.SkipChildren;

            public override AstVisitAction VisitIndexExpression(IndexExpressionAst functionDefinitionAst) =>
                // We don't want to discover any variables in index expressions (`$something[0]`)
                AstVisitAction.SkipChildren;
        }
    }
}
