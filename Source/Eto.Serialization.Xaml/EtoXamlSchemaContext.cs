using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Eto.Drawing;
#if PORTABLE
using Portable.Xaml;
using Portable.Xaml.Markup;
#else
using System.Xaml;
using System.Windows.Markup;
#endif

namespace Eto.Serialization.Xaml
{
	class EtoXamlSchemaContext : XamlSchemaContext
	{
		public const string EtoFormsNamespace = "http://schema.picoe.ca/eto.forms";
		readonly Dictionary<Type, XamlType> typeCache = new Dictionary<Type, XamlType>();

		public bool DesignMode { get; set; }

		static readonly Assembly EtoAssembly = typeof(Platform).GetTypeInfo().Assembly;

		protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
		{
			XamlType type = null;
			try
			{
				return base.GetXamlType(xamlNamespace, name, typeArguments);
			}
			catch
			{
				if (DesignMode && type == null && name.IndexOf('.') == -1)
				{
					// in designer mode, fail gracefully
					return new EtoDesignerType(typeof(DesignerMarkupExtension), this) { TypeName = name, Namespace = xamlNamespace };
				}
				throw;
			}
		}

		public override XamlType GetXamlType(Type type)
		{
			XamlType xamlType;
			if (typeCache.TryGetValue(type, out xamlType))
				return xamlType;

			var info = type.GetTypeInfo();

			if (
				info.IsSubclassOf(typeof(Widget))
				|| info.Assembly == EtoAssembly // struct
				|| (
					// nullable struct
				    info.IsGenericType
				    && info.GetGenericTypeDefinition() == typeof(Nullable<>)
					&& Nullable.GetUnderlyingType(type).GetTypeInfo().Assembly == EtoAssembly
				))
			{
				xamlType = new EtoXamlType(type, this);
				typeCache.Add(type, xamlType);
				return xamlType;
			}
			return base.GetXamlType(type);
		}
	}
}