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

        public override bool Equals(object obj)
        {
            if (obj is Operator)
                return Common == ((Operator) obj).Common;
            return false;
        }

        public static bool operator ==(Operator o1, Operator o2) => o1?.Common != null && o2?.Common != null && o1.Common == o2.Common;
        public static bool operator !=(Operator o1, Operator o2) => !(o1 == o2);
    }
}