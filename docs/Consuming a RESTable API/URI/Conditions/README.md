# Conditions

Conditions are made up of three parts: a **property locator**, an **operator** and a **value literal**. They encode predicates that are either true or false of entities in the selected resource. Conditions that have the EQUALS `=` operator are sometimes referred to as **parameters** in the context of GET requests.

## Property locators

A property locator is a string used to locate a property in a resource entity. Dot notation is used to access the contents of inner objects. If entities in a resource `User` contain ids, names, accounts and email addresses, and accounts contain account numbers, we can imagine using the following property locators in requests. Property locators are case-insensitive. Examples:

```
Name
id
Account.accountnr
```

For dynamic resources, all declared property names are prepended with the '$' character. So, for a resource that is a dictionary, all dictionary keys are displayed under their real names, but the `Count` property of the dictionary – which is declared and represents the number of dictionary entries, is displayed as `$Count`.


## Operators

The REST API supports the following six operators in conditions:

### `=`

> **Equals**: the two values are the same

### `!=`

> **Not equals**: the two values are different

### `<`

> **Less than**: the two values are numbers or datetimes and the first is less than the second, or the two values are strings and the first is alphabetically sorted ahead of the second

### `>`

> **Greater than**: the two values are numbers or datetimes and the first is greater than the second, or the two values are strings and the second is alphabetically sorted ahead of the first

### `<=`

> **Less than or equals**: the two values are numbers or datetimes, and the first // is less than or equal to the second, or the two values are strings and the // first is aphabetically sorted equally to or ahead of the second

### `>=`

> **Greater than or equals**: the two values are numbers or datetimes and the // first is greater than or equal to the second, or the two values are strings // and the second is alphabetically sorted equally to or ahead of the first

## Value literal

The value literal is a string that encodes some value. RESTable will parse value literals and find the correct data type for the value. An error message is then returned if there is a type mismatch against the resource property specified by the property locator.

To force the value literal to be handled as a `string`, when it would otherwise be understood as, for example, a number – wrap it in quotation marks. For example, `123` would be parsed as an integer. If we want to encode the string `"123"` in a value literal, we can surround the literal with `"`-characters or `'`-characters in the URI.

Apart from the usual convention regarding URI-safe characters, the following characters are reserved by RESTable, and always need to be escaped if used in value literals:

Character | Use instead
--------- | -----------
`!`       | `%21`

**Always URI encode value literal that contain special characters**

**Value literals are case-sensitive.**

## Examples

```
id=A123&name=Michael%20Bluth&account.accountnr>100
ID=A123&NAME!=George%20Bluth
ID="123"
```
