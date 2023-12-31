using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aspros.SaaS.System.Infrastructure
{
    public static class ModelBuilderExtenions
    {
        public static IEnumerable<Type> GetMappingTypes(this Assembly assembly, Type mappingInterface)
        {
            return
                assembly.GetTypes()
                    .Where(
                        t =>
                            !t.GetTypeInfo().IsAbstract &&
                            t.GetInterfaces()
                                .Any(
                                    i =>
                                        i.GetTypeInfo().IsGenericType &&
                                        i.GetGenericTypeDefinition() == mappingInterface));
        }

        /// <summary>
        ///     AddEntityConfigurationsFromAssembly 是对 ModelBuilder 的扩展，用于多个实体映射配置，OnModelCreating 中只需一行代码
        /// </summary>
        public static void AddEntityConfigurationsFromAssembly(this ModelBuilder modelBuilder, Assembly assembly)
        {
            var mappingTypes = assembly.GetMappingTypes(typeof(IEntityMappingConfiguration<>));
            foreach (var config in mappingTypes.Select(Activator.CreateInstance).Cast<IEntityMappingConfiguration>())
                config.Map(modelBuilder);
        }

        public interface IEntityMappingConfiguration
        {
            void Map(ModelBuilder modelBuilder);
        }

        public interface IEntityMappingConfiguration<T> : IEntityMappingConfiguration where T : class
        {
            void Map(EntityTypeBuilder<T> entityTypeBuilder);
        }

        public abstract class EntityMappingConfiguration<T> : IEntityMappingConfiguration<T> where T : class
        {
            public abstract void Map(EntityTypeBuilder<T> entityTypeBuilder);

            public void Map(ModelBuilder modelBuilder)
            {
                Map(modelBuilder.Entity<T>());
            }
        }
    }
}