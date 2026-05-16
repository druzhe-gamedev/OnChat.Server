namespace OnChat.Common.IQueryableUtils;

// ReSharper disable once InconsistentNaming
public static class IQueryableHelper
{
    extension<T>(IQueryable<T> collection)
    {
        public IQueryable<T> Page(int page, int quantity) => collection.Skip((page - 1) * quantity).Take(quantity);
    }
}