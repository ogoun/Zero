using System;
using System.Collections.Generic;
using System.Reflection;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.Reflection;

namespace ZeroLevel.Sleopok.Engine.Models
{
    internal class IndexInfo<T>
    {
        private readonly Func<T, string> _identityExtractor;
        private readonly List<SleoField> _fields;

        public string GetId(T item) => _identityExtractor.Invoke(item);

        public IReadOnlyCollection<SleoField> Fields => _fields;

        public IndexInfo(Func<T, string> identityExtractor)
        {
            _identityExtractor = identityExtractor;
            _fields = new List<SleoField>();
            typeof(T).GetMembers(
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy |
                BindingFlags.GetField |
                BindingFlags.GetProperty |
                BindingFlags.Instance).
                Do(members =>
                {
                    foreach (var member in members)
                    {
                        var sleoAttribute = member.GetCustomAttribute<SleoIndexAttribute>();
                        if (sleoAttribute == null) continue;

                        Type memberType;
                        Func<object, object> getter;
                        switch (member.MemberType)
                        {
                            case MemberTypes.Field:
                                memberType = ((FieldInfo)member).FieldType;
                                getter = TypeGetterSetterBuilder.BuildGetter((FieldInfo)member);
                                break;
                            case MemberTypes.Property:
                                memberType = ((PropertyInfo)member).PropertyType;
                                getter = TypeGetterSetterBuilder.BuildGetter((PropertyInfo)member);
                                break;
                            default: return;
                        }

                        var type = SleoFieldType.Single;
                        if (memberType != typeof(string)
                            && (TypeHelpers.IsArray(memberType)
                                || TypeHelpers.IsGenericCollection(memberType)
                                || TypeHelpers.IsEnumerable(memberType)))
                        {
                            type = SleoFieldType.Array;
                        }
                        var name = FSUtils.FileNameCorrection(string.IsNullOrWhiteSpace(sleoAttribute.Name) ? member.Name : sleoAttribute.Name);
                        _fields.Add(new SleoField(type, name, sleoAttribute.Boost, sleoAttribute.AvaliableForExactMatch, getter));
                    }
                });
        }
    }
}
