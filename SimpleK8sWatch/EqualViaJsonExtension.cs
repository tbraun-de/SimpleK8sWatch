using k8s;
using k8s.Models;
using Newtonsoft.Json;

namespace SimpleK8sWatch
{
    public static class EqualViaJsonExtension
    {
        public static bool IsEqualViaJson(this IMetadata<V1ObjectMeta> object1, IMetadata<V1ObjectMeta> object2)
        {
            if (ReferenceEquals(object1, object2)) return true;
            if (object1 == null || object2 == null) return false;
            if (object1.GetType() != object2.GetType()) return false;

            var json1 = JsonConvert.SerializeObject(object1);
            var json2 = JsonConvert.SerializeObject(object2);

            return json1.Equals(json2);
        }
    }
}