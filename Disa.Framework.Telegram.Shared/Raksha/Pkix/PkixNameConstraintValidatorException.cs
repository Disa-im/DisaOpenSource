using System;

namespace Raksha.Pkix
{
    public class PkixNameConstraintValidatorException : Exception
    {
        public PkixNameConstraintValidatorException(String msg)
            : base(msg)
        {
        }
    }
}
