using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    [Serializable]
    public class BlackboardDictionnary: SerializableDictionary<string,object>
    {
    }
    
    [CreateAssetMenu(fileName = "Blackboard", menuName = "AI/Blackboard", order = 0)]
    public class Blackboard : ScriptableObject
    {
       [SerializeField] private BlackboardDictionnary _blackboard = new BlackboardDictionnary();

        public BlackboardDictionnary BlackBoard => _blackboard;

        public void AddKey<T>(string key, T value)
        {
            if (_blackboard.ContainsKey(key))
                return;

            _blackboard[key] = value;
        }

        public void Edit<T>(string key,T value)
        {
            if (!_blackboard.ContainsKey(key))
            {
                throw new Exception("No object of type " + typeof(T) + " was found in blackboard with key " + key);
            }
            _blackboard[key] = value;
        }

        public T GetValue<T>(string key)
        {
            object obj = _blackboard[key];
            if (typeof(object) == typeof(T))
                return (T) obj;

            throw new Exception("No object of type " + typeof(T) + " was found in blackboard with key " + key);
        }

        public object GetValue(string key)
        {
            return _blackboard[key];
        }
    }
}