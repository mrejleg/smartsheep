using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace Project.Core.Extentions
{
    public static class MapperExtensions
    {
        public static TDestination AutoMapper<TSource, TDestination>(this TSource value)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TSource, TDestination>();
            }, NullLoggerFactory.Instance);

            var mapper = config.CreateMapper();
            return mapper.Map<TDestination>(value);
        }
    }
}