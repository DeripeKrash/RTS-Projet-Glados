using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    public static class Planner
    {
        static public int maxPlannerDepth = 5;
        
        public class Node
        {
            public Node parent = null;
            public Action action;
            public float cost; 
            public WorldState state;

            public Node(Node parent, float cost, Action action, WorldState state)
            {
                this.parent = parent;
                this.action = action;
                this.cost = cost;
                this.state = state;
            }
        }
        
        public static Queue<Action> Plan(List<Action> validActions, WorldState originalState, WorldState goal)
        {
            Node root = new Node(null, 0, null, originalState);
            List<Node> leaves = new List<Node>();

            float cheapestCost = float.MaxValue;
            BuildGraph(root, leaves, validActions, goal, ref cheapestCost, 0);

            if (leaves.Count == 0)
                return new Queue<Action>();
            
            Node cheapest = leaves[0];

            for (int i = 1; i < leaves.Count; i++)
            {
                if (cheapest.cost > leaves[i].cost)
                    cheapest = leaves[i];
            }

            return BuildArray(cheapest);
        }

        private static Queue<Action> BuildArray(Node leaf)
        {
            List<Node> result = new List<Node>();

            Node n = leaf;

            do
            {
                result.Add(n);
                n = n.parent;
            } while (n != null);

            result.Reverse();
            Queue<Action> plan = new Queue<Action>(result.Count);

            if (result.Count > 1)
            {
                for (int i = 1; i < result.Count; i++)
                {
                    plan.Enqueue(result[i].action);
                }
            }

            return plan;
        }

        private static bool BuildGraph(Node parent, List<Node> leaves, List<Action> validActions, WorldState goal, ref float cheapestCost, int depth)
        {
            if (depth > maxPlannerDepth)
                return false;
            
            bool found = false; // track if a path has be found or not, useful to build a full tree and not only the first solution

            foreach (Action action in validActions)
            {
                if (action.IsValid(parent.state))
                {
                    WorldState s = action.GetProcessedState(parent.state);

                    Node node = new Node(parent, parent.cost + action.GetCost(parent.state), action, s);

                    if (node.cost < cheapestCost)
                    {
                        if (GoalAchieved(goal, s))
                        {
                            cheapestCost = node.cost;
                            leaves.Add(node);
                            found = true;
                        }
                        else
                        {
                            List<Action> newActions = new List<Action>();

                            for (int i = 0; i < validActions.Count; i++)
                            {
                                if (validActions[i] != action || action.Repeatable)
                                    newActions.Add(validActions[i]);
                            }
                            
                            if (newActions.Count == 0)
                                return false;

                            if (BuildGraph(node, leaves, newActions, goal, ref cheapestCost, depth + 1))
                            {
                                found = true;
                            }
                        }
                    }
                }
            }

            return found;
        }

        private static bool GoalAchieved(WorldState goal, WorldState current)
        {
            return goal.Compare(current);
        }
    }
}