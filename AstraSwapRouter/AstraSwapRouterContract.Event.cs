using System.ComponentModel;

namespace AstraSwapRouter
{
    partial class AstraSwapRouterContract
    {
        /// <summary>
        /// params: message, extend data
        /// </summary>
        [DisplayName("Fault")]
        public static event FaultEvent onFault;
        public delegate void FaultEvent(string message, params object[] paras);

    }
}
