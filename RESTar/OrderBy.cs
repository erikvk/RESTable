namespace RESTar
{
    public class OrderBy
    {
        public bool Descending;
        public bool Ascending => !Descending;
        public string Key;

        public string SQL => $"ORDER BY t.{Key} {(Descending ? "DESC" : "ASC")}";
    }
}
