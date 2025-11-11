using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Worlds.Stack {
    public class UpdateLoopTracker<TObj, TStack> : IStackTracker<TObj, TStack> where TObj : Obj where TStack : ObjectStack<TObj, TStack> {
        private TStack? stack;
        private Action<TObj, GameTime> action;
        public UpdateLoopTracker(Action<TObj, GameTime> action) {
            this.action = action;
        }
        
        private void OnUpdate(GameTime time) {
            if (stack == null) return;
            foreach (var obj in stack.data) {
                action(obj, time);
            }
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
