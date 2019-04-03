using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using ZeroLevel.Contracts.Specification.Building;

namespace ZeroLevel.Specification
{
    public class SpecificationConstructor : ISpecificationConstructor
    {
        private readonly Type _specificationType;

        private readonly Dictionary<string, List<SpecificationParameter>> _specificationActivateVariants =
            new Dictionary<string, List<SpecificationParameter>>();

        public IEnumerable<string> VariantNames
        {
            get
            {
                return _specificationActivateVariants.Keys;
            }
        }

        public string Name
        {
            get
            {
                var a = _specificationType.GetCustomAttribute<DescriptionAttribute>();
                if (null == a) return _specificationType.Name;
                return a.Description;
            }
        }

        public SpecificationConstructor(Type specificationType)
        {
            if (null == specificationType) throw new ArgumentNullException(nameof(specificationType));
            _specificationType = specificationType;
            AnalizeConstructors();
        }

        public ISpecificationBuilder GetVariant(string variantName)
        {
            if (false == _specificationActivateVariants.ContainsKey(variantName))
                throw new InvalidOperationException($"Not found variant name {variantName}");
            return new SpecificationBuilder(variantName, _specificationActivateVariants[variantName], _specificationType);
        }

        private void AnalizeConstructors()
        {
            int index = 0;
            foreach (var ctor in _specificationType.GetConstructors())
            {
                var vName = $"{_specificationType.Name} #{index:D2}";
                var ca = ctor.GetCustomAttribute<DescriptionAttribute>();
                if (null != ca)
                {
                    vName = ca.Description;
                }
                var parameters = new List<SpecificationParameter>();
                foreach (var param in ctor.GetParameters())
                {
                    var a = param.GetCustomAttribute<DescriptionAttribute>();
                    var displayName = param.Name;
                    if (null != a) displayName = a.Description;
                    parameters.Add(new SpecificationParameter { DisplayName = displayName, ParameterType = param.ParameterType, ParameterName = param.Name });
                }
                _specificationActivateVariants.Add(vName, parameters);
                index++;
            }
        }
    }
}