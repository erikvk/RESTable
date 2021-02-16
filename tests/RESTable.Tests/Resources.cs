using System;
using RESTable.Resources.Operations;

namespace RESTable.Tests
{
    
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public DateTime DateOfBirth { get; set; }
    }
    
    public class ValidatablePerson : Person, IValidator<ValidatablePerson>
    {
        public bool IsValid(ValidatablePerson entity, out string invalidReason)
        {
            if (string.IsNullOrWhiteSpace(entity.FirstName))
            {
                invalidReason = "Missing first name";
                return false;
            }
            if (entity.DateOfBirth > DateTime.Now)
            {
                invalidReason = "Invalid date of birth. Can't be in the future";
                return false;
            }
            invalidReason = null;
            return true;
        }
    }
}