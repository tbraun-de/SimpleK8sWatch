using System.Collections.Generic;
using k8s;
using k8s.Models;

namespace SimpleK8sWatch
{
    public interface IWatchedResource<TEntity> where TEntity : IMetadata<V1ObjectMeta>
    {
        public delegate void EntityChangeEvent(WatchEventType eventType, TEntity entity);

        // ReSharper disable once UnusedMemberInSuper.Global
        public IEnumerable<T> GetAll<T>() where T : TEntity;

        // ReSharper disable once EventNeverSubscribedTo.Global
        public event EntityChangeEvent EntityChanged;
    }
}