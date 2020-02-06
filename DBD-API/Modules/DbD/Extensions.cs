using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DBD_API.Modules.DbD
{
    public static class Extensions
    {
        public static T[] Subset<T>(this T[] array, int start, int count)
        {
            var result = new T[count];
            Array.Copy(array, start, result, 0, count);
            return result;
        }

        public static T SearchOne<T>(this IDictionary<string, T> dict, Predicate<T> test)
        {
            var result = dict.FirstOrDefault(x => test(x.Value));
            if (!result.Equals(default) && result.Key != null)
                return result.Value;

            return default;
        }

        public static IEnumerable<T> SearchMany<T>(this IDictionary<string, T> dict, Predicate<T> test)
        {
            return dict.Where(x => test(x.Value))
                .Select(x => x.Value);
        }
    }
}
