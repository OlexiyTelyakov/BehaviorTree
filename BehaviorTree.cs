using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace KiwiKaleidoscope.AI
{
    /// <summary>
    /// States a Behavior Tree node can return.
    /// </summary>
    public enum BTState
    {
        Success,
        Failure,
        Running
    }

    public static class BT
    {
        public static Selector Selector() { return new Selector(); }
        public static Sequence Sequence() { return new Sequence(); }
        public static Action Action(Func<BTState> action) { return new Action(action); }
        public static Conditional If(Func<bool> condition) { return new Conditional(condition); }
    }

    /// <summary>
    /// Behavior Tree base node implementation.
    /// </summary>
    public abstract class Node
    {
        //Node state getter.
        protected BTState state;
        public BTState State { get { return state; } }

        /// <summary>
        /// Update method for Behavior Tree nodes.
        /// </summary>
        /// <returns></returns>
        public abstract BTState Tick();
    }

    public abstract class Block : Node
    {
        protected List<Node> nodes = new List<Node>();

        public virtual Block AddNodes(params Node[] nodes)
        {
            for(int i = 0; i < nodes.Length; i++)
            {
                this.nodes.Add(nodes[i]);
            }
            return this;
        }
    }

    /// <summary>
    /// Run Tick on all children until one succeds and report success.
    /// If all children fail, report failure.
    /// </summary>
    public class Selector : Block
    {
        //Update
        public override BTState Tick()
        {
            //Selector returns true if any children return true and skips over failures.
            for(int i = 0; i < nodes.Count; i++)
            {
                switch (nodes[i].Tick())
                {
                    case BTState.Success:
                        return BTState.Success;
                    case BTState.Failure:
                        continue;
                    case BTState.Running:
                        return BTState.Running;
                    default:
                        continue;
                }
            }
            //Base case is failure.
            return BTState.Failure;
        }
    }

    public class Sequence : Block
    {
        //Update
        public override BTState Tick()
        {
            //Sequence returns fail if any nodes fail and success if all nodes are successful.
            bool runningNodes = false;
            for(int i = 0; i < nodes.Count; i++)
            {
                switch (nodes[i].Tick())
                {
                    case BTState.Success:
                        continue;
                    case BTState.Failure:
                        return BTState.Failure;
                    case BTState.Running:
                        runningNodes = true;
                        continue;
                    default:
                        return BTState.Failure;
                }
            }
            //Base case is Success or Running if any nodes are running.
            return runningNodes ? BTState.Running : BTState.Success;
        }
    }

    public class Action : Node
    {
        protected Func<BTState> action;

        //Constructor.
        public Action(Func<BTState> action)
        {
            this.action = action;
        }

        public override BTState Tick()
        {
            //Actions fire off passed in logic and base their evaluation from that.
            switch (action())
            {
                case BTState.Success:
                    return BTState.Success;
                case BTState.Failure:
                    return BTState.Failure;
                case BTState.Running:
                    return BTState.Running;
                default:
                    return BTState.Failure;
            }
        }
    }

    /// <summary>
    /// If boolean fails, reports failure. 
    /// Otherwise runs Tick on all children, following Selector logic (run till 1 success).
    /// </summary>
    public class Conditional : Block
    {
        protected Func<bool> condition;

        public Conditional(Func<bool> condition)
        {
            this.condition = condition;
        }

        public override BTState Tick()
        {
            //If condition isn't true, block is failed.
            if (!condition()) return BTState.Failure;
            //Otherwise it runs same logic to a selector.
            for (int i = 0; i < nodes.Count; i++)
            {
                switch (nodes[i].Tick())
                {
                    case BTState.Success:
                        return BTState.Success;
                    case BTState.Failure:
                        continue;
                    case BTState.Running:
                        return BTState.Running;
                    default:
                        continue;
                }
            }
            //Base case is failure.
            return BTState.Failure;
        }
    }
}
