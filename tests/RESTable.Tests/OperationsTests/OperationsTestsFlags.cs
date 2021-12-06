namespace RESTable.Tests.OperationsTests;

public class OperationsTestsFlags
{
    public bool SelectorWasCalled { get; set; }
    public bool AsyncSelectorWasCalled { get; set; }
    public bool InserterWasCalled { get; set; }
    public bool AsyncInserterWasCalled { get; set; }
    public bool UpdaterWasCalled { get; set; }
    public bool AsyncUpdaterWasCalled { get; set; }
    public bool DeleterWasCalled { get; set; }
    public bool AsyncDeleterWasCalled { get; set; }
    public bool CounterWasCalled { get; set; }
    public bool AsyncCounterWasCalled { get; set; }
    public bool ValidatorWasCalled { get; set; }
    public bool AuthenticatorWasCalled { get; set; }
    public bool AsyncAuthenticatorWasCalled { get; set; }

    public void Reset()
    {
        SelectorWasCalled = false;
        AsyncSelectorWasCalled = false;
        InserterWasCalled = false;
        AsyncInserterWasCalled = false;
        UpdaterWasCalled = false;
        AsyncUpdaterWasCalled = false;
        DeleterWasCalled = false;
        AsyncDeleterWasCalled = false;
        CounterWasCalled = false;
        AsyncCounterWasCalled = false;
        ValidatorWasCalled = false;
        AuthenticatorWasCalled = false;
        AsyncAuthenticatorWasCalled = false;
    }
}