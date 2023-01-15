using AI;
using UnityEditor;

namespace SerializableDictionary.Editor
{
    [CustomPropertyDrawer(typeof(WorldState))]
    public class WorldStateEditor : SerializableDictionaryPropertyDrawer
    {
    }
    
    [CustomPropertyDrawer(typeof(BlackboardDictionnary))]
    public class BlackboardDictionnaryEditor : SerializableDictionaryPropertyDrawer
    {
    }
}