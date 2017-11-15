using System;
using RESTar.Operations;

namespace RESTar.Internal
{
    /// <summary>
    /// Represents a RESTar resource view
    /// </summary>
    public class View<T> where T : class
    {
        /// <summary>
        /// The name of the view
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The selector of the view
        /// </summary>
        public Selector<T> Select { get; }

        internal View(Type viewType)
        {
            Name = viewType.Name;
            Select = DelegateMaker.GetViewSelector<T>(viewType);
        }
    }
}