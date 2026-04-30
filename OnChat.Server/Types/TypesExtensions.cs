namespace OnChat.Types;

public static class TypesExtensions
{
    extension(Type type)
    {
        public bool IsConcrete => type is { IsAbstract: false, IsInterface: false };
    }
}