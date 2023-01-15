using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


/* WorldState used in the game
 *  resourcesAvailable int
 *
 *  heavyFactory int 
 *  lightFactory int
 *
 *  militaryPower float
 *  EnemyMilitaryPower float
 *  unitCount int
 *  targetCaptured int
 *  baseCaptured bool
 * 
 */

namespace AI
{
    public enum Objective
    {
        Inf,
        Sup,
        Eq,
        Ind
    }

    public enum DataType
    {
        Integer,
        Float,
        Boolean,
        Unsupported
    }

    [Serializable]
    public class Data
    {
        private Dictionary<Type, DataType> _dataTypes = new Dictionary<Type, DataType>();

        public Objective objective;
        public DataType type;
        public object value;

        public Data(Data data)
        {
            _dataTypes.Add(typeof(int), DataType.Integer);
            _dataTypes.Add(typeof(float), DataType.Float);
            _dataTypes.Add(typeof(bool), DataType.Boolean);

            this.objective = data.objective;
            this.value = data.value;
            this.type = data.type;
        }

        public Data(Objective objective, object value) // why add all 3
        {
            _dataTypes.Add(typeof(int), DataType.Integer);
            _dataTypes.Add(typeof(float), DataType.Float);
            _dataTypes.Add(typeof(bool), DataType.Boolean);

            this.objective = objective;
            this.value = value;
            this.type = GetDataType(value.GetType());
        }

        private DataType GetDataType(Type objectType)
        {
            return _dataTypes.ContainsKey(objectType) ? _dataTypes[objectType] : DataType.Unsupported;
        }
    }

    [Serializable]
    public class WorldState : Dictionary<string, Data>
    {
        public WorldState()
        {
        }

        public WorldState(WorldState refState)
        {
            foreach (KeyValuePair<string, Data> pair in refState)
            {
                Add(pair.Key, new Data(pair.Value));
            }
        }

        public WorldState Copy()
        {
            WorldState newWorldState = new WorldState();

            foreach (KeyValuePair<string, Data> pair in this)
            {
                newWorldState.Add(pair.Key, new Data(pair.Value));
            }

            return newWorldState;
        }

        public bool Compare(WorldState test) // Why bool ?
        {
            foreach (string key in Keys)
            {
                Data data = test[key];
                Data thisData = this[key];

                if (thisData.objective != Objective.Ind)
                {
                    bool result;

                    switch (thisData.type)
                    {
                        case DataType.Integer:
                        {
                            int thisv = (int) thisData.value;
                            int v = (int) data.value;

                            result = thisData.objective switch
                            {
                                Objective.Inf => thisv >= v,
                                Objective.Sup => thisv <= v,
                                Objective.Eq => thisv == v,
                                _ => false
                            };


                            break;
                        }
                        case DataType.Float:
                        {
                            float thisv = (float) thisData.value;
                            float v = (float) data.value;

                            result = thisData.objective switch
                            {
                                Objective.Inf => thisv >= v,
                                Objective.Sup => thisv <= v,
                                Objective.Eq => System.Math.Abs(thisv - v) < float.Epsilon,
                                _ => false
                            };
                            break;
                        }
                        case DataType.Boolean:
                        {
                            bool thisv = (bool) thisData.value;
                            bool v = (bool) data.value;

                            Func<bool, bool, bool> booltest = (a, b) => a == b;

                            result = thisData.objective switch
                            {
                                Objective.Inf => booltest(thisv, v),
                                Objective.Sup => booltest(thisv, v),
                                Objective.Eq => booltest(thisv, v),
                                _ => false
                            };
                            break;
                        }
                        case DataType.Unsupported:
                        default:
                            result = false;
                            break;
                    }

                    if (!result)
                        return false;
                }
            }

            return true;
        }

        public WorldState GetCopy()
        {
            WorldState newState = new WorldState();

            foreach (string s in Keys)
            {
                newState.Add(s, this[s]);
            }

            return newState;
        }
    }
}