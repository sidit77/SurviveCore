using System.Collections.Generic;

namespace SurviveCore.World.Utils {
    
    public class ObjectPool<T> where T : new(){

        private int size;
        private Stack<T> objects;

        public int Count => objects.Count;
        
        public ObjectPool(int size) {
            this.size = size;
            objects = new Stack<T>(size);
        }

        public bool Add(T o) {
            if (objects.Count >= size)
                return false;
            objects.Push(o);
            return true;
        }

        public T Get() {
            return objects.Count > 0 ? objects.Pop() : new T();
        }
        
    }
    
}