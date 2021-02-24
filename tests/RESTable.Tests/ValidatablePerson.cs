using System;
using System.Collections.Generic;
using RESTable.Resources.Operations;

namespace RESTable.Tests
{
    public class ValidatablePerson : Person, IValidator<ValidatablePerson>
    {
        public IEnumerable<InvalidMember> Validate(ValidatablePerson entity)
        {
            if (string.IsNullOrWhiteSpace(entity.FirstName))
            {
                yield return this.Invalidate(e => e.FirstName, "Missing first name");
            }
            if (entity.DateOfBirth > DateTime.Now)
            {
                yield return this.Invalidate(e => e.DateOfBirth, "Invalid date of birth. Can't be in the future");
            }
        }
    }
}