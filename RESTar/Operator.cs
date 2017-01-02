namespace RESTar
{
    public class Operator
    {
        public readonly string Common;
        public readonly string SQL;

        internal Operator(string common, string sql)
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