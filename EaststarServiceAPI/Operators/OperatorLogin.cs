namespace EaststarServiceAPI.Operators
{
    public class OperatorLogin
    {
        public bool LoginOperator(string username, string passwordHash)
        {

            EaststarContext eaststarContext = new();

            var user = eaststarContext.OperatorsInfo
                .FirstOrDefault(x => x.Username == username);

            if (user == null)
            {
                return false;
            }

            if (user.Password != passwordHash)
            {
                return false;
            }

            return true;
        }
    }
}
