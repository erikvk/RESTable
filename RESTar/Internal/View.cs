using System;
using RESTar.Operations;

namespace RESTar.Internal
{
    public class View<T> where T : class
    {
        public string Name { get; }
        public Selector<T> Select { get; }

        internal View(Type viewType)
        {
            Name = viewType.Name;
            Select = DelegateMaker.GetViewSelector<T>(viewType);
        }
    }
}