using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using RESTable.Resources;

namespace RESTable;

public class TerminalSubjectAccessor
{
    public TerminalSubjectAccessor(ISubject<Terminal> subject)
    {
        Subject = subject;
    }

    internal ISubject<Terminal> Subject { get; }
}

public interface ITerminalObservable : IObservable<Terminal> { }

public interface ITerminalObservable<out T> : IObservable<T> where T : Terminal { }

public class TerminalObservable : ITerminalObservable
{
    public TerminalObservable(TerminalSubjectAccessor terminalSubjectAccessor)
    {
        TerminalSubject = terminalSubjectAccessor.Subject;
    }

    private ISubject<Terminal> TerminalSubject { get; }

    public IDisposable Subscribe(IObserver<Terminal> observer)
    {
        return TerminalSubject.Subscribe(observer);
    }
}

public class TerminalObservable<T> : ITerminalObservable<T> where T : Terminal
{
    public TerminalObservable(TerminalSubjectAccessor terminalSubjectAccessor)
    {
        TerminalSubject = terminalSubjectAccessor.Subject;
    }

    private ISubject<Terminal> TerminalSubject { get; }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        return TerminalSubject.OfType<T>().Subscribe(observer);
    }

    public IDisposable Subscribe(IObserver<Terminal> observer)
    {
        return TerminalSubject.Subscribe(observer);
    }
}
