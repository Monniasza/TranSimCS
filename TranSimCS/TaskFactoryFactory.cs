using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS {
    public static class TaskFactoryFactory<T> {
        private static TaskFactory<T> taskFactory;

        public static TaskFactory<T> GetFactory() {
            if (taskFactory == null) 
                taskFactory = new TaskFactory<T>();
            return taskFactory;
        }
    }
}
