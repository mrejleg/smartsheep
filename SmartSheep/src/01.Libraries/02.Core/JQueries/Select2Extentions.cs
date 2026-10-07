using Project.Base.Models.JQueries.Select2s;

namespace Project.Core.JQueries
{
    public static class Select2Extentions
    {
        public static Select2Result BindingSelect2Result(this IReadOnlyList<Select2Binding> result, object id, string text)
        {
            var select2Result = new Select2Result
            {
                Key = id,
                Result = result,
                Value = text
            };

            return select2Result;
        }
    }
}
