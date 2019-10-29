using System;
using System.Collections.Generic;

namespace ZeroLevel.SqlServer
{
    public static class DbMapperFactory
    {
        private static readonly Dictionary<Type, IDbMapper> _mapperPool = new Dictionary<Type, IDbMapper>();
        private static readonly object _poolLocker = new object();
        /// <summary>
        /// Создание маппера
        /// </summary>
        /// <param name="entityType">Тип представляющий модель данных</param>
        /// <param name="mapOnlyMarkedMembers">В случае задания в true, все поля класса считаются данными модели, в т.ч. не отвеченные аттрибутом DbMember</param>
        /// <returns>Маппер</returns>
        public static IDbMapper Create(Type entityType, bool mapOnlyMarkedMembers = false)
        {
            if (null == entityType)
                throw new ArgumentNullException(nameof(entityType));
            lock (_poolLocker)
            {
                if (false == _mapperPool.ContainsKey(entityType))
                {
                    var gt = typeof(IDbMapper<>);
                    var rt = gt.MakeGenericType(new Type[] { entityType });

                    _mapperPool.Add(entityType, new DbMapper(rt, mapOnlyMarkedMembers));
                }
            }
            return _mapperPool[entityType];
        }
        /// <summary>
        /// Создание маппера
        /// </summary>
        /// <param name="mapOnlyMarkedMembers">В случае задания в true, все поля класса считаются данными модели, в т.ч. не отвеченные аттрибутом DbMember</param>
        /// <returns>Маппер</returns>
        public static IDbMapper<T> Create<T>(bool mapOnlyMarkedMembers = false)
        {
            var entityType = typeof(T);
            lock (_poolLocker)
            {
                if (false == _mapperPool.ContainsKey(entityType))
                {
                    _mapperPool.Add(entityType, new DbMapper<T>(mapOnlyMarkedMembers));
                }
            }
            return (IDbMapper<T>)_mapperPool[entityType];
        }
    }
}
