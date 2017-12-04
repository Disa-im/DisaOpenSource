namespace Disa.Framework
{
    [DisaFramework]
    public interface IMessageIntent
    {
        string PhoneNumberToServiceAddress(string number);
    }
}