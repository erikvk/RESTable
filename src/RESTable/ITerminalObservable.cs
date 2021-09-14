using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using RESTable.Resources;

namespace RESTable
{
    public class TerminalSubjectAccessor
    {
        internal ISubject<Terminal> Subject { get; }

        public TerminalSubjectAccessor(ISubject<Terminal> subject)
        {
            Subject = subject;
        }
    }

    public interface ITerminalObservable : IObservable<Terminal> { }

    public interface ITerminalObservable<out T> : IObservable<T> where T : Terminal { }

    public class TerminalObservable : ITerminalObservable
    {
        private ISubject<Terminal> TerminalSubject { get; }

        public TerminalObservable(TerminalSubjectAccessor terminalSubjectAccessor)
        {
            TerminalSubject = terminalSubjectAccessor.Subject;
        }

        public IDisposable Subscribe(IObserver<Terminal> observer) => TerminalSubject.Subscribe(observer);
    }

    public class TerminalObservable<T> : ITerminalObservable<T> where T : Terminal
    {
        private ISubject<Terminal> TerminalSubject { get; }

        public TerminalObservable(TerminalSubjectAccessor terminalSubjectAccessor)
        {
            TerminalSubject = terminalSubjectAccessor.Subject;
        }

        public IDisposable Subscribe(IObserver<Terminal> observer) => TerminalSubject.Subscribe(observer);
        public IDisposable Subscribe(IObserver<T> observer) => TerminalSubject.OfType<T>().Subscribe(observer);
    }
}