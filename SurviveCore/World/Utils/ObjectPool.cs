using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SurviveCore.World.Utils {
    
    public class ObjectPool<T>{

        private readonly int size;
        private readonly Stack<T> objects;
        private readonly Func<T> constructor;

        public int Count => objects.Count;
        
        public ObjectPool(int size, Func<T> constructor) {
            this.size = size;
            this.constructor = constructor;
            objects = new Stack<T>(size);
        }

        public bool Add(T o) {
            if (objects.Count >= size)
                return false;
            objects.Push(o);
            return true;
        }

        public T Get() {
            if (objects.TryPop(out T res)) {
                return res;
            }
            return constructor();
        }
        
    }
    
    public class ConcurrentObjectPool<T>{

        private readonly int size;
        private readonly ConcurrentStack<T> objects;
        private readonly Func<T> constructor;

        public int Count => objects.Count;
        
        public ConcurrentObjectPool(int size, Func<T> constructor) {
            this.size = size;
            this.constructor = constructor;
            objects = new ConcurrentStack<T>();
        }

        public bool Add(T o) {
            if (objects.Count >= size)
                return false;
            objects.Push(o);
            return true;
        }

        public T Get() {
            if (objects.TryPop(out T res)) {
                return res;
            }
            return constructor();
        }
        
    }
    
}