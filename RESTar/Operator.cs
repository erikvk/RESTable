namespace RESTar
{
    internal class Operator
    {
        public readonly string Common;
        public readonly string SQL;

        public Operator(string common, string sql)
        {
            Common = common;
            SQL = sql;
        }

        public override string ToString()
        {
            return Common;
        }
    }
}