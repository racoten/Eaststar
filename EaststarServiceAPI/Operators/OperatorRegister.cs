namespace EaststarServiceAPI.Operators
{
    public class OperatorRegister
    {
        public bool RegisterOperator(OperatorsInfo operatorsInfo)
        {
            using var context = new EaststarContext();

            try
            {
                Guid guid = Guid.NewGuid();

                var existingUser = context.OperatorsInfo
                    .FirstOrDefault(o => o.Username == operatorsInfo.Username);

                if (existingUser != null)
                {
                    return false;
                }

                operatorsInfo.Id = guid.ToString();
                context.OperatorsInfo.Add(operatorsInfo);
                context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
