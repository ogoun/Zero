using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using ZeroLevel.Services.Reflection;

namespace ZeroLevel.Specification
{
    public class AssemblySpecificationFactory : ISpecificationFinder
    {
        private readonly Dictionary<string, Type> _filterTypes;

        public AssemblySpecificationFactory(Assembly assembly)
        {
            var baseFilterType = typeof(ISpecification<>);
            _filterTypes = assembly.
                GetTypes().
                Where(t => TypeExtensions.IsAssignableToGenericType(t, baseFilterType) && t.IsAbstract == false).
                ToDictionary(t =>
                {
                    var a = t.GetCustomAttribute<DescriptionAttribute>();
                    if (null == a) return t.Name;
                    return a.Description;
                });
        }

        public IEnumerable<string> Filters
        {
            get
            {
                return _filterTypes.Keys;
            }
        }

        public ISpecification<T> GetFilter<T>(string filterName, params object[] args)
        {
            if (false == _filterTypes.ContainsKey(filterName))
            {
                throw new KeyNotFoundException(string.Format("Not found specification '{0}'", filterName));
            }

            return (ISpecification<T>)Activator.CreateInstance(_filterTypes[filterName], args);
        }

        public Type GetFilterType(string filterName)
        {
            if (false == _filterTypes.ContainsKey(filterName))
            {
                throw new KeyNotFoundException(string.Format("Not found specification '{0}'", filterName));
            }
            return _filterTypes[filterName];
        }
    }
}
