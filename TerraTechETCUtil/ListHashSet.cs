using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Collections.Specialized;

namespace TerraTechETCUtil
{
    /// <summary>
    /// WAY MORE MEMORY-HEAVY and takes more processing to add and remove, but insures both list order AND
    /// hash lookup speed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListHashSet<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, 
        IList, ICollection, IReadOnlyList<T>, IReadOnlyCollection<T>, ISerializable, 
        IDeserializationCallback, ISet<T> where T : class
    {
        private List<T> List = new List<T>();
        private HashSet<T> Hash = new HashSet<T>();
        public T this[int index]
        {
            get => List[index];
            set
            {
                if (index >= 0 && index < List.Count)
                    Hash.Remove(List[index]);
                List[index] = value;
                Hash.Add(value);
            }
        }
        object IList.this[int index]
        {
            get => List[index];
            set
            {
                if (!(index is T value2))
                    throw new ArgumentException("index is not of correct type " + typeof(T).FullName);
                if (index >= 0 && index < List.Count)
                    Hash.Remove(List[index]);
                List[index] = value2;
                Hash.Add(value2);
            }
        }
        public bool this[T value]
        {
            get => Hash.Contains(value);
        }
        public int Count => List.Count;
        public bool IsFixedSize => false;
        public bool IsReadOnly => ((ICollection<T>)List).IsReadOnly;
        public bool IsSynchronized => ((ICollection)List).IsSynchronized;
        public object SyncRoot => ((ICollection)List).SyncRoot;

        public IEnumerator<T> GetEnumerator() => List.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();

        public bool Contains(T value) => Hash.Contains(value);
        bool IList.Contains(object value)
        {
            if (!(value is T value2))
                throw new ArgumentException("value is not of correct type " + typeof(T).FullName);
            return Hash.Contains(value2);
        }
        public int IndexOf(T value) => List.IndexOf(value);
        int IList.IndexOf(object value)
        {
            if (!(value is T value2))
                throw new ArgumentException("value is not of correct type " + typeof(T).FullName);
            return List.IndexOf(value2);
        }

        public bool Add(T value)
        {
            if (Hash.Add(value))
            {
                List.Add(value);
                return true;
            }
            return false;
        }
        void ICollection<T>.Add(T value)
        {
            if (Hash.Add(value))
                List.Add(value);
        }
        int IList.Add(object value)
        {
            if (!(value is T value2))
                throw new ArgumentException("value is not of correct type " + typeof(T).FullName);
            if (Hash.Add(value2))
                return ((IList)List).Add(value2);
            return Count -1;
        }

        public bool Insert(int index, T value)
        {
            if (Hash.Add(value))
            {
                List.Insert(index, value);
                return true;
            }
            return false;
        }
        void IList<T>.Insert(int index, T value)
        {
            if (Hash.Add(value))
                List.Insert(index, value);
            else
                throw new InvalidOperationException("value was already assigned");
        }
        void IList.Insert(int index, object value)
        {
            if (!(value is T value2))
                throw new ArgumentException("value is not of correct type " + typeof(T).FullName);
            if (Hash.Add(value2))
                List.Insert(index, value2);
            throw new InvalidOperationException("value was already assigned");
        }
        public bool RemoveAt(int index)
        {
            if (Hash.Remove(List[index]))
            {
                List.RemoveAt(index);
                return true;
            }
            return false;
        }
        void IList<T>.RemoveAt(int index)
        {
            Hash.Remove(List[index]);
            List.RemoveAt(index);
        }
        void IList.RemoveAt(int index)
        {
            Hash.Remove(List[index]);
            List.RemoveAt(index);
        }
        public bool Remove(T value)
        {
            if (Hash.Remove(value))
            {
                List.Remove(value);
                return true;
            }
            return false;
        }
        void IList.Remove(object value)
        {
            if (!(value is T value2))
                throw new ArgumentException("value is not of correct type " + typeof(T).FullName);
            if (Hash.Remove(value2))
                List.Remove(value2);
        }
        public void CopyTo(T[] value, int arrayIndex) => List.CopyTo(value, arrayIndex);
        void ICollection.CopyTo(Array value, int arrayIndex)
        {
            if (!(value is T[] value2))
                throw new ArgumentException("value is not of correct type " + typeof(T[]).FullName);
            List.CopyTo(value2, arrayIndex);
        }
        public void Clear()
        {
            Hash.Clear();
            List.Clear();
        }


        public void GetObjectData(SerializationInfo info, StreamingContext context) => Hash.GetObjectData(info, context);
        public void OnDeserialization(object sender) => Hash.OnDeserialization(sender);


        public void UnionWith(IEnumerable<T> other) => Hash.UnionWith(other);
        public void SymmetricExceptWith(IEnumerable<T> other) => Hash.SymmetricExceptWith(other);
        public void IntersectWith(IEnumerable<T> other) => Hash.IntersectWith(other);
        public void ExceptWith(IEnumerable<T> other) => Hash.ExceptWith(other);


        public bool SetEquals(IEnumerable<T> other) => Hash.SetEquals(other);
        public bool Overlaps(IEnumerable<T> other) => Hash.Overlaps(other);
        public bool IsSubsetOf(IEnumerable<T> other) => Hash.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => Hash.IsSupersetOf(other);
        public bool IsProperSubsetOf(IEnumerable<T> other) => Hash.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => Hash.IsProperSupersetOf(other);

        public ListHashSetSorting<T> OrderBy<K>(Func<T, K> keySelector)
        {
            return new ListHashSetSorting<T>
            {
                Origin = this,
                List = List.OrderBy(keySelector),
            };
        }
        public ListHashSetSorting<T> OrderByDescending<K>(Func<T, K> keySelector)
        {
            return new ListHashSetSorting<T>
            {
                Origin = this,
                List = List.OrderByDescending(keySelector),
            };
        }

        public class ListHashSetSorting<T> where T : class
        {
            internal ListHashSet<T> Origin;
            internal IOrderedEnumerable<T> List;
            public ListHashSetSorting<T> ThenBy<K>(Func<T, K> keySelector)
            {
                List = List.ThenBy(keySelector);
                return this;
            }
            public ListHashSetSorting<T> ThenByDescending<K>(Func<T, K> keySelector)
            {
                List = List.ThenByDescending(keySelector);
                return this;
            }
            public ListHashSet<T> ToList()
            {
                Origin.List = List.ToList();
                return Origin;
            }
        }
    }
}
