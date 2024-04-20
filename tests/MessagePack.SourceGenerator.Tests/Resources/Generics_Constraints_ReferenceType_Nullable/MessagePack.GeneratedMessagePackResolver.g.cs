﻿// <auto-generated />

#pragma warning disable 618, 612, 414, 168, CS1591, SA1129, SA1309, SA1312, SA1403, SA1649

using MsgPack = global::MessagePack;

[assembly: MsgPack::Internal.GeneratedAssemblyMessagePackResolverAttribute(typeof(MessagePack.GeneratedMessagePackResolver), 3, 0)]

namespace MessagePack {

/// <summary>A MessagePack resolver that uses generated formatters for types in this assembly.</summary>
partial class GeneratedMessagePackResolver : MsgPack::IFormatterResolver
{
	/// <summary>An instance of this resolver that only returns formatters specifically generated for types in this assembly.</summary>
	public static readonly MsgPack::IFormatterResolver Instance = new GeneratedMessagePackResolver();

	private GeneratedMessagePackResolver()
	{
	}

	public MsgPack::Formatters.IMessagePackFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.Formatter;
	}

	private static class FormatterCache<T>
	{
		internal static readonly MsgPack::Formatters.IMessagePackFormatter<T> Formatter;

		static FormatterCache()
		{
			var f = GeneratedMessagePackResolverGetFormatterHelper.GetFormatter(typeof(T));
			if (f != null)
			{
				Formatter = (MsgPack::Formatters.IMessagePackFormatter<T>)f;
			}
		}
	}

	private static class GeneratedMessagePackResolverGetFormatterHelper
	{
		private static readonly global::System.Collections.Generic.Dictionary<global::System.Type, int> openTypeLookup = new(1)
		{
			{ typeof(global::TempProject.MyGenericObject<,>), 0 },
		};

		internal static object GetFormatter(global::System.Type t)
		{
			if (t.IsGenericType && openTypeLookup.TryGetValue(t.GetGenericTypeDefinition(), out int openKey))
			{
				return openKey switch
				{
					0 => global::System.Activator.CreateInstance(typeof(global::MessagePack.GeneratedMessagePackResolver.TempProject.MyGenericObjectFormatter<,>).MakeGenericType(t.GenericTypeArguments)),
					_ => null, // unreachable
				};
			}

			return null;
		}
	}
}

}
