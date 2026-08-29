using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using NLog;

namespace TranSimCS.Worlds.Stack {
    public class UpdateLoopTracker<TObj, TStack> : IStackTracker<TObj, TStack> where TObj : Obj where TStack : ObjectStack<TObj, TStack> {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private TStack? stack;
        private Action<TObj, float> action;
        public UpdateLoopTracker(Action<TObj, float> action) {
            this.action = action;
        }
        
        private void OnUpdate(float time) {
            if (stack == null) return;
            log.Trace($"Updating {stack.data.Count} {typeof(TObj).Name}s");
            Stopwatch timer = Stopwatch.StartNew();
            foreach (var obj in stack.data) {
                action(obj, time);
            }
            timer.Stop();
            log.Trace($"Updated {stack.data.Count} {typeof(TObj).Name}s in {timer.Elapsed.TotalMilliseconds} ms");
        }

        public void ElementAdded(TObj element) {
            //unused
        }

        public void ElementModified(TObj element, PropertyChangedEventArgs args) {
            //unused
        }

        public void ElementRemoved(TObj element) {
            //unused
        }

        public void OnThisAdded(TStack stk) {
            if(stack != null) throw new InvalidOperationException("UpdateLoopTracker can have only one parent ObjectStack");
            stack = stk;
            stk.World.OnUpdate += OnUpdate;
        }

        public void OnThisRemoved(TStack stk) {
            if (stack == stk) {
                stack = null;
                stk.World.OnUpdate -= OnUpdate;
            }
        }
    }
}
