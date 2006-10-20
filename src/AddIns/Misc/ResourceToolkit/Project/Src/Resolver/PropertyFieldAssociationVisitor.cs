﻿// <file>
//     <copyright see="prj:///Doc/copyright.txt"/>
//     <license see="prj:///Doc/license.txt"/>
//     <owner name="Christian Hornung" email="c-hornung@gmx.de"/>
//     <version>$Revision$</version>
// </file>

using System;

using ICSharpCode.Core;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Dom;

namespace Hornung.ResourceToolkit.Resolver
{
	/// <summary>
	/// Finds connections between fields and properties.
	/// TODO: This currently only works if the field and the property are declared in the same source file.
	/// </summary>
	public class PropertyFieldAssociationVisitor : PositionTrackingAstVisitor
	{
		IMember memberToFind;
		IMember associatedMember;
		
		/// <summary>
		/// Gets the field that has been found to be associated with the property specified at the constructor call.
		/// </summary>
		public IField AssociatedField {
			get {
				return this.associatedMember as IField;
			}
		}
		
		/// <summary>
		/// Gets the property that has been found to be associated with the field specified at the constructor call.
		/// </summary>
		public IProperty AssociatedProperty {
			get {
				return this.associatedMember as IProperty;
			}
		}
		
		// ********************************************************************************************************************************
		
		private enum VisitorContext
		{
			Default,
			PropertyGetRegion,
			PropertySetRegion
		}
		
		private VisitorContext currentContext = VisitorContext.Default;
		
		protected override void BeginVisit(INode node)
		{
			base.BeginVisit(node);
			if (node is PropertyGetRegion) {
				this.currentContext = VisitorContext.PropertyGetRegion;
			} else if (node is PropertySetRegion) {
				this.currentContext = VisitorContext.PropertySetRegion;
			}
		}
		
		protected override void EndVisit(INode node)
		{
			if (node is PropertyGetRegion || node is PropertySetRegion) {
				this.currentContext = VisitorContext.Default;
			}
			base.EndVisit(node);
		}
		
		public override object TrackedVisit(PropertyDeclaration propertyDeclaration, object data)
		{
			
			if (this.memberToFind is IProperty) {
				
				// If we are looking for a specified property:
				// find out if this is the one by comparing the location.
				if (propertyDeclaration.StartLocation.X == this.memberToFind.Region.BeginColumn &&
				    propertyDeclaration.StartLocation.Y == this.memberToFind.Region.BeginLine) {
					data = true;
				}
				
			} else if (this.memberToFind is IField) {
				
				// If we are looking for a specifield field:
				// store the property info for future reference.
				data = propertyDeclaration;
				
			}
			
			return base.TrackedVisit(propertyDeclaration, data);
		}
		
		public override object TrackedVisit(ReturnStatement returnStatement, object data)
		{
			// If we are in a property get region,
			// this may be the statement where the field value is returned.
			if (this.associatedMember == null &&	// skip if already found to improve performance
			    this.currentContext == VisitorContext.PropertyGetRegion && data != null) {
				
				// Fix some type casting and parenthesized expressions
				Expression expr = returnStatement.Expression;
				while (true) {
					CastExpression ce = expr as CastExpression;
					if (ce != null) {
						expr = ce.Expression;
						continue;
					}
					ParenthesizedExpression pe = expr as ParenthesizedExpression;
					if (pe != null) {
						expr = pe.Expression;
						continue;
					}
					break;
				}
				
				// Resolve the expression.
				MemberResolveResult mrr = this.Resolve(expr, this.memberToFind.DeclaringType.CompilationUnit.FileName) as MemberResolveResult;
				if (mrr != null && mrr.ResolvedMember is IField) {
					
					PropertyDeclaration pd;
					
					#if DEBUG
					LoggingService.Debug("ResourceToolkit: PropertyFieldAssociationVisitor, inside PropertyGetRegion, resolved field: "+mrr.ResolvedMember.ToString());
					#endif
					
					if (data as bool? ?? false) {
						
						// We are looking for this property.
						#if DEBUG
						LoggingService.Debug("ResourceToolkit: PropertyFieldAssociationVisitor, inside PropertyGetRegion, this property seems to reference field "+mrr.ResolvedMember.ToString());
						#endif
						this.associatedMember = mrr.ResolvedMember;
						
					} else if ((pd = (data as PropertyDeclaration)) != null) {
						
						// We are looking for the field in this.memberToFind.
						if (this.memberToFind.CompareTo(mrr.ResolvedMember) == 0) {
							
							// Resolve the property.
							MemberResolveResult prr = NRefactoryAstCacheService.ResolveLowLevel(this.memberToFind.DeclaringType.CompilationUnit.FileName, pd.StartLocation.Y, pd.StartLocation.X+1, null, pd.Name, ExpressionContext.Default) as MemberResolveResult;
							if (prr != null) {
								
								#if DEBUG
								LoggingService.Debug("ResourceToolkit: PropertyFieldAssociationVisitor, inside PropertyGetRegion, resolved property: "+prr.ResolvedMember.ToString());
								#endif
								
								if (prr.ResolvedMember is IProperty) {
									#if DEBUG
									LoggingService.Debug("ResourceToolkit: PropertyFieldAssociationVisitor, inside PropertyGetRegion, property "+prr.ResolvedMember.ToString()+" seems to reference field "+mrr.ResolvedMember.ToString());
									#endif
									this.associatedMember = prr.ResolvedMember;
								}
								
							}
							
						}
						
					}
					
				}
				
			}
			
			return base.TrackedVisit(returnStatement, data);
		}
		
		public override object TrackedVisit(AssignmentExpression assignmentExpression, object data)
		{
			// If we are in a property set region,
			// this may be the statement where the field value is assigned.
			if (this.associatedMember == null &&	// skip if already found to improve performance
			    this.currentContext == VisitorContext.PropertySetRegion &&
			    assignmentExpression.Op == AssignmentOperatorType.Assign && data != null) {
				
				// Resolve the expression.
				MemberResolveResult mrr = this.Resolve(assignmentExpression.Left, this.memberToFind.DeclaringType.CompilationUnit.FileName) as MemberResolveResult;
				if (mrr != null && mrr.ResolvedMember is IField && !((IField)mrr.ResolvedMember).IsLocalVariable) {
					
					PropertyDeclaration pd;
					
					#if DEBUG
					LoggingService.Debug("ResourceToolkit: PropertyFieldAssociationVisitor, inside PropertySetRegion, resolved field: "+mrr.ResolvedMember.ToString());
					#endif
					
					if (data as bool? ?? false) {
						
						// We are looking for this property.
						#if DEBUG
						LoggingService.Debug("ResourceToolkit: PropertyFieldAssociationVisitor, inside PropertySetRegion, this property seems to reference field "+mrr.ResolvedMember.ToString());
						#endif
						this.associatedMember = mrr.ResolvedMember;
						
					} else if ((pd = (data as PropertyDeclaration)) != null) {
						
						// We are looking for the field in this.memberToFind.
						if (this.memberToFind.CompareTo(mrr.ResolvedMember) == 0) {
							
							// Resolve the property.
							MemberResolveResult prr = NRefactoryAstCacheService.ResolveLowLevel(this.memberToFind.DeclaringType.CompilationUnit.FileName, pd.StartLocation.Y, pd.StartLocation.X+1, null, pd.Name, ExpressionContext.Default) as MemberResolveResult;
							if (prr != null) {
								
								#if DEBUG
								LoggingService.Debug("ResourceToolkit: PropertyFieldAssociationVisitor, inside PropertySetRegion, resolved property: "+prr.ResolvedMember.ToString());
								#endif
								
								if (prr.ResolvedMember is IProperty) {
									#if DEBUG
									LoggingService.Debug("ResourceToolkit: PropertyFieldAssociationVisitor, inside PropertySetRegion, property "+prr.ResolvedMember.ToString()+" seems to reference field "+mrr.ResolvedMember.ToString());
									#endif
									this.associatedMember = prr.ResolvedMember;
								}
								
							}
							
						}
						
					}
					
				}
				
			}
			
			return base.TrackedVisit(assignmentExpression, data);
		}
		
		// ********************************************************************************************************************************
		
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyFieldAssociationVisitor"/> class.
		/// </summary>
		/// <param name="property">The property to find the associated field for.</param>
		public PropertyFieldAssociationVisitor(IProperty property) : base()
		{
			if (property == null) {
				throw new ArgumentNullException("property");
			}
			this.memberToFind = property;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyFieldAssociationVisitor"/> class.
		/// </summary>
		/// <param name="field">The field to find the associated property for.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String,System.String)")]
		public PropertyFieldAssociationVisitor(IField field) : base()
		{
			if (field == null) {
				throw new ArgumentNullException("field");
			} else if (field.IsLocalVariable) {
				throw new ArgumentException("The specified IField must not be a local variable.", "field");
			}
			this.memberToFind = field;
		}
		
	}
}
