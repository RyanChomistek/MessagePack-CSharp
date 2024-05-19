// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class MessagePackObjectAttribute : Attribute
    {
        public bool KeyAsPropertyName { get; private set; }

        public MessagePackObjectAttribute(bool keyAsPropertyName = false)
        {
            this.KeyAsPropertyName = keyAsPropertyName;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class KeyAttribute : Attribute
    {
        public int? IntKey { get; private set; }

        public string? StringKey { get; private set; }

        public KeyAttribute(int x)
        {
            this.IntKey = x;
        }

        public KeyAttribute(string x)
        {
            this.StringKey = x ?? throw new ArgumentNullException(nameof(x));
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class IgnoreMemberAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class UnionAttribute : Attribute
    {
        /// <summary>
        /// Gets the distinguishing value that identifies a particular subtype.
        /// </summary>
        public int Key { get; private set; }

        /// <summary>
        /// Gets the derived or implementing type.
        /// </summary>
        public Type SubType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnionAttribute"/> class.
        /// </summary>
        /// <param name="key">The distinguishing value that identifies a particular subtype.</param>
        /// <param name="subType">The derived or implementing type.</param>
        public UnionAttribute(int key, Type subType)
        {
            this.Key = key;
            this.SubType = subType ?? throw new ArgumentNullException(nameof(subType));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnionAttribute"/> class.
        /// </summary>
        /// <param name="key">The distinguishing value that identifies a particular subtype.</param>
        /// <param name="subType">The full name (should be assembly qualified) of the derived or implementing type.</param>
        public UnionAttribute(int key, string subType)
        {
            this.Key = key;
            this.SubType = Type.GetType(subType, throwOnError: true);
        }
    }

    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
    public class SerializationConstructorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class MessagePackFormatterAttribute : Attribute
    {
        public Type FormatterType { get; private set; }

        public object?[]? Arguments { get; private set; }

        public MessagePackFormatterAttribute(Type formatterType)
        {
            this.FormatterType = formatterType ?? throw new ArgumentNullException(nameof(formatterType));
        }

        public MessagePackFormatterAttribute(Type formatterType, params object?[]? arguments)
        {
            this.FormatterType = formatterType ?? throw new ArgumentNullException(nameof(formatterType));
            this.Arguments = arguments;
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class DynamicUnionAttribute : Attribute
    {
        public DynamicUnionAttribute()
        {
        }

        public UnionAttribute[] GetUnionedTypes(Type type)
        {
            // Get Generic Types that have type specializations
            List<Type> dataWithGenerics = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                           from subType in domainAssembly.GetTypes()
                                           where type.IsAssignableFrom(subType)
                                           where subType.GetTypeInfo().GetCustomAttribute<MessagePackObjectAttribute>() != null
                                           where subType != type
                                           where subType.IsGenericType
                                           where !subType.IsAbstract
                                           select subType).ToList();

            List<Type> GenericTypesWithSpecializations = new List<Type>();

            foreach (var genericType in dataWithGenerics)
            {
                var typeInfo = genericType.GetTypeInfo();
                var genericTypeArgs = genericType.GetTypeInfo().GetCustomAttributes<GenericTypePackAttribute>();
                foreach (var typeArgPack in genericTypeArgs)
                {
                    GenericTypesWithSpecializations.Add(genericType.MakeGenericType(typeArgPack.TypePack));
                }
            }

            List<Type> data = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                               from subType in domainAssembly.GetTypes()
                               where type.IsAssignableFrom(subType)
                               where subType.GetTypeInfo().GetCustomAttribute<MessagePackObjectAttribute>() != null
                               where subType != type
                               where !subType.IsGenericType
                               where !subType.IsAbstract
                               select subType).ToList();

            data.AddRange(GenericTypesWithSpecializations);

            data.Sort((left, right) => left.Name.CompareTo(right.Name));

            List<UnionAttribute> attributes = new List<UnionAttribute>();

            for (int i = 0; i < data.Count; i++)
            {
                attributes.Add(new UnionAttribute(i, data[i]));
            }

            return attributes.ToArray();
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class GenericTypePackAttribute : Attribute
    {
        public Type[] TypePack { get; set; }

        public GenericTypePackAttribute(Type genericTypeArguments)
        {
            TypePack = new Type[] { genericTypeArguments };
        }

        public GenericTypePackAttribute(Type[] genericTypeArguments)
        {
            TypePack = genericTypeArguments;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DynamicKeyAttribute : Attribute
    {
        public int? IntKey { get; protected set; }

        public void GetKey(Type type, MemberInfo memberInfo)
        {
            List<MemberInfo> rawMemberInfos = GetAllProperties(type).Cast<MemberInfo>().Concat(GetAllFields(type)).ToList();

            // remove everything that doesn't have a dynamic key
            var filteredMemberInfos = rawMemberInfos.Where(member =>
            {
                return member.GetCustomAttribute<DynamicKeyAttribute>() != null;
            }).ToList();

            IntKey = filteredMemberInfos.IndexOf(memberInfo);
        }

        private static IEnumerable<PropertyInfo> GetAllProperties(Type type)
        {
            if (type.BaseType is object)
            {
                foreach (var item in GetAllProperties(type.BaseType))
                {
                    yield return item;
                }
            }

            // sort the member infos by their name (this is so if there is any randomness with how members are ordered we get a constant order)
            var members = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
            //members.Sort((l, r) => l.Name.CompareTo(r.Name));

            // with declared only
            foreach (var item in members)
            {
                yield return item;
            }
        }

        private static IEnumerable<FieldInfo> GetAllFields(Type type)
        {
            if (type.BaseType is object)
            {
                foreach (var item in GetAllFields(type.BaseType))
                {
                    yield return item;
                }
            }

            // sort the member infos by their name (this is so if there is any randomness with how members are ordered we get a constant order)
            var members = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
            //members.Sort((l, r) => l.Name.CompareTo(r.Name));

            // with declared only
            foreach (var item in members)
            {
                yield return item;
            }
        }
    }
}
