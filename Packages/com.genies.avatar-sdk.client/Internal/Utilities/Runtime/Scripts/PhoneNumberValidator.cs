using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PhoneNumbers;

namespace Genies.Utilities
{
    public static class PhoneNumberValidator
    {
        public static bool IsPhoneNumberValid(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }
            
            return PhoneNumber.TryParse(phone.Trim(), out IEnumerable<PhoneNumber> candidates)
                   && candidates.Any();
        }
    }
}
