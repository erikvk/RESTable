namespace RESTar.Requests
{
    internal interface IViewRequest : IRequest
    {
        void DeleteFromList(string id);
        void SaveItem();
        void CloseItem();
        void RemoveElementFromArray(string input);
        void AddElementToArray(string input);
    }
}