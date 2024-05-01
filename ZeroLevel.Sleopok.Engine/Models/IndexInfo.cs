using System;
using System.Collections.Generic;
using System.Reflection;
using ZeroLevel;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.Reflection;
using ZeroLevel.Services.Extensions;

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

                        Func<object, object> getter;
                        switch (member.MemberType)
                        {
                            case MemberTypes.Field:
                                getter = TypeGetterSetterBuilder.BuildGetter(member as FieldInfo);
                                break;
                            case MemberTypes.Property:
                                getter = TypeGetterSetterBuilder.BuildGetter(member as PropertyInfo);
                                break;
                            default: return;
                        }
                        var name = FSUtils.FileNameCorrection(string.IsNullOrWhiteSpace(sleoAttribute.Name) ? member.Name : sleoAttribute.Name);
                        _fields.Add(new SleoField
                        {
                            Boost = sleoAttribute.Boost,
                            Name = name,
                            Getter = getter,
                            ExactMatch = sleoAttribute.AvaliableForExactMatch
                        });
                    }
                });
        }
    }
}
